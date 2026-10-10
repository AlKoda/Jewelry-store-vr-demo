# Controls and integration contract

The instructor has unrestricted use. These are methods to connect through UI/XR events, not a prescribed training sequence.

| Action | Entry point |
|---|---|
| New cone | ToolStation.SpawnCone() |
| New numbered marker | ToolStation.SpawnMarker() |
| New tape post | ToolStation.SpawnTapePost() |
| Select first/second tape endpoint | ToolStation.SelectTapePost(DeployedTool) |
| Automatically connect deliberately placed post | ToolStation.NotifyPlaced(DeployedTool), called by ToolHolder.Place() |
| Start a separate tape run | ToolStation.StartNewTapeRun() |
| Cancel pending tape | ToolStation.CancelTapeSelection() |
| Remove an existing tool | ToolStation.RemoveTool(DeployedTool) |
| Place/reposition | DeployedTool.PlaceAt(Vector3, float), or XR grabbing |
| Photograph | EvidenceCamera.CapturePhoto() |
| Open photo folder on Windows | EvidenceCamera.OpenPhotoFolder() |
| Reset session | DemoSession.ResetSession() |

UnityEvents: ToolsChanged, SessionReset, PhotoSaved(string path), CaptureFailed(string message) and Shutter. EvidenceCamera also exposes LastPhoto (texture of the last photograph), Viewfinder (live texture while the body is held) and PhotoCount.

Placing successive new posts connects tape automatically. Moving an already placed post adjusts its existing tape without creating another ribbon. Remove a pointed tool with R/Delete, or open Tab and use its Placed tools > Remove button. Removing a post removes attached tape. In VR, grip the deployed tool and press B/Y to remove it.

## Ownership

All deployed tools and ribbons belong under DeploymentRoot. Keep its scale at (1,1,1). XR grabbing must retain or restore that ownership; otherwise reset and station removal will not find reparented objects. Prevent reset while tools are actively held, or release XR selections first in the future adapter.

Static evidence belongs under FixedEvidence and is never deleted by tool reset. Add ResettableSceneObject and register it with DemoSession if a scene prop becomes movable. Registered resettable objects must retain their original parent. Camera filenames continue across reset.

## Desktop adapter (DesktopInteractor)

Pointer = mouse, or the screen centre while right-click mouse look is toggled on. Placement only lands on upward-facing surfaces inside InteriorBounds; otherwise the held tool stays put and placement is refused. The panel starts closed; opening it pauses desktop walking and tool hotkeys.

| Input | Action |
|---|---|
| WASD / arrows, Shift | Walk, run (ShopWalkController) |
| Right click / Escape | Toggle mouse look / release cursor |
| Left click | Pick up the pointed tool / place the held tool / take a new tool from a rack sample / pick up the camera |
| 1, 2, 3, 4, 5 | New cone, marker, tape post, L-scale or measuring reel straight into the hand |
| Q / E, mouse wheel | Rotate the held tool in 15° steps |
| T | SelectTapePost on the held or pointed post or measuring reel (same-kind pairs connect) |
| X | CancelTapeSelection |
| R / Delete / Backspace | Remove the held or pointed tool |
| F | Hold the evidence camera in front of the view / return it to the rack |
| P | CapturePhoto |
| Home | Return to the start position |
| I | Intact / robbed store view (CrimeSceneState; also a panel button) |
| N | Night / day lighting preset (DemoLighting) |
| L | Flashlight on the view (DemoLighting) |
| B | Instructor beacon at the pointed spot (DemoBeacon; also click the panel's overview map) |
| Tab | Cycle the presenter panel: hidden → strip → full (DemoDesktopPanel) |
| [ / ] | Presenter panel UI scale (persisted; also +/- buttons) |

Walls, the storefront barrier and InteriorBounds keep the player inside. InteriorBounds.Contains/Clamp are the shared definition of the interior; use them to filter XR teleport destinations.

DemoSession.Resetting fires before anything is cleared; adapters release held objects there so parents and poses restore correctly. The desktop adapter does this; an XR adapter must do the same.

## One player, two modes (DemoModeSwitch)

The Player object carries both the desktop components (ShopWalkController, DesktopInteractor) and the VR ones (XRHeadTracking on the camera, LeftHand/RightHand with HandInteractor + XRControllerInput). DemoModeSwitch enables one set: VR when a headset is active at start (XRSettings.isDeviceActive), desktop otherwise. **F9** flips between them at any time, so the demo can be tested with mouse and keyboard without unplugging anything. The presenter panel stays on the monitor in both modes.

## VR controls (XRControllerInput, package-free)

Poses and buttons come from Unity's built-in XR input API (UnityEngine.XR.InputDevices), which the OpenXR runtime feeds once XR Plug-in Management and the OpenXR plugin are installed and enabled. No toolkit, Input System or TrackedPoseDriver is required.

| Controller | Action |
|---|---|
| Grip (either hand) | Hold the nearest tool within 25 cm, else take a new one from a rack sample, else the camera; release puts it down on the surface beneath |
| Trigger | Photograph while holding the camera; select a held tape post or measuring reel |
| A / X (primary) | New tool of the hand's current kind, straight into the hand |
| B / Y (secondary) | Remove the held tool; with empty hands, cycle the kind (cone → marker → tape post → scale → measuring tape) |
| Left thumbstick forward, release | Teleport along a ballistic arc (green when the landing is valid, red otherwise; refused outside the shop); a comfort fade blinks on arrival |
| Right thumbstick left / right | Snap turn 45° (with comfort fade) |
| Right thumbstick click | Flashlight on the right hand |
| Left menu button | Show / hide the hand hint labels |

If controller poses do not arrive under a future OpenXR version, the fallback is a TrackedPoseDriver (Input System package) on the camera and both hands and an input binder that calls the same HandInteractor/XRLocomotion methods; nothing else changes.

### HandInteractor and XRLocomotion methods

| Method | Effect |
|---|---|
| HandInteractor.Grab() / Release() | Hold the nearest tool or the camera; drop onto the surface below, inside the bounds |
| HandInteractor.Trigger() | Photograph with the held camera; select a held tape post |
| HandInteractor.SpawnIntoHand(kind) / RemoveHeld() | New tool in the hand; remove the held one |
| XRLocomotion.FindDestination(Ray, out Vector3) | Valid floor point under the pointer for a preview marker, or false |
| XRLocomotion.TryTeleport(Ray) / TryTeleport(Vector3) | Move the rig so the head lands over the target; refused outside InteriorBounds or more than MaxStepHeight above the floor |
| XRLocomotion.SnapLeft() / SnapRight() | Turn around the head |

Both adapters derive from ToolHolder, which owns the held tool, the camera hand-off (EvidenceCamera.HoldBy / ReturnToRack) and the release on DemoSession.Resetting. Tools stay under DeploymentRoot while held, so reset and removal keep working. Physical headset movement is not constrained by any of this; only teleport targets are.
