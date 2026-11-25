using System;
using System.Reflection;
using UnityEngine;

namespace WaterFeaturesPlus
{
    public sealed partial class WaterController
    {
        private WaterBinding Add(string key, Type type, Func<FoamSettings, bool> active,
            Func<FoamSettings, object, object> value, params string[] names)
        {
            FieldInfo field = null;
            if (surfaceType != null)
                foreach (string name in names)
                {
                    var f = surfaceType.GetField(name, FieldFlags);
                    if (f != null && f.FieldType == type && !f.IsInitOnly)
                    {
                        field = f;
                        break;
                    }
                }
            var binding = new WaterBinding { Key = key, Field = field, Enabled = active, Value = value };
            bindings.Add(binding);
            bindingsByKey.Add(key, binding);
            return binding;
        }
        private void AddSuspended(string key, Type type, string reason, params string[] names)
        {
            // Keep metadata for read-only diagnosis; never create a writable slot.
            Add(key, type, s => false, (s, o) => o, names).SuspendedReason = reason;
        }
        private static string Describe(WaterBinding binding)
        {
            string result = binding.Field == null ? "UNAVAILABLE" : binding.Field.Name;
            if (binding.SuspendedReason != null)
                result += " [SUSPENDED: " + binding.SuspendedReason + "]";
            if (binding.Failed)
                result += " [FAILED]";
            return result;
        }
        private static float P(int value)
        {
            return value / 100f;
        }
        private static float Relative(object original, int percent)
        {
            float v = (float)original;
            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new InvalidOperationException("Non-finite native value");
            return v * P(percent);
        }
        private void ConfigureBindings()
        {
            // Explicit whitelist. No arbitrary Material.SetFloat names or shader replacement.
            Add("FoamSwitch", typeof(bool), s => s.FoamEnabled, (s, o) => true, "foam");
            Add("SimulationFoam", typeof(bool), s => s.FoamEnabled, (s, o) => true, "simulationFoam");
            Add("Amount", typeof(float), s => s.FoamEnabled, (s, o) => P(s.Amount), "simulationFoamAmount");
            Add("Wind", typeof(AnimationCurve), s => s.FoamEnabled, (s, o) => BlendWind(o as AnimationCurve, P(s.WindIndependence)), "windFoamCurve", "simulationFoamWindCurve").ValueRevision = s => s.WindIndependence;
            Add("FoamRoughness", typeof(float), s => s.FoamEnabled && s.FoamLookEnabled, (s, o) => 1f - P(s.FoamRoughness), "simulationFoamSmoothness", "foamSmoothness");
            Add("FoamTiling", typeof(float), s => s.FoamEnabled && s.FoamLookEnabled, (s, o) => s.FoamTiling, "foamTextureTiling");
            AddSuspended("FoamPersistence", typeof(float), "Foam lifetime has no confirmed visual effect", "simulationFoamDrag");
            Add("Absorption", typeof(float), s => s.OpticsEnabled, (s, o) => s.Absorption, "absorptionDistance");
            Add("RefractionDistance", typeof(float), s => s.OpticsEnabled, (s, o) => s.RefractionDistance, "maxRefractionDistance");
            Add("RefractionColor", typeof(Color), s => s.RefractionColorEnabled, (s, o) => new Color(P(s.RefractionR), P(s.RefractionG), P(s.RefractionB), 1f), "refractionColor");
            Add("ScatteringColor", typeof(Color), s => s.ScatteringColorEnabled, (s, o) => new Color(P(s.ScatteringR), P(s.ScatteringG), P(s.ScatteringB), 1f), "scatteringColor");
            Add("Ambient", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Ambient), "ambientScattering");
            AddSuspended("Height", typeof(float), "No visible effect reported in CS2 1.6.4f1; render path unverified", "heightScattering");
            AddSuspended("Displacement", typeof(float), "No visible effect reported in CS2 1.6.4f1; render path unverified", "displacementScattering");
            AddSuspended("Tip", typeof(float), "No visible effect reported in CS2 1.6.4f1; render path unverified", "directLightTipScattering");
            Add("Body", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Body), "directLightBodyScattering");
            Add("NearSmoothness", typeof(float), s => s.ReflectionEnabled, (s, o) => P(s.NearSmoothness), "startSmoothness");
            Add("FarSmoothness", typeof(float), s => s.ReflectionEnabled, (s, o) => P(s.FarSmoothness), "endSmoothness");
            Add("SmoothFadeStart", typeof(float), s => s.ReflectionEnabled, (s, o) => s.SmoothFadeStart, "smoothnessFadeStart");
            Add("SmoothFadeDistance", typeof(float), s => s.ReflectionEnabled, (s, o) => s.SmoothFadeDistance, "smoothnessFadeDistance");
            var longBand = Add("LargeWaves", typeof(float), s => s.WavesEnabled || s.SwellEnabled,
                (s, original) => s.SwellEnabled ? P(s.SwellAmplitude) : Relative(original, s.LargeWaves), "largeBand0Multiplier");
            var mediumBand = Add("MediumWaves", typeof(float), s => s.WavesEnabled || s.SwellEnabled,
                (s, original) => s.SwellEnabled ? P(s.SwellMediumAmplitude) : Relative(original, s.MediumWaves), "largeBand1Multiplier");
            longBand.Eligible = mediumBand.Eligible = (s, features) => !s.SwellEnabled || features.Swell;
            Add("SwellPatchSize", typeof(float), s => s.SwellEnabled, (s, o) => s.SwellPatchSize, "repetitionSize").Eligible = (s, features) => features.Swell;
            Add("SwellWind", typeof(float), s => s.SwellEnabled, (s, o) => s.SwellWind, "largeWindSpeed").Eligible = (s, features) => features.Swell;
            Add("SwellChaos", typeof(float), s => s.SwellEnabled, (s, o) => P(s.SwellChaos), "largeChaos").Eligible = (s, features) => features.Swell;
            var direction = Add("SwellDirection", typeof(float), s => s.DirectionOverride,
                (s, original) => (float)s.SwellDirection, "largeWindOrientationValue");
            direction.Eligible = (s, features) => features.Swell;

