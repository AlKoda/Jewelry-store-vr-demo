# Next-session feedback — October 23, 2026

Koda requested **notes only** on October 23; later the same day Koda asked Claude to optimise and code everything, so the door, case, floor, tape and lamp items below are now **coded in Prototype (unverified: cloud compile only)**, see Docs/HANDOFF.md. The sandbox direction and the glass visibility review remain open. Earlier automated checks do not establish that these interactions or visuals are acceptable in the headset.

## Governing direction: an unguided forensic sandbox

- [ ] Remove the added quests/tasks, guided hints, required progression and restrictions. Teaching and assistance come from outside the project. Free exploration and investigation are the intended experience.
- [ ] Make tools work on every appropriate compatible object or trace, rather than only a few designated targets. For example, powder and the fingerprint brush should work on any suitable fingerprint, with the other forensic tools following the same principle.
- [ ] Before implementing further forensic mechanics, research how real scenes are secured, examined and processed, and use that practice to inform tools, evidence and interactions. Research is deferred with the implementation; it was not performed for this notes batch.

The sandbox request supersedes the recent guided survey/task direction and any assumption that designated clue targets are sufficient. An evidence record must not become a checklist that directs or gates the player's investigation.

## Doors and glass

- [ ] Broken glass remaining in a door must move with that door. Koda reports that the current shattered glass stays behind.
- [ ] Give every door eased, damped movement. Touching, pushing or trying to examine a door must not make it fly open abruptly; cover body and controller contact.
- [ ] Rework the visibility and realism of intact display-case and storefront glass. Koda reports that both are effectively invisible in the current build despite the previous material pass. Judge the result in the actual build/headset, not from opacity numbers or an isolated render alone.

Retain the door's accepted visual design while addressing these reported movement and visibility failures.

## Portable tool case and placement

- [ ] Fix competition between the case carry handle and the individual tool pickup areas. The handle is too close to tools, or its selection area is too large, so most tools cannot be picked up reliably.
- [ ] Allow the case to be placed on any suitable supporting surface, including display-case tops/glass panels. Keep supported placement and prevent floating or clipping; do not limit it unnecessarily to the floor or its original table.

## VR floor and reach

- [ ] Correct the floor/height alignment. Koda reaches the physical floor before the virtual hand reaches the in-game floor, making evidence gathering impossible.
- [ ] Diagnose tracking origin, floor calibration, world scale and any player/camera offsets before choosing a correction. Verify that a real crouch and hand-to-floor reach align; another arbitrary height increase is not the requested fix.

## Continuous crime-scene tape

- [ ] Repair unreliable tape connections and implement this workflow:
  1. Place the first post/endpoint. A new endpoint appears in hand, already linked to it.
  2. Place the held endpoint. That segment stays connected, and another linked endpoint appears in hand. Continue for as many segments as needed.
  3. Remove/cancel the endpoint in hand to finish the run. Already placed tape remains intact.
  4. The next run starts independently; it must not connect automatically to the finished run.
  5. Bring a new line/endpoint close to a placed endpoint to snap to it and offer a deliberate connection when placing. Joining existing runs is optional, not forced.

## Scene lamps

- [ ] Every placed lamp must remain illuminated. Koda again reports that placing another turns the previous lamp off. The request supersedes the current two-lit-lamp cap; allowing only the newest lamp(s) to illuminate is not acceptable.
- [ ] Normal grab moves the whole lamp.
- [ ] Using the use/place control near the pole adjusts its telescoping height up or down instead of moving the entire lamp. Keep those two actions distinct and easy to select.

## General follow-through

- [ ] Refine mechanical reliability, selection areas, contact behavior and interaction consistency around these changes. Prioritize the reported blockers before adding more guided content.
- [ ] Verify the resulting interactions in the actual headset, including floor reach, door contact, tool selection, raised-surface case placement, tape continuation/join/cancel and several simultaneously illuminated lamps.

No runtime code, models, shaders, saved scene, player, APK or official ZIP was changed for this notes-only request. The Oman preview remains local; Koda authorized GitHub and snapshot promotion on October 23 so Claude can continue.

