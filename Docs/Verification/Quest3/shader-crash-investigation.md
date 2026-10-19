# VR startup crash investigation

October 19, 2026. Player crash reproduced immediately after eye texture creation and FOCUSED.

The bundled Unity 6000.6.2f1 Windows player PDB resolved the native stack to:

- core::vector<LocalKeywordInfo>::assign
- ShaderRuntime::Variants::VariantUploadData::VariantUploadData
- ShaderRuntime::Variants::ShaderVariantMonitor::FrameFinished
- PostLateUpdateFinishFrameRendering

No managed hand exception was present. This identifies the failing rendering subsystem; it does not establish the exact engine defect. The first mitigation enables keepLoadedShadersAlive in the authoritative build path, avoiding shader object unloading across XR/scene startup. Runtime survival and headset visual acceptance must be checked separately from compilation and rendered hand geometry validation.

Result: strict saved-scene build passed with keepLoadedShadersAlive=1. Koda confirmed that the subsequent VR player stayed running. Hand orientation remained incorrect and is a separate unresolved acceptance item.
