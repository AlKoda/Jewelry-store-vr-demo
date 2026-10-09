# Setup and first validation

## Before choosing package versions

Record the installed Unity Editor version, current project's render pipeline (if any), and existing XR package versions. Keep a compatible installed version rather than upgrading without a reason. URP is a candidate for a fresh project, pending inspection.

## Create the project on the Windows PC

1. Download or clone this repository.
2. Create the Unity project under UnityProject using Unity Hub.
3. Record the Editor version and chosen pipeline in this file.
4. Install compatible OpenXR, XR Plug-in Management, Input System and XR Interaction Toolkit packages using Package Manager.
5. Configure Windows PC OpenXR and controller support following the installed package documentation.
6. Set up the Quest PC connection and active OpenXR runtime using the relevant PC VR software.
7. Test a minimal XR scene with head/controller tracking, teleportation and snap turning before store art.
8. Commit Assets (including .meta files), Packages and ProjectSettings. Do not commit Library or generated builds.

These are setup stages, not verified version-specific click instructions.

## Integrate EvidenceCamera.cs

1. Copy Prototype/Scripts/EvidenceCamera.cs into UnityProject/Assets/CrimeSceneDemo/Scripts/.
2. Attach it to a handheld camera object.
3. Add a dedicated child Camera looking out of the lens. Disable its normal automatic rendering. Do not assign the headset camera.
4. Assign that Camera to the component's Photo Camera field.
5. Exclude demo UI from the photo camera's culling mask.
6. Connect the installed XR toolkit's activation event to CapturePhoto(). Exact wiring depends on toolkit version.
7. Test in the Editor and then a Windows build. The dedicated camera is enabled for one normal frame; validate the active render pipeline configuration and rendered output.

The updated script provides normal-frame rendering followed by synchronous PNG saving, sequential filenames, shutter sound/events, status feedback and Windows folder access. Live preview and VR activation remain pending. See TOOLS_BATCH_02.md for automatic station/camera setup.

## Photograph storage

Uses Application.persistentDataPath/EvidencePhotos. On Windows this is normally under the user's AppData/LocalLow folder, using Unity's configured company/product names. The exact saved path is logged after each capture. Saved files remain after scene reset.

## Camera acceptance checks

- Assigned camera produces a readable image from the lens viewpoint.
- Multiple photographs create distinct filenames.
- Missing camera assignment reports an error.
- Photo capture restores render state.
- Files can be opened after closing the demo.
- Capture behavior and any brief frame stall are acceptable in the headset.

## Package Manager startup failure (open)

Unity reported that the Package Manager local server did not start. Unity launches a helper process (UnityPackageManager.exe, under the Editor's Data\Resources\PackageManager\Server folder) and connects to it on 127.0.0.1; the error means that connection failed. Reports from other users point at these causes, in a sensible order to try. None is a confirmed fix for this PC.

1. Antivirus or firewall blocking UnityPackageManager.exe or the local connection. Add an exception for the Unity Editor folder rather than disabling protection; re-enable anything turned off for a test.
2. Proxy settings. If HTTP_PROXY/HTTPS_PROXY variables exist, set UNITY_NOPROXY to `localhost,127.0.0.1`.
3. Cache permissions. The account must have full control of `%LOCALAPPDATA%\Unity\cache`. Deleting that folder and the project's Library folder is safe; Unity regenerates both.
4. Unity Hub → the Editor's menu → Package Manager diagnostics. Keep the report; `%LOCALAPPDATA%\Unity\Editor\upm.log` holds the server's own log.
5. Reinstall the Editor through the Hub if the above fail.

Until this is resolved the launchers use -noUpm, which blocks installing any package.

## XR package plan (after Package Manager works)

Install through Package Manager, Unity Registry, with the versions the Editor offers for 6000.x: XR Plug-in Management, OpenXR Plugin, Input System and XR Interaction Toolkit 3.x (3.0.7 is listed as released for Unity 6; verify in the Package Manager). Enable OpenXR for the Windows build target and add the Meta Quest Touch controller interaction profile. Quest 2 connects by Link cable or Air Link with the Meta runtime set as the active OpenXR runtime.

The project already contains toolkit-independent XR scripts: XRLocomotion (teleport inside InteriorBounds, snap turn) and HandInteractor (grab, carry, drop, camera, trigger). Crime Scene Demo → Create XR Rig Skeleton creates the rig; then add the XR camera under Head, tracked pose drivers to Head and both hands, and bind controller actions to the methods listed in CONTROLS_AND_INTEGRATION.md. Record the exact package versions here when installed.

## Windows build

Build-Windows.cmd at the repository root runs DemoBuild.BuildWindows in batch mode and writes Builds\Windows\JewelryStoreDemo.exe; Run-Demo.cmd starts it. The same build is available from the Crime Scene Demo menu. Builds are not committed. Keep a copy of a working build on separate storage before the presentation.

## Recorded configuration

- Unity Editor: 6000.6.2f1 (tested October 8)
- Render pipeline: Built-in (temporary package-free project)
- XR packages: pending
- Quest connection: pending
- Successful Windows/headset build: pending

## October 8 PC verification

See VERIFICATION_2026-10-08.md and the downloadable generated project. All source compiled and core Play mode checks passed. Camera capture now waits for camera render callbacks. Unity Package Manager local-server startup failed; -noUpm is a temporary geometry-stage workaround and must be resolved before XR setup.
