# Working notes for Claude

**Read [AGENTS.md](AGENTS.md) first.** It holds the rules shared with ChatGPT, including "one project: edit it, never fork it", where every kind of file goes, and the who-is-working-on-what table to update before starting. Then read Docs/HANDOFF.md (current state, source map) and Docs/ROADMAP.md (open work).

Claude-specific notes:
- Cloud compile check: Roslyn C# 9 against Unity 2021.1 reference DLLs. The two known errors are NightLighting.cs `lightmapCompression` (a Unity 6 API). It is not a Unity compile; say so in reports.
- Refresh the zip with `python3 -I Tools/zip_snapshot.py`. It writes any missing `.meta` into Prototype; commit those.
- From this cloud environment HTTP reaches only raw.githubusercontent.com, but `git clone --filter=blob:none --depth 1 --no-checkout https://github.com/<owner>/<repo>` followed by `git sparse-checkout set --no-cone <paths>` and `git checkout HEAD` fetches any GitHub repository (used for KayKit and google/filament).
- Pull `main` before editing and before pushing; ChatGPT commits to the same branch.
