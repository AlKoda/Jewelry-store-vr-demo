# Portable fieldwork — local delivery, October 23

The saved JewelryStoreNight scene now has the field table on the left sidewalk,
with an upright carryable case. This replaces the indoor field cart from the
first evidence pass; it is not a second scene or player version.

## What is added

- Rugged hollow polymer case, rubber corners/feet, ribs, hinges, metal latches,
  front carry handle, inset Arabic label and raised lid with physical guide/register.
- Four separate field tools: forceps, swab, modeled fingerprint brush and lift card;
  existing modeled powder jar and eight sealed sample packets.
- Tools detach while held in the other hand; release returns to the case's current
  dock. The case follows only yaw, with no dynamic rigidbody or player forces.
- Solid supporting-surface/corner/clearance checks cover the whole open lid.
  Placement refuses walls, floating positions, low shelves and unsupported edges;
  a safe return to the exterior home table is the fallback when no clear surface exists.
- Two designated developable ridge marks on the actual curved entrance pull bars.
  Partial brushing reveals the pattern progressively. Hold input, bounded reach
  and unobstructed targeting are required. A completed mark can be lifted once.
  Photograph with the existing camera before lifting; no identity inference or
  requirement to simulate laboratory identification.
- Fourteen evidence IDs, eight sample targets in total. Existing UV handle traces,
  physical evidence, document-box interaction and persisted session logs remain.

## Controls and guest demonstration

1. Start at the exterior table, left of the entrance. In VR, grip the case handle,
   carry it to a clear area, release to set it down. PC: G/click handle; point at
   floor/table and click, or R to put it down below the carried position.
2. Take the brush from the partitioned tray. Hold trigger (PC G/left mouse) and
   sweep at the designated pull-bar surface until the mark develops.
3. Return the brush (VR grip release; PC R), photograph the mark with the existing
   camera, then take the lift card and trigger/click to collect. The case register
   updates and a sealed packet appears. Click the register to page through records.
4. Use forceps/swab on the other suitable traces; open the office document box to
   inspect its service receipt. Scene reset restores fieldwork without deleting
   completed JSON records or photo paths.

The case can stay in one hand while the other works. Only the four field tools
are case contents; the established six-slot deployment wheel is preserved.
Existing camera/torch grip offsets and accepted hand poses were not changed.

## Verification

- Unity compilation and actual rendered scene views passed. Powder visibility is
  compared against its undeveloped render; 1,299 changed pixels at the chosen
  threshold in the final close view. This establishes visibility, not realism.
- Edit/play checks exercise actual grip from inside its trigger, desktop pointer
  pickup, two-hand exclusion, free-hand work, moving docks, supported table and
  pavement placement, wall/edge/air/low-shelf rejection, latent/partial/complete
  brushing, occlusion, correct lift method, duplicates and complete session reset.
- Actual Start/event/disk/session-reset and same-hand torch replacement pass.
  Completed JSON records remain after reset; no user records were removed.
- Windows existing-scene build, VR visual, white/UV occluder shadows, existing
  street/glass/car and missing-reference/material/normal/duplicate checks pass.
  See DELIVERY.json for exact log markers, source/GUID parity and output hashes.
- All 21 car transforms/mesh/material references and official ZIP are unchanged;
  PROTECTION.json records comparison against Before-Portable-Kit-20261023.

Authored fieldwork geometry totals 5,606 triangles plus two 12-triangle ridge
patches outside the fieldwork root. The active play hierarchy check reports
5,990, below its 15,000 budget. Case parts are combined by material; mobile tools
reuse existing assets/shaders. No lights or dynamic rigidbodies were added;
movable case parts are excluded from static reflection captures. Fresh bake and
14 reflection captures retain the intended shop/night lighting.

Quest hardware frame time, stereo text legibility and real controller grip fit
remain unverified. No new APK was built or installed in this pass. The current
Windows player serves keyboard and PCVR; standalone packaging remains Claude's
work. The Oman art branch is local-only: no commit/push or official ZIP refresh.
Research sources and design inferences: ../Fieldwork/RESEARCH.md.
