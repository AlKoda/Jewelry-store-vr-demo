# Performance audit plan (October 14, verified findings)

# Implementation plan: confirmed performance and realism fixes (8 findings)

**Scope.** The input was cut off partway through R2's second vote. This plan covers the 8 findings I received in full or nearly in full: R1, R2 and CPU-GC-1 to CPU-GC-6. If more findings came after R2, I did not get them.

**Impact order.** The ratings below are the skeptics' corrected ratings, not the finders':

| Order | Finding | Impact |
|---|---|---|
| 1 | R1 | high |
| 2 | R2 | high |
| 3 | CPU-GC-3 | low-medium (VR spike) |
| 4 | CPU-GC-2 | low-medium (VR render thread) |
| 5 | CPU-GC-1 + CPU-GC-5 (DemoUi part) | low-medium |
| 6 | CPU-GC-5 + CPU-GC-6 (DemoDesktopPanel part) | low to low-medium |
| 7 | CPU-GC-5 (DeployedTool, DemoLauncher) | low |
| 8 | CPU-GC-4 | low-medium, once per photo |

**Groups.** Each file belongs to exactly one group, so the groups can be applied in any order without conflicts. One exception: DemoDesktopPanel.cs needs DemoUi.Content and DeployedTool.Title from groups E and G, so apply E and G before F. No new files are created, so no new .meta files are needed.

**Checked against the code.** HEAD is `23dc3eb` and was up to date with main. Every line number below was checked against it.

---

## A. R1: what lighting the build ships (high)
Files: NightStoreBuilder.cs, DemoBuild.cs, NightLighting.cs, DemoLighting.cs, DemoToolGeometry.cs, Start-Demo.cmd

**What was dropped from the finder's fix:**
- `bool baked = bake && NightLighting.Bake(out string report); if (bake) Debug.Log(report);` does not compile (CS0165: `report` is not definitely assigned).
- Making `bake` the default in Start-Demo.cmd is a product decision. It would run a bake path that has never been run on every pull, adding minutes each time. It is deferred until RunNight passes and the bake time has been measured.

### A1. Prototype/Editor/NightStoreBuilder.cs, `BuildScene` (lines 86-104)

Change it to return bool and to fall back to the preview whenever the bake fails. Use the same success test as NightStoreValidation lines 64-66.

```csharp
// Returns true when the scene ships baked lighting. A failed or skipped bake falls back to
// the realtime preview, so a build never ships baked-only lights without lightmaps (black).
public static bool BuildScene(bool bake, bool askToSave = true)
{
    if (EditorApplication.isPlaying || (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())) return false;
    NightLighting.UseLinearColorSpace();
    ThirdPartyModelImport.GenerateLightmapUVs();
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    Camera camera = DemoValidation.CreatePreviewCamera(150);
    GameObject root = Build(true);
    NightLighting.Apply(root.transform, camera);
    DemoValidation.SaveScene(L.SceneName, false);
    bool baked = false;
    if (bake)
    {
        baked = NightLighting.Bake(out string report) && LightmapSettings.lightmaps.Length > 0;
        Debug.Log(report);
        if (!baked) { Debug.LogWarning("JEWELRY_BUILD_LIGHTING bake failed; using the realtime preview."); Lightmapping.Clear(); }
    }
    if (!baked) NightLighting.UseRealtimeFallback(root.transform);
    // The rig is created after the bake so it never enters it (as in the validator).
    CreatePlayer(camera);
    NightLighting.ConfigureCamera(camera);
    DemoValidation.SaveScene();
    Debug.Log("JEWELRY_BUILD_LIGHTING " + (baked ? "baked" : "preview"));
    return baked;
}
```

The menu items on lines 35 and 38 (`{ BuildScene(false); }`) still compile unchanged.

### A2. Prototype/Editor/DemoBuild.cs

- In `BuildLatestReport` (line 68), replace the call with `bool baked = NightStoreBuilder.BuildScene(bake, false);`. Change line 72 to `WriteBuildInfo(bake, baked);`.
- Replace `WriteBuildInfo` (lines 101-109) so the start menu footer reports the real lighting mode:

```csharp
private static void WriteBuildInfo(bool bake, bool baked)
{
    Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoAsset));
    string commit = Environment.GetEnvironmentVariable("JEWELRY_COMMIT");
    if (string.IsNullOrEmpty(commit) || commit == "unknown") commit = GitCommit();
    string lighting = baked ? "baked" : bake ? "preview (the bake failed, see build-latest.log)" : "preview (run Start-Demo.cmd bake for baked lighting)";
    File.WriteAllText(BuildInfoAsset, "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   ·   source " + commit + "   ·   lighting " + lighting);
    AssetDatabase.ImportAsset(BuildInfoAsset);
}
```

### A3. Prototype/Editor/NightLighting.cs, `UseRealtimeFallback` (line 438)

- Change `light.renderMode = LightRenderMode.ForcePixel;` to `light.renderMode = LightRenderMode.Auto;`.
- Update the header comment (lines 423-426): the headlights are now Auto, so the torch and lamps keep their per-pixel slots in the preview.
- UseBaked already resets every light to Auto.

### A4. Prototype/Scripts/DemoLighting.cs, `Create` (line 341)

