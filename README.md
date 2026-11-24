# Simple Water Foam

Configurable water, foam, and wave visuals for **Cities: Skylines II**.

Simple Water Foam exposes selected parameters of the game's native water system through an in-game settings panel. Adjust foam coverage, water color, surface smoothness, wave strength, and caustics, then compare the result with the original appearance using an A/B toggle.

**Current version: 1.3.0 — experimental Ocean Swell controls.**

The development baseline is **Cities: Skylines II 1.6.4f1**. The existing 1.2.1 controls have been reported working in that version. The new 1.3.0 swell controls have passed controller-level checks, but their visual behavior still needs verification in the game.

## Features

Settings are organized into six tabs, with English and Brazilian Portuguese localization.

| Tab | Controls |
| --- | --- |
| **General** | Master A/B toggle, presets, runtime status, and diagnostics |
| **Water** | Absorption distance, refraction, transmission and scattering colors, and selected lighting contributions |
| **Reflections** | Near and distant smoothness, with a configurable distance transition |
| **Waves** | Large and medium wave multipliers, an optional ripple-wind override, and experimental ocean swell controls |
| **Foam** | Amount, wind independence, roughness, texture tiling, and experimental persistence |
| **Caustics** | Enable/disable override, intensity, vertical blending, and projection distance |

Version 1.3.0 includes **34 sliders**. Four remain deliberately suspended; the other 30 depend on available engine fields, surface type, and enabled settings groups.

Changes apply when you release a slider. Settings are global to the mod rather than specific to each city.

## Ocean Swell

The **Waves → Ocean swell** group is intended to make broad undulations more prominent while retaining smaller waves and surface ripples.

The earlier amplitude controls multiply each surface's original values. That changes wave strength, but does not adjust the spatial scale of the spectrum. An original multiplier of zero also remains zero.

Ocean Swell additionally adjusts the native simulation domain and generating wind, and provides absolute strength controls for the two existing swell bands. It uses the native HDRP water simulation; it does not introduce an additional wave band or a replacement displacement shader.

| Control | Range | Starting preset |
| --- | --- | --- |
| **Ocean spectrum patch size** | 250–5000 m | 1500 m |
| **Swell-generating wind** | 0–100 km/h | 45 km/h |
| **Long-band strength** | 0–100% | 85% |
| **Secondary-band strength** | 0–100% | 40% |
| **Swell irregularity** | 0–100 | 40 |

- **Patch size** is the size of the periodic simulation domain, not an exact crest-to-crest wavelength. It also affects the secondary band.
- **Generating wind** changes the native wave spectrum. It does not change the city's weather, and its effect on wave height is not linear.
- **Band strength** maps directly to a native multiplier from 0 to 1. For example, 85% sets 0.85; it is not a height in meters or 85% of the previous value.
- **Irregularity** changes how strongly the swell follows the existing wind direction. It does not select a new direction.

The two swell bands share parts of the native spectrum configuration. They are not completely independent wave generators.

### Enable and compare

1. Load a city with water.
2. Open **Options → Simple Water Foam → Waves → Ocean swell**.
3. Select **Preset: long swell**.
4. Return to the city and compare from the same camera position and lighting conditions.

To isolate the broad waves, temporarily set **Secondary-band strength** to **0** and compare **Long-band strength** at **0** and **85**. Then bring the secondary band back to around **25–40**.

The swell preset preserves your water colors, foam, reflection settings, and other groups. Swell starts disabled in a new configuration. Changing the spectrum may produce a visible transition as it is regenerated; changing generating wind can also affect foam production.

### Surface compatibility

Swell requires a surface identified by the engine as `OceanSeaLake` or `Ocean`, together with the required spectrum fields. The panel displays the number of compatible surfaces after a city is loaded.

This classification belongs to the engine surface, not to geographical regions. If the game represents a sea, lake, and river sections with one shared ocean surface, the changes apply across that surface. The mod does not convert river surfaces into oceans or provide a geographical selection mask.

