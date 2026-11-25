# Water Features Plus

Adjust the whitecaps and ocean swell in **Cities: Skylines II**. Start with scattered foam on open water, then layer medium waves over a longer swell. The mod uses the game's own water renderer and gives you an A/B switch to compare the same view with the original water.

**Version 1.4.1** · Game target **1.6.4f1** · English and Brazilian Portuguese

## Install and try it

For the published release, open **Paradox Mods** in the game, add **Water Features Plus** to your active playset and restart the game. Load a city with open water, then open **Options → Water Features Plus**. The GitHub source package is for building the mod yourself. Players installing through Paradox Mods only need the game.

1. Aim the camera across open water so you can distinguish individual crests. The default profile has **Foam amount** at **40** and **Wind independence** at **100**. Move **Foam → Foam amount** and release the slider to compare coverage.
2. Open **Waves → Ocean swell** and press **Preset: long swell**. Compare the long-band strength at **0%** and **100%** while keeping the secondary band fixed. Then repeat with the secondary band. The first changes broad undulations and the second adds smaller moving detail.
3. Switch **General → Apply adjustments (A/B comparison)** off and on to compare both looks. This keeps your slider values. **Foam only (default)** and **Balanced starting preset** reset slider values when selected.

## Controls

| Tab | What it changes |
| --- | --- |
| **General** | A/B toggle, presets, detected controls and a diagnostic log button. **Use game values** releases every override and keeps the slider values. |
| **Foam** | Coverage, response to wind, roughness and texture scale. Use coverage to change how much foam forms. |
| **Waves** | Long and secondary swell strengths, patch size, wind, irregularity, direction, native amplitude multipliers and fine ripple details. The two swell strengths range from 0% to 200%. |
| **Water** | Absorption and refraction distances, transmission and scattering colors, ambient scattering and wave-body lighting where available. |
| **Reflections** | Near and far surface smoothness and the distance over which they blend. |
| **Caustics** | Underwater light patterns, intensity, vertical blending, projection distance and an optional ocean source band. Compare in shallow water. |

**Distance fade under Waves** controls fine ripples as the camera moves. A start of 50 m and a length of 200 m give a transition from 50 to 250 m. A change in foam lifetime has not been established for the target game version. The experimental persistence slider from the development builds is hidden.

**Wave modes.** Ocean swell sets absolute strengths on eligible ocean-type surfaces. Native amplitude controls multiply each surface's original values, so a band starting at zero stays at zero in that mode. **Resume saved swell** returns to your previous swell profile. Patch size controls the simulation domain in metres. The height of an individual wave also depends on wind and band strength.

Settings are shared across cities. The options depend on the fields and surface types available in the running game. Load a city for surface-dependent controls. The game controls water animation speed and the city's water simulation. If two mods adjust the same water property, disable the overlapping group in one of them before comparing results.

## Troubleshooting

- **The panel is missing.** Confirm the mod is enabled in the active playset. For local builds, check that the mod project deployed the new DLL and that only the intended copy is active.
- **Swell is unavailable.** Check the compatible surface count under **Waves**. The renderer classifies surfaces, and one surface can cover different parts of a map.
- **A value changes but the surface looks the same.** Keep the camera and time of day fixed, change one setting, then use A/B. For ripple distance, compare the same patch at different camera distances. For caustics, keep the bottom visible.
- **Reporting an issue.** Choose **General → Write diagnostics to log**, return to the city for about 15 seconds and share the relevant entries, mod and game versions, active groups, and steps. The Windows log is at `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\WaterFeaturesPlus.log`.

## Build from source on Windows

Install the code-modding toolchain in **Options → Modding** and close the game. Open the included **SimpleWaterFoam.csproj** in Visual Studio, choose **Release** and rebuild the project. The template imports the installed game tools through `CSII_TOOLPATH`. It supplies the game references and deploys the local mod. A successful startup writes `WaterFeaturesPlus 1.4.1 loaded` to the mod log. If an existing development copy is present, the build targets the same local mod folder.

The entry point is in `src/Mod.cs`. The runtime, field bindings and diagnostic routines are in `src/Runtime/`. The options and translations are in `src/Settings/`. The standalone regression suite runs with `dotnet run --project tests/ControllerTests.csproj -c Release` on the .NET 8 SDK. It exercises the controller with substitute game and Unity types. A release build and visual testing require the installed game.

## References and acknowledgements

Unity's HDRP sources informed the field mappings. The game ships its own materials and engine build. [HDRP 14 water properties](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html) · [Unity water runtime, 2022.3 branch](https://github.com/Unity-Technologies/Graphics/tree/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water)

Public examples consulted during development cover water controls, settings tabs, slider formatting and status rows. These mods are optional and can be used as references when working on the source. [WaterVisualTweaks](https://github.com/HX2003/WaterVisualTweaks) · [TrafficToolEssentials](https://github.com/ZessonsDE/TrafficToolEssentials) · [AreaBucket](https://github.com/Cmyna/AreaBucket) · [CS2MultiplayerMod](https://github.com/Rollocraft/CS2MultiplayerMod)
