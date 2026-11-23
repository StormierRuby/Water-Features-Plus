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
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimpleWaterFoam
{
    // v1.1: native foam controller with persistent in-game options.
    public sealed class Mod : IMod
    {
        internal static readonly ILog Logger = LogManager.GetLogger("SimpleWaterFoam").SetShowsErrorsInUI(false);
        private FoamSettings settings;
        private GameObject host;
        private FoamController controller;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logger.Info("SimpleWaterFoam 1.1.0: OnLoad (sliders build)");
            try
            {
                settings = new FoamSettings(this);
                var locales = GameManager.instance.localizationManager;
                locales.AddSource("en-US", new FoamLocale(settings, false));
                locales.AddSource("pt-BR", new FoamLocale(settings, true));
                AssetDatabase.global.LoadSettings(nameof(FoamSettings), settings, new FoamSettings(this));
                settings.RegisterInOptionsUI();
                host = new GameObject("SimpleWaterFoam.Runtime");
                Object.DontDestroyOnLoad(host);
                controller = host.AddComponent<FoamController>();
                controller.Initialize(settings);
            }
            catch (Exception error)
            {
                Logger.Error("Initialization failed: " + error);
                OnDispose();
                throw;
            }
        }

        public void OnDispose()
        {
            if (controller != null) controller.Restore();
            if (host != null) Object.Destroy(host);
            controller = null;
            host = null;
            if (settings != null) settings.UnregisterInOptionsUI();
            settings = null;
        }
    }

    public sealed class FoamController : MonoBehaviour
    {
        private FoamSettings settings;
        private int lastAmount = -1;
        private int lastWind = -1;
        private bool wasEnabled;
        private float FoamAmount { get { return settings.Amount / 100f; } }
        private const float ScanInterval = 5f;
        private const string SurfaceTypeName = "UnityEngine.Rendering.HighDefinition.WaterSurface";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private Type surfaceType;
        private FieldInfo registry;
        private FieldInfo foam;
        private FieldInfo amount;
        private FieldInfo simulationFoam;
        private FieldInfo windCurve;
        private readonly Dictionary<Object, List<SavedField>> saved = new Dictionary<Object, List<SavedField>>();
        private readonly AnimationCurve fullWind = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        private readonly List<Object> removed = new List<Object>();
        private float nextScan;
        private bool running;
        private bool reportedEmpty;

        private sealed class SavedField
        {
            public FieldInfo Field;
            public object Original;
            public object Applied;
            public object LastWritten;
            public bool HasWritten;
        }

        private static void Log(string message)
        {
            Mod.Logger.Info("[SimpleWaterFoam] " + message);
        }

        public void Initialize(FoamSettings options)
        {
            settings = options;
            Log("Native foam controller 1.1.0. Game=" + Application.version +
                "; Unity=" + Application.unityVersion);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                surfaceType = assembly.GetType(SurfaceTypeName, false);
                if (surfaceType != null) break;
            }
            if (surfaceType == null || !typeof(Behaviour).IsAssignableFrom(surfaceType))
            {
                Log("INCOMPATIBLE: HDRP WaterSurface component not found. No changes applied.");
                enabled = false;
                return;
            }
            Log("Water assembly=" + surfaceType.Assembly.GetName().FullName);
            foreach (FieldInfo field in surfaceType.GetFields(Fields))
            {
                if (field.Name.IndexOf("foam", StringComparison.OrdinalIgnoreCase) >= 0)
                    Log("API: " + field.FieldType.Name + " " + field.Name);
            }
            foam = FindField(typeof(bool), "foam");
            amount = FindField(typeof(float), "simulationFoamAmount");
            simulationFoam = FindField(typeof(bool), "simulationFoam");
            windCurve = FindField(typeof(AnimationCurve), "windFoamCurve", "simulationFoamWindCurve");
            registry = surfaceType.GetField("instances", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (foam == null || amount == null)
            {
                Log("INCOMPATIBLE: required foam/amount fields missing. No changes applied.");
                enabled = false;
                return;
            }
            if (windCurve == null)
                Log("Wind dimmer field unavailable; preserving native wind behavior.");
            running = true;
            nextScan = 0f;
        }

        private FieldInfo FindField(Type type, params string[] names)
        {
            foreach (string name in names)
            {
                FieldInfo field = surfaceType.GetField(name, Fields);
                if (field != null && field.FieldType == type && !field.IsInitOnly) return field;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (!running) return;
            try
            {
                if (!settings.EnableEffect)
                {
                    if (wasEnabled)
                    {
                        RestoreSurfaces();
                        wasEnabled = false;
                        Log("Effect disabled; original surface settings restored.");
                    }
                    return;
                }
                if (!wasEnabled)
                {
                    wasEnabled = true;
                    nextScan = 0f;
                }
                bool changed = lastAmount != settings.Amount || lastWind != settings.WindIndependence;
                if (changed)
                {
                    lastAmount = settings.Amount;
                    lastWind = settings.WindIndependence;
                    Log("OPTIONS: amount=" + lastAmount + "; wind independence=" + lastWind);
                }
                if (Time.realtimeSinceStartup >= nextScan)
                {
                    nextScan = Time.realtimeSinceStartup + ScanInterval;
                    Discover();
                }
                removed.Clear();
                foreach (KeyValuePair<Object, List<SavedField>> entry in saved)
                {
                    Behaviour surface = entry.Key as Behaviour;
                    if (surface == null) { removed.Add(entry.Key); continue; }
                    if (!surface.isActiveAndEnabled || !surface.gameObject.scene.isLoaded)
                    {
                        RestoreOne(surface, entry.Value);
                        removed.Add(entry.Key);
                        continue;
                    }
                    if (changed) UpdateDesiredValues(entry.Value);
                    // Reapply cached scalar fields: some game versions may reset them.
                    // No resource searches, texture creation or wave simulation here.
                    foreach (SavedField field in entry.Value)
                    {
                        field.Field.SetValue(surface, field.Applied);
                        field.LastWritten = field.Applied;
                        field.HasWritten = true;
                    }
                }
                foreach (Object item in removed) saved.Remove(item);
            }
            catch (Exception error)
            {
                Log("ERROR: " + error + "; stopping and restoring owned changes.");
                Restore();
                enabled = false;
            }
        }

        private void Discover()
        {
            // Prefer the native active-surface registry. Fall back only if unavailable.
            IEnumerable surfaces = registry == null ? null : registry.GetValue(null) as IEnumerable;
            if (surfaces == null) surfaces = Resources.FindObjectsOfTypeAll(surfaceType);
            foreach (object item in surfaces)
            {
                Behaviour surface = item as Behaviour;
                if (surface == null || !surface.isActiveAndEnabled ||
                    !surface.gameObject.scene.IsValid() || !surface.gameObject.scene.isLoaded ||
                    saved.ContainsKey(surface)) continue;

                FieldInfo kind = surfaceType.GetField("surfaceType", Fields);
                string kindName = kind == null ? "unknown" : Convert.ToString(kind.GetValue(surface));
                // HDRP pools do not provide wave-simulation foam.
                if (string.Equals(kindName, "Pool", StringComparison.OrdinalIgnoreCase)) continue;

                var values = new List<SavedField>();
                saved.Add(surface, values);
                Capture(values, surface, foam, true);
                Capture(values, surface, amount, Mathf.Clamp01(FoamAmount));
                Capture(values, surface, simulationFoam, true);
                // Keep the original curve for blending and restoring; never edit it in-place.
                Capture(values, surface, windCurve, null);
                UpdateDesiredValues(values);
                Log("BOUND: " + surface.name + "; type=" + kindName + "; amount=" + FoamAmount +
                    ". Fields found; visible foam still requires compatible shader and pipeline.");
                ReportMaterial(surface);
            }
            if (saved.Count == 0 && !reportedEmpty)
            {
                reportedEmpty = true;
                Log("WAITING: no eligible active water surface. Load a city; scanning continues every 5 seconds.");
            }
        }

        private void UpdateDesiredValues(List<SavedField> values)
        {
            foreach (SavedField value in values)
            {
                if (value.Field == amount) value.Applied = Mathf.Clamp01(FoamAmount);
                else if (value.Field == windCurve)
                    value.Applied = BlendWindCurve(value.Original as AnimationCurve, settings.WindIndependence / 100f);
            }
        }

        private AnimationCurve BlendWindCurve(AnimationCurve original, float independence)
        {
            float t = Mathf.Clamp01(independence);
            if (t <= 0f) return original;
            if (t >= 1f) return fullWind;
            // A null native curve has unknown semantics: preserve it except at full override.
            if (original == null || original.length == 0) return original;
            Keyframe[] keys = original.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value = Mathf.Lerp(key.value, 1f, t);
                // Preserve infinite tangents, which encode constant/stepped segments.
                if (!float.IsInfinity(key.inTangent)) key.inTangent *= 1f - t;
                if (!float.IsInfinity(key.outTangent)) key.outTangent *= 1f - t;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = original.preWrapMode,
                postWrapMode = original.postWrapMode
            };
        }

        private static void Capture(List<SavedField> values, Object surface, FieldInfo field, object applied)
        {
            if (field == null) return;
            object original = field.GetValue(surface);
            values.Add(new SavedField { Field = field, Original = original, Applied = applied });
            Log("  " + field.Name + ": " + Convert.ToString(original) + " -> " + Convert.ToString(applied));
        }

        private void ReportMaterial(Object surface)
        {
            FieldInfo field = surfaceType.GetField("customMaterial", Fields);
            Material material = field == null ? null : field.GetValue(surface) as Material;
            if (material == null)
            {
                Log("Material: no custom material exposed on WaterSurface.");
                return;
            }
            Log("Material=" + material.name + "; shader=" + (material.shader == null ? "null" : material.shader.name));
            // Only inspect exposed properties; this does not prove shader feature support.
            Shader shader = material.shader;
            if (shader == null) return;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string name = shader.GetPropertyName(i);
                if (name.IndexOf("foam", StringComparison.OrdinalIgnoreCase) >= 0)
                    Log("  Shader property: " + name);
            }
        }

        private static void RestoreOne(Object surface, List<SavedField> values)
        {
            if (surface == null) return;
            foreach (SavedField value in values)
            {
                try
                {
                    // Avoid undoing a newer value written by another system after us.
                    if (value.HasWritten && Equals(value.Field.GetValue(surface), value.LastWritten))
                        value.Field.SetValue(surface, value.Original);
                }
                catch (Exception error) { Log("Restore warning: " + error.Message); }
            }
        }

        public void Restore()
        {
            running = false;
            RestoreSurfaces();
        }

        private void RestoreSurfaces()
        {
            foreach (KeyValuePair<Object, List<SavedField>> entry in saved)
                RestoreOne(entry.Key, entry.Value);
            saved.Clear();
        }

        private void OnDestroy() { Restore(); }
    }
}

