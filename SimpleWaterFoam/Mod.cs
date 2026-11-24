using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimpleWaterFoam
{
    public sealed class Mod : IMod
    {
        internal static readonly ILog Logger = LogManager.GetLogger("SimpleWaterFoam").SetShowsErrorsInUI(false);
        internal static WaterController Controller;
        private FoamSettings settings;
        private GameObject host;
        public void OnLoad(UpdateSystem updateSystem)
        {
            Logger.Info("SimpleWaterFoam 1.2.0: native water controls");
            try
            {
                settings = new FoamSettings(this);
                AssetDatabase.global.LoadSettings(nameof(FoamSettings), settings, new FoamSettings(this));
                GameManager.instance.localizationManager.AddSource("en-US", new FoamLocale(settings, false));
                GameManager.instance.localizationManager.AddSource("pt-BR", new FoamLocale(settings, true));
                host = new GameObject("SimpleWaterFoam.Runtime");
                Object.DontDestroyOnLoad(host);
                Controller = host.AddComponent<WaterController>();
                Controller.Initialize(settings);
                settings.RegisterInOptionsUI();
            }
            catch (Exception e) { Logger.Error("Initialization failed: " + e); OnDispose(); throw; }
        }
        public void OnDispose()
        {
            if (Controller != null) Controller.Restore();
            if (host != null) Object.Destroy(host);
            Controller = null;
            host = null;
            if (settings != null) settings.UnregisterInOptionsUI();
            settings = null;
        }
    }

    // Independent of the scene: each binding owns only the fields it actually writes.
    internal sealed class WaterBinding
    {
        internal string Key;
        internal FieldInfo Field;
        internal Func<FoamSettings, bool> Enabled;
        internal Func<FoamSettings, object, object> Value;
        internal bool Failed;
        internal bool Supported { get { return Field != null && !Failed; } }
    }

    internal sealed class WaterSlot
    {
        internal readonly WaterBinding Binding;
        internal object Original, Desired, LastWritten;
        internal bool Owned;
        private int revision = -1;
        internal WaterSlot(WaterBinding binding) { Binding = binding; }

        internal void Apply(object target, FoamSettings settings)
        {
            if (!Binding.Supported || !settings.EnableEffect || !Binding.Enabled(settings))
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
            if (!Owned || revision != settings.Revision)
            {
                Desired = Binding.Value(settings, Original);
                revision = settings.Revision;
            }
            Binding.Field.SetValue(target, Desired);
            LastWritten = Desired;
            Owned = true;
        }

        internal void Restore(object target)
        {
            if (!Owned) return;
            try
            {
                // Keep a newer value if another system wrote after our last update.
                if (Equals(Binding.Field.GetValue(target), LastWritten))
                    Binding.Field.SetValue(target, Original);
            }
            finally { Owned = false; Original = Desired = LastWritten = null; revision = -1; }
        }
    }

    public sealed class WaterController : MonoBehaviour
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private FoamSettings settings;
        private Type surfaceType;
        private FieldInfo registry;
        private readonly List<WaterBinding> bindings = new List<WaterBinding>();
        private readonly Dictionary<Object, List<WaterSlot>> surfaces = new Dictionary<Object, List<WaterSlot>>();
        private readonly List<Object> removed = new List<Object>();
        private float nextScan;
        private bool running;
        private int lastRevision = -1;
        private bool emptyReported;

        internal bool Supports(string key)
        {
            if (!running) return false;
            foreach (WaterBinding binding in bindings) if (binding.Key == key) return binding.Supported;
            return false;
        }
        internal string Status
        {
            get
            {
                int count = 0;
                foreach (var b in bindings) if (b.Supported) count++;
                return "v1.2.0 | API " + count + "/" + bindings.Count + " | surfaces " + surfaces.Count + " | " + (running ? "ready" : "stopped");
            }
        }
        internal void Initialize(FoamSettings options)
        {
            settings = options;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                surfaceType = a.GetType("UnityEngine.Rendering.HighDefinition.WaterSurface", false);
                if (surfaceType != null) break;
            }
            if (surfaceType != null && !typeof(Behaviour).IsAssignableFrom(surfaceType)) surfaceType = null;
            ConfigureBindings();
            foreach (var b in bindings)
                Mod.Logger.Info("API " + b.Key + ": " + (b.Supported ? b.Field.Name : "UNAVAILABLE"));
            if (surfaceType == null)
            {
                Mod.Logger.Warn("HDRP WaterSurface unavailable. Controls disabled; no changes made.");
                return;
            }
            registry = surfaceType.GetField("instances", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Mod.Logger.Info("Unity=" + Application.unityVersion + "; water=" + surfaceType.Assembly.GetName().FullName);
            running = true;
        }

        private void Add(string key, Type type, Func<FoamSettings, bool> active,
            Func<FoamSettings, object, object> value, params string[] names)
        {
            FieldInfo field = null;
            if (surfaceType != null)
                foreach (string name in names)
                {
                    var f = surfaceType.GetField(name, FieldFlags);
                    if (f != null && f.FieldType == type && !f.IsInitOnly) { field = f; break; }
                }
            bindings.Add(new WaterBinding { Key = key, Field = field, Enabled = active, Value = value });
        }
        private static float P(int value) { return value / 100f; }
        private static float Relative(object original, int percent)
        {
            float v = (float)original;
            if (float.IsNaN(v) || float.IsInfinity(v)) throw new InvalidOperationException("Non-finite native value");
            return v * P(percent);
        }
        private void ConfigureBindings()
        {
            // Explicit whitelist. No arbitrary Material.SetFloat names or shader replacement.
            Add("FoamSwitch", typeof(bool), s => s.FoamEnabled, (s, o) => true, "foam");
            Add("SimulationFoam", typeof(bool), s => s.FoamEnabled, (s, o) => true, "simulationFoam");
            Add("Amount", typeof(float), s => s.FoamEnabled, (s, o) => P(s.Amount), "simulationFoamAmount");
            Add("Wind", typeof(AnimationCurve), s => s.FoamEnabled, (s, o) => BlendWind(o as AnimationCurve, P(s.WindIndependence)), "windFoamCurve", "simulationFoamWindCurve");
            Add("FoamRoughness", typeof(float), s => s.FoamEnabled && s.FoamLookEnabled, (s, o) => 1f - P(s.FoamRoughness), "simulationFoamSmoothness", "foamSmoothness");
            Add("FoamTiling", typeof(float), s => s.FoamEnabled && s.FoamLookEnabled, (s, o) => s.FoamTiling, "foamTextureTiling");
            Add("FoamPersistence", typeof(float), s => s.FoamEnabled && s.FoamPersistenceEnabled, (s, o) => P(s.FoamPersistence), "simulationFoamDrag");
            Add("Absorption", typeof(float), s => s.OpticsEnabled, (s, o) => s.Absorption, "absorptionDistance");
            Add("RefractionDistance", typeof(float), s => s.OpticsEnabled, (s, o) => s.RefractionDistance, "maxRefractionDistance");
            Add("RefractionColor", typeof(Color), s => s.RefractionColorEnabled, (s, o) => new Color(P(s.RefractionR), P(s.RefractionG), P(s.RefractionB), 1f), "refractionColor");
            Add("ScatteringColor", typeof(Color), s => s.ScatteringColorEnabled, (s, o) => new Color(P(s.ScatteringR), P(s.ScatteringG), P(s.ScatteringB), 1f), "scatteringColor");
            Add("Ambient", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Ambient), "ambientScattering");
            Add("Height", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Height), "heightScattering");
            Add("Displacement", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Displacement), "displacementScattering");
            Add("Tip", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Tip), "directLightTipScattering");
            Add("Body", typeof(float), s => s.LightingEnabled, (s, o) => P(s.Body), "directLightBodyScattering");
            Add("NearSmoothness", typeof(float), s => s.ReflectionEnabled, (s, o) => P(s.NearSmoothness), "startSmoothness");
            Add("FarSmoothness", typeof(float), s => s.ReflectionEnabled, (s, o) => P(s.FarSmoothness), "endSmoothness");
            Add("SmoothFadeStart", typeof(float), s => s.ReflectionEnabled, (s, o) => s.SmoothFadeStart, "smoothnessFadeStart");
            Add("SmoothFadeDistance", typeof(float), s => s.ReflectionEnabled, (s, o) => s.SmoothFadeDistance, "smoothnessFadeDistance");
            Add("LargeWaves", typeof(float), s => s.WavesEnabled, (s, o) => Relative(o, s.LargeWaves), "largeBand0Multiplier");
            Add("MediumWaves", typeof(float), s => s.WavesEnabled, (s, o) => Relative(o, s.MediumWaves), "largeBand1Multiplier");
            Add("Ripples", typeof(float), s => s.WavesEnabled, (s, o) => Relative(o, s.Ripples), "ripplesWindSpeed");
            Add("AnimationSpeed", typeof(float), s => s.WavesEnabled, (s, o) => Relative(o, s.AnimationSpeed), "timeMultiplier");
            Add("CausticsSwitch", typeof(bool), s => s.CausticsOverride, (s, o) => s.CausticsOn, "caustics");
            Add("CausticsIntensity", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => P(s.CausticsIntensity), "causticsIntensity");
            Add("CausticsBlend", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => s.CausticsBlend, "causticsPlaneBlendDistance");
            Add("CausticsProjection", typeof(float), s => s.CausticsOverride && s.CausticsOn, (s, o) => s.CausticsProjection, "virtualPlaneDistance");
        }
        internal static AnimationCurve BlendWind(AnimationCurve original, float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return original;
            if (t >= 1f) return AnimationCurve.Linear(0f, 1f, 1f, 1f);
            if (original == null || original.length == 0) return original;
            var keys = original.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i]; k.value = Mathf.Lerp(k.value, 1f, t);
                if (!float.IsInfinity(k.inTangent)) k.inTangent *= 1f - t;
                if (!float.IsInfinity(k.outTangent)) k.outTangent *= 1f - t;
                keys[i] = k;
            }
            return new AnimationCurve(keys) { preWrapMode = original.preWrapMode, postWrapMode = original.postWrapMode };
        }
        private void LateUpdate()
        {
            if (!running) return;
            try
            {
                if (Time.realtimeSinceStartup >= nextScan)
                {
                    nextScan = Time.realtimeSinceStartup + 5f;
                    Discover();
                }
                if (lastRevision != settings.Revision)
                {
                    lastRevision = settings.Revision;
                    Mod.Logger.Info("OPTIONS revision=" + lastRevision + "; enabled=" + settings.EnableEffect + "; foam=" + settings.Amount);
                }
                removed.Clear();
                foreach (var entry in surfaces)
                {
                    var surface = entry.Key as Behaviour;
                    if (surface == null) { removed.Add(entry.Key); continue; }
                    if (!surface.isActiveAndEnabled || !surface.gameObject.scene.isLoaded)
                    { RestoreSlots(surface, entry.Value); removed.Add(entry.Key); continue; }
                    foreach (var slot in entry.Value)
                    {
                        try { slot.Apply(surface, settings); }
                        catch (Exception e)
                        {
                            slot.Binding.Failed = true;
                            try { slot.Restore(surface); } catch { }
                            Mod.Logger.Warn("Disabled control " + slot.Binding.Key + ": " + e);
                        }
                    }
                }
                foreach (var item in removed) surfaces.Remove(item);
            }
            catch (Exception e) { Mod.Logger.Error("Controller stopped: " + e); Restore(); }
        }
        private void Discover()
        {
            IEnumerable items = registry == null ? null : registry.GetValue(null) as IEnumerable;
            if (items == null) items = Resources.FindObjectsOfTypeAll(surfaceType);
            foreach (object item in items)
            {
                var surface = item as Behaviour;
                if (surface == null || !surface.isActiveAndEnabled || !surface.gameObject.scene.IsValid() ||
                    !surface.gameObject.scene.isLoaded || surfaces.ContainsKey(surface)) continue;
                var kind = surfaceType.GetField("surfaceType", FieldFlags);
                if (kind != null && Convert.ToString(kind.GetValue(surface)) == "Pool") continue;
                var slots = new List<WaterSlot>();
                foreach (var binding in bindings) if (binding.Supported) slots.Add(new WaterSlot(binding));
                surfaces.Add(surface, slots);
                Mod.Logger.Info("BOUND: " + surface.name + "; controls=" + slots.Count);
                ReportMaterial(surface);
            }
            if (surfaces.Count == 0 && !emptyReported)
            { emptyReported = true; Mod.Logger.Info("WAITING: load a city with water."); }
        }
        private void ReportMaterial(Object surface)
        {
            try
            {
                var field = surfaceType.GetField("customMaterial", FieldFlags);
                var material = field == null ? null : field.GetValue(surface) as Material;
                if (material == null) { Mod.Logger.Info("Material: no exposed custom material."); return; }
                var shader = material.shader;
                Mod.Logger.Info("Material=" + material.name + "; shader=" + (shader == null ? "null" : shader.name));
                if (shader == null) return;
                for (int i = 0; i < shader.GetPropertyCount(); i++) Mod.Logger.Info("Shader property: " + shader.GetPropertyName(i));
            }
            catch (Exception e) { Mod.Logger.Warn("Material inspection: " + e.Message); }
        }
        internal void Dump()
        {
            Mod.Logger.Info("DIAGNOSTICS " + Status + ". Exposed field does not prove shader support.");
            foreach (var b in bindings) Mod.Logger.Info(b.Key + ": " + (b.Supported ? b.Field.Name : "UNAVAILABLE"));
            foreach (var entry in surfaces)
            {
                if (entry.Key == null) continue;
                ReportMaterial(entry.Key);
                foreach (var slot in entry.Value)
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
        private static void RestoreSlots(Object surface, List<WaterSlot> slots)
        {
            if (surface == null) return;
            foreach (var slot in slots)
                try { slot.Restore(surface); } catch (Exception e) { Mod.Logger.Warn("Restore: " + e.Message); }
        }
        public void Restore()
        {
            running = false;
            foreach (var entry in surfaces) RestoreSlots(entry.Key, entry.Value);
            surfaces.Clear();
        }
        private void OnDestroy() { Restore(); }
    }
}

