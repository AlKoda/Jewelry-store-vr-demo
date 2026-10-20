# Jewelry store crime-scene demo

Unity **6000.6.2f1**, Built-in renderer. One saved night-store scene and one Windows player for keyboard/mouse or **Meta Quest 3 over Quest Link/Air Link**. Since October 20 the same project also builds a **standalone Quest 3 app**: double-click **Build-Quest-App.cmd** (see [Docs/QUEST_STANDALONE.md](Docs/QUEST_STANDALONE.md); not yet built).

## Three entry points

- **Play-Keyboard.cmd**: keyboard/mouse, full screen.
- **Play-VR.cmd**: starts OpenXR and enters the night scene when Link is available.
- **Open-In-Unity.cmd**: opens the same project and saved scene for moving/editing objects.

Unity's default Hub installation is detected; set JEWELRY_UNITY to a different Unity.exe path if necessary. A fresh checkout unpacks the single Downloads/JewelryStore_Unity6000.6.2f1.zip snapshot into UnityProject_Demo and syncs source/large asset packs. Keep the complete repository checkout; the compact ZIP deliberately omits large packs. Package Manager must be enabled to resolve the included pinned packages.

Both play launchers accept **run** to use the existing player, **rebuild** to compile the saved scene, or **bake** for explicit scene regeneration and lighting. **Normal rebuild preserves manual object positions. bake regenerates the scene and can replace manual layout edits.** Build output is the complete Builds/Windows folder, not just its EXE. Copy that whole folder to another PC with the appropriate Meta runtime to test without Unity.

Source is authoritative in Prototype's grouped Scripts/Editor folders; preserve every .meta GUID. UnityProject_Demo and Builds are local generated output. The snapshot includes the saved scene, baked data, XR settings and package locks. Library/caches and captured photos are not in Git.

## Current state

October 19: reorganization reconciled with local glass, lighting, Arabic/English UI, audio and scene fixes. OpenXR setup, C# compilation, strict Windows build, existing-scene rebuild and actual-player desktop regression passed. A real Meta OpenXR session reached VISIBLE and FOCUSED. Human Quest 3 hand/comfort/control acceptance and headset profiling are still required.

Start with [VR testing and controls](Docs/VR_TESTING.md), [current handoff](Docs/HANDOFF.md), [open work](Docs/ROADMAP.md) and [assistant rules](AGENTS.md). Licences and source attributions live beside retained packs and are copied beside the player.
