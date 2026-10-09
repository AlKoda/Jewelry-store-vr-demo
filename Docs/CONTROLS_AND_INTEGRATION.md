# Controls and integration contract

The instructor has unrestricted use. These are methods to connect through UI/XR events, not a prescribed training sequence.

| Action | Entry point |
|---|---|
| New cone | ToolStation.SpawnCone() |
| New numbered marker | ToolStation.SpawnMarker() |
| New tape post | ToolStation.SpawnTapePost() |
| Select first/second tape endpoint | ToolStation.SelectTapePost(DeployedTool) |
| Cancel pending tape | ToolStation.CancelTapeSelection() |
| Remove an existing tool | ToolStation.RemoveTool(DeployedTool) |
| Place/reposition | DeployedTool.PlaceAt(Vector3, float), or XR grabbing |
| Photograph | EvidenceCamera.CapturePhoto() |
| Open photo folder on Windows | EvidenceCamera.OpenPhotoFolder() |
| Reset session | DemoSession.ResetSession() |

UnityEvents: ToolsChanged, SessionReset, PhotoSaved(string path), CaptureFailed(string message) and Shutter.

## Ownership

All deployed tools and ribbons belong under DeploymentRoot. Keep its scale at (1,1,1). XR grabbing must retain or restore that ownership; otherwise reset and station removal will not find reparented objects. Prevent reset while tools are actively held, or release XR selections first in the future adapter.

Static evidence belongs under FixedEvidence and is never deleted by tool reset. Add ResettableSceneObject and register it with DemoSession if a scene prop becomes movable. Registered resettable objects must retain their original parent. Camera filenames continue across reset.

## Desktop adapter (DesktopInteractor)

Pointer = mouse, or the screen centre while the right button is held for looking. Placement only lands on upward-facing surfaces inside InteriorBounds; otherwise the held tool stays put.

| Input | Action |
|---|---|
| WASD / arrows, Shift | Walk, run (ShopWalkController) |
| Hold right mouse | Look |
| Left click | Pick up the pointed tool / place the held tool / pick up the camera |
| 1, 2, 3 | New cone, marker, tape post straight into the hand |
| Q / E, mouse wheel | Rotate the held tool in 15° steps |
| T | SelectTapePost on the held or pointed post |
| X | CancelTapeSelection |
| Delete / Backspace | Remove the held or pointed tool |
| F | Hold the evidence camera in front of the view / return it to the rack |
| P | CapturePhoto |
| Home | Return to the start position |
| Tab | Show / hide the presenter panel (DemoDesktopPanel) |

Walls, the storefront barrier and InteriorBounds keep the player inside. InteriorBounds.Contains/Clamp are the shared definition of the interior; use them to filter XR teleport destinations.

DemoSession.Resetting fires before anything is cleared; adapters release held objects there so parents and poses restore correctly. The desktop adapter does this; an XR adapter must do the same.

## XR adapter (HandInteractor and XRLocomotion, package-free)

One HandInteractor per hand on a transform that a tracked pose driver moves. One XRLocomotion on the rig root with Head set to the XR camera. Bind controller actions to these methods; nothing else is toolkit-specific.

| Controller action | Method |
|---|---|
| Grip pressed | HandInteractor.Grab() — nearest tool within GrabRadius, else the camera |
| Grip released | HandInteractor.Release() — drops the tool onto the surface below, inside the bounds |
| Trigger | HandInteractor.Trigger() — photograph while holding the camera; select a held tape post |
| Tool menu choice | HandInteractor.SpawnIntoHand(kind) |
| Remove | HandInteractor.RemoveHeld() |
| Teleport confirm | XRLocomotion.TryTeleport(Ray) or TryTeleport(Vector3) — refused outside InteriorBounds |
| Thumbstick left / right | XRLocomotion.SnapLeft() / SnapRight() |

Both adapters derive from ToolHolder, which owns the held tool, the camera hand-off (EvidenceCamera.HoldBy / ReturnToRack) and the release on DemoSession.Resetting. Tools stay under DeploymentRoot while held, so reset and removal keep working.

Physical headset movement is not constrained by any of this; only teleport targets are.

## Proposed controller behavior

Grip: grab/reposition. Camera trigger: capture while held. Tool menu: spawn a selected tool near the hand/station. Tape-post action: select endpoint. Teleportation and snap turn: follow toolkit defaults once installed.

Exact buttons, handedness and HUD placement remain to validate on Quest. Do not hard-code a toolkit API until the package version is known.