After `light.type = LightType.Spot;` add:

```csharp
light.renderMode = LightRenderMode.ForcePixel; // a handheld light always keeps a pixel slot (pixelLightCount 4)
```

This covers both the flashlight and the UV torch.

### A5. Prototype/Scripts/DemoToolGeometry.cs, `Lamp` (after line 178, `spot.type = LightType.Spot;`)

Add `spot.renderMode = LightRenderMode.ForcePixel;`. The generated lamp prefabs pick this up the next time they are regenerated.

### A6. Start-Demo.cmd (optional, after A1 to A5): remember the bake choice instead of making it the default

1. Add after line 16:
   ```
   set "LIGHTING=%ROOT%Builds\Windows\lighting.txt"
   ```
2. Replace lines 66-67:
   ```
   set "BAKEFLAG="
   set "REMEMBERED="
   if exist "%LIGHTING%" set /p REMEMBERED=<"%LIGHTING%"
   if /i "%REMEMBERED%"=="bake" set "BAKEFLAG=-demoBake"
   if /i "%MODE%"=="bake" set "BAKEFLAG=-demoBake"
   if /i "%MODE%"=="rebuild" set "BAKEFLAG="
   if defined BAKEFLAG echo Baked lighting: the bake adds several minutes, much longer without a supported GPU.
   ```
3. After line 73 (`>"%STAMP%" echo %COMMIT%`):
   ```
   if /i "%MODE%"=="bake" (>"%LIGHTING%" echo bake)
   if /i "%MODE%"=="rebuild" if exist "%LIGHTING%" del "%LIGHTING%"
   ```
4. Usage comments on lines 5-7:
   - default: "build when the source changed, with the lighting last chosen"
   - `bake`: "... and keep baking on later updates"
   - `rebuild`: "rebuild now with preview lighting and stop baking"
5. Update README.md line 5 and the HANDOFF Oct-12 entry to match.

The `:fail` filter already matches the `JEWELRY_BUILD` prefix, so the `JEWELRY_BUILD_LIGHTING` line shows up there.

### Validator impact (A1 to A5)

- No validator calls BuildScene, DemoBuild or the launcher.
- No validator reads `renderMode`. The only use is ShopInteriorFinish line 120, which sets fixture lights.
- "Night store realtime light budget" counts lights, not render modes, so it is unchanged (3/3/3, one shadow).
- Unchanged: "Only the flashlight casts realtime shadows" (shadows, intensity and range only), "Night quality settings applied", "Streetlights and moonlight are baked", and "Lighting uses at most four shadowless lights".
- RunNightQuick still calls UseRealtimeFallback directly.
- In the baked build nothing renders differently: at most 3 handheld lights, which fits within 4.

---

## B. R2: Prototype/Shaders/CrimeSceneTape.shader (high)

Replace the whole file with a lit surface shader. It keeps the same name, the same properties, the same `_Span` property block (set by SceneTape line 57) and the same sag and stripe maths. Each `#pragma` goes on its own line: the finder's one-line version does not compile.

```
Shader "CrimeScene/Tape"
{
 Properties { _Color ("Yellow", Color) = (1,0.78,0.03,1) _Span ("Metres", Float) = 1 }
 SubShader
 {
  Tags { "RenderType"="Opaque" }
  CGPROGRAM
  // Lit vinyl: base pass, ForwardAdd (flashlight, lamps), light probes, fog, shadows and
  // single-pass instanced stereo come from the surface shader. A runtime-only object, so
  // no lightmap, meta or deferred variants.
  #pragma surface surf Standard vertex:vert addshadow nolightmap nodynlightmap nodirlightmap nometa exclude_path:deferred exclude_path:prepass
  #pragma target 3.0
  fixed4 _Color; float _Span;
  struct Input { float2 stripe; };
  void vert(inout appdata_full v, out Input o)
  {
   UNITY_INITIALIZE_OUTPUT(Input, o);
   float height = v.vertex.y;
   // Sag: up to 3.5 cm at mid-span (object y is scaled by the 8 cm ribbon height).
   v.vertex.y -= (1 - 4 * v.vertex.z * v.vertex.z) * min(.035, _Span * .012) / .08;
   o.stripe = float2(v.vertex.z * _Span, height);
  }
  void surf(Input i, inout SurfaceOutputStandard o)
  {
   float band = abs(i.stripe.y) < .28 ? 1 : step(.28, frac(i.stripe.x * 4 + i.stripe.y * .25));
   o.Albedo = lerp(fixed3(.025, .025, .018), _Color.rgb, band);
   o.Metallic = 0;
   o.Smoothness = 0.3;
   // Optional, if the instructor wants the tape faintly readable in the dark: o.Emission = o.Albedo * 0.02;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
```

Notes:
- Do not add `noforwardadd`; the torch and lamp lighting needs that pass.
- `addshadow` puts the sag into the shadow caster.
- The normals were checked: FlexibleRibbon's diagonal corner normals, under the (0.004, 0.08, L) scale, come out as the broad faces.
- The daytime shop's PoliceTape.mat (ShopGlassRefinement line 149) is now shaded too.
- The black CRIME SCENE text (WorldText) stays black.

