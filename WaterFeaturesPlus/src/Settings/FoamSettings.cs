using System;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;

namespace WaterFeaturesPlus
{
    public enum CausticsSource
    {
        Game = 0,
        Long = 1,
        Secondary = 2,
        Ripples = 3
    }

    // Keep the original settings path for upgrades from Simple Water Foam.
    [FileLocation("ModsSettings/SimpleWaterFoam/FoamSettings")]
    [SettingsUITabOrder("General", "Water", "Reflections", "Waves", "Foam", "Caustics")]
    [SettingsUIGroupOrder("Global", "Presets", "Diagnostics", "Optics", "Transmission", "ScatterColor", "Light", "Surface", "Motion", "Swell", "RippleFade", "Coverage", "Texture", "CausticControls")]
    [SettingsUIShowGroupName("Global", "Presets", "Diagnostics", "Optics", "Transmission", "ScatterColor", "Light", "Surface", "Motion", "Swell", "RippleFade", "Coverage", "Texture", "CausticControls")]
    public sealed class FoamSettings : ModSetting
    {
        internal int Revision
        {
            get; private set;
        }
        public FoamSettings(IMod mod) : base(mod) { SetDefaults(); }
        private static int Limit(int v, int lo, int hi)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }
        private static float Limit(float v, float lo, float hi)
        {
            if (float.IsNaN(v) || float.IsInfinity(v))
                return lo;
            return Math.Max(lo, Math.Min(hi, v));
        }
        private void Set<T>(ref T field, T value) where T : IEquatable<T>
        {
            if (field.Equals(value))
                return;
            field = value;
            Revision++;
        }
        private static bool Supports(string key) => Mod.Controller?.Supports(key) == true;

        private bool _EnableEffect;
        [SettingsUISection("General", "Global")]
        public bool EnableEffect
        {
            get => _EnableEffect;
            set => Set(ref _EnableEffect, value);
        }