While swell is enabled, the older large/medium amplitude sliders are disabled. Turning swell off restores the previous spectrum settings and returns amplitude control to the older mode if it was enabled. The older amplitude overrides are also released on non-ocean surfaces while swell is active.

## Build and install

This source package is intended for a project generated with the official Cities: Skylines II C# mod template. It is not a precompiled mod or a standalone build project.

### Requirements

- Cities: Skylines II, with the official modding toolchain configured.
- A C# mod project created from the game's template.
- Visual Studio or another compatible C# development environment.
- The game assemblies and .NET SDK expected by that template.

The development setup used the **Unity 2022.3.62f2** toolchain selected by game version **1.6.4f1**. There is no custom shader or AssetBundle build step for this implementation.

### Update an existing project

1. Close the game completely.
2. Back up your working `Mod.cs` outside the project directory.
3. Open the actual mod solution/project in your IDE.
4. Replace the project's `Mod.cs` with the supplied implementation. Do not keep a second copy of the same classes in the project.
5. Preserve your `.csproj`, toolchain imports, and build configuration.
6. Rebuild the project using its configured local mod deployment.
7. Launch the game, load a city, and open **Options → Simple Water Foam**.

The deployed local mod should be under:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\SimpleWaterFoam
```

Building a loose file opened from a ZIP does not update the project or its deployed DLL. Verify that the runtime log contains:

```text
SimpleWaterFoam 1.3.0: native water controls
```

The assembly version displayed elsewhere may still come from your `.csproj`.

### Assembly references

The template and its imports should resolve the game's assemblies. The implementation uses:

```xml
<ItemGroup>
  <Reference Include="Game"><Private>false</Private></Reference>
  <Reference Include="UnityEngine.CoreModule"><Private>false</Private></Reference>
  <Reference Include="Colossal.Core"><Private>false</Private></Reference>
  <Reference Include="Colossal.Logging"><Private>false</Private></Reference>
  <Reference Include="Colossal.IO.AssetDatabase"><Private>false</Private></Reference>
  <Reference Include="Colossal.Localization"><Private>false</Private></Reference>
</ItemGroup>
```

Add only references that are missing. In particular, a `CS0012` error involving `LocalizationManager` requires `Colossal.Localization`.

Water fields are discovered through reflection, so the implementation does not require a direct reference to the HDRP assembly. The mods listed under References are not runtime dependencies.

## Presets and restoration

| Option | Behavior |
| --- | --- |
| **Apply adjustments (A/B comparison)** | Suspends or reapplies the selected groups without discarding slider values |
| **Balanced starting preset** | Resets settings and enables a starting combination of water, reflection, wave amplitude, and foam appearance adjustments; swell remains disabled |
| **Foam only (previous version)** | Resets settings and enables foam amount and wind-independence adjustments |
| **Use game values** | Disables the override groups, including swell, while preserving slider values |
| **Preset: long swell** | Enables swell with the values above while retaining other appearance settings |

The controller captures original field values when an override becomes active. When it releases a field, it restores the captured value if that field still contains the mod's last write. A more recent external write is preserved when it can be detected.

Avoid enabling multiple mods that continuously override the same water parameters. Restoration logic cannot resolve competing active controllers.

## Current limitations

- The mod adjusts native water parameters. It does not replace the water shader, add custom foam textures, or implement shoreline breaking waves, boat wakes, or boat buoyancy.
- It does not modify the city's hydrological simulation.
- Reflection controls adjust smoothness; they do not add SSR, ray tracing, or a separate reflection system.
- Finding an engine field does not guarantee that the game's material visibly uses it. Masks, geometry, distance fading, lighting, and the rendering path can affect the result.
- Swell cannot guarantee a specific wave height or wavelength. Its visual effect in the game's custom rendering path remains experimental.
- There are no in-game FPS measurements for version 1.3.0.

### Suspended controls

| Control | Reason |
| --- | --- |
| Height scattering | No visible effect was reported in the tested CS2 setup |
| Displacement scattering | No visible effect was reported in the tested CS2 setup |
| Wave-tip lighting | No visible effect was reported in the tested CS2 setup |
| Animation speed | Removed from active control to prevent a captured pause value from freezing water animation |

These controls remain visible but disabled, preserving existing saved settings. The mod does **not** write to `timeMultiplier`; pause, resume, and simulation speed remain under game control.

## Diagnostics and troubleshooting

The mod writes to:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\SimpleWaterFoam.log
```