namespace SimpleWaterFoam
{
    [FileLocation("ModsSettings/SimpleWaterFoam/FoamSettings")]
    [SettingsUIGroupOrder(Controls, Actions)]
    [SettingsUIShowGroupName(Controls, Actions)]
    public sealed class FoamSettings : ModSetting
    {
        public const string Tab = "Foam";
        public const string Controls = "Controls";
        public const string Actions = "Actions";
        private int amount;
        private int windIndependence;

        public FoamSettings(IMod mod) : base(mod) { SetDefaults(); }

        [SettingsUISection(Tab, Controls)]
        public bool EnableEffect { get; set; }

        [SettingsUISection(Tab, Controls)]
        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1,
            unit = "integer", updateOnDragEnd = true)]
        public int Amount
        {
            get { return amount; }
            set { amount = Math.Max(0, Math.Min(100, value)); }
        }

        [SettingsUISection(Tab, Controls)]
        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1,
            unit = "integer", updateOnDragEnd = true)]
        public int WindIndependence
        {
            get { return windIndependence; }
            set { windIndependence = Math.Max(0, Math.Min(100, value)); }
        }

        [SettingsUISection(Tab, Actions)]
        [SettingsUIButton]
        public bool ResetFoam
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        public override void SetDefaults()
        {
            EnableEffect = true;
            Amount = 40;
            WindIndependence = 100;
        }
    }

    public sealed class FoamLocale : IDictionarySource
    {
        private readonly FoamSettings settings;
        private readonly bool portuguese;
        public FoamLocale(FoamSettings options, bool usePortuguese)
        {
            settings = options;
            portuguese = usePortuguese;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { settings.GetSettingsLocaleID(), "Simple Water Foam" },
                { settings.GetOptionTabLocaleID(FoamSettings.Tab), portuguese ? "Espuma" : "Foam" },
                { settings.GetOptionGroupLocaleID(FoamSettings.Controls), portuguese ? "Aparência" : "Appearance" },
                { settings.GetOptionGroupLocaleID(FoamSettings.Actions), portuguese ? "Restaurar" : "Reset" },
                { settings.GetOptionLabelLocaleID(nameof(FoamSettings.EnableEffect)),
                    portuguese ? "Ativar ajustes de espuma" : "Enable foam adjustments" },
                { settings.GetOptionDescLocaleID(nameof(FoamSettings.EnableEffect)), portuguese
                    ? "Ativa os ajustes deste mod. Ao desativar, restaura os parâmetros anteriores da água; isso não garante ausência de espuma nativa."
                    : "Enables this mod's adjustments. Turning it off restores the previous water parameters; native foam may remain." },
                { settings.GetOptionLabelLocaleID(nameof(FoamSettings.Amount)),
                    portuguese ? "Quantidade de espuma (0–100)" : "Foam amount (0–100)" },
                { settings.GetOptionDescLocaleID(nameof(FoamSettings.Amount)), portuguese
                    ? "Controla a quantidade/tamanho das manchas. 40 reproduz o valor inicial do mod. Não é um controle de opacidade. Solte o slider para aplicar; a água pode levar alguns segundos para responder."
                    : "Controls foam amount/patch size. 40 matches the initial mod. This is not opacity. Release the slider to apply; the water may take a few seconds to respond." },
                { settings.GetOptionLabelLocaleID(nameof(FoamSettings.WindIndependence)),
                    portuguese ? "Independência do vento (0–100)" : "Wind independence (0–100)" },
                { settings.GetOptionDescLocaleID(nameof(FoamSettings.WindIndependence)), portuguese
                    ? "0 mantém a curva original de atenuação pelo vento. 100 remove essa atenuação, como na primeira versão. Valores intermediários misturam as duas curvas. Não altera o vento nem cria ondas."
                    : "0 keeps the original wind attenuation curve. 100 removes that attenuation, matching the first version. Intermediate values blend the curves. Does not change wind or create waves." },
                { settings.GetOptionLabelLocaleID(nameof(FoamSettings.ResetFoam)),
                    portuguese ? "Restaurar valores iniciais" : "Reset to initial values" },
                { settings.GetOptionDescLocaleID(nameof(FoamSettings.ResetFoam)), portuguese
                    ? "Ativa o mod, ajusta a quantidade para 40 e a independência do vento para 100, e salva."
                    : "Enables the mod, sets amount to 40 and wind independence to 100, and saves." }
            };
        }

        public void Unload() { }
    }
}
