using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WaterFeaturesPlus
{
    public sealed partial class WaterController
    {
        private void ReportMaterial(Object surface)
        {
            try
            {
                var field = surfaceType.GetField("customMaterial", FieldFlags);
                var material = field == null ? null : field.GetValue(surface) as Material;
                if (material == null)
                {
                    Mod.Logger.Info("Material: no exposed custom material.");
                    return;
                }
                var shader = material.shader;
                Mod.Logger.Info("Material=" + material.name + "; shader=" + (shader == null ? "null" : shader.name));
                if (shader == null)
                    return;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                    Mod.Logger.Info("Shader property: " + shader.GetPropertyName(i));
            }
            catch (Exception e) { Mod.Logger.Warn("Material inspection: " + e.Message); }
        }
        internal void Dump()
        {
            Mod.Logger.Info("DIAGNOSTICS " + Status + ". Exposed field does not prove shader support.");
            Mod.Logger.Info("SWELL " + SwellStatus);
            LogObservations("NOW");
            pendingObservations = running ? 2 : 0;
            nextObservation = Time.realtimeSinceStartup + 5f;
            if (pendingObservations > 0)
                Mod.Logger.Info("Two read-only samples scheduled in 5 and approximately 15 seconds; return to the city.");
            foreach (var b in bindings)
                Mod.Logger.Info("API " + b.Key + ": " + Describe(b));
            foreach (var entry in surfaces)
            {
                if (entry.Key == null)
                    continue;
                ReportMaterial(entry.Key);
                foreach (var slot in entry.Value.Slots)
                {
                    try
                    {
                        Mod.Logger.Info(entry.Key.name + " / " + slot.Binding.Key + " current=" +
                        slot.Binding.Field.GetValue(entry.Key) + " owned=" + slot.Owned +
                        " original=" + slot.Original + " requested=" + slot.Desired);
                    }
                    catch (Exception e) { Mod.Logger.Warn(e.Message); }
                }
            }
        }
        private static object ReadMember(object target, string name)
        {
            if (target == null)
                return null;
            var field = target.GetType().GetField(name, FieldFlags);
            return field == null ? null : field.GetValue(target);
        }
        private void ReportWaveState(Object surface, string phase)
        {
            try
            {
                object simulation = ReadMember(surface, "simulation");
                object spectrum = ReadMember(simulation, "spectrum");
                object rendering = ReadMember(simulation, "rendering");
                Mod.Logger.Info("WAVE_STATE " + phase + " surface=" + surface.name +
                    " kind=" + ReadMember(surface, "surfaceType") + " geometry=" + ReadMember(surface, "geometryType") +
                    " patch=" + ReadMember(surface, "repetitionSize") + " wind=" + ReadMember(surface, "largeWindSpeed") +
                    " band0=" + ReadMember(surface, "largeBand0Multiplier") + " band1=" + ReadMember(surface, "largeBand1Multiplier") +
                    " fade0=" + ReadMember(surface, "largeBand0FadeToggle") +
                    " fadeStart=" + ReadMember(surface, "largeBand0FadeStart") +
                    " fadeDistance=" + ReadMember(surface, "largeBand0FadeDistance") +
                    " direction=" + ReadMember(surface, "largeWindOrientationValue") +
                    " ripples=" + ReadMember(surface, "ripples") +
                    " rippleFade=" + ReadMember(surface, "ripplesFadeToggle") +
                    " rippleStart=" + ReadMember(surface, "ripplesFadeStart") +
                    " rippleLength=" + ReadMember(surface, "ripplesFadeDistance") +
                    " causticsBand=" + ReadMember(surface, "causticsBand"));
                Mod.Logger.Info("WAVE_OUTPUT " + phase + " spectrum=" + (spectrum == null ? "not exposed" : "available") +
                    " activeBands=" + ReadMember(spectrum, "numActiveBands") + " patchSizes=" + ReadMember(spectrum, "patchSizes") +
                    " amplitudes=" + ReadMember(rendering, "patchAmplitudeMultiplier"));
            }
            catch (Exception e) { Mod.Logger.Warn("Wave inspection: " + e.Message); }
        }
        private void LogObservations(string phase)
        {
            foreach (var entry in surfaces)
            {
                if (entry.Key == null)
                    continue;
                ReportWaveState(entry.Key, phase);
                foreach (var binding in bindings)
                {
                    if (binding.SuspendedReason == null || binding.Field == null)
                        continue;
                    try
                    {
                        Mod.Logger.Info("OBSERVE " + phase + " t=" + Time.realtimeSinceStartup +
                            " surface=" + entry.Key.name + " field=" + binding.Field.Name +
                            " value=" + binding.Field.GetValue(entry.Key) + " [read-only]");
                    }
                    catch (Exception e) { Mod.Logger.Warn("Observation: " + e.Message); }
                }
            }
        }
    }
}