            rippleFadeSwitch = Add("RippleFadeSwitch", typeof(bool), s => s.RippleFadeEnabled,
                (s, original) => true, "ripplesFadeToggle");
            rippleFadeStart = Add("RippleFadeStart", typeof(float), s => s.RippleFadeEnabled,
                (s, original) => (float)s.RippleFadeStart, "ripplesFadeStart");
            rippleFadeDistance = Add("RippleFadeDistance", typeof(float), s => s.RippleFadeEnabled,
                (s, original) => (float)s.RippleFadeDistance, "ripplesFadeDistance");
            rippleFadeSwitch.Eligible = rippleFadeStart.Eligible = rippleFadeDistance.Eligible = (s, features) => features.RippleFade;

            var sourceBand = Add("SourceBand", typeof(int), s => s.CausticsOverride && s.CausticsOn && s.SourceBand != CausticsSource.Game,
                (s, original) => (int)s.SourceBand - 1, "causticsBand");
            sourceBand.Eligible = (s, features) => features.CausticsSource;
            Add("Ripples", typeof(float), s => s.WavesEnabled && s.RippleOverrideEnabled, (s, o) => Relative(o, s.Ripples), "ripplesWindSpeed");
            // Do not own timeMultiplier: a snapshot taken while paused can pin animation to zero.
            AddSuspended("AnimationSpeed", typeof(float), "Engine-owned clock; snapshot override removed in 1.2.1", "timeMultiplier");
            Add("CausticsSwitch", typeof(bool), s => s.CausticsOverride, (s, o) => s.CausticsOn, "caustics");
            Add("CausticsIntensity", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => P(s.CausticsIntensity), "causticsIntensity");
            Add("CausticsBlend", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => s.CausticsBlend, "causticsPlaneBlendDistance");
            Add("CausticsProjection", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => s.CausticsProjection, "virtualPlaneDistance");
        }
        internal static AnimationCurve BlendWind(AnimationCurve original, float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f)
                return original;
            if (t >= 1f)
                return AnimationCurve.Linear(0f, 1f, 1f, 1f);
            if (original == null || original.length == 0)
                return original;
            var keys = original.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                k.value = Mathf.Lerp(k.value, 1f, t);
                if (!float.IsInfinity(k.inTangent))
                    k.inTangent *= 1f - t;
                if (!float.IsInfinity(k.outTangent))
                    k.outTangent *= 1f - t;
                keys[i] = k;
            }
            return new AnimationCurve(keys) { preWrapMode = original.preWrapMode, postWrapMode = original.postWrapMode };
        }
    }
}
