# Latest checkpoint: recording build, October 18, 2026

User resumed work to prepare a supervisor recording. Implemented the screenshot notes: replaced the fracture import path with metre-scale bevelled glass, corrected oversized case shards, replaced broken door glass, layered/readable debris and local reflection/contact shading, removed floating stationery, and enabled filtered 2K flashlight shadows (including the runtime quality override). Start-Latest.cmd now opens Builds/Recording20261018/JewelryStoreDemo.exe. Read [RECORDING_BUILD_20261018.md](RECORDING_BUILD_20261018.md) first. Current editable source is Prototype; tested project is still .asset-cache/combined-20261017/UnityProject_Demo. Earlier pause notes below are historical and have now been acted on. No commit/push; actual headset test still pending.

---

# User review notes — October 17

Implemented in the October 17 combined review build; visual and hardware acceptance remains with the user. Full changes and verification: COMBINED_REVIEW_20261017.md.

Original notes:
- Improve glass in the actual demo, especially shattered panes and floor shards.
- Improve surrounding buildings; their current appearance harms the presentation.
- Fix the empty skybox and add distant fog with a photographic/textured city continuation behind it.
- Repair see-through/missing undersides on interior objects.
- Correct drawer sizes and build real recesses/openings behind pulled drawers.
- Finish shop materials and add coherent interior detail/models.
- Road, people and traffic are lower priority: the road can be closed for investigation.
- Let both entrance leaves open when pushed by the desktop or VR body.
- Make glass, metal, polished wood and other surfaces respond appropriately to the flashlight and car lighting.

Current integration work and validation are in .asset-cache/combined-20261017; preserve the older builds and local art sources.

Additional review requests applied: quieter ambience, recorded glass walking foley, curved fogged city horizon and ground continuation, independent held-torch beam click, camera/torch hand transfer, office/service door body contact, translucent tracked-hand contact on all four doors. Select future Claude assets for suitability rather than including everything.

## Paused at user's request — next session, October 17

Do not implement these until work resumes. User tested the latest build and supplied two screenshots, preserved in ReviewReferences20261017/:

- counter-props-and-shadows.png: repair the malformed/floating stationery and other visibly defective countertop props, or remove them entirely if a convincing repair is not worthwhile. Check mesh transforms, scale, grounding and material/rendering errors before choosing replacement models.
- oversized-transparent-geometry.png: investigate and repair/remove the two enormous translucent jagged vertical objects circled above the display cases. They appear to be incorrectly scaled/oriented fracture geometry; confirm the actual mesh/source before changing it. These objects must not extend through the room or ceiling.
- Improve shadow realism: current flashlight shadows look pixelated/stair-stepped. Review shadow-map resolution, filtering, bias and light range while preserving useful contact shadows and VR performance. Verify in the actual playable build at close range, not only an editor preview.

No scene/code/build changes made for this report. Leave the current build as the checkpoint. On resuming, read these screenshots and COMBINED_REVIEW_20261017.md first. User requests stopping here for now.

### Additional next-session request: replace broken-glass entrance doors

Replace the current broken-glass door models; the user finds their fracture shape unconvincing. Either author better door/glass geometry or source an appropriately licensed model online that resembles an actual shattered glass storefront door. Prioritize believable fracture patterns, remaining glass held in the frame, glass thickness and correctly scaled debris. Preserve the working hinges, body/controller-hand pushing, doorway clearance and reset behavior when replacing visuals. Review the replacement in the playable build. Note only for now; no implementation until the user resumes work.

### Glass appearance target supplied by user

Reference: ReviewReferences20261017/approved-glass-appearance-target.png. The user strongly prefers this previously shown presentation: believable fractured panes in frames, accumulated/layered glass debris at the base, visible shard edges, reflections and convincing grounding/contact shadows. The playable build currently falls short of this reference. Treat this image as the visual acceptance target for replacing/improving the broken doors and glass debris. Investigate the difference between the reference presentation and Unity's actual mesh transforms, materials, reflection probes and lighting; do not assume that matching geometry alone reproduces the appearance. Compare close-up screenshots from the actual playable build under the intended store lighting and flashlight, rather than approving an isolated Blender/material preview. Preserve credible scale, clear doorway access, working door interactions and performance. Note only; work remains paused until the user resumes.
