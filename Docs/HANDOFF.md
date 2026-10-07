# Assistant handoff

Read README.md, PROJECT_BRIEF.md, ROADMAP.md and SETUP.md before implementation.

## Objective

Deliver a working instructor-led PC VR jewelry-store demonstration by October 27, 2026, for Quest 2. User works with this assistant and Claude Code.

## Current state

Repository contains design documents and an untested portable camera script. There is no Unity project, imported art, executable build or validated XR configuration yet.

## Next action on the PC

Inspect installed Unity version and any existing project. Record pipeline and XR versions. Create the project under UnityProject and validate Quest head/controller tracking before expanding the scene.

## Working rules

- Keep instructor-led free exploration: no scores, timers, forced task order or automatic judgments.
- Favor simple working interactions and free assets.
- Mark code written and code tested separately.
- Update ROADMAP.md and the verification log after meaningful changes.
- Record third-party asset licenses and acquisition links.
- Preserve asset .meta files and avoid generated Unity folders.
- Keep session reset separate from deletion of saved photographs.
- Coordinate changes through Git commits; pull latest changes before editing.
- Consult installed XR/render-pipeline documentation rather than assuming a package API version.

## Outstanding technical risk

The camera prototype's manual rendering path must be validated against the chosen Unity render pipeline. It has not been compiled or run.