Select **General → Write diagnostics to log**, return to the city, and let it run for at least **20 seconds**. The diagnostic captures an immediate reading and two additional readings after approximately 5 and 15 seconds.

| Log entry | Meaning |
| --- | --- |
| `API` | Field availability, including suspended controls |
| `BOUND` | Discovered surface and engine surface type |
| `SWELL` | Compatible surface count and required API availability |
| `WAVE_STATE` | Current domain, wind, band strengths, geometry, and long-band distance fading |
| `WAVE_OUTPUT` | Internal band count, patch sizes, and rendering multipliers, when exposed |
| `OBSERVE` | Read-only samples of suspended fields, including the animation clock |
| `Disabled control` | A field failed and its override was isolated |
| `Controller stopped` | A broader failure stopped the controller and triggered a restoration attempt |

If `WAVE_OUTPUT` reports `not exposed` or empty values, the internal structure could not be inspected. That alone does not mean the simulation has stopped.

| Symptom | Check |
| --- | --- |
| No settings panel or version message | Confirm that the actual project was rebuilt and its updated DLL was deployed |
| A slider is disabled | Load a city, enable the master toggle and the relevant group, then check field availability; suspended controls remain disabled |
| Swell is unavailable | Check **Swell-compatible surfaces** and the `SWELL` diagnostic entry |
| Old amplitude sliders are disabled | This is expected while swell is active |
| Values change but appearance does not | Compare clearly separated values under the same conditions; inspect `WAVE_STATE` and `WAVE_OUTPUT` before assuming shader support |
| No mod messages in `Player.log` | Check the dedicated `Logs\SimpleWaterFoam.log` file |

For a useful bug report, include the game version, the mod version line, enabled settings groups, relevant diagnostics, and reproducible steps. For visual issues, include before/after screenshots from the same view and lighting conditions.

## Validation

The 1.3.0 implementation passed **78 automated controller checks** using substitute API types: 51 checks for existing behavior and 27 for swell. A separate regression check covered pause, resume, and speed changes.

Checks covered activation from a zero amplitude, mode switching, restoration, missing fields, surface eligibility, value limits, and preservation of other settings. They did not execute the actual water shader or simulation, compile against the installed game assemblies, or validate the real options UI, serialization, or performance.

## Version notes

- **1.3.0:** Added the Ocean Swell group, five sliders, a dedicated preset, surface compatibility status, and wave diagnostics. Preserved the animation fix.
- **1.2.1:** Removed writes to the water animation clock, separated the ripple-wind override, and suspended three lighting controls without observed visual effects.

## References

Native water behavior was investigated using Unity's upstream HDRP sources. Those sources describe the reference implementation, not the exact binaries or custom materials shipped with Cities: Skylines II.

- [Unity HDRP 14 — Water properties](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html)
- [Unity Graphics — WaterSurface.cs, 2022.3 branch](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSurface.cs)
- [Unity Graphics — Water simulation parameters](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.cs)
- [Unity Graphics — Water simulation update](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.Simulation.cs)

Public examples consulted for water modding and native settings UI patterns:

- [WaterVisualTweaks](https://github.com/HX2003/WaterVisualTweaks) — water modding and options reference.
- [TrafficToolEssentials settings](https://github.com/ZessonsDE/TrafficToolEssentials/blob/main/TrafficToolEssentials/Settings.cs) — tabs, sliders, and conditional availability.
- [AreaBucket settings](https://github.com/Cmyna/AreaBucket/blob/master/Setting.cs) — fractional slider formatting.
- [CS2MultiplayerMod settings](https://github.com/Rollocraft/CS2MultiplayerMod/blob/master/CS2MultiplayerMod/Setting.cs) — tabs, buttons, and status entries.
