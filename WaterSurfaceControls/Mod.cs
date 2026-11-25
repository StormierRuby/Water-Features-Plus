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
        internal const string Version = "1.4.1";
        internal static readonly ILog Logger = LogManager.GetLogger("WaterSurfaceControls").SetShowsErrorsInUI(false);
        internal static WaterController Controller;
        private FoamSettings settings;
        private GameObject host;
        public void OnLoad(UpdateSystem updateSystem)
        {
            try
            {
                settings = new FoamSettings(this);
                AssetDatabase.global.LoadSettings(nameof(FoamSettings), settings, new FoamSettings(this));
                GameManager.instance.localizationManager.AddSource("en-US", new FoamLocale(settings, false));
                GameManager.instance.localizationManager.AddSource("pt-BR", new FoamLocale(settings, true));
                host = new GameObject("WaterSurfaceControls.Runtime");
                Object.DontDestroyOnLoad(host);
                Controller = host.AddComponent<WaterController>();
                Controller.Initialize(settings);
                settings.RegisterInOptionsUI();
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
            host = null;
            if (settings != null)
                settings.UnregisterInOptionsUI();
            settings = null;
        }
    }
}
