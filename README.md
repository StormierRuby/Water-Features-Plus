# Water Surface Controls

Tune whitecap coverage, layer medium waves over a long swell, and adjust how water reflects and scatters light in **Cities: Skylines II**. The mod uses the game's native water surface. Its settings are visual and apply across the cities you load.

**Mod version** 1.5.1 · **Target game version** 1.6.4f1 · **Languages** English and Brazilian Portuguese

[Leia em português](LEIA-ME.md)

## Install from Paradox Mods

Search for **Water Surface Controls** in Paradox Mods, add it to your active playset, and restart the game. Load a city and open **Options → Water Surface Controls**.

## Getting started

1. Load a city, point the camera across a stretch of open water, and open **Options → Water Surface Controls**.
2. Open **Foam**, move **Foam amount**, and release the slider to apply the change. A new profile adjusts only foam, with **Foam amount** at **40** and **Wind independence** at **100**.
3. To try larger waves, open **Waves → Ocean swell** and select **Preset: long swell**. It sets the strength of both wave bands, the patch size, the wind speed, and the irregularity. Your foam, color, and reflection settings carry over.
4. In **General**, turn off **Enable water adjustments** to restore the game’s water. Turn it on again to resume your settings.

## Save and load your presets

1. Adjust the water, then open **General → My presets**.
2. Under **Preset slot**, choose one of the three slots. Select **Save preset** to save into an empty slot.
3. To restore a look, select its slot and choose **Load preset**. Selecting the slot alone keeps your current settings.
4. To update an existing preset, select its slot, choose **Replace preset**, and confirm. The other two slots keep their presets.

Each preset stores all 32 sliders, group switches, and the caustics source band, including values for groups you have turned off. Each slot uses a separate text file that you can use in any city. Loading a preset keeps **Enable water adjustments** in its current position. Applying a built-in preset also preserves your saved copies.

### Where to find the files

Open **General → My presets → Open presets folder**. The default Windows path is:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsSettings\WaterSurfaceControls\Presets
```

| Slot | File |
| --- | --- |
| Slot 1 | `Preset-1.ini` |
| Slot 2 | `Preset-2.ini` |
| Slot 3 | `Preset-3.ini` |

**To share or back up a preset**, copy its file. **To import one**, copy it into this folder, give it the filename of an empty slot, choose **Refresh preset files**, then **Load preset**. If all slots are occupied, back up the file you intend to replace.

Each file holds all 47 settings as plain text. To edit foam coverage, for example, change `Amount=40` to `Amount=80`, save the file, then load the preset. Keep the `[WaterSurfaceControls.v1]` header and the other entries. Use a decimal point for fractional values.

**Upgrading from 1.5.0.** On startup, the mod exports the presets embedded in your existing preferences. If a slot already contains a different file, the old copy gets a name such as `Preset-2-v1.5.0.ini`. To load that copy later, follow the import steps above. If export fails, the mod keeps the embedded preset and retries at next startup.

## Controls by tab

The panel has **32 sliders across six tabs**, plus toggles, selectors, and presets. Available controls depend on which groups are enabled, the loaded water surface, and the fields the game exposes.

| Tab | What you can adjust |
| --- | --- |
| **General** | Enable water adjustments, save and load personal presets, apply built-in presets, and write diagnostics to the log. |
| **Water** | Absorption distance, maximum refraction distance, transmission and scattering colors (RGB), ambient scattering, and wave-body lighting. Increase absorption distance to see farther through the water where the bottom is visible. |
| **Reflections** | Surface smoothness near and far from the camera, plus the start and length of the transition between them. Higher smoothness produces sharper highlights. |
| **Waves** | Swell band strengths, patch size, wind, irregularity, direction, native amplitude multipliers, ripple wind, and the distance at which fine ripples fade. |
| **Foam** | Coverage, wind independence, roughness, and texture tiling. **Foam amount** controls how much of the surface has whitecaps. Higher texture tiling makes the details within each patch smaller. |
| **Caustics** | Underwater light patterns, their intensity, vertical blend distance, projection distance, and the wave band that generates them. Compare settings in shallow water where you can see the bottom. |

### Long swell, medium waves, and fine ripples

**Ocean swell** lets you tune two wave bands independently. The long band produces broad undulations. The secondary band adds medium-scale detail on top. Ripples supply the finest surface detail.

| Swell control | Range | Default |
| --- | --- | --- |
| Ocean spectrum patch size (m) | 250–5,000 m | 1,500 m |
| Swell-generating wind (km/h) | 0–100 km/h | 45 km/h |
| Long-band strength (%) | 0–200% | 85% |
| Secondary-band strength (%) | 0–200% | 40% |
| Swell irregularity | 0–100 | 40 |
| Swell direction (°) | 0–360° relative to the map's X axis | The game sets the direction by default. The slider starts at 0° and takes effect when you enable **Override swell direction**. |

To see what each band contributes, apply the long swell preset and set the secondary band to **0%**. Switch the long band between **0% and 100%**. Then leave it at **85%** and bring the secondary band back to **40%**. To check direction, lower the irregularity and compare **0° and 90°**, using a quay as a fixed reference.

Patch size sets the area over which the wave pattern repeats. Crest spacing and visible wave height also depend on wind and the mix of bands. In Ocean swell mode, **100%** assigns an absolute multiplier of **1.0**, so you can bring back a band whose original strength was zero.

Select **Use native amplitude controls** to scale each surface's original wave amplitudes instead. In this mode, **100%** preserves the original amplitude, **150%** multiplies it by 1.5, and a band that starts at zero stays at zero. **Resume saved swell** brings back your custom swell settings without reapplying the preset.

Ocean swell requires a surface the game classifies as `OceanSeaLake` or `Ocean` and the corresponding wave spectrum fields. Check **Swell-compatible surfaces** under **Waves**. A single surface can span several regions of a map. The mod skips pool surfaces.

### Ripple distance and foam coverage

Under **Waves → Fine ripples by distance**, enable **Override ripple visibility**. **Fade start (m)** ranges from 0 to 5,000 m, and **Fade length (m)** ranges from 5 to 10,000 m. The defaults of **50 m** and **200 m** give a transition from **50 to 250 m from the camera**. To see the effect, watch one patch of water roughly 100 m away. Set the fade length to **5 m** and switch the start between **0 m and 1,000 m**. Look for a change in the fine detail on that patch.

Distance fading requires active ripples and works independently of ripple wind. To change the wind that generates ripples, enable both **Override wave amplitude** and **Override ripple wind (experimental)**. The ripple wind multiplier scales the surface's original wind speed.

Use **Foam** to adjust whitecap coverage. **Wind independence** at **0** preserves the surface's native response to wind. At **100**, it removes the reduction in foam caused by that wind response. The foam persistence control stays hidden because its effect on foam lifetime could not be confirmed. To test ripple fading, change the viewing distance or fade settings. Elapsed simulation time alone does not test it.

### Caustics

Turn on **Override caustics** and **Enable caustics**. On an ocean surface, **Source band** lets you choose **Long**, **Secondary**, or **Ripples** to generate the light pattern on the bottom. Select **Game** to restore the surface's original choice. The mod releases the override if the selected band is inactive.

## Built-in presets and game values

- **Use game values** turns off all override groups and keeps your slider values for later.
- **Foam only (default)** resets the sliders and enables only the foam override.
- **Balanced starting preset** applies the built-in water profile. The swell uses a 1,250 m patch, 61 km/h wind, long-band strength of 153%, and secondary-band strength of 59%. Foam amount is 40, wind independence is 100, and caustics intensity is 82 using the secondary band. Swell follows the game’s wind direction.
- **Preset: long swell** applies the five values listed above for patch size, wind speed, the two band strengths, and irregularity. Choose **Resume saved swell** to return to your own settings.

## Building and upgrading on Windows

1. With the official modding toolchain installed, close the game and open **WaterSurfaceControls.sln** in Visual Studio.
2. Select **Release**, then **Build → Rebuild Solution**. The entry point is `WaterSurfaceControls/Mod.cs`. The runtime, settings, and localization files are in `WaterSurfaceControls/src/`. The project includes the `Colossal.Localization` reference.
3. With the default installation, the build deploys the DLL to `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\WaterSurfaceControls\WaterSurfaceControls.dll`. Start the game and open **Options → Water Surface Controls**.

Water Surface Controls still stores its active settings at `ModsSettings/WaterSurfaceControls/FoamSettings`. Upgrading an installation that already uses this path retains your settings. If you used a build with a different project name, check your values when you first open the panel.

## Diagnostics

A successful startup writes `WaterSurfaceControls 1.5.1 loaded` to the mod log.

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\WaterSurfaceControls.log
```

