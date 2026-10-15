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
| Show / hide a presenter set (alarm bell, ambient wear, case evidence) | PresenterSet.Toggle() or SetShown(bool); PresenterSet.All lists the sets in the scene |
| Light bar flashing / steady | DemoLighting.ToggleLightBarFlashing() (DemoLighting.LightBarFlashing reads it) |
| Underscore music on / off | DemoMusic.Instance.ToggleBed() (only when DemoMusic.Instance.Bed is set) |

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
| 1–6 | New cone, marker, tape post, L-scale, measuring reel or scene lamp straight into the hand |
| Q / E, mouse wheel | Rotate the held tool in 15° steps |
| T | On a held or pointed scene lamp, switch it on/off (at most two lit, the oldest goes out); otherwise SelectTapePost on the held or pointed post or measuring reel (same-kind pairs connect) |
| X | CancelTapeSelection |
| R / Delete / Backspace | Remove the held or pointed tool |
| F | Hold the evidence camera in front of the view / return it to the rack |
| P | CapturePhoto |
| Home | Return to the start position |
| I | Intact / robbed store view (CrimeSceneState; also a panel button) |
| N | Night / day lighting preset; in the night store: diagnostic daylight on/off |
| L | White flashlight on the view (DemoLighting) |
| U | UV torch (exclusive with the white flashlight; night store) |
| B | Instructor beacon at the pointed spot (DemoBeacon; also click the panel's overview map) |
| C | Crouch / stand (eye 0.95 m, half speed) — October 13, unverified |
| Z (hold) | Zoom to 30°, slower look — October 13, unverified |
| F1 / H | Controls overlay (mouse and keyboard page, Quest 2 page); pauses walking like the drawer |
| F2 | Hide / show the HUD (chips, hotbar, prompts, toasts) while this map is open; reopening it (F10) or a new launch starts with it on |
| Esc | Release the cursor; also closes the controls overlay or the drawer |
| F10 | Back to the start menu (built program) |
| Tab | Cycle the presenter panel: hidden → strip → drawer (DemoDesktopPanel) |
| [ / ] | Interface size x1–x2 (persisted; also the Settings tab) |

The number pad's 1–6 work like the main row. The hotbar at the bottom (tools 1–6, camera F, flashlight L, UV U) is clickable; the slot of the held tool or active light is outlined in brass and each tool slot shows how many are placed.

### Desktop interface (October 13, unverified)

- HUD (hidden layout): chips top left (mode, night, torches, crouch/zoom, tools placed and next marker, photos, pending tape step), the hotbar, a crosshair with the target's name while mouse look is on (brass when there is something to act on), a tooltip beside the cursor otherwise, key prompts for the current situation above the hotbar with a warning line when placement is impossible, toasts top right for every action (DemoFeedback; the headset toast repeats them except presenter-only ones: interface size, HUD on, settings restored), the camera's live view with thirds while it is held, a white flash on each photo and a banner while a reset is armed.
- Strip: status chips and Photo / Robbed-Intact / Reset / Panel buttons with the last photo and live view.
- Drawer (right, 420 wide): Tools (new-tool tiles, tape options, placed tools with Remove), Camera (status, photo/hold/folder, live view, last photo, instructor review), Map (overview, click to place the beacon, legend), Scene (robbed/intact, night/day, flashlight, UV torch, UV examples, light bar flashing, scene dressing sets, underscore music, beacon, reset; see below), Settings (interface size, HUD, prompts, look sensitivity, field of view, invert Y, restore defaults, credits).
- Settings persist in PlayerPrefs (DemoPanelScale, DemoShowPrompts, DemoPanelTab, DemoLookSensitivity, DemoInvertY, DemoFieldOfView). F2 (hide the HUD) is not saved: every launch, and every map opened from the start menu, starts with the HUD on, and the DemoHudVisible value older builds saved is deleted (October 15, unverified).

### Scene-tab switches (October 15, unverified, night store only)

The drawer's Scene tab (Tab, Scene) gains presenter switches. They have no key and no headset control, and their toasts show on the monitor only. A session reset returns the light bar and the scene-dressing sets to their arrival state; the underscore music keeps its setting. The old shop has none of them, so its Scene tab is unchanged.

| Switch | Section | At arrival | Effect |
|---|---|---|---|
| Light bar flashing | LIGHTING, when the police car has a light bar | On | Off holds both halves steady and dim and turns off the red and blue washes on the street and the shop floor, for visitors sensitive to flashing light |
| Alarm bell | SCENE DRESSING | Off | The alarm bell rings from the bell box on the fascia, muffled inside the shop; whether it rings at arrival is Koda's decision (listed only when the NightSoundscape bell clip is present) |
| Ambient wear | SCENE DRESSING | Off | Water stain, heel scuffs, fine scratches and wall-base dust (listed only when the SurfaceDecals pack is present) |
| Case evidence | SCENE DRESSING | Off | Placeholder shoe trails, pry marks, latent prints, a handprint and three discarded tools (Koda's decisions 2 and 5) |
| Underscore music | MUSIC, when the Music pack is present | Off | Fades the investigation bed in or out. A session reset plays a short sting either way and leaves the switch as it is (the bed ducks under the sting) |

SCENE DRESSING lists one switch per PresenterSet in the scene, sorted by label, each with its note underneath.

Also new (October 15, unverified):
- Footsteps sound while the desktop visitor walks (one per 0.7 m, glass steps in the glass debris zones). In VR they stay silent: room-scale walking does not move the rig, and teleports and snap turns are skipped.
- On the start menu, a second key press or click while a map loads is ignored. The menu music fades out over 0.6 s before the map loads. Esc quits even when no map is listed.

Walls, the storefront barrier and InteriorBounds keep the player inside. InteriorBounds.Contains/Clamp are the shared definition of the interior; use them to filter XR teleport destinations.

DemoSession.Resetting fires before anything is cleared; adapters release held objects there so parents and poses restore correctly. The desktop adapter does this; an XR adapter must do the same.

## One player, two modes (DemoModeSwitch)

The Player object carries both the desktop components (ShopWalkController, DesktopInteractor) and the VR ones (XRHeadTracking on the camera, LeftHand/RightHand with HandInteractor + XRControllerInput). DemoModeSwitch enables one set: VR when a headset is active at start (XRSettings.isDeviceActive), desktop otherwise. **F9** flips between them at any time, so the demo can be tested with mouse and keyboard without unplugging anything. The presenter panel stays on the monitor in both modes.

## VR controls (XRControllerInput, package-free)

Poses and buttons come from Unity's built-in XR input API (UnityEngine.XR.InputDevices), which the OpenXR runtime feeds once XR Plug-in Management and the OpenXR plugin are installed and enabled. No toolkit, Input System or TrackedPoseDriver is required.

| Controller | Action |
|---|---|
| Grip (either hand) | Hold the nearest tool within 25 cm, else take a new one from a rack sample, else the camera; release puts it down on the surface beneath |
| Trigger | Photograph while holding the camera; switch a held scene lamp on/off (at most two lit, the oldest goes out); select a held tape post or measuring reel |
| A / X (primary) | New tool of the hand's current kind, straight into the hand |
| B / Y (secondary) | Remove the held tool; with empty hands, cycle the kind (cone → marker → tape post → scale → measuring tape → scene lamp) |
| Left thumbstick forward, release | Teleport along a ballistic arc (blue with a ring marker when the landing is valid, red otherwise; refused outside the shop, with a strong vibration on an invalid release); a comfort fade blinks on arrival |
| Right thumbstick left / right | Snap turn 45° (with comfort fade) |
| Right thumbstick click | Flashlight on the right hand |
| Left menu button | Show / hide the controller hints on both hands |
| Left thumbstick click | UV torch in the left hand |

### Headset interface (XRHandHud, October 13, unverified)

- Each hand shows a Quest 2 style controller model; a button turns brass while pressed.
- Hints sit beside the buttons with leader lines: shown for the first 20 seconds, afterwards when you look at the controller (within 30° and 0.8 m). The A/X hint names the tool that button spawns.
- B/Y with an empty hand shows a carousel of the six tools above the controller for 2.5 seconds, the new one enlarged and named.
- Look at the left wrist for the scene status: tools placed, next marker, photos, torches, the pending tape step.
- Messages from every action float briefly below your view (the same ones the monitor shows).
- Vibrations: grab, spawn, place, photo, teleport, select, and the strongest for a refusal (XRControllerInput.Pulse).

If controller poses do not arrive under a future OpenXR version, the fallback is a TrackedPoseDriver (Input System package) on the camera and both hands and an input binder that calls the same HandInteractor/XRLocomotion methods; nothing else changes.

### HandInteractor and XRLocomotion methods

| Method | Effect |
|---|---|
| HandInteractor.Grab() / Release() | Hold the nearest tool or the camera; drop onto the surface below, inside the bounds |
| HandInteractor.Trigger() | Photograph with the held camera; switch a held scene lamp; select a held tape post or reel |
| HandInteractor.SpawnIntoHand(kind) / RemoveHeld() | New tool in the hand; remove the held one |
| XRLocomotion.FindDestination(Ray, out Vector3) | Valid floor point under the pointer for a preview marker, or false |
| XRLocomotion.TryTeleport(Ray) / TryTeleport(Vector3) | Move the rig so the head lands over the target; refused outside InteriorBounds or more than MaxStepHeight above the floor |
| XRLocomotion.SnapLeft() / SnapRight() | Turn around the head |

Both adapters derive from ToolHolder, which owns the held tool, the camera hand-off (EvidenceCamera.HoldBy / ReturnToRack) and the release on DemoSession.Resetting. Tools stay under DeploymentRoot while held, so reset and removal keep working. Physical headset movement is not constrained by any of this; only teleport targets are.