        private bool _OpticsEnabled;
        [SettingsUISection("Water", "Optics")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledOpticsEnabled))]
        public bool OpticsEnabled
        {
            get => _OpticsEnabled;
            set => Set(ref _OpticsEnabled, value);
        }
        public bool DisabledOpticsEnabled()
        {
            return !EnableEffect;
        }

        private float _Absorption;
        [SettingsUISection("Water", "Optics")]
        [SettingsUISlider(min = 0.5f, max = 100.0f, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAbsorption))]
        public float Absorption
        {
            get => _Absorption;
            set => Set(ref _Absorption, Limit(value, 0.5f, 100.0f));
        }
        public bool DisabledAbsorption()
        {
            return !(EnableEffect && OpticsEnabled) || !Supports("Absorption");
        }

        private float _RefractionDistance;
        [SettingsUISection("Water", "Optics")]
        [SettingsUISlider(min = 0.1f, max = 5.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionDistance))]
        public float RefractionDistance
        {
            get => _RefractionDistance;
            set => Set(ref _RefractionDistance, Limit(value, 0.1f, 5.0f));
        }
        public bool DisabledRefractionDistance()
        {
            return !(EnableEffect && OpticsEnabled) || !Supports("RefractionDistance");
        }

        private bool _RefractionColorEnabled;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionColorEnabled))]
        public bool RefractionColorEnabled
        {
            get => _RefractionColorEnabled;
            set => Set(ref _RefractionColorEnabled, value);
        }
        public bool DisabledRefractionColorEnabled()
        {
            return !(EnableEffect) || !Supports("RefractionColor");
        }

        private int _RefractionR;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionR))]
        public int RefractionR
        {
            get => _RefractionR;
            set => Set(ref _RefractionR, Limit(value, 0, 100));
        }
        public bool DisabledRefractionR()
        {
            return !(EnableEffect && RefractionColorEnabled) || !Supports("RefractionColor");
        }

        private int _RefractionG;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionG))]
        public int RefractionG
        {
            get => _RefractionG;
            set => Set(ref _RefractionG, Limit(value, 0, 100));
        }
        public bool DisabledRefractionG()
        {
            return !(EnableEffect && RefractionColorEnabled) || !Supports("RefractionColor");
        }

        private int _RefractionB;
        [SettingsUISection("Water", "Transmission")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRefractionB))]
        public int RefractionB
        {
            get => _RefractionB;
            set => Set(ref _RefractionB, Limit(value, 0, 100));
        }
        public bool DisabledRefractionB()
        {
            return !(EnableEffect && RefractionColorEnabled) || !Supports("RefractionColor");
        }

        private bool _ScatteringColorEnabled;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringColorEnabled))]
        public bool ScatteringColorEnabled
        {
            get => _ScatteringColorEnabled;
            set => Set(ref _ScatteringColorEnabled, value);
        }
        public bool DisabledScatteringColorEnabled()
        {
            return !(EnableEffect) || !Supports("ScatteringColor");
        }

        private int _ScatteringR;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringR))]
        public int ScatteringR
        {
            get => _ScatteringR;
            set => Set(ref _ScatteringR, Limit(value, 0, 100));
        }
        public bool DisabledScatteringR()
        {
            return !(EnableEffect && ScatteringColorEnabled) || !Supports("ScatteringColor");
        }

        private int _ScatteringG;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringG))]
        public int ScatteringG
        {
            get => _ScatteringG;
            set => Set(ref _ScatteringG, Limit(value, 0, 100));
        }
        public bool DisabledScatteringG()
        {
            return !(EnableEffect && ScatteringColorEnabled) || !Supports("ScatteringColor");
        }

        private int _ScatteringB;
        [SettingsUISection("Water", "ScatterColor")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledScatteringB))]
        public int ScatteringB
        {
            get => _ScatteringB;
            set => Set(ref _ScatteringB, Limit(value, 0, 100));
        }
        public bool DisabledScatteringB()
        {
            return !(EnableEffect && ScatteringColorEnabled) || !Supports("ScatteringColor");
        }

        private bool _LightingEnabled;
        [SettingsUISection("Water", "Light")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledLightingEnabled))]
        public bool LightingEnabled
        {
            get => _LightingEnabled;
            set => Set(ref _LightingEnabled, value);
        }
        public bool DisabledLightingEnabled()
        {
            return !EnableEffect;
        }

        private int _Ambient;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAmbient))]
        public int Ambient
        {
            get => _Ambient;
            set => Set(ref _Ambient, Limit(value, 0, 100));
        }
        public bool DisabledAmbient()
        {
            return !(EnableEffect && LightingEnabled) || !Supports("Ambient");
        }

        private int _Height;
        [SettingsUIHidden]
        public int Height
        {
            get => _Height;
            set => Set(ref _Height, Limit(value, 0, 100));
        }
        public bool DisabledHeight()
        {
            return !(EnableEffect && LightingEnabled) || !Supports("Height");
        }

        private int _Displacement;
        [SettingsUIHidden]
        public int Displacement
        {
            get => _Displacement;
            set => Set(ref _Displacement, Limit(value, 0, 100));
        }
        public bool DisabledDisplacement()
        {
            return !(EnableEffect && LightingEnabled) || !Supports("Displacement");
        }

        private int _Tip;
        [SettingsUIHidden]
        public int Tip
        {
            get => _Tip;
            set => Set(ref _Tip, Limit(value, 0, 100));
        }
        public bool DisabledTip()
        {
            return !(EnableEffect && LightingEnabled) || !Supports("Tip");
        }

        private int _Body;
        [SettingsUISection("Water", "Light")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledBody))]
        public int Body
        {
            get => _Body;
            set => Set(ref _Body, Limit(value, 0, 100));
        }
        public bool DisabledBody()
        {
            return !(EnableEffect && LightingEnabled) || !Supports("Body");
        }

        private bool _ReflectionEnabled;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledReflectionEnabled))]
        public bool ReflectionEnabled
        {
            get => _ReflectionEnabled;
            set => Set(ref _ReflectionEnabled, value);
        }
        public bool DisabledReflectionEnabled()
        {
            return !EnableEffect;
        }

        private int _NearSmoothness;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledNearSmoothness))]
        public int NearSmoothness
        {
            get => _NearSmoothness;
            set => Set(ref _NearSmoothness, Limit(value, 0, 100));
        }
        public bool DisabledNearSmoothness()
        {
            return !(EnableEffect && ReflectionEnabled) || !Supports("NearSmoothness");
        }

        private int _FarSmoothness;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFarSmoothness))]
        public int FarSmoothness
        {
            get => _FarSmoothness;
            set => Set(ref _FarSmoothness, Limit(value, 0, 100));
        }
        public bool DisabledFarSmoothness()
        {
            return !(EnableEffect && ReflectionEnabled) || !Supports("FarSmoothness");
        }

        private float _SmoothFadeStart;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 0.0f, max = 3000.0f, step = 25.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSmoothFadeStart))]
        public float SmoothFadeStart
        {
            get => _SmoothFadeStart;
            set => Set(ref _SmoothFadeStart, Limit(value, 0.0f, 3000.0f));
        }
        public bool DisabledSmoothFadeStart()
        {
            return !(EnableEffect && ReflectionEnabled) || !Supports("SmoothFadeStart");
        }

        private float _SmoothFadeDistance;
        [SettingsUISection("Reflections", "Surface")]
        [SettingsUISlider(min = 25.0f, max = 4000.0f, step = 25.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSmoothFadeDistance))]
        public float SmoothFadeDistance
        {
            get => _SmoothFadeDistance;
            set => Set(ref _SmoothFadeDistance, Limit(value, 25.0f, 4000.0f));
        }
        public bool DisabledSmoothFadeDistance()
        {
            return !(EnableEffect && ReflectionEnabled) || !Supports("SmoothFadeDistance");
        }

        private bool _WavesEnabled;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledWavesEnabled))]
        public bool WavesEnabled
        {
            get => _WavesEnabled;
            set => Set(ref _WavesEnabled, value);
        }
        public bool DisabledWavesEnabled()
        {
            return !EnableEffect;
        }

        private int _LargeWaves;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledLargeWaves))]
        public int LargeWaves
        {
            get => _LargeWaves;
            set => Set(ref _LargeWaves, Limit(value, 0, 150));
        }
        public bool DisabledLargeWaves()
        {
            return !(EnableEffect && WavesEnabled) || !Supports("LargeWaves") || SwellEnabled;
        }

        private int _MediumWaves;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledMediumWaves))]
        public int MediumWaves
        {
            get => _MediumWaves;
            set => Set(ref _MediumWaves, Limit(value, 0, 150));
        }
        public bool DisabledMediumWaves()
        {
            return !(EnableEffect && WavesEnabled) || !Supports("MediumWaves") || SwellEnabled;
        }

        private bool _RippleOverrideEnabled;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRippleOverrideEnabled))]
        public bool RippleOverrideEnabled
        {
            get => _RippleOverrideEnabled;
            set => Set(ref _RippleOverrideEnabled, value);
        }
        public bool DisabledRippleOverrideEnabled()
        {
            return !(EnableEffect && WavesEnabled) || !Supports("Ripples");
        }

        private int _Ripples;
        [SettingsUISection("Waves", "Motion")]
        [SettingsUISlider(min = 0.0f, max = 150.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRipples))]
        public int Ripples
        {
            get => _Ripples;
            set => Set(ref _Ripples, Limit(value, 0, 150));
        }
        public bool DisabledRipples()
        {
            return !(EnableEffect && WavesEnabled && RippleOverrideEnabled) || !Supports("Ripples");
        }

        private int _AnimationSpeed;
        [SettingsUIHidden]
        public int AnimationSpeed
        {
            get => _AnimationSpeed;
            set => Set(ref _AnimationSpeed, Limit(value, 25, 200));
        }
        public bool DisabledAnimationSpeed()
        {
            return !(EnableEffect && WavesEnabled) || !Supports("AnimationSpeed");
        }

        private bool _SwellEnabled;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellEnabled))]
        public bool SwellEnabled
        {
            get => _SwellEnabled;
            set => Set(ref _SwellEnabled, value);
        }
        public bool DisabledSwellEnabled()
        {
            return !(EnableEffect) || !SwellEnabled && (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private float _SwellPatchSize;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 250.0f, max = 5000.0f, step = 250.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellPatchSize))]
        public float SwellPatchSize
        {
            get => _SwellPatchSize;
            set => Set(ref _SwellPatchSize, Limit(value, 250.0f, 5000.0f));
        }
        public bool DisabledSwellPatchSize()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("SwellPatchSize") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private float _SwellWind;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellWind))]
        public float SwellWind
        {
            get => _SwellWind;
            set => Set(ref _SwellWind, Limit(value, 0.0f, 100.0f));
        }
        public bool DisabledSwellWind()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("SwellWind") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private int _SwellAmplitude;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 0.0f, max = 200.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellAmplitude))]
        public int SwellAmplitude
        {
            get => _SwellAmplitude;
            set => Set(ref _SwellAmplitude, Limit(value, 0, 200));
        }
        public bool DisabledSwellAmplitude()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("LargeWaves") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private int _SwellMediumAmplitude;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 0.0f, max = 200.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellMediumAmplitude))]
        public int SwellMediumAmplitude
        {
            get => _SwellMediumAmplitude;
            set => Set(ref _SwellMediumAmplitude, Limit(value, 0, 200));
        }
        public bool DisabledSwellMediumAmplitude()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("MediumWaves") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private int _SwellChaos;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellChaos))]
        public int SwellChaos
        {
            get => _SwellChaos;
            set => Set(ref _SwellChaos, Limit(value, 0, 100));
        }
        public bool DisabledSwellChaos()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("SwellChaos") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private bool _DirectionOverride;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledDirectionOverride))]
        public bool DirectionOverride
        {
            get => _DirectionOverride;
            set => Set(ref _DirectionOverride, value);
        }
        public bool DisabledDirectionOverride()
        {
            return !(EnableEffect && SwellEnabled) || !Supports("SwellDirection") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private int _SwellDirection;
        [SettingsUISection("Waves", "Swell")]
        [SettingsUISlider(min = 0.0f, max = 360.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSwellDirection))]
        public int SwellDirection
        {
            get => _SwellDirection;
            set => Set(ref _SwellDirection, Limit(value, 0, 360));
        }
        public bool DisabledSwellDirection()
        {
            return !(EnableEffect && SwellEnabled && DirectionOverride) || !Supports("SwellDirection") || (Mod.Controller == null || !Mod.Controller.CanUseSwell);
        }

        private bool _RippleFadeEnabled;
        [SettingsUISection("Waves", "RippleFade")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRippleFadeEnabled))]
        public bool RippleFadeEnabled
        {
            get => _RippleFadeEnabled;
            set => Set(ref _RippleFadeEnabled, value);
        }
        public bool DisabledRippleFadeEnabled()
        {
            return !(EnableEffect) || !RippleFadeEnabled && (Mod.Controller == null || !Mod.Controller.CanUseRippleFade);
        }

        private int _RippleFadeStart;
        [SettingsUISection("Waves", "RippleFade")]
        [SettingsUISlider(min = 0.0f, max = 5000.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRippleFadeStart))]
        public int RippleFadeStart
        {
            get => _RippleFadeStart;
            set => Set(ref _RippleFadeStart, Limit(value, 0, 5000));
        }
        public bool DisabledRippleFadeStart()
        {
            return !(EnableEffect && RippleFadeEnabled) || !Supports("RippleFadeStart") || (Mod.Controller == null || !Mod.Controller.CanUseRippleFade);
        }

        private int _RippleFadeDistance;
        [SettingsUISection("Waves", "RippleFade")]
        [SettingsUISlider(min = 5.0f, max = 10000.0f, step = 5.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledRippleFadeDistance))]
        public int RippleFadeDistance
        {
            get => _RippleFadeDistance;
            set => Set(ref _RippleFadeDistance, Limit(value, 5, 10000));
        }
        public bool DisabledRippleFadeDistance()
        {
            return !(EnableEffect && RippleFadeEnabled) || !Supports("RippleFadeDistance") || (Mod.Controller == null || !Mod.Controller.CanUseRippleFade);
        }

        private bool _FoamEnabled;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamEnabled))]
        public bool FoamEnabled
        {
            get => _FoamEnabled;
            set => Set(ref _FoamEnabled, value);
        }
        public bool DisabledFoamEnabled()
        {
            return !EnableEffect;
        }

        private int _Amount;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledAmount))]
        public int Amount
        {
            get => _Amount;
            set => Set(ref _Amount, Limit(value, 0, 100));
        }
        public bool DisabledAmount()
        {
            return !(EnableEffect && FoamEnabled) || !Supports("Amount");
        }

        private int _WindIndependence;
        [SettingsUISection("Foam", "Coverage")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledWindIndependence))]
        public int WindIndependence
        {
            get => _WindIndependence;
            set => Set(ref _WindIndependence, Limit(value, 0, 100));
        }
        public bool DisabledWindIndependence()
        {
            return !(EnableEffect && FoamEnabled) || !Supports("Wind");
        }

        private bool _FoamLookEnabled;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamLookEnabled))]
        public bool FoamLookEnabled
        {
            get => _FoamLookEnabled;
            set => Set(ref _FoamLookEnabled, value);
        }
        public bool DisabledFoamLookEnabled()
        {
            return !(EnableEffect && FoamEnabled);
        }

        private int _FoamRoughness;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamRoughness))]
        public int FoamRoughness
        {
            get => _FoamRoughness;
            set => Set(ref _FoamRoughness, Limit(value, 0, 100));
        }
        public bool DisabledFoamRoughness()
        {
            return !(EnableEffect && FoamEnabled && FoamLookEnabled) || !Supports("FoamRoughness");
        }

        private float _FoamTiling;
        [SettingsUISection("Foam", "Texture")]
        [SettingsUISlider(min = 0.1f, max = 2.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledFoamTiling))]
        public float FoamTiling
        {
            get => _FoamTiling;
            set => Set(ref _FoamTiling, Limit(value, 0.1f, 2.0f));
        }
        public bool DisabledFoamTiling()
        {
            return !(EnableEffect && FoamEnabled && FoamLookEnabled) || !Supports("FoamTiling");
        }
        // Keep both properties for profiles saved by earlier versions. The controller only observes the native field.

        private bool _FoamPersistenceEnabled;
        [SettingsUIHidden]
        public bool FoamPersistenceEnabled
        {
            get => _FoamPersistenceEnabled;
            set => Set(ref _FoamPersistenceEnabled, value);
        }

        private int _FoamPersistence;
        [SettingsUIHidden]
        public int FoamPersistence
        {
            get => _FoamPersistence;
            set => Set(ref _FoamPersistence, Limit(value, 0, 100));
        }

        private bool _CausticsOverride;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsOverride))]
        public bool CausticsOverride
        {
            get => _CausticsOverride;
            set => Set(ref _CausticsOverride, value);
        }
        public bool DisabledCausticsOverride()
        {
            return !(EnableEffect) || !Supports("CausticsSwitch");
        }

        private bool _CausticsOn;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsOn))]
        public bool CausticsOn
        {
            get => _CausticsOn;
            set => Set(ref _CausticsOn, value);
        }
        public bool DisabledCausticsOn()
        {
            return !(EnableEffect && CausticsOverride) || !Supports("CausticsSwitch");
        }

        private int _CausticsIntensity;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.0f, max = 100.0f, step = 1.0f, scalarMultiplier = 1, unit = "integer", updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsIntensity))]
        public int CausticsIntensity
        {
            get => _CausticsIntensity;
            set => Set(ref _CausticsIntensity, Limit(value, 0, 100));
        }
        public bool DisabledCausticsIntensity()
        {
            return !(EnableEffect && CausticsOverride && CausticsOn) || !Supports("CausticsIntensity");
        }

        private float _CausticsBlend;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.1f, max = 10.0f, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsBlend))]
        public float CausticsBlend
        {
            get => _CausticsBlend;
            set => Set(ref _CausticsBlend, Limit(value, 0.1f, 10.0f));
        }
        public bool DisabledCausticsBlend()
        {
            return !(EnableEffect && CausticsOverride && CausticsOn) || !Supports("CausticsBlend");
        }

        private float _CausticsProjection;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUISlider(min = 0.5f, max = 15.0f, step = 0.5f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction, updateOnDragEnd = true)]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledCausticsProjection))]
        public float CausticsProjection
        {
            get => _CausticsProjection;
            set => Set(ref _CausticsProjection, Limit(value, 0.5f, 15.0f));
        }
        public bool DisabledCausticsProjection()
        {
            return !(EnableEffect && CausticsOverride && CausticsOn) || !Supports("CausticsProjection");
        }
        private CausticsSource _SourceBand;
        [SettingsUISection("Caustics", "CausticControls")]
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledSourceBand))]
        public CausticsSource SourceBand
        {
            get
            {
                return _SourceBand;
            }
            set
            {
                var v = Enum.IsDefined(typeof(CausticsSource), value) ? value : CausticsSource.Game;
                if (_SourceBand != v)
                {
                    _SourceBand = v;
                    Revision++;
                }
            }
        }
        public bool DisabledSourceBand()
        {
            return !(EnableEffect && CausticsOverride && CausticsOn) || !Supports("SourceBand") || (Mod.Controller == null || Mod.Controller.OceanCount == 0);
        }
        [SettingsUISection("General", "Diagnostics")]
        public string RuntimeStatus
        {
            get
            {
                return Mod.Controller == null ? "Not initialized" : Mod.Controller.Status;
            }
        }
        [SettingsUISection("Waves", "Swell")]
        public string SwellStatus
        {
            get
            {
                return Mod.Controller == null ? "Load a city with water" : Mod.Controller.SwellStatus;
            }
        }
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledUseNativeAmplitudes))]
        [SettingsUISection("Waves", "Motion")]
        [SettingsUIButton]
        public bool UseNativeAmplitudes
        {
            set
            {
                if (DisabledUseNativeAmplitudes())
                    return;
                EnableEffect = true;
                SwellEnabled = false;
                WavesEnabled = true;
                ApplyAndSave();
            }
        }
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledOceanSwellPreset))]
        [SettingsUISection("Waves", "Swell")]
        [SettingsUIButton]
        public bool ResumeSavedSwell
        {
            set
            {
                if (DisabledOceanSwellPreset())
                    return;
                EnableEffect = true;
                SwellEnabled = true;
                ApplyAndSave();
            }
        }
        [SettingsUIDisableByCondition(typeof(FoamSettings), nameof(DisabledOceanSwellPreset))]
        [SettingsUISection("Waves", "Swell")]
        [SettingsUIButton]
        public bool OceanSwellPreset
        {
            set
            {
                if (Mod.Controller == null || !Mod.Controller.CanUseSwell)
                    return;
                EnableEffect = true;
                SwellPatchSize = 1500f;
                SwellWind = 45f;
                SwellAmplitude = 85;
                SwellMediumAmplitude = 40;
                SwellChaos = 40;
                SwellEnabled = true;
                ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool BalancedPreset
        {
            set
            {
                SetDefaults();
                EnableEffect = true;
                OpticsEnabled = true;
                RefractionColorEnabled = true;
                ScatteringColorEnabled = true;
                ReflectionEnabled = true;
                WavesEnabled = true;
                FoamLookEnabled = true;
                Amount = 30;
                WindIndependence = 30;
                LargeWaves = 90;
                MediumWaves = 90;
                ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool FoamOnlyPreset
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Presets")]
        [SettingsUIButton]
        public bool NativePreset
        {
            set
            {
                EnableEffect = true;
                FoamEnabled = OpticsEnabled = RefractionColorEnabled = ScatteringColorEnabled = LightingEnabled = ReflectionEnabled = WavesEnabled = SwellEnabled = RippleFadeEnabled = RippleOverrideEnabled = FoamLookEnabled = FoamPersistenceEnabled = CausticsOverride = false;
                ApplyAndSave();
            }
        }
        [SettingsUISection("General", "Diagnostics")]
        [SettingsUIButton]
        public bool WriteDiagnostics
        {
            set
            {
                if (Mod.Controller != null)
                    Mod.Controller.Dump();
            }
        }
        public bool DisabledUseNativeAmplitudes()
        {
            return Mod.Controller == null || (!Mod.Controller.Supports("LargeWaves") && !Mod.Controller.Supports("MediumWaves"));
        }
        public bool DisabledOceanSwellPreset()
        {
            return Mod.Controller == null || !Mod.Controller.CanUseSwell;
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
            RippleOverrideEnabled = false;
            Ripples = 100;
            AnimationSpeed = 100;
            SwellEnabled = false;
            SwellPatchSize = 1500.0f;
            SwellWind = 45.0f;
            SwellAmplitude = 85;
            SwellMediumAmplitude = 40;
            SwellChaos = 40;
            DirectionOverride = false;
            SwellDirection = 0;
            RippleFadeEnabled = false;
            RippleFadeStart = 50;
            RippleFadeDistance = 200;
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
            SourceBand = CausticsSource.Game;
        }
    }
}