To investigate a control, select **General → Write diagnostics to log** and return to your city for about 20 seconds. The mod takes one reading immediately and two more after approximately **5 and 15 seconds**.

| Log entry | What to look for |
| --- | --- |
| `BOUND` and `SWELL` | Discovered surfaces and availability of the fields required for swell. |
| `API` | Available, missing, or suspended fields. |
| `WAVE_STATE` | Patch size, wind, band strengths, direction, ripple state, fade distances, and caustics source band. |
| `WAVE_OUTPUT` | Active bands, patch sizes, and multipliers that the runtime exposes. |
| `OBSERVE` | Read-only samples of suspended fields, including foam persistence, height and displacement scattering, wave-tip lighting, and the animation clock. The game controls pause, resume, and animation speed. |

A changed log value confirms that the parameter changed. To judge the visual effect, compare the same view under the same lighting and change one control at a time. When reporting an issue, include your game version, the active settings groups, and the relevant log entries.

## Validation and references

Automated tests covered the water controls, pause and resume, and saving, reading and replacing real files. Migration tests used presets produced by the 1.5.0 code. Tests used stand-ins for game and Unity APIs. The options panel and opening the folder from the game still need verification on Windows.

The implementation uses Unity HDRP as a reference for interpreting water fields. Cities: Skylines II supplies the material that renders the surface. [HDRP 14 water properties](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html) · [WaterSurface.cs](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSurface.cs) · [Water system](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.cs) · [Simulation update](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.Simulation.cs)

These projects provided examples of water controls and settings interfaces during development. Water Surface Controls runs independently of them. [WaterVisualTweaks](https://github.com/HX2003/WaterVisualTweaks) · [TrafficToolEssentials](https://github.com/ZessonsDE/TrafficToolEssentials/blob/main/TrafficToolEssentials/Settings.cs) · [AreaBucket](https://github.com/Cmyna/AreaBucket/blob/master/Setting.cs) · [CS2MultiplayerMod](https://github.com/Rollocraft/CS2MultiplayerMod/blob/master/CS2MultiplayerMod/Setting.cs)
