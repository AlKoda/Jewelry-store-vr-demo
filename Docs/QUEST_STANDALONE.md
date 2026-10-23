# Quest 3 standalone app

The demo can run on the Meta Quest 3 by itself, with no PC or cable during the show. It is the **same project and the same saved night scene** as the PC demo, built a second way: an Android app (APK) installed on the headset. Every change made to the project reaches both: rebuild and reinstall to update the headset.

*Written October 20 (Claude). Not yet built or run: the first build on the PC is the first test.*

## One-time setup

1. **Android support for Unity.** In Unity Hub: **Installs > 6000.6.2f1 > ⚙ > Add modules > Android Build Support**, with **OpenJDK** and **Android SDK & NDK Tools** ticked. About 5 GB.
2. **Developer mode on the headset.**
   - In the **Meta Horizon** phone app: **Devices > your Quest 3 > Headset settings > Developer mode > on**.
   - Meta asks you to create or join a free developer organisation the first time, on developer.meta.com.
3. **USB.** Connect the headset to the PC with a USB-C data cable, put it on, and accept **Allow USB debugging** (tick "Always allow from this computer").

## Build and install

Double-click **Build-Quest-App.cmd**:
- It copies the latest code into `UnityProject_Demo` and builds `Builds\Quest\JewelryStoreDemo.apk`. It never regenerates the scene, so objects you moved by hand stay where you put them.
- It then installs the app on the connected headset, replacing the previous version. Photos and settings already on the headset are kept.
- The **first build takes 20–60 minutes**: Unity converts every texture for the headset once. Later builds take a few minutes.
- `Build-Quest-App.cmd rebuild` forces a build. `Build-Quest-App.cmd install` only installs the APK already built.
- If the build fails, the window shows the errors. The full log is `Builds\build-quest.log`.

## Starting it on the headset

**App Library > filter "Unknown Sources" > Jewelry Store Crime Scene.** It opens straight into the night store; there is no PC start menu. Unplug the cable first if you want to walk freely.

## Running the demo without the PC keyboard

The presenter keys are in a menu inside the headset:

| Action | Controller |
|---|---|
| Open the presenter menu | **Hold the left menu button** (≡) for about a second |
| Choose a button | Point the right controller at it and pull the **right trigger** |
| Close the menu | **Close** button, or a short press of the left menu button |
| Controller hints on/off | Short press of the left menu button (as before) |
| Quit the app | Hold **left menu + Y** for two seconds (as before), or the Meta button |

Menu buttons:
- **Reset scene:** clears placed tools, keeps photos.
- **Movement:** smooth walking or teleport.
- **Comfort vignette:** on or off.
- **Language:** Arabic or English.
- **Height:** +5 cm or −5 cm.
- **Light bar:** flashing or steady.
- **Controller hints.**
- **Close.**

The labels follow the interface language.

To show the audience what the trainee sees, cast the headset to a TV, laptop or phone: in the headset, **Camera > Cast**. It needs the same Wi-Fi network.

## Photos

Photos taken in the app are saved on the headset. To copy them to the PC, connect it by USB and run **Tools\pull-quest-photos.cmd**. They land in `Photos-From-Quest` beside the README (not uploaded to GitHub).

## What is different on the headset

The scene, baked lighting, interactions and sounds are the same. To hold the headset's frame rate:
- **Shadows:** realtime shadows are hard instead of soft, and their maps are half size: flashlight 1024, UV torch and lamp 512. There are at most two per-pixel lights, and shadows reach out to 8 m.
- **Headlights:** their realtime highlight lights are off. The baked headlight lighting stays.
- **PC screens:** the presenter panel, the overview map and the language button are switched off.
- **Camera screen:** the camera's rear screen refreshes 8 times a second instead of 15.
- **Textures:** they are capped at 1024 pixels and compressed for the headset (ASTC).

All of this is in `DemoPlatform`, `NightQuality` and `PackImport`. The PC build is unchanged.

## For developers

- **Build code:** `Prototype/Editor/Build/QuestBuild.cs`.
  - Android settings: package `com.alkoda.jewelrystorecrimescene`, IL2CPP, ARM64, Vulkan, ASTC, minimum API 32.
  - XR settings: OpenXR for Android with Meta Quest Support and the Touch / Touch Plus controller profiles, single-pass (multiview) stereo, XR started with the app.
  - The version code grows with every build, so each install replaces the last.
- **Runtime switch:** `DemoPlatform.Standalone` (Android). Validators set `DemoPlatform.ForceStandalone` to check the headset settings on the PC.
- **Presenter menu:** `Prototype/Scripts/VR/XRPresenterMenu.cs`, created on the left controller by XRControllerInput.
- **Windows launchers:** both PC launchers now pass `-buildTarget Win64`, so the project returns to Windows after a Quest build. Unity keeps both platforms' imported textures, so switching back and forth is fast after the first time.
- **Open questions to check in the headset:**
  - frame rate (72 Hz target) in the showroom with the flashlight on;
  - glass and reflections;
  - the size of the presenter menu.

  Since October 23 (unverified) NightQuality applies `QuestEyeResolutionScale` 0.9 and fixed foveation `QuestFoveation` 0.5 on the headset alone (QuestBuild enables the OpenXR Foveated Rendering feature), the camera flash casts no shadow there, older lit lamps are vertex lights and the glass shaders skip ForwardAdd shadow sampling. If the frame rate is still low, raise the foveation level or lower the scale, then the number of cases or the glass layers.
