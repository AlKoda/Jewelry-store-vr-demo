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

## Recorded configuration

- Unity Editor: pending
- Render pipeline: pending
- XR packages: pending
- Quest connection: pending
- Successful Windows/headset build: pending
