using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WaterFeaturesPlus
{
    public sealed partial class WaterController : MonoBehaviour
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private FoamSettings settings;
        private Type surfaceType;
        private FieldInfo registry;
        private FieldInfo surfaceKind;
        private object oceanSeaLakeKind, oceanKind;
        private FieldInfo ripplesEnabledField;
        private FieldInfo simulationField, spectrumField, bandCountField;
        private WaterBinding rippleFadeSwitch, rippleFadeStart, rippleFadeDistance;
        private readonly List<WaterBinding> bindings = new List<WaterBinding>();
        private readonly Dictionary<Object, SurfaceOverrides> surfaces = new Dictionary<Object, SurfaceOverrides>();
        private readonly Dictionary<string, WaterBinding> bindingsByKey = new Dictionary<string, WaterBinding>(StringComparer.Ordinal);
        private readonly List<Object> removed = new List<Object>();
        private float nextScan;
        private bool running;

        private bool emptyReported;
        private int pendingObservations;
        private float nextObservation;

        internal bool Supports(string key)
        {
            return running && bindingsByKey.TryGetValue(key, out var binding) && binding.Supported;
        }
        private bool IsOcean(object target)
        {
            if (surfaceKind == null || !surfaceKind.FieldType.IsEnum)
                return false;
            object kind = surfaceKind.GetValue(target);
            return kind != null && (kind.Equals(oceanSeaLakeKind) || kind.Equals(oceanKind));
        }
        private bool HasSwellApi
        {
            get
            {
                return surfaceKind != null && surfaceKind.FieldType.IsEnum && Supports("SwellPatchSize") &&
                Supports("SwellWind") && Supports("LargeWaves") && Supports("MediumWaves");
            }
        }
        internal int OceanCount
        {
            get
            {
                int count = 0;
                foreach (var entry in surfaces)
                {
                    var surface = entry.Key as Behaviour;
                    if (surface != null && surface.isActiveAndEnabled && surface.gameObject.scene.isLoaded && IsOcean(surface))
                        count++;
                }
                return count;
            }
        }
        internal bool CanUseSwell
        {
            get
            {
                return running && HasSwellApi && OceanCount > 0;
            }
        }
        internal string SwellStatus
        {
            get
            {
                return "OceanSeaLake/Ocean: " + OceanCount + " | spectrum API: " + (HasSwellApi ? "available" : "unavailable");
            }
        }
        private bool HasRipples(object surface)
        {
            return ripplesEnabledField != null && (bool)ripplesEnabledField.GetValue(surface);
        }

        private bool HasRippleFadeFields
        {
            get
            {
                return running && ripplesEnabledField != null && rippleFadeSwitch.Supported &&
                         rippleFadeStart.Supported && rippleFadeDistance.Supported;
            }
        }

        internal bool CanUseRippleFade
        {
            get
            {
                if (!HasRippleFadeFields)
                    return false;
                foreach (var entry in surfaces)
                {
                    var surface = entry.Key as Behaviour;
                    if (surface != null && surface.isActiveAndEnabled && surface.gameObject.scene.isLoaded &&
                        HasRipples(surface))
                        return true;
                }
                return false;
            }
        }

        // Surface kind, ripple state and the selected band may change while the city runs.
        private SurfaceFeatures GetFeatures(FoamSettings options, object surface)
        {
            var features = new SurfaceFeatures();
            if (!options.EnableEffect)
                return features;
            bool selectBand = options.CausticsOverride && options.CausticsOn && options.SourceBand != CausticsSource.Game;
            bool needsRipples = options.RippleFadeEnabled || (selectBand && options.SourceBand == CausticsSource.Ripples);
            bool ripples = needsRipples && HasRipples(surface);
            bool ocean = (options.SwellEnabled || selectBand) && IsOcean(surface);
            features.Swell = ocean && options.SwellEnabled && HasSwellApi;
            features.RippleFade = options.RippleFadeEnabled && ripples && HasRippleFadeFields;
            features.CausticsSource = selectBand && ocean && SourceBandAvailable(options.SourceBand, surface, ripples);
            return features;
        }

        private bool SourceBandAvailable(CausticsSource source, object surface, bool ripples)
        {
            if (source == CausticsSource.Ripples && ripplesEnabledField != null && !ripples)
                return false;
            // The exposed spectrum can lag behind a runtime change to the ripples flag.
            if (bandCountField != null)
            {
                var simulation = simulationField.GetValue(surface);
                if (simulation == null)
                    return false;
                var spectrum = spectrumField.GetValue(simulation);
                return spectrum != null && (int)bandCountField.GetValue(spectrum) >= (int)source;
            }
            return source != CausticsSource.Ripples || ripples;
        }
        internal string Status
        {
            get
            {
                int count = 0;
                foreach (var b in bindings)
                    if (b.Supported)
                        count++;
                return "v" + Mod.Version + " | API " + count + "/" + bindings.Count + " | surfaces " + surfaces.Count + " | " + (running ? "ready" : "stopped");
            }
        }
        internal void Initialize(FoamSettings options)
        {
            settings = options;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                surfaceType = a.GetType("UnityEngine.Rendering.HighDefinition.WaterSurface", false);
                if (surfaceType != null)
                    break;
            }
            if (surfaceType != null && !typeof(Behaviour).IsAssignableFrom(surfaceType))
                surfaceType = null;
            surfaceKind = surfaceType == null ? null : surfaceType.GetField("surfaceType", FieldFlags);
            if (surfaceKind != null && surfaceKind.FieldType.IsEnum)
            {
                var kindType = surfaceKind.FieldType;
                if (Enum.IsDefined(kindType, "OceanSeaLake")) oceanSeaLakeKind = Enum.Parse(kindType, "OceanSeaLake");
                if (Enum.IsDefined(kindType, "Ocean")) oceanKind = Enum.Parse(kindType, "Ocean");
            }
            ripplesEnabledField = surfaceType?.GetField("ripples", FieldFlags);
            if (ripplesEnabledField?.FieldType != typeof(bool))
                ripplesEnabledField = null;
            simulationField = surfaceType?.GetField("simulation", FieldFlags);
            spectrumField = simulationField?.FieldType.GetField("spectrum", FieldFlags);
            bandCountField = spectrumField?.FieldType.GetField("numActiveBands", FieldFlags);
            if (bandCountField?.FieldType != typeof(int))
                bandCountField = null;
            ConfigureBindings();
            if (surfaceType == null)
            {
                Mod.Logger.Warn("HDRP WaterSurface unavailable. Controls disabled; no changes made.");
                return;
            }
            registry = surfaceType.GetField("instances", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Mod.Logger.Info("Unity=" + Application.unityVersion + "; water=" + surfaceType.Assembly.GetName().FullName);
            running = true;
        }

        private void LateUpdate()
        {
            if (!running)
                return;
            try
            {
                if (Time.realtimeSinceStartup >= nextScan)
                {
                    nextScan = Time.realtimeSinceStartup + 5f;
                    Discover();
                }
                removed.Clear();
                foreach (var entry in surfaces)
                {
                    var surface = entry.Key as Behaviour;
                    if (surface == null)
                    {
                        removed.Add(entry.Key);
                        continue;
                    }
                    if (!surface.isActiveAndEnabled || !surface.gameObject.scene.isLoaded)
                    {
                        entry.Value.Restore(surface);
                        removed.Add(entry.Key);
                        continue;
                    }
                    entry.Value.Update(surface, settings, GetFeatures(settings, surface));
                }
                foreach (var item in removed)
                    surfaces.Remove(item);
                if (pendingObservations > 0 && Time.realtimeSinceStartup >= nextObservation)
                {
                    LogObservations("DELAYED");
                    pendingObservations--;
                    nextObservation = Time.realtimeSinceStartup + 10f;
                }
            }
            catch (Exception e) { Mod.Logger.Error("Controller stopped: " + e); Restore(); }
        }
        private void Discover()
        {
            IEnumerable items = registry == null ? null : registry.GetValue(null) as IEnumerable;
            if (items == null)
                items = Resources.FindObjectsOfTypeAll(surfaceType);
            foreach (object item in items)
            {
                var surface = item as Behaviour;
                if (surface == null || !surface.isActiveAndEnabled || !surface.gameObject.scene.IsValid() ||
                    !surface.gameObject.scene.isLoaded || surfaces.ContainsKey(surface))
                    continue;
                var kind = surfaceKind;
                if (kind != null && Convert.ToString(kind.GetValue(surface)) == "Pool")
                    continue;
                var state = new SurfaceOverrides(bindings);
                surfaces.Add(surface, state);
                Mod.Logger.Info("BOUND: " + surface.name + "; controls=" + state.Slots.Count);
            }
            if (surfaces.Count == 0 && !emptyReported)
            {
                emptyReported = true;
                Mod.Logger.Info("WAITING: load a city with water.");
            }
        }
        public void Restore()
        {
            running = false;
            pendingObservations = 0;
            foreach (var entry in surfaces)
                if (entry.Key != null)
                    entry.Value.Restore(entry.Key);
            surfaces.Clear();
        }
        private void OnDestroy()
        {
            Restore();
        }
    }
}