**Affects:** GlassToolValidation "Shader compiles: CrimeScene/Tape" and NightStoreValidation line 829 "Night tool prefabs are styled (...)" (shader name only). Both still pass. The DemoValidation tape checks are geometric only.

---

## C. CPU-GC-3: Prototype/Scripts/DemoOverviewMap.cs (low-medium; VR spike every 0.25 s while the drawer is open)

I used the second skeptic's version: put the tab test into the predicate, so a validator can check it through the public API.

Line 19-20, before:
```csharp
// Only the full sidebar shows the map; the strip and hidden layouts skip the render.
public bool ShouldRender => panel != null && panel.isActiveAndEnabled && panel.Layout == PanelLayout.Full;
```
After:
```csharp
// Only the full drawer's Map tab shows the map; every other layout and tab skips the render.
public bool ShouldRender => panel != null && panel.isActiveAndEnabled && panel.Layout == PanelLayout.Full && panel.Tab == PanelTab.Map;
```

- `Update` and `Render` stay unchanged.
- Optional micro-change: add `overview.allowHDR = false; overview.allowMSAA = false;` in Awake. The map texture is LDR at 256 px and nothing checks these settings.
- Dropped: the save and restore of `QualitySettings.shadowDistance` around Render. Its premise, sun shadow cascades, does not exist: the daytime sun is forced shadowless (ShopInteriorFinish line 115) and the night lights are baked. Revisit only if the Frame Debugger shows OverviewCamera shadow passes with lamps lit in the daytime shop.
- Known and harmless: on the first frame after switching to the Map tab, the previous map image (or an empty texture) shows for one frame.

**Affects:** ShopRobberyDressing "Overview rendering resumes when menu opens" would now fail, because Tab comes from PlayerPrefs. Section H changes its setup. "Hidden overview skips automatic rendering" still passes. MapChecks and BackdropChecks call `map.Render()` directly and are unaffected.

---

## D. CPU-GC-2: Prototype/Scripts/XRHandHud.cs (low-medium; 55 transparent draws per frame in VR)

1. In `Tip` (lines 39-47), add `public Renderer KeyRenderer, ActionRenderer;`. In `AddTip`, after `tip.Action` is set (after line 237), add:
   ```csharp
   tip.KeyRenderer = tip.Key.GetComponent<Renderer>();
   tip.ActionRenderer = tip.Action.GetComponent<Renderer>();
   ```
2. Add a field `private bool tipsRendering = true;` and a helper:
   ```csharp
   // Faded-out hints stop rendering, as the wrist card does (55 transparent renderers otherwise).
   private void SetTipsRendering(bool on)
   {
       if (on == tipsRendering) return;
       tipsRendering = on;
       foreach (Tip tip in tips)
       {
           tip.Panel.enabled = tip.KeyPanel.enabled = tip.Line.enabled = on;
           tip.KeyRenderer.enabled = tip.ActionRenderer.enabled = on;
       }
   }
   ```
3. Replace `ShowHints` (lines 277-285). It must still switch the renderers at once, because ShopCityValidation reads them in the same frame:
   ```csharp
   public void ShowHints(bool visible)
   {
       hintsShown = visible;
       appliedTipAlpha = -1;        // the next LateUpdate re-applies alpha and visibility
       tipsRendering = !visible;    // force the write
       SetTipsRendering(visible);
   }
   ```
4. In `ApplyTipAlpha` (lines 287-299), add right after `appliedTipAlpha = alpha;`:
   ```csharp
   bool visible = hintsShown && alpha > 0.001f;
   SetTipsRendering(visible);
   if (!visible) return;
   ```
   The existing colour loop stays as it is.
5. Line 482: `if (hintsShown) appliedTipAlpha = -1;` becomes `if (hintsShown && tipAlpha > 0) appliedTipAlpha = -1;`.

Use `renderer.enabled`. Do not use SetActive on tip.Root: an inactive object still reports `Renderer.enabled == true`, so the hint toggle check could not see the hints hide.

