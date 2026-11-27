using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace WaterSurfaceControls
{
    public enum PresetSlot { One = 1, Two = 2, Three = 3 }

    // Only these visual options belong to a personal preset. Keep the master switch
    // and the preset slots themselves independent of any saved look.
    internal sealed class WaterPreset
    {
        private const string FileHeader = "[WaterSurfaceControls.v1]";
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly PropertyInfo[] Fields = new[]
        {
            nameof(WaterSurfaceSettings.OpticsEnabled),
            nameof(WaterSurfaceSettings.Absorption),
            nameof(WaterSurfaceSettings.RefractionDistance),
            nameof(WaterSurfaceSettings.RefractionColorEnabled),
            nameof(WaterSurfaceSettings.RefractionR),
            nameof(WaterSurfaceSettings.RefractionG),
            nameof(WaterSurfaceSettings.RefractionB),
            nameof(WaterSurfaceSettings.ScatteringColorEnabled),
            nameof(WaterSurfaceSettings.ScatteringR),
            nameof(WaterSurfaceSettings.ScatteringG),
            nameof(WaterSurfaceSettings.ScatteringB),
            nameof(WaterSurfaceSettings.LightingEnabled),
            nameof(WaterSurfaceSettings.Ambient),
            nameof(WaterSurfaceSettings.Body),
            nameof(WaterSurfaceSettings.ReflectionEnabled),
            nameof(WaterSurfaceSettings.NearSmoothness),
            nameof(WaterSurfaceSettings.FarSmoothness),
            nameof(WaterSurfaceSettings.SmoothFadeStart),
            nameof(WaterSurfaceSettings.SmoothFadeDistance),
            nameof(WaterSurfaceSettings.WavesEnabled),
            nameof(WaterSurfaceSettings.LargeWaves),
            nameof(WaterSurfaceSettings.MediumWaves),
            nameof(WaterSurfaceSettings.RippleOverrideEnabled),
            nameof(WaterSurfaceSettings.Ripples),
            nameof(WaterSurfaceSettings.SwellEnabled),
            nameof(WaterSurfaceSettings.SwellPatchSize),
            nameof(WaterSurfaceSettings.SwellWind),
            nameof(WaterSurfaceSettings.SwellAmplitude),
            nameof(WaterSurfaceSettings.SwellMediumAmplitude),
            nameof(WaterSurfaceSettings.SwellChaos),
            nameof(WaterSurfaceSettings.DirectionOverride),
            nameof(WaterSurfaceSettings.SwellDirection),
            nameof(WaterSurfaceSettings.RippleFadeEnabled),
            nameof(WaterSurfaceSettings.RippleFadeStart),
            nameof(WaterSurfaceSettings.RippleFadeDistance),
            nameof(WaterSurfaceSettings.FoamEnabled),
            nameof(WaterSurfaceSettings.Amount),
            nameof(WaterSurfaceSettings.WindIndependence),
            nameof(WaterSurfaceSettings.FoamLookEnabled),
            nameof(WaterSurfaceSettings.FoamRoughness),
            nameof(WaterSurfaceSettings.FoamTiling),
            nameof(WaterSurfaceSettings.CausticsOverride),
            nameof(WaterSurfaceSettings.CausticsOn),
            nameof(WaterSurfaceSettings.CausticsIntensity),
            nameof(WaterSurfaceSettings.CausticsBlend),
            nameof(WaterSurfaceSettings.CausticsProjection),
            nameof(WaterSurfaceSettings.SourceBand)
        }.Select(name => typeof(WaterSurfaceSettings).GetProperty(name)).ToArray();

        private readonly object[] values;

        private WaterPreset(object[] values)
        {
            this.values = values;
        }

        internal static WaterPreset Capture(WaterSurfaceSettings settings)
        {
            return new WaterPreset(Fields.Select(field => field.GetValue(settings)).ToArray());
        }

        internal string[] ToFileLines()
        {
            var lines = new string[Fields.Length + 1];
            lines[0] = FileHeader;
            for (int i = 0; i < Fields.Length; ++i)
            {
                object value = values[i];
                string formatted = value is float number ? number.ToString("R", Invariant) : Convert.ToString(value, Invariant);
                lines[i + 1] = Fields[i].Name + "=" + formatted;
            }
            return lines;
        }

        internal static WaterPreset FromLegacy(string data)
        {
            return string.IsNullOrEmpty(data) ? null : Decode(data.Split('|'), "WSC1");
        }

        internal static WaterPreset FromFile(IEnumerable<string> lines)
        {
            var content = lines.Select(line => line.Trim())
                .Where(line => line.Length != 0 && !line.StartsWith(";") && !line.StartsWith("#"));
            return Decode(content.ToArray(), FileHeader);
        }

        private static WaterPreset Decode(string[] parts, string header)
        {
            if (parts.Length != Fields.Length + 1 || parts[0] != header)
                return null;

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < parts.Length; ++i)
            {
                int separator = parts[i].IndexOf('=');
                if (separator <= 0)
                    return null;
                string key = parts[i].Substring(0, separator).Trim();
                if (entries.ContainsKey(key))
                    return null;
                entries.Add(key, parts[i].Substring(separator + 1).Trim());
            }

            var values = new object[Fields.Length];
            for (int i = 0; i < Fields.Length; ++i)
            {
                if (!entries.TryGetValue(Fields[i].Name, out string text))
                    return null;
                values[i] = Parse(Fields[i].PropertyType, text);
                if (values[i] == null)
                    return null;
            }
            return new WaterPreset(values);
        }

        private static object Parse(Type type, string text)
        {
            if (type == typeof(bool) && bool.TryParse(text, out bool toggle))
                return toggle;
            if (type == typeof(int) && int.TryParse(text, NumberStyles.Integer, Invariant, out int integer))
                return integer;
            if (type == typeof(float) && float.TryParse(text, NumberStyles.Float, Invariant, out float number)
                    && !float.IsNaN(number) && !float.IsInfinity(number))
                return number;
            if (type == typeof(CausticsSource) && Enum.TryParse(text, out CausticsSource band)
                    && Enum.IsDefined(typeof(CausticsSource), band))
                return band;
            return null;
        }

        internal void Apply(WaterSurfaceSettings settings)
        {
            // Use the normal setters so bounds and the controller's revision tracking still apply.
            for (int i = 0; i < Fields.Length; ++i)
                Fields[i].SetValue(settings, values[i]);
        }
    }
}