namespace SimpleWaterFoam
{
    [FileLocation("ModsSettings/SimpleWaterFoam/FoamSettings")]
    [SettingsUITabOrder("General", "Water", "Reflections", "Waves", "Foam", "Caustics")]
    [SettingsUIGroupOrder("Global", "Presets", "Diagnostics", "Optics", "Transmission", "ScatterColor", "Light", "Surface", "Motion", "Coverage", "Texture", "Lifetime", "CausticControls")]
    [SettingsUIShowGroupName("Global", "Presets", "Diagnostics", "Optics", "Transmission", "ScatterColor", "Light", "Surface", "Motion", "Coverage", "Texture", "Lifetime", "CausticControls")]
    public sealed class FoamSettings : ModSetting
    {
        internal int Revision { get; private set; }
        public FoamSettings(IMod mod) : base(mod) { SetDefaults(); }
        private static int Limit(int v, int lo, int hi) { return Math.Max(lo, Math.Min(hi, v)); }
        private static float Limit(float v, float lo, float hi) { if (float.IsNaN(v) || float.IsInfinity(v)) return lo; return Math.Max(lo, Math.Min(hi, v)); }
        private bool _EnableEffect;
        [SettingsUISection("General", "Global")]
        public bool EnableEffect { get { return _EnableEffect; } set { var v = value; if (_EnableEffect != v) { _EnableEffect = v; Revision++; } } }
        private bool _OpticsEnabled;
        [SettingsUISection("Water", "Optics")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledOpticsEnabled))]
        public bool OpticsEnabled { get { return _OpticsEnabled; } set { var v = value; if (_OpticsEnabled != v) { _OpticsEnabled = v; Revision++; } } }
        public bool DisabledOpticsEnabled() { return !(EnableEffect); }
        private float _Absorption;
        [SettingsUISection("Water", "Optics")]
        [SettingsUISlider(min = 0.5f, max = 100.0f, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAbsorption))]
        public float Absorption { get { return _Absorption; } set { var v = Limit(value, 0.5f, 100.0f); if (_Absorption != v) { _Absorption = v; Revision++; } } }
        public bool DisabledAbsorption() { return !(EnableEffect && OpticsEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Absorption")); }
        private float _RefractionDistance;
        [SettingsUISection("Water", "Optics")]
        [SettingsUISlider(min = 0.1f, max = 5.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionDistance))]
        public float RefractionDistance { get { return _RefractionDistance; } set { var v = Limit(value, 0.1f, 5.0f); if (_RefractionDistance != v) { _RefractionDistance = v; Revision++; } } }
        public bool DisabledRefractionDistance() { return !(EnableEffect && OpticsEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("RefractionDistance")); }
        private bool _RefractionColorEnabled;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionColorEnabled))]
        public bool RefractionColorEnabled { get { return _RefractionColorEnabled; } set { var v = value; if (_RefractionColorEnabled != v) { _RefractionColorEnabled = v; Revision++; } } }
        public bool DisabledRefractionColorEnabled() { return !(EnableEffect) || (Mod.Controller == null || !Mod.Controller.Supports("RefractionColor")); }
        private int _RefractionR;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionR))]
        public int RefractionR { get { return _RefractionR; } set { var v = Limit(value, 0, 100); if (_RefractionR != v) { _RefractionR = v; Revision++; } } }
        public bool DisabledRefractionR() { return !(EnableEffect && RefractionColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("RefractionColor")); }
        private int _RefractionG;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionG))]
        public int RefractionG { get { return _RefractionG; } set { var v = Limit(value, 0, 100); if (_RefractionG != v) { _RefractionG = v; Revision++; } } }
        public bool DisabledRefractionG() { return !(EnableEffect && RefractionColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("RefractionColor")); }
        private int _RefractionB;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionB))]
        public int RefractionB { get { return _RefractionB; } set { var v = Limit(value, 0, 100); if (_RefractionB != v) { _RefractionB = v; Revision++; } } }
        public bool DisabledRefractionB() { return !(EnableEffect && RefractionColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("RefractionColor")); }
        private bool _ScatteringColorEnabled;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringColorEnabled))]
        public bool ScatteringColorEnabled { get { return _ScatteringColorEnabled; } set { var v = value; if (_ScatteringColorEnabled != v) { _ScatteringColorEnabled = v; Revision++; } } }
        public bool DisabledScatteringColorEnabled() { return !(EnableEffect) || (Mod.Controller == null || !Mod.Controller.Supports("ScatteringColor")); }
        private int _ScatteringR;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringR))]
        public int ScatteringR { get { return _ScatteringR; } set { var v = Limit(value, 0, 100); if (_ScatteringR != v) { _ScatteringR = v; Revision++; } } }
        public bool DisabledScatteringR() { return !(EnableEffect && ScatteringColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("ScatteringColor")); }
        private int _ScatteringG;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringG))]
        public int ScatteringG { get { return _ScatteringG; } set { var v = Limit(value, 0, 100); if (_ScatteringG != v) { _ScatteringG = v; Revision++; } } }
        public bool DisabledScatteringG() { return !(EnableEffect && ScatteringColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("ScatteringColor")); }
        private int _ScatteringB;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringB))]
        public int ScatteringB { get { return _ScatteringB; } set { var v = Limit(value, 0, 100); if (_ScatteringB != v) { _ScatteringB = v; Revision++; } } }
        public bool DisabledScatteringB() { return !(EnableEffect && ScatteringColorEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("ScatteringColor")); }
        private bool _LightingEnabled;
        [SettingsUISection("Water", "Light")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledLightingEnabled))]
        public bool LightingEnabled { get { return _LightingEnabled; } set { var v = value; if (_LightingEnabled != v) { _LightingEnabled = v; Revision++; } } }
        public bool DisabledLightingEnabled() { return !(EnableEffect); }
        private int _Ambient;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAmbient))]
        public int Ambient { get { return _Ambient; } set { var v = Limit(value, 0, 100); if (_Ambient != v) { _Ambient = v; Revision++; } } }
        public bool DisabledAmbient() { return !(EnableEffect && LightingEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Ambient")); }
        private int _Height;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledHeight))]
        public int Height { get { return _Height; } set { var v = Limit(value, 0, 100); if (_Height != v) { _Height = v; Revision++; } } }
        public bool DisabledHeight() { return !(EnableEffect && LightingEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Height")); }
        private int _Displacement;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledDisplacement))]
        public int Displacement { get { return _Displacement; } set { var v = Limit(value, 0, 100); if (_Displacement != v) { _Displacement = v; Revision++; } } }
        public bool DisabledDisplacement() { return !(EnableEffect && LightingEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Displacement")); }
        private int _Tip;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledTip))]
        public int Tip { get { return _Tip; } set { var v = Limit(value, 0, 100); if (_Tip != v) { _Tip = v; Revision++; } } }
        public bool DisabledTip() { return !(EnableEffect && LightingEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Tip")); }
        private int _Body;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledBody))]
        public int Body { get { return _Body; } set { var v = Limit(value, 0, 100); if (_Body != v) { _Body = v; Revision++; } } }
        public bool DisabledBody() { return !(EnableEffect && LightingEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Body")); }
        private bool _ReflectionEnabled;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledReflectionEnabled))]
        public bool ReflectionEnabled { get { return _ReflectionEnabled; } set { var v = value; if (_ReflectionEnabled != v) { _ReflectionEnabled = v; Revision++; } } }
        public bool DisabledReflectionEnabled() { return !(EnableEffect); }
        private int _NearSmoothness;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledNearSmoothness))]
        public int NearSmoothness { get { return _NearSmoothness; } set { var v = Limit(value, 0, 100); if (_NearSmoothness != v) { _NearSmoothness = v; Revision++; } } }
        public bool DisabledNearSmoothness() { return !(EnableEffect && ReflectionEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("NearSmoothness")); }
        private int _FarSmoothness;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFarSmoothness))]
        public int FarSmoothness { get { return _FarSmoothness; } set { var v = Limit(value, 0, 100); if (_FarSmoothness != v) { _FarSmoothness = v; Revision++; } } }
        public bool DisabledFarSmoothness() { return !(EnableEffect && ReflectionEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("FarSmoothness")); }
        private float _SmoothFadeStart;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 3000.0f, step = 25.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSmoothFadeStart))]
        public float SmoothFadeStart { get { return _SmoothFadeStart; } set { var v = Limit(value, 0.0f, 3000.0f); if (_SmoothFadeStart != v) { _SmoothFadeStart = v; Revision++; } } }
        public bool DisabledSmoothFadeStart() { return !(EnableEffect && ReflectionEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("SmoothFadeStart")); }
        private float _SmoothFadeDistance;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 25.0f, max = 4000.0f, step = 25.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSmoothFadeDistance))]
        public float SmoothFadeDistance { get { return _SmoothFadeDistance; } set { var v = Limit(value, 25.0f, 4000.0f); if (_SmoothFadeDistance != v) { _SmoothFadeDistance = v; Revision++; } } }
        public bool DisabledSmoothFadeDistance() { return !(EnableEffect && ReflectionEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("SmoothFadeDistance")); }
        private bool _WavesEnabled;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledWavesEnabled))]
        public bool WavesEnabled { get { return _WavesEnabled; } set { var v = value; if (_WavesEnabled != v) { _WavesEnabled = v; Revision++; } } }
        public bool DisabledWavesEnabled() { return !(EnableEffect); }
        private int _LargeWaves;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledLargeWaves))]
        public int LargeWaves { get { return _LargeWaves; } set { var v = Limit(value, 0, 150); if (_LargeWaves != v) { _LargeWaves = v; Revision++; } } }
        public bool DisabledLargeWaves() { return !(EnableEffect && WavesEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("LargeWaves")); }
        private int _MediumWaves;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledMediumWaves))]
        public int MediumWaves { get { return _MediumWaves; } set { var v = Limit(value, 0, 150); if (_MediumWaves != v) { _MediumWaves = v; Revision++; } } }
        public bool DisabledMediumWaves() { return !(EnableEffect && WavesEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("MediumWaves")); }
        private int _Ripples;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRipples))]
        public int Ripples { get { return _Ripples; } set { var v = Limit(value, 0, 150); if (_Ripples != v) { _Ripples = v; Revision++; } } }
        public bool DisabledRipples() { return !(EnableEffect && WavesEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Ripples")); }
        private int _AnimationSpeed;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 25.0f, max = 200.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAnimationSpeed))]
        public int AnimationSpeed { get { return _AnimationSpeed; } set { var v = Limit(value, 25, 200); if (_AnimationSpeed != v) { _AnimationSpeed = v; Revision++; } } }
        public bool DisabledAnimationSpeed() { return !(EnableEffect && WavesEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("AnimationSpeed")); }
        private bool _FoamEnabled;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamEnabled))]
        public bool FoamEnabled { get { return _FoamEnabled; } set { var v = value; if (_FoamEnabled != v) { _FoamEnabled = v; Revision++; } } }
        public bool DisabledFoamEnabled() { return !(EnableEffect); }
        private int _Amount;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAmount))]
        public int Amount { get { return _Amount; } set { var v = Limit(value, 0, 100); if (_Amount != v) { _Amount = v; Revision++; } } }
        public bool DisabledAmount() { return !(EnableEffect && FoamEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Amount")); }
        private int _WindIndependence;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledWindIndependence))]
        public int WindIndependence { get { return _WindIndependence; } set { var v = Limit(value, 0, 100); if (_WindIndependence != v) { _WindIndependence = v; Revision++; } } }
        public bool DisabledWindIndependence() { return !(EnableEffect && FoamEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("Wind")); }
        private bool _FoamLookEnabled;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamLookEnabled))]
        public bool FoamLookEnabled { get { return _FoamLookEnabled; } set { var v = value; if (_FoamLookEnabled != v) { _FoamLookEnabled = v; Revision++; } } }
        public bool DisabledFoamLookEnabled() { return !(EnableEffect && FoamEnabled); }
        private int _FoamRoughness;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamRoughness))]
        public int FoamRoughness { get { return _FoamRoughness; } set { var v = Limit(value, 0, 100); if (_FoamRoughness != v) { _FoamRoughness = v; Revision++; } } }
        public bool DisabledFoamRoughness() { return !(EnableEffect && FoamEnabled && FoamLookEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("FoamRoughness")); }
        private float _FoamTiling;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUISlider(min = 0.1f, max = 2.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamTiling))]
        public float FoamTiling { get { return _FoamTiling; } set { var v = Limit(value, 0.1f, 2.0f); if (_FoamTiling != v) { _FoamTiling = v; Revision++; } } }
        public bool DisabledFoamTiling() { return !(EnableEffect && FoamEnabled && FoamLookEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("FoamTiling")); }
        private bool _FoamPersistenceEnabled;
        [SettingsUISection("Foam", "Lifetime")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamPersistenceEnabled))]
        public bool FoamPersistenceEnabled { get { return _FoamPersistenceEnabled; } set { var v = value; if (_FoamPersistenceEnabled != v) { _FoamPersistenceEnabled = v; Revision++; } } }
        public bool DisabledFoamPersistenceEnabled() { return !(EnableEffect && FoamEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("FoamPersistence")); }
        private int _FoamPersistence;
        [SettingsUISection("Foam", "Lifetime")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamPersistence))]
        public int FoamPersistence { get { return _FoamPersistence; } set { var v = Limit(value, 0, 100); if (_FoamPersistence != v) { _FoamPersistence = v; Revision++; } } }
        public bool DisabledFoamPersistence() { return !(EnableEffect && FoamEnabled && FoamPersistenceEnabled) || (Mod.Controller == null || !Mod.Controller.Supports("FoamPersistence")); }
        private bool _CausticsOverride;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsOverride))]
        public bool CausticsOverride { get { return _CausticsOverride; } set { var v = value; if (_CausticsOverride != v) { _CausticsOverride = v; Revision++; } } }
        public bool DisabledCausticsOverride() { return !(EnableEffect) || (Mod.Controller == null || !Mod.Controller.Supports("CausticsSwitch")); }
        private bool _CausticsOn;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsOn))]
        public bool CausticsOn { get { return _CausticsOn; } set { var v = value; if (_CausticsOn != v) { _CausticsOn = v; Revision++; } } }
        public bool DisabledCausticsOn() { return !(EnableEffect && CausticsOverride) || (Mod.Controller == null || !Mod.Controller.Supports("CausticsSwitch")); }
        private int _CausticsIntensity;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsIntensity))]
        public int CausticsIntensity { get { return _CausticsIntensity; } set { var v = Limit(value, 0, 100); if (_CausticsIntensity != v) { _CausticsIntensity = v; Revision++; } } }
        public bool DisabledCausticsIntensity() { return !(EnableEffect && CausticsOverride && CausticsOn) || (Mod.Controller == null || !Mod.Controller.Supports("CausticsIntensity")); }
        private float _CausticsBlend;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.1f, max = 10.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsBlend))]
        public float CausticsBlend { get { return _CausticsBlend; } set { var v = Limit(value, 0.1f, 10.0f); if (_CausticsBlend != v) { _CausticsBlend = v; Revision++; } } }
        public bool DisabledCausticsBlend() { return !(EnableEffect && CausticsOverride && CausticsOn) || (Mod.Controller == null || !Mod.Controller.Supports("CausticsBlend")); }
        private float _CausticsProjection;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.5f, max = 15.0f, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsProjection))]
        public float CausticsProjection { get { return _CausticsProjection; } set { var v = Limit(value, 0.5f, 15.0f); if (_CausticsProjection != v) { _CausticsProjection = v; Revision++; } } }
        public bool DisabledCausticsProjection() { return !(EnableEffect && CausticsOverride && CausticsOn) || (Mod.Controller == null || !Mod.Controller.Supports("CausticsProjection")); }
        [SettingsUISection("General", "Diagnostics")]
        public string RuntimeStatus { get { return Mod.Controller == null ? "Not initialized" : Mod.Controller.Status; } }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool BalancedPreset
        {
            set
            {
                SetDefaults(); EnableEffect = true; OpticsEnabled = true; RefractionColorEnabled = true; ScatteringColorEnabled = true; ReflectionEnabled = true; WavesEnabled = true; FoamLookEnabled = true; Amount = 30; WindIndependence = 30; LargeWaves = 90; MediumWaves = 90; Ripples = 65; ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool FoamOnlyPreset
        {
            set
            {
                SetDefaults(); ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool NativePreset
        {
            set
            {
                EnableEffect = true; FoamEnabled = OpticsEnabled = RefractionColorEnabled = ScatteringColorEnabled = LightingEnabled = ReflectionEnabled = WavesEnabled = FoamLookEnabled = FoamPersistenceEnabled = CausticsOverride = false; ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Diagnostics")]
        [SettingsUIButton]
        public bool WriteDiagnostics
        {
            set
            {
                if (Mod.Controller != null) Mod.Controller.Dump();
            }
        }
        public override void SetDefaults()
        {
            EnableEffect = true;
            OpticsEnabled = false;
            Absorption = 8.0f;
            RefractionDistance = 1.0f;
            RefractionColorEnabled = false;
            RefractionR = 6;
            RefractionG = 30;
            RefractionB = 40;
            ScatteringColorEnabled = false;
            ScatteringR = 6;
            ScatteringG = 25;
            ScatteringB = 22;
            LightingEnabled = false;
            Ambient = 10;
            Height = 10;
            Displacement = 30;
            Tip = 60;
            Body = 40;
            ReflectionEnabled = false;
            NearSmoothness = 85;
            FarSmoothness = 80;
            SmoothFadeStart = 100.0f;
            SmoothFadeDistance = 500.0f;
            WavesEnabled = false;
            LargeWaves = 100;
            MediumWaves = 100;
            Ripples = 100;
            AnimationSpeed = 100;
            FoamEnabled = true;
            Amount = 40;
            WindIndependence = 100;
            FoamLookEnabled = false;
            FoamRoughness = 65;
            FoamTiling = 0.2f;
            FoamPersistenceEnabled = false;
            FoamPersistence = 20;
            CausticsOverride = false;
            CausticsOn = true;
            CausticsIntensity = 30;
            CausticsBlend = 1.0f;
            CausticsProjection = 5.0f;
        }
    }
    public sealed class FoamLocale : IDictionarySource
    {
        private readonly FoamSettings settings; private readonly bool pt;
        public FoamLocale(FoamSettings s, bool portuguese) { settings = s; pt = portuguese; }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string> {
{settings.GetSettingsLocaleID(),"Simple Water Foam"},
{settings.GetOptionTabLocaleID("General"),pt?"Geral":"General"},
{settings.GetOptionTabLocaleID("Water"),pt?"Água":"Water"},
{settings.GetOptionTabLocaleID("Reflections"),pt?"Reflexos":"Reflections"},
{settings.GetOptionTabLocaleID("Waves"),pt?"Ondas":"Waves"},
{settings.GetOptionTabLocaleID("Foam"),pt?"Espuma":"Foam"},
{settings.GetOptionTabLocaleID("Caustics"),pt?"Cáusticas":"Caustics"},
{settings.GetOptionGroupLocaleID("Global"),pt?"Comparação":"Comparison"},
{settings.GetOptionGroupLocaleID("Presets"),pt?"Presets e restauração":"Presets and reset"},
{settings.GetOptionGroupLocaleID("Diagnostics"),pt?"Diagnóstico":"Diagnostics"},
{settings.GetOptionGroupLocaleID("Optics"),pt?"Transparência e refração":"Transparency and refraction"},
{settings.GetOptionGroupLocaleID("Transmission"),pt?"Cor de transmissão":"Transmission color"},
{settings.GetOptionGroupLocaleID("ScatterColor"),pt?"Cor de dispersão":"Scattering color"},
{settings.GetOptionGroupLocaleID("Light"),pt?"Luz no volume da água":"Volume lighting"},
{settings.GetOptionGroupLocaleID("Surface"),pt?"Suavidade e distância":"Smoothness and distance"},
{settings.GetOptionGroupLocaleID("Motion"),pt?"Escalas e movimento":"Scales and motion"},
{settings.GetOptionGroupLocaleID("Coverage"),pt?"Cobertura":"Coverage"},
{settings.GetOptionGroupLocaleID("Texture"),pt?"Material e textura":"Material and texture"},
{settings.GetOptionGroupLocaleID("Lifetime"),pt?"Duração":"Lifetime"},
{settings.GetOptionGroupLocaleID("CausticControls"),pt?"Luz sob a água":"Underwater light"},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.EnableEffect)),pt?"Aplicar ajustes (comparação A/B)":"Apply adjustments (A/B comparison)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.EnableEffect)),pt?"Desative para restaurar os valores anteriores sem perder a configuração escolhida.":"Apply adjustments (A/B comparison). See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.OpticsEnabled)),pt?"Ajustar transparência e refração":"Override transparency and refraction"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.OpticsEnabled)),pt?"Ativa os valores deste grupo.":"Override transparency and refraction. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Absorption)),pt?"Distância de absorção (m)":"Absorption distance (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Absorption)),pt?"Valores maiores permitem enxergar mais longe através da água; depende da iluminação e do fundo.":"Absorption distance (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RefractionDistance)),pt?"Distância máxima de refração (m)":"Maximum refraction distance (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RefractionDistance)),pt?"Limita a profundidade usada na distorção da refração. Não é a distância de desenho do cenário.":"Maximum refraction distance (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RefractionColorEnabled)),pt?"Ajustar cor de transmissão":"Override transmission color"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RefractionColorEnabled)),pt?"Altera os componentes RGB nativos. Não altera a cor do céu refletido.":"Override transmission color. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RefractionR)),pt?"Vermelho — transmissão":"Red — transmission"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RefractionR)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Red — transmission. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RefractionG)),pt?"Verde — transmissão":"Green — transmission"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RefractionG)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Green — transmission. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RefractionB)),pt?"Azul — transmissão":"Blue — transmission"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RefractionB)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Blue — transmission. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.ScatteringColorEnabled)),pt?"Ajustar cor de dispersão":"Override scattering color"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.ScatteringColorEnabled)),pt?"Altera os componentes RGB nativos. Não altera a cor do céu refletido.":"Override scattering color. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.ScatteringR)),pt?"Vermelho — dispersão":"Red — scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.ScatteringR)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Red — scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.ScatteringG)),pt?"Verde — dispersão":"Green — scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.ScatteringG)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Green — scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.ScatteringB)),pt?"Azul — dispersão":"Blue — scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.ScatteringB)),pt?"Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.":"Blue — scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.LightingEnabled)),pt?"Ajustar dispersão de luz":"Override light scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.LightingEnabled)),pt?"Controla a contribuição da luz no volume e nas ondas; valores excessivos podem produzir aparência luminosa.":"Override light scattering. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Ambient)),pt?"Luz ambiente":"Ambient scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Ambient)),pt?"Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.":"Ambient scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Height)),pt?"Dispersão por altura da onda":"Height scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Height)),pt?"Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.":"Height scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Displacement)),pt?"Dispersão por deslocamento":"Displacement scattering"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Displacement)),pt?"Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.":"Displacement scattering. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Tip)),pt?"Luz nas cristas":"Wave-tip lighting"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Tip)),pt?"Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.":"Wave-tip lighting. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Body)),pt?"Luz no corpo das ondas":"Wave-body lighting"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Body)),pt?"Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.":"Wave-body lighting. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.ReflectionEnabled)),pt?"Ajustar resposta aos reflexos":"Override reflection response"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.ReflectionEnabled)),pt?"Ajusta a suavidade do material. Não ativa SSR, ray tracing ou uma nova renderização de reflexos.":"Override reflection response. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.NearSmoothness)),pt?"Suavidade próxima":"Near smoothness"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.NearSmoothness)),pt?"Mais alto concentra o brilho; mais baixo aumenta a rugosidade. Não equivale à intensidade do reflexo.":"Near smoothness. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FarSmoothness)),pt?"Suavidade distante":"Far smoothness"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FarSmoothness)),pt?"Controla a suavidade após a transição por distância.":"Far smoothness. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.SmoothFadeStart)),pt?"Início da transição (m)":"Transition start (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.SmoothFadeStart)),pt?"Distância da câmera onde começa a mudança de suavidade.":"Transition start (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.SmoothFadeDistance)),pt?"Comprimento da transição (m)":"Transition length (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.SmoothFadeDistance)),pt?"Comprimento da transição, contado a partir de seu início.":"Transition length (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.WavesEnabled)),pt?"Ajustar ondas visuais":"Override visual waves"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.WavesEnabled)),pt?"Multiplicadores relativos ao valor encontrado ao ativar o grupo. Não altera fontes, vazão ou nível da simulação hidrológica.":"Override visual waves. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.LargeWaves)),pt?"Ondas grandes — multiplicador (%)":"Large waves — multiplier (%)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.LargeWaves)),pt?"100 mantém o valor capturado ao ativar. A agitação fina multiplica o vento da banda de ripples, não diretamente sua amplitude. Se o valor original for zero, o resultado permanece zero.":"Large waves — multiplier (%). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.MediumWaves)),pt?"Ondas médias — multiplicador (%)":"Medium waves — multiplier (%)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.MediumWaves)),pt?"100 mantém o valor capturado ao ativar. A agitação fina multiplica o vento da banda de ripples, não diretamente sua amplitude. Se o valor original for zero, o resultado permanece zero.":"Medium waves — multiplier (%). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Ripples)),pt?"Agitação fina — multiplicador (%)":"Ripple wind — multiplier (%)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Ripples)),pt?"100 mantém o valor capturado ao ativar. A agitação fina multiplica o vento da banda de ripples, não diretamente sua amplitude. Se o valor original for zero, o resultado permanece zero.":"Ripple wind — multiplier (%). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.AnimationSpeed)),pt?"Velocidade da animação (%)":"Animation speed (%)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.AnimationSpeed)),pt?"Multiplica o tempo da água visual. 100 mantém o valor capturado. Não acelera a simulação da cidade.":"Animation speed (%). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamEnabled)),pt?"Ajustar espuma":"Override foam"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamEnabled)),pt?"Ativa a espuma nativa. Desativar devolve ao jogo o controle dos parâmetros de espuma.":"Override foam. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.Amount)),pt?"Quantidade de espuma":"Foam amount"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.Amount)),pt?"Altera a quantidade/tamanho das manchas. Não é um controle de opacidade.":"Foam amount. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.WindIndependence)),pt?"Independência do vento":"Wind independence"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.WindIndependence)),pt?"0 preserva a curva nativa; 100 remove sua atenuação. Não cria ondas nem altera o vento do mapa.":"Wind independence. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamLookEnabled)),pt?"Ajustar aparência da espuma":"Override foam appearance"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamLookEnabled)),pt?"Controla a resposta ao brilho e o detalhe da textura existente.":"Override foam appearance. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamRoughness)),pt?"Rugosidade da espuma":"Foam roughness"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamRoughness)),pt?"Mais alto torna a resposta especular mais difusa. Internamente aplica 1 menos a rugosidade à suavidade nativa.":"Foam roughness. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamTiling)),pt?"Repetição da textura (por metro)":"Texture tiling (per meter)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamTiling)),pt?"Mais alto repete a textura mais vezes e reduz o tamanho do detalhe. Não altera diretamente a cobertura.":"Texture tiling (per meter). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamPersistenceEnabled)),pt?"Ajustar persistência experimental":"Override experimental persistence"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamPersistenceEnabled)),pt?"Usa somente simulationFoamDrag quando disponível; a unidade nativa não é segundos.":"Override experimental persistence. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamPersistence)),pt?"Persistência (escala nativa)":"Persistence (native scale)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamPersistence)),pt?"0–100 mapeia o parâmetro para 0–1. Teste gradualmente; depende do suporte do shader e da simulação de espuma.":"Persistence (native scale). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.CausticsOverride)),pt?"Ajustar cáusticas":"Override caustics"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.CausticsOverride)),pt?"Padrões de luz projetados sob a água. O ganho visual depende da profundidade e do material.":"Override caustics. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.CausticsOn)),pt?"Cáusticas ligadas":"Enable caustics"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.CausticsOn)),pt?"Permite desligar o efeito nativo enquanto este grupo está ativo.":"Enable caustics. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.CausticsIntensity)),pt?"Intensidade das cáusticas":"Caustics intensity"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.CausticsIntensity)),pt?"Comece com valores baixos para evitar faixas muito claras ou ciano junto às margens.":"Caustics intensity. Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.CausticsBlend)),pt?"Transição vertical (m)":"Vertical blend distance (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.CausticsBlend)),pt?"Distância de mistura vertical das cáusticas.":"Vertical blend distance (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.CausticsProjection)),pt?"Distância de projeção (m)":"Projection distance (m)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.CausticsProjection)),pt?"Valores maiores podem produzir padrões mais nítidos e também artefatos.":"Projection distance (m). Release the slider to apply. Unavailable controls are disabled. See LEIA-ME.md for ranges and behavior."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.BalancedPreset)),pt?"Preset inicial equilibrado":"Balanced starting preset"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.BalancedPreset)),pt?"Configuração experimental para comparação: ajusta água, reflexos, ondas e aparência da espuma. Cáusticas e persistência ficam sob controle do jogo.":"Balanced starting preset. See LEIA-ME.md for details."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.FoamOnlyPreset)),pt?"Somente espuma (versão anterior)":"Foam only (previous version)"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.FoamOnlyPreset)),pt?"Restaura os valores iniciais: espuma 40, independência 100, outros grupos desativados.":"Foam only (previous version). See LEIA-ME.md for details."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.NativePreset)),pt?"Usar valores do jogo":"Use game values"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.NativePreset)),pt?"Desativa todos os grupos sem apagar os valores dos sliders. O mod permanece disponível.":"Use game values. See LEIA-ME.md for details."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.WriteDiagnostics)),pt?"Registrar diagnóstico no log":"Write diagnostics to log"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.WriteDiagnostics)),pt?"Registra campos detectados, material e valores atuais em Logs/SimpleWaterFoam.log.":"Write diagnostics to log. See LEIA-ME.md for details."},
{settings.GetOptionLabelLocaleID(nameof(FoamSettings.RuntimeStatus)),pt?"Controles detectados":"Detected controls"},
{settings.GetOptionDescLocaleID(nameof(FoamSettings.RuntimeStatus)),pt?"Campo detectado não garante efeito visual no material. Grupos novos começam desativados. Reabra as opções se a disponibilidade mudar.":"An exposed field does not guarantee a visible material effect. New groups start disabled. Reopen options if availability changes."}
};
        }
        public void Unload() { }
    }
}