**Affects:**
- ShopCityValidation "Hand hints toggle together" (lines 184-190) reads the A/X Label renderer, which is `tip.Action`. It still passes because ShowHints sets the renderers synchronously. (The finder's risk note missed this check.)
- DemoValidation InterfaceChecks: "Controller tooltips and status card build on both hands" and "Controller interface updates a frame and counts a new tool". The LateUpdate name and signature are unchanged.
- XRControllerInput.ApplyHints only touches the Label when the hud is null, which never happens.

---

## E. CPU-GC-1, plus the DemoUi part of CPU-GC-5: Prototype/Scripts/DemoUi.cs

Do not add `using System;`: `Object.FindFirstObjectByType` on line 44 would become ambiguous.

### E1. Value-keyed texture caches

Replace line 64 (`textures`) with:
```csharp
// Keyed by value: Color is IEquatable<Color>, so lookups neither box nor allocate
// (string keys formatted every colour on every IMGUI event).
private static readonly Dictionary<(int, Color, Color, int), Texture2D> rounded = new Dictionary<(int, Color, Color, int), Texture2D>();
private static readonly Dictionary<(int, int, Color), Texture2D> rings = new Dictionary<(int, int, Color), Texture2D>();
private static readonly Texture2D[] icons = new Texture2D[System.Enum.GetValues(typeof(UiIcon)).Length];
```

Then:
- **Rounded** (lines 69-70, 86): `var key = (radius, fill, border, borderWidth); if (rounded.TryGetValue(key, out Texture2D cached) && cached != null) return cached;` and finish with `rounded[key] = texture;`.
- **Ring** (lines 95-96, 112): the same pattern with `(radius, thickness, color)` and `rings`.
- **Icon** (lines 122-123, 135):
  ```csharp
  int slot = (int)icon;
  bool cacheable = slot >= 0 && slot < icons.Length;
  if (cacheable && icons[slot] != null) return icons[slot];
  ```
  and finish with `if (cacheable) icons[slot] = texture;`.

This also fixes a latent key collision: `Ring(16,5)` and `Ring(1,65)` currently produce the same string key.

### E2. Box and BoxStyle (lines 247-262)

```csharp
public static void Box(Rect rect, int radius, Color fill, Color border)
{
    // Drawn on Repaint only, so Layout and input events skip the style lookup.
    if (Event.current.type != EventType.Repaint) return;
    BoxStyle(radius, fill, border).Draw(rect, false, false, false, false);
}
private static readonly Dictionary<(int, Color, Color), GUIStyle> boxes = new Dictionary<(int, Color, Color), GUIStyle>();
private static GUIStyle BoxStyle(int radius, Color fill, Color border)
{
    var key = (radius, fill, border);
    if (boxes.TryGetValue(key, out GUIStyle style) && style.normal.background != null) return style;
    style = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
    style.normal.background = Rounded(radius, fill, border);
    boxes[key] = style;
    return style;
}
```

### E3. Cached text (CPU-GC-5)

I used the skeptic's immutable content cache instead of the finder's shared mutable `Temp()`. In Unity 6 a GUIContent text change and `GUI.Label(Rect, string, …)` each copy the string; a cached GUIContent avoids that and cannot be clobbered by another caller.

```csharp
// One immutable GUIContent per distinct string: stable labels measure and draw without allocating.
private static readonly Dictionary<string, GUIContent> contents = new Dictionary<string, GUIContent>(256);
public static GUIContent Content(string text)
{
    if (text == null) text = string.Empty;
    if (contents.TryGetValue(text, out GUIContent content)) return content;
    if (contents.Count >= 512) contents.Clear(); // toasts and counters stay few; bounded anyway
    contents[text] = content = new GUIContent(text);
    return content;
}
private static readonly Dictionary<string, string[]> keyParts = new Dictionary<string, string[]>();
private static string[] KeyParts(string keys)
{
    if (!keyParts.TryGetValue(keys, out string[] parts)) keyParts[keys] = parts = keys.Split('/');
    return parts;
}
```

### E4. Hint helpers (lines 405-435)

`HintWidth` and `DrawHints` take `IReadOnlyList<string> pairs`. `string[]` callers compile unchanged. Use index loops only (`pairs.Count`).

```csharp
string[] keys = KeyParts(pairs[i]);
for (int k = 0; k < keys.Length; k++)
{
    GUIContent key = Content(keys[k]);
    Vector2 size = s.Keycap.CalcSize(key); size.x = Mathf.Max(size.x, 24);
    GUI.Label(new Rect(x, rect.y + (rect.height - size.y) / 2, size.x, size.y), key, s.Keycap);
    x += size.x + 4;
}
GUIContent action = Content(pairs[i + 1]);
Vector2 label = s.HintText.CalcSize(action);
GUI.Label(new Rect(x + 2, rect.y, label.x, rect.height), action, s.HintText);
x += label.x + 18;
```

HintWidth gets the same treatment. In HintRow, line 443 becomes `foreach (string key in KeyParts(keys)) GUILayout.Label(Content(key), s.Keycap, GUILayout.MinWidth(24));` and the action label becomes `Content(action)`.

**Affects:** none. Nothing in Prototype/Editor uses DemoUi's caches, Box or the hint helpers; DemoValidation only looks up DemoUiTheme. OnGUI does not run in batch mode.

---

## F. CPU-GC-5 + CPU-GC-6: Prototype/Scripts/DemoDesktopPanel.cs

Apply after groups E and G. The order inside this file is by payoff.

### F1. Only the drawer and the overlay get the extra Layout pass (CPU-GC-5 g; halves the HUD's OnGUI work)

Replace lines 32 and 41 with backing fields:
```csharp
public PanelLayout Layout { get => layout; set { layout = value; SyncLayoutFlag(); } }
public bool HelpOpen { get => helpOpen; set { helpOpen = value; SyncLayoutFlag(); } }
private PanelLayout layout = PanelLayout.Hidden;
private bool helpOpen;
// IMGUI sends a Layout event before every event only while useGUILayout is on. Only the
// drawer and the controls overlay use GUILayout; the HUD, strip, toasts and banner do not.
private void SyncLayoutFlag() { useGUILayout = layout == PanelLayout.Full || helpOpen; }
```
Call `SyncLayoutFlag();` at the end of Awake.

The setter covers deferred clicks inside OnGUI (the strip's Panel button, "Controls (F1)", the overlay buttons), because Unity reads useGUILayout on every event.

Medium risk: any GUILayout call reached while the flag is off logs layout errors. Verify by hand (see Bookkeeping).

### F2. Per-event strings and arrays become static or cached (CPU-GC-5 a to f)

1. Static fields (TabNames matches the PanelTab enum names):
   ```csharp
   private static readonly string[] ViewfinderKeysVR = { "Trigger", "Photograph" }, ViewfinderKeysDesktop = { "P", "Photo", "F", "Return" };
   private static readonly string[] ChipHintsVR = { "Tab", "Presenter panel", "F1", "Controls", "F9", "Desktop" }, ChipHintsDesktop = { "F1", "Controls", "Tab", "Panel" };
   private static readonly string[] DrawerHints = { "Tab/Esc", "Close", "F1", "Controls", "[/]", "Size" }, HelpHints = { "F1/H", "Close", "Esc", "Close" };
   private static readonly string[] TabNames = { "Tools", "Camera", "Map", "Scene", "Settings" };
   private static readonly string[] Numbers = MakeNumbers(100);
   private static readonly string[] TakeLabels = MakeTakeLabels(); // "Take a " + KindName per DemoToolKind
   private static string[] MakeNumbers(int n) { var a = new string[n]; for (int i = 0; i < n; i++) a[i] = i.ToString(); return a; }
   private static string[] MakeTakeLabels() { var a = new string[KindCount]; for (int i = 0; i < a.Length; i++) a[i] = "Take a " + DeployedTool.KindName((DemoToolKind)i); return a; }
   private static string Count(int n) => n >= 0 && n < Numbers.Length ? Numbers[n] : n.ToString(); // ToolStation has no cap
   ```
   Declare `MakeTakeLabels` after `KindCount` (line 121). Static initializers run in textual order.

2. Replace the literal arrays with the static fields:
   - line 233: `vr ? ViewfinderKeysVR : ViewfinderKeysDesktop`
   - line 258: `vr ? ChipHintsVR : ChipHintsDesktop`
   - line 618: `DrawerHints`
   - line 1025: `HelpHints`

3. Split ContextPrompt (lines 309-338) into a fill step and a public wrapper. The public signature and contents stay the same, because DemoValidation lines 237 and 239 call `Array.IndexOf` on the result.
   ```csharp
   public string[] ContextPrompt() { FillPrompt(); return pairs.ToArray(); }
   private void FillPrompt()
   {
       pairs.Clear();
       if (!DesktopActive) return;
       DeployedTool held = Interactor.Held;
       if (Interactor.HoldingCamera) { Add("P", "Photograph"); Add("F", "Return camera"); }
       else if (held != null)
       {
           if (Interactor.HasPlacementTarget) Add("LMB", "Place");
           Add("Q/E", "Rotate");
           if (held.Lamp != null) Add("T", "Lamp on/off"); else if (held.TapeAnchor != null) Add("T", "Select end");
           Add("R", "Remove");
       }
       else if (Interactor.Hovered != null)
       {
           Add("LMB", "Pick up");
           if (Interactor.Hovered.Lamp != null) Add("T", "Lamp on/off"); else if (Interactor.Hovered.TapeAnchor != null) Add("T", "Select end");
           Add("R", "Remove");
       }
       else if (Interactor.HoveredSample != null) Add("LMB", TakeLabels[(int)Interactor.HoveredSample.Kind]);
       else if (Interactor.HoveringCamera) { Add("LMB", "Take camera"); Add("P", "Photo from here"); }
       else { Add("WASD", "Walk"); Add("RMB", Looking ? "Free cursor" : "Mouse look"); Add("1-6", "Tools"); Add("F1", "Help"); }
   }
   private void Add(string key, string action) { pairs.Add(key); pairs.Add(action); }
   ```

4. In DrawPrompt (lines 348-363):
   - Call `FillPrompt();` instead of `ContextPrompt()`.
   - Use `pairs.Count > 0`, `DemoUi.HintWidth(pairs)` and `DemoUi.DrawHints(..., pairs)`.
   - Use `GUIContent content = DemoUi.Content(message);`.

5. Cache the count strings. Add `private int shownDeployed = -1, shownMarker = -1, shownPhotos = -2; private string toolChip, toolsText, markerText, photoText, statusLine;` and:
   ```csharp
   private void RefreshCountText()
   {
       int marker = Station.NextMarkerNumber, photos = EvidenceCamera != null ? EvidenceCamera.PhotoCount : -1;
       if (deployed == shownDeployed && marker == shownMarker && photos == shownPhotos) return;
       shownDeployed = deployed; shownMarker = marker; shownPhotos = photos;
       toolsText = deployed + (deployed == 1 ? " tool" : " tools");
       toolChip = toolsText + "   ·   marker " + marker + " next";
       markerText = "Marker " + marker + " next";
       photoText = photos < 0 ? null : photos + (photos == 1 ? " photo" : " photos");
       statusLine = toolsText + " in the scene   ·   marker " + marker + " next" + (photoText != null ? "   ·   " + photoText : "");
   }
   ```
   - Call it first inside `if (Station != null)` in OnGUI (line 197).
   - Use the cached strings in DrawChips (lines 247 and 249), DrawStrip (lines 553-555) and `StatusLine()` (which returns `statusLine`). The text is byte-for-byte the same as now.

6. Use `DemoUi.Content(...)` and pass the GUIContent to GUI.Label / GUI.Button in:
   - Chip (263), StripChip (563), StripButton (571: `GUI.Button(r, content, style)`)
   - DrawCrosshair (293), DrawPointerTip (303), DrawToasts (484), DrawResetBanner (503)
   - the "LIVE" badge (466), the strip title (549)
   - the hotbar SlotKeys and SlotNames, and `DemoUi.Content(Count(kindCounts[i]))` (lines 412-415)
   - ToolTile: `SlotKeys[index]` instead of `(index + 1).ToString()`, `Count(...)` for the counts, and Title (680)

7. Drawer closures (lower priority). Capture loop variables only inside the click branch. Otherwise the closure object is allocated for every tab button on every event:
   ```csharp
   for (int i = 0; i < TabNames.Length; i++)
       if (GUILayout.Button(DemoUi.Content(TabNames[i]), Tab == (PanelTab)i ? s.TabActive : s.Tab)) { PanelTab chosen = (PanelTab)i; Defer(() => SelectTab(chosen)); }
   ```
   - Do the same for the Remove row (F3) and ScalePresets (line 917).
   - Optionally cache the strip's `() => Layout = PanelLayout.Full`, `() => RequestReset()` and the `Photograph` method-group delegates in fields set in Awake.

### F3. One hierarchy walk per ToolsChanged instead of per event (CPU-GC-6, low)

Replace Recount (lines 1052-1062):
```csharp
private readonly List<DeployedTool> placed = new List<DeployedTool>(32);
private readonly string[] kindChips = new string[KindCount];
private void Recount()
{
    for (int i = 0; i < kindCounts.Length; i++) kindCounts[i] = 0;
    placed.Clear();
    if (Station != null && Station.DeploymentRoot != null) Station.DeploymentRoot.GetComponentsInChildren(placed); // active only, like before
    for (int i = 0; i < placed.Count; i++) kindCounts[(int)placed[i].Kind]++;
    deployed = placed.Count;
    for (int i = 0; i < KindCount; i++) kindChips[i] = kindCounts[i] > 0 ? kindCounts[i] + " " + KindName((DemoToolKind)i, kindCounts[i]) : null;
}
```

PlacedTools (lines 702-728):
- Delete line 704.
- `if (placed.Count == 0) { ...Nothing placed yet...; return; }`
- Chips: `if (kindChips[i] != null) GUILayout.Label(DemoUi.Content(kindChips[i]), s.Chip);`
- `int first = Mathf.Max(0, placed.Count - RecentTools);`
- Loop: `for (int i = placed.Count - 1; i >= first; i--) { DeployedTool tool = placed[i]; if (tool == null) continue; ... if (GUILayout.Button("Remove", s.ButtonGhost, GUILayout.Width(84))) { DeployedTool target = tool; Defer(() => RemoveTool(target)); } ... }`

MapTab (lines 793-795):
```csharp
for (int i = 0; i < placed.Count; i++) { DeployedTool tool = placed[i]; if (tool != null) Glyph(mapRect, Map.ToMap(tool.transform.position), KindColor(tool.Kind), 9); }
```

Dropped: the finder's `activeInHierarchy` filter inside the row loop. Every removal path (ToolStation.RemoveTool, ClearTools, DeployedTool.RemoveTool) calls SetActive(false) and then fires ToolsChanged synchronously, so the list is never stale. Filtering in the loop would also put the count out of step with the rows drawn.

**Affects (all of F):**
- DemoValidation "Help overlay pauses desktop input" sets HelpOpen through the property; it still works.
- "Context prompt follows what the desktop visitor holds": same arrays.
- ShopCityValidation "Panel layout cycles hidden, strip, full" and the strip hit-test checks only use getters and setters.
- ShopRobberyDressing uses Visible, which goes through the Layout setter.
- No validator reads the drawer lists, kindCounts or OnGUI output.

---

## G. CPU-GC-5 leftovers: DeployedTool.cs and DemoLauncher.cs (low)

### Prototype/Scripts/DeployedTool.cs, lines 48-55

```csharp
private static readonly string[] titles = MakeTitles();
private static string[] MakeTitles()
{
    string[] result = new string[System.Enum.GetValues(typeof(DemoToolKind)).Length];
    for (int i = 0; i < result.Length; i++) { string name = KindName((DemoToolKind)i); result[i] = char.ToUpperInvariant(name[0]) + name.Substring(1); }
    return result;
}
public static string Title(DemoToolKind kind)
{
    int i = (int)kind;
    if (i >= 0 && i < titles.Length) return titles[i];
    string name = KindName(kind);
    return char.ToUpperInvariant(name[0]) + name.Substring(1);
}
private string markerName; // not serialized, so Instantiate never copies it
public string DisplayName => Kind == DemoToolKind.Marker && MarkerNumber > 0 ? (markerName ?? (markerName = "Marker " + MarkerNumber)) : Title(Kind);
```

In SetMarkerNumber (line 25), add `markerName = null;`.

**Affects:** DemoValidation line 270, the carousel check comparing against `Title(Marker)`. The value is unchanged ("Marker").

### Prototype/Scripts/DemoLauncher.cs

Start-menu scene only, so lowest priority:
- In Awake, add `useGUILayout = false;`. Its OnGUI and BuildStyles use GUI.* only.
- Move the line-116 array to `private static readonly string[] Hints = {...}`.
- Line 104: `DemoUi.Content((i + 1).ToString())`, or a static "1".."9" array.
- Line 109: `DemoUi.Content("LATEST")`, passed to both CalcSize and Label.

No validator touches the launcher.

---

## H. CPU-GC-4: Prototype/Scripts/EvidenceCamera.cs (Step 1 only)

Replace lines 270-292:
```csharp
// Reused between photos; IsCapturing guards it until LastPhoto.LoadRawTextureData has run.
private byte[] pixelBuffer;

// Copies the rendered frame into the scratch texture's CPU copy and returns its raw RGB bytes
// in a reused buffer. No Apply: nothing samples the scratch texture on the GPU.
private byte[] ReadPixels()
{
    RenderTexture previousActive=RenderTexture.active;
    try
    {
        RenderTexture.active=target;
        if(scratch==null || scratch.width!=width || scratch.height!=height)
        {
            if(scratch!=null) Destroy(scratch);
            scratch=new Texture2D(width,height,TextureFormat.RGB24,false) { name="EvidencePhotoScratch" };
        }
        scratch.ReadPixels(new Rect(0,0,width,height),0,0);
        var raw=scratch.GetRawTextureData<byte>(); // NativeArray view of the CPU copy, no allocation
        if(pixelBuffer==null || pixelBuffer.Length!=raw.Length) pixelBuffer=new byte[raw.Length];
        raw.CopyTo(pixelBuffer);
        return pixelBuffer;
    }
    finally { RenderTexture.active=previousActive; }
}
```

In OnDisable (lines 338-344), add `pixelBuffer=null;` after `Restore();`, so a save task still running keeps its own array.

Deferred (not to ship blind): the AsyncGPUReadback version (Step 2). If it is ever needed: request RGBA32, strip alpha inside the Task.Run, restore the camera before the first yield, and release the RT only after `request.done`.

**Affects:** DemoValidation.PhotoStage ("Runtime photograph saved", "Photo count follows saved photographs", "Photo frame shows the saved photograph", "View photograph restores dedicated camera pose") and ResetStage. The bytes are identical, so all still pass. ShopCityValidation's viewfinder check is unaffected.

---

## Validator changes (one group per file)

Each new check marked "new" adds one PASS line: update the expected counts in HANDOFF.

### Prototype/Editor/ShopRobberyDressing.cs, lines 98-101 (required by C; otherwise "Overview rendering resumes…" fails)

```csharp
DemoValidation.Check(!map.ShouldRender,"Hidden overview skips automatic rendering");
panel.Visible=true; panel.Tab=PanelTab.Tools;
DemoValidation.Check(!map.ShouldRender,"Overview skips rendering on other drawer tabs");   // new
panel.Tab=PanelTab.Map;
DemoValidation.Check(map.ShouldRender,"Overview rendering resumes when menu opens");
panel.Tab=PanelTab.Tools; panel.Visible=false;
```

### Prototype/Editor/NightStoreValidation.cs

1. **For C**, in MapChecks after `map.Render();` (line 990), so the night store covers it too (ShopRobberyDressing only runs in some RunGlass passes):
   ```csharp
   DemoDesktopPanel panel = DemoValidation.Find<DemoDesktopPanel>();
   PanelLayout layout = panel.Layout; PanelTab tab = panel.Tab;
   panel.Layout = PanelLayout.Full; panel.Tab = PanelTab.Tools; bool other = !map.ShouldRender;
   panel.Tab = PanelTab.Map; bool shown = map.ShouldRender;
   panel.Layout = PanelLayout.Strip; bool strip = !map.ShouldRender;
   panel.Layout = layout; panel.Tab = tab;
   DemoValidation.Check(other && shown && strip, "Overview renders only on the drawer's Map tab");
   ```

2. **For R1** (high), in RealtimeLightChecks after `lighting.UVTorch(true);` (line 648), while both torch lights exist:
   ```csharp
   bool pixel = lighting.FlashlightLight.renderMode == LightRenderMode.ForcePixel && lighting.UVTorchLight.renderMode == LightRenderMode.ForcePixel
       && second.Lamp.renderMode == LightRenderMode.ForcePixel;
   bool headlights = true;
   if (!baked) foreach (Light l in group.GetComponentsInChildren<Light>(true)) if (l.name.StartsWith("Headlight_")) headlights &= l.renderMode == LightRenderMode.Auto;
   ```
   After the existing budget check: `DemoValidation.Check(pixel && headlights, "Handheld lights keep a per-pixel slot");`.
   - BuildScene's bake-failure fallback cannot be triggered by a validator. Verify it with one `Start-Demo.cmd bake`: check the `JEWELRY_BUILD_LIGHTING` line in build-latest.log and the start-menu footer.

3. **For R2** (high), after the styling check at line 831:
   ```csharp
   bool litTape = false, castsTape = false;
   if (ribbon != null && ribbon.sharedMaterial != null)
   {
       Shader shader = ribbon.sharedMaterial.shader;
       var mode = new UnityEngine.Rendering.ShaderTagId("LightMode");
       for (int i = 0; i < shader.passCount; i++)
       {
           string m = shader.FindPassTagValue(i, mode).name;
           litTape |= m == "ForwardAdd"; castsTape |= m == "ShadowCaster";
       }
   }
   DemoValidation.Check(litTape && castsTape, "Police tape is lit by the torches and lamps and casts shadows");
   ```
   If `passCount` or `FindPassTagValue` is missing in 6000.6, fall back to `ribbon.sharedMaterial.FindPass("ShadowCaster") >= 0 && ribbon.sharedMaterial.FindPass("FORWARD_DELTA") >= 0`.

### Prototype/Editor/DemoValidation.cs, InterfaceChecks (for D)

Declare `bool faded = false;` before `if (built)` (line 276). Inside `if (built)`, after line 283:
```csharp
// Faded hints stop rendering: past onboarding and not looked at, the tips fade to 0 and switch off.
float onboard = right.Hud.OnboardingSeconds, look = right.Hud.LookDistance;
right.Hud.OnboardingSeconds = 0; right.Hud.LookDistance = 0;
Renderer hint = right.Label.GetComponent<Renderer>();
for (int i = 0; i < 2000 && hint.enabled; i++) hudFrame.Invoke(right.Hud, null); // MoveTowards by unscaledDeltaTime * 5 per call
faded = !hint.enabled;
right.Hud.OnboardingSeconds = onboard; right.Hud.LookDistance = look;
right.Hud.ShowHints(XRControllerInput.HintsVisible);
```
After `mode.Apply(false);` add `Check(faded, "Faded controller hints stop rendering");`.

This assumes `Time.unscaledDeltaTime > 0` (true at stage 1, frame 10 or later) and a non-null head (VR mode is applied). It runs after ShopCityValidation's "Hand hints toggle together", which is unaffected.

### Optional, for H

In PhotoStage after line 381, decode `evidence.LastPhotoPath` with `ImageConversion.LoadImage` and check that the pixel at (960, 540) equals `evidence.LastPhoto.GetPixel(960, 540)`: "Saved photograph matches the in-memory photo". This is a cheap guard against corruption from buffer reuse.

### No validator possible (E, F, G)

OnGUI does not run in batch mode, so these need a manual Profiler check (see Bookkeeping).

---

## Dropped or deferred, with reasons

| Item | Status | Reason |
|---|---|---|
| R1 compound `bake && Bake(out string report)` | Dropped | Does not compile (CS0165). Replaced by A1. |
| R1 `bake` as Start-Demo.cmd default | Deferred | Untested bake, minutes added per pull. A6 makes the choice sticky instead. |
| CPU-GC-3 `QualitySettings.shadowDistance = 0` around Render | Dropped | Neither scene has sun cascades; it is a global quality write on the render path. |
| CPU-GC-4 Step 2 (AsyncGPUReadback) | Deferred | RGB24 conversion, row order, RT lifetime and RestoreCamera split cannot be verified here. |
| CPU-GC-5 mutable `DemoUi.Temp()` | Replaced | By the immutable `DemoUi.Content()` cache. |
| CPU-GC-6 `activeInHierarchy` filter in the row loop | Dropped | Count would mismatch the rows drawn; the null check is enough. |
| CPU-GC-2 risk note | Corrected | ShopCityValidation does read the Label renderer; never SetActive tip.Root. |

---

## Bookkeeping and run order (CLAUDE.md)

1. `git pull` main before editing (ChatGPT also pushes to it). Commit directly to main.
2. Copy every changed Prototype file into `Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip` at `Assets/CrimeSceneDemo/{Scripts,Editor,Shaders}/` (same names; keep the existing .meta files; no new files).
   - Scripts: DemoUi, DemoDesktopPanel, DeployedTool, DemoLauncher, XRHandHud, DemoOverviewMap, EvidenceCamera, DemoLighting, DemoToolGeometry
   - Editor: NightStoreBuilder, DemoBuild, NightLighting, NightStoreValidation, ShopRobberyDressing, DemoValidation
   - Shaders: CrimeSceneTape.shader
3. Add a new top entry in Docs/HANDOFF.md and Docs/ROADMAP.md marked unverified and never compiled. Include:
   - the new check names and expected PASS-count changes: +1 ShopRobberyDressing, +3 NightStoreValidation, +1 DemoValidation;
   - the README line 5 and HANDOFF line 20 wording for the launcher (if A6 is applied).
4. On the PC:
   1. NightStoreValidation.RunNightQuick
   2. RunNight (this is the first real bake; record the bake time)
   3. StoreKitValidation.RunGallery
   4. ShopCityValidation.RunGlass
5. Manual checks:
   - Open and close the drawer and F1 with mouse and keyboard; click the strip's Panel button, the drawer's x, and Settings "Controls (F1)". The console must show no GUILayout errors (F1).
   - Profiler GC Alloc for DemoDesktopPanel.OnGUI near 0 B/frame on the idle desktop HUD, and in VR mode.
   - In the Frame Debugger, no OverviewCamera render while the drawer shows the Tools tab.
   - In the headset, the controller hints disappear when faded, and the tape is dark until the flashlight hits it.
   - `Start-Demo.cmd bake`: check the start-menu footer reads "lighting baked".