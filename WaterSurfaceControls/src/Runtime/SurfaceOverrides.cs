using System;
using System.Collections.Generic;
using System.Reflection;

namespace WaterSurfaceControls
{
    // Independent of the scene: each binding owns only the fields it actually writes.
    internal sealed class WaterBinding
    {
        internal string Key;
        internal FieldInfo Field;
        internal Func<FoamSettings, bool> Enabled;
        internal Func<FoamSettings, object, object> Value;
        internal Func<FoamSettings, SurfaceFeatures, bool> Eligible;
        internal Func<FoamSettings, int> ValueRevision;
        internal bool Failed;
        internal string SuspendedReason;
        internal bool Supported
        {
            get
            {
                return Field != null && !Failed && SuspendedReason == null;
            }
        }
    }

    internal sealed class WaterSlot
    {
        internal readonly WaterBinding Binding;
        internal object Original, Desired, LastWritten;
        internal bool Owned;
        private int revision = -1;
        internal WaterSlot(WaterBinding binding)
        {
            Binding = binding;
        }

        internal void Apply(object target, FoamSettings settings, SurfaceFeatures features)
        {
            if (!Binding.Supported || (Binding.Eligible != null && !Binding.Eligible(settings, features)))
            {
                Restore(target);
                return;
            }
            if (!Owned)
            {
                // Capture at activation, not at discovery. Disabled groups never freeze defaults.
                Original = Binding.Field.GetValue(target);
                revision = -1;
            }
            int currentRevision = Binding.ValueRevision == null ? settings.Revision : Binding.ValueRevision(settings);
            if (!Owned || revision != currentRevision)
            {
                Desired = Binding.Value(settings, Original);
                revision = currentRevision;
            }
            Binding.Field.SetValue(target, Desired);
            LastWritten = Desired;
            Owned = true;
        }

        internal void Restore(object target)
        {
            if (!Owned)
                return;
            try
            {
                // Keep a newer value if another system wrote after our last update.
                if (Equals(Binding.Field.GetValue(target), LastWritten))
                    Binding.Field.SetValue(target, Original);
            }
            finally { Owned = false; Original = Desired = LastWritten = null; revision = -1; }
        }

    }

    internal struct SurfaceFeatures
    {
        internal bool Swell;
        internal bool RippleFade;
        internal bool CausticsSource;
    }

    internal sealed class SurfaceOverrides
    {
        internal readonly List<WaterSlot> Slots = new List<WaterSlot>();
        private readonly List<WaterSlot> active = new List<WaterSlot>();
        private int revision = -1;

        internal SurfaceOverrides(List<WaterBinding> bindings)
        {
            foreach (var binding in bindings)
                if (binding.Supported)
                    Slots.Add(new WaterSlot(binding));
        }

        internal void Update(object surface, FoamSettings settings, SurfaceFeatures features)
        {
            if (revision != settings.Revision)
            {
                active.Clear();
                foreach (var slot in Slots)
                {
                    if (settings.EnableEffect && slot.Binding.Supported && slot.Binding.Enabled(settings))
                        active.Add(slot);
                    else
                        Release(surface, slot);
                }
                revision = settings.Revision;
            }

            foreach (var slot in active)
            {
                try
                {
                    slot.Apply(surface, settings, features);
                }
                catch (Exception error)
                {
                    slot.Binding.Failed = true;
                    Release(surface, slot);
                    Mod.Logger.Warn("Disabled control " + slot.Binding.Key + ": " + error);
                }
            }
        }

        internal void Restore(object surface)
        {
            foreach (var slot in Slots)
                Release(surface, slot);
        }

        private static void Release(object surface, WaterSlot slot)
        {
            try
            {
                slot.Restore(surface);
            }
            catch (Exception error) { Mod.Logger.Warn("Restore " + slot.Binding.Key + ": " + error.Message); }
        }
    }
}
