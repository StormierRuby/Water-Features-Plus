using System;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WaterSurfaceControls
{
    public sealed class Mod : IMod
    {
        internal const string Version = "1.5.1";
        internal static readonly ILog Logger = LogManager.GetLogger("WaterSurfaceControls").SetShowsErrorsInUI(false);
        internal static WaterController Controller;
        internal static PresetFiles Presets { get; private set; }
        private WaterSurfaceSettings settings;
        private GameObject host;
        public void OnLoad(UpdateSystem updateSystem)
        {
            try
            {
                settings = new WaterSurfaceSettings(this);
                AssetDatabase.global.LoadSettings(WaterSurfaceSettings.SettingsAssetName, settings, new WaterSurfaceSettings(this));
                Presets = new PresetFiles(System.IO.Path.Combine(Application.persistentDataPath, "ModsSettings", "WaterSurfaceControls", "Presets"));
                Presets.Refresh();
                GameManager.instance.localizationManager.AddSource("en-US", new WaterSurfaceLocale(settings, false));
                GameManager.instance.localizationManager.AddSource("pt-BR", new WaterSurfaceLocale(settings, true));
                host = new GameObject("WaterSurfaceControls.Runtime");
                Object.DontDestroyOnLoad(host);
                Controller = host.AddComponent<WaterController>();
                Controller.Initialize(settings);
                settings.RegisterInOptionsUI();
                settings.MigrateLegacyPresets();
                Logger.Info("WaterSurfaceControls " + Version + " loaded");
            }
            catch (Exception e) { Logger.Error("Initialization failed: " + e); OnDispose(); throw; }
        }
        public void OnDispose()
        {
            if (Controller != null)
                Controller.Restore();
            if (host != null)
                Object.Destroy(host);
            Controller = null;
            Presets = null;
            host = null;
            if (settings != null)
                settings.UnregisterInOptionsUI();
            settings = null;
        }
    }
}
