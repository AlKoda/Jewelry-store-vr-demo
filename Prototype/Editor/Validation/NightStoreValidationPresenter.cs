#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The presenter's Scene-tab switches (PresenterSet: the alarm bell, the ambient wear and the case
// evidence; DemoMusic's underscore only as off at arrival and kept by a reset, its switch being
// SoundUnderscore's), what a session reset does to them, the reset sting, and the credits the
// build writes (Resources/DemoCredits.txt) with the licence notices a player build ships. In Edit mode after the lighting pass and before the first save (PresenterBuildChecks,
// then CreditChecks, from Generate: Build has written the credits file and the sets' content is
// inactive); in Play mode at arrival (PresenterArrivalChecks, from Tick stage 1 after
// ArrivalPrintChecks), just before the shared reset (PresenterDisturb, the last step of
// DisturbBeforeReset) and after it (PresenterResetChecks, after ResetChecks). The sets expected
// follow the pack files present, and the credits expected follow what the scene holds (the asset
// path of every mesh and clip it or a prefab it references uses), never the builders' own
// records, so a project unpacked from the ZIP alone (no realism packs: the case evidence only,
// the KhronosSamples cone the one credit) is checked too. The light bar's steady switch is
// GlowSteadyChecks' (NightStoreValidationLighting); here only a real reset makes it flash again.
// File-local members carry the Presenter or Credit prefix, so the other area files of this
// partial class never collide with them.
public static partial class NightStoreValidation
{
    // ---------------------------------------------------------------- presenter sets (Edit mode)

    // Every set stays active (so it stays enabled and listed) and switches a Content child of its
    // own that is built inactive with something to show (an enabled renderer, or the bell's looping
    // clip that starts by itself once shown), not shown at start, under a label of its own. The
    // sets expected follow the packs: Case evidence always (the tools, or their stand-ins without
    // ForensicModels), Ambient wear with SurfaceDecals' Ambient page and one of its wear cells,
    // Alarm bell with NightSoundscape's AlarmBell_Close_Loop.wav; no other set.
    private static void PresenterBuildChecks(Transform root)
    {
        SortedSet<string> expected = PresenterExpected(out string why);
        PresenterSet[] sets = UnityEngine.Object.FindObjectsByType<PresenterSet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SortedSet<string> labels = new SortedSet<string>(StringComparer.Ordinal);
        List<string> built = new List<string>(), wrong = new List<string>();
        foreach (PresenterSet set in sets)
        {
            string label = string.IsNullOrEmpty(set.Label) ? set.name : set.Label;
            built.Add(label + " (" + DemoGeometry.PathUnder(set.transform, root) + ")");
            if (string.IsNullOrEmpty(set.Label) || !labels.Add(set.Label)) wrong.Add(label + ": label empty or repeated");
            if (!set.transform.IsChildOf(root) || !set.enabled || !set.gameObject.activeInHierarchy) wrong.Add(label + ": set not active under the store");
            if (set.ShownAtStart) wrong.Add(label + ": shown at start");
            GameObject content = set.Content;
            if (content == null || content == set.gameObject || content.transform.parent != set.transform)
            {
                wrong.Add(label + ": content is not a child of the set");
                continue;
            }
            if (content.activeSelf) wrong.Add(label + ": content built active");
            if (!PresenterHasContent(content)) wrong.Add(label + ": nothing to show");
        }
        foreach (string label in expected)
            if (!labels.Contains(label)) wrong.Add(label + " missing");
        foreach (string label in labels)
            if (!expected.Contains(label)) wrong.Add(label + " not expected");
        DemoValidation.Info("Presenter sets built: " + (built.Count > 0 ? string.Join(", ", built) : "none") + "; expected " + string.Join(", ", expected)
            + " (" + why + ")" + (wrong.Count > 0 ? "; wrong: " + string.Join("; ", wrong) : ""));
        DemoValidation.Check(sets.Length > 0 && wrong.Count == 0, "Every presenter set is built hidden, its content a child of the set");
    }

    // The labels the build should list, from the pack files alone, and why.
    private static SortedSet<string> PresenterExpected(out string why)
    {
        SortedSet<string> expected = new SortedSet<string>(StringComparer.Ordinal) { "Case evidence" };
        string layoutPath = DemoAssetLibrary.Path(NightDecals.Pack, NightDecals.AmbientAtlas + ".json");
        bool page = DemoAssetLibrary.HasTexture(NightDecals.Pack, NightDecals.AmbientAtlas + "_Color.png");
        bool layout = AssetDatabase.LoadAssetAtPath<TextAsset>(layoutPath) != null;
        Dictionary<string, Rect> cells = page && layout ? DemoAssetLibrary.Atlas(NightDecals.Pack, NightDecals.AmbientAtlas + ".json") : null;
        bool wear = false;
        if (cells != null)
            foreach (string cell in NightDecals.AmbientWearCells) wear |= cells.ContainsKey(cell);
        if (wear) expected.Add("Ambient wear");
        bool bell = AssetDatabase.LoadAssetAtPath<AudioClip>(DemoAssetLibrary.Path(NightAudioBuilder.SoundPack, NightAudioBuilder.AlarmBellClip)) != null;
        if (bell) expected.Add("Alarm bell");
        why = NightDecals.Pack + " Ambient page " + (!page ? "absent" : !layout ? "without its layout" : wear ? "with wear cells" : "without wear cells")
            + ", " + NightAudioBuilder.SoundPack + " " + NightAudioBuilder.AlarmBellClip + (bell ? " present" : " absent");
        return expected;
    }

    // Whether the content would show something once switched on: an enabled renderer with a
    // material, or a sound (each source enabled, looping, with a clip, and playing by itself on
    // being shown, as the alarm bell must).
    private static bool PresenterHasContent(GameObject content)
    {
        bool any = false;
        foreach (Renderer r in content.GetComponentsInChildren<Renderer>(true)) any |= r.enabled && r.sharedMaterial != null;
        foreach (AudioSource s in content.GetComponentsInChildren<AudioSource>(true))
        {
            if (!s.enabled || s.clip == null || !s.loop || !s.playOnAwake) return false;
            any = true;
        }
        return any;
    }

    // ---------------------------------------------------------------- credits (Edit mode)

    // The credits file's first line starts with the whole built-in line (DemoUi.DefaultCredits); the
    // rest is exactly the distinct attribution lines that what the scene ships needs: the CC BY or
    // MIT model behind every mesh in the scene (the hidden presenter sets included) or in a prefab
    // one of its scripts references (the tool prefabs the station spawns), and the attributable
    // clip behind every AudioSource or script field (arrays and nested groups included). The model
    // or clip is read from the asset's path (the pack folder under DemoAssetLibrary.Root, the file
    // stem), so a placed model that skipped its credit, or a credit left behind by a model that was
    // replaced or removed, shows. The file must also import as a TextAsset, for the player build.
    private static void CreditChecks(Transform root)
    {
        Dictionary<string, string> required = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> packs = new HashSet<string>(StringComparer.Ordinal);
        List<GameObject> prefabs = new List<GameObject>();
        int meshes = 0, clips = 0, shipped = 0;
        foreach (MeshFilter f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            meshes += CreditNeed(f.sharedMesh, true, required, packs);
        foreach (SkinnedMeshRenderer s in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            meshes += CreditNeed(s.sharedMesh, true, required, packs);
        foreach (AudioSource s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            clips += CreditNeed(s.clip, false, required, packs);
        foreach (MonoBehaviour b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b != null) clips += CreditReferences(b, prefabs, required, packs);
        foreach (GameObject prefab in prefabs)
        {
            foreach (MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true)) shipped += CreditNeed(f.sharedMesh, true, required, packs);
            foreach (SkinnedMeshRenderer s in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)) shipped += CreditNeed(s.sharedMesh, true, required, packs);
            foreach (AudioSource s in prefab.GetComponentsInChildren<AudioSource>(true)) shipped += CreditNeed(s.clip, false, required, packs);
            foreach (MonoBehaviour b in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                if (b != null) shipped += CreditReferences(b, null, required, packs);
        }

        string file = DemoAssetLibrary.CreditsAsset;
        List<string> lines = new List<string>();
        if (System.IO.File.Exists(file))
            foreach (string raw in System.IO.File.ReadAllLines(file))
                if (raw.Trim().Length > 0) lines.Add(raw.Trim());
        bool imported = AssetDatabase.LoadAssetAtPath<TextAsset>(file) != null;
        // The whole built-in line heads the file (the start menu shows the first line).
        bool head = lines.Count > 0 && lines[0].StartsWith(DemoUi.DefaultCredits.Trim(), StringComparison.Ordinal);
        HashSet<string> listed = new HashSet<string>(StringComparer.Ordinal);
        List<string> repeated = new List<string>(), missing = new List<string>(), extra = new List<string>();
        for (int i = 1; i < lines.Count; i++)
            if (!listed.Add(lines[i])) repeated.Add(CreditShort(lines[i]));
        foreach (KeyValuePair<string, string> need in required)
            if (!listed.Contains(need.Key)) missing.Add(need.Value + ": " + CreditShort(need.Key));
        foreach (string line in listed)
            if (!required.ContainsKey(line)) extra.Add(CreditShort(line));
        List<string> sorted = new List<string>(packs);
        sorted.Sort(StringComparer.Ordinal);
        DemoValidation.Info("Credits: " + (System.IO.File.Exists(file) ? Mathf.Max(0, lines.Count - 1) + " attribution lines after " + (head ? "the built-in one" : "a first line that is NOT the built-in one") : file + " missing")
            + "; needed by " + meshes + " scene meshes, " + clips + " clips and " + shipped + " parts of " + prefabs.Count + " referenced prefabs, from "
            + (sorted.Count > 0 ? string.Join(", ", sorted) : "no attributable pack (only the built-in line expected)")
            + (missing.Count > 0 ? "; missing: " + string.Join("; ", missing) : "") + (extra.Count > 0 ? "; not placed: " + string.Join("; ", extra) : "")
            + (repeated.Count > 0 ? "; repeated: " + string.Join("; ", repeated) : "") + (imported ? "" : "; not imported as a TextAsset"));
        DemoValidation.Check(imported && head && repeated.Count == 0 && missing.Count == 0 && extra.Count == 0,
            "The credits file names every placed CC BY and MIT asset and nothing else");
        CreditLicenceChecks(packs);
    }

    // Records the attribution an asset needs (a pack model's mesh when mesh is true, else a pack
    // clip), by its path under DemoAssetLibrary.Root: the pack is the first folder there, the model
    // the file stem (CreditFor strips the LOD suffixes). Returns 1 when it needs one, else 0.
    private static int CreditNeed(UnityEngine.Object asset, bool mesh, Dictionary<string, string> required, HashSet<string> packs)
    {
        if (asset == null) return 0;
        string path = AssetDatabase.GetAssetPath(asset), prefix = DemoAssetLibrary.Root + "/";
        if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.Ordinal)) return 0;
        if (mesh && !path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return 0;
        int slash = path.IndexOf('/', prefix.Length);
        if (slash <= prefix.Length) return 0;
        string pack = path.Substring(prefix.Length, slash - prefix.Length), model = System.IO.Path.GetFileNameWithoutExtension(path);
        string line = DemoAssetLibrary.CreditFor(pack, model);
        if (line == null) return 0;
        line = line.Trim();
        if (!required.ContainsKey(line)) required[line] = pack + "/" + model;
        packs.Add(pack);
        return 1;
    }

    // The clips a script's serialized fields reference (arrays and nested groups included), and,
    // when prefabs is given, the prefab assets they reference (each root once). Unity's own m_
    // fields (the script, the owner, prefab links) are skipped, and strings are not entered.
    private static int CreditReferences(MonoBehaviour b, List<GameObject> prefabs, Dictionary<string, string> required, HashSet<string> packs)
    {
        int count = 0;
        using (SerializedObject serialized = new SerializedObject(b))
        {
            SerializedProperty p = serialized.GetIterator();
            bool enter = true;
            while (p.Next(enter))
            {
                enter = p.propertyType != SerializedPropertyType.String;
                if (p.propertyType != SerializedPropertyType.ObjectReference || p.propertyPath.StartsWith("m_", StringComparison.Ordinal)) continue;
                UnityEngine.Object value = p.objectReferenceValue;
                if (value is AudioClip) count += CreditNeed(value, false, required, packs);
                else if (prefabs != null)
                {
                    GameObject g = value is GameObject o ? o : value is Component c ? c.gameObject : null;
                    if (g == null || !EditorUtility.IsPersistent(g)) continue;
                    GameObject top = g.transform.root.gameObject;
                    if (!prefabs.Contains(top)) prefabs.Add(top);
                }
            }
        }
        return count;
    }

    private static string CreditShort(string line) => line.Length <= 60 ? line : line.Substring(0, 57) + "...";

    // The player build's notices (DemoBuild.LicenseFiles, which DemoBuild copies to
    // ThirdPartyLicenses beside the executable): every pack folder's LICENSE.txt as <pack>.txt and
    // every .txt in its Licenses folder as <pack>_<file>, each from that very file, every name once
    // and every source on disk; a credited pack without a LICENSE.txt fails. The copy itself and the
    // removal of stale notices happen only in a player build (Builds/Windows) and are not run here.
    private static void CreditLicenceChecks(HashSet<string> credited)
    {
        string third = DemoBuild.ThirdPartyFolder;
        Dictionary<string, string> shipped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> wrong = new List<string>();
        foreach (KeyValuePair<string, string> notice in DemoBuild.LicenseFiles(third))
        {
            if (shipped.ContainsKey(notice.Value)) wrong.Add(notice.Value + " listed twice");
            else shipped[notice.Value] = notice.Key;
            if (!System.IO.File.Exists(notice.Key)) wrong.Add(notice.Value + " from a missing file");
        }
        int folders = 0, nested = 0;
        if (System.IO.Directory.Exists(third))
            foreach (string folder in System.IO.Directory.GetDirectories(third))
            {
                string pack = System.IO.Path.GetFileName(folder);
                if (pack.EndsWith("~", StringComparison.Ordinal) || pack.StartsWith(".", StringComparison.Ordinal)) continue;
                folders++;
                string top = System.IO.Path.Combine(folder, "LICENSE.txt");
                if (System.IO.File.Exists(top)) CreditNotice(shipped, pack + ".txt", top, wrong);
                else if (credited.Contains(pack)) wrong.Add(pack + " is credited but has no LICENSE.txt");
                string licences = System.IO.Path.Combine(folder, "Licenses");
                if (!System.IO.Directory.Exists(licences)) continue;
                foreach (string file in System.IO.Directory.GetFiles(licences, "*.txt"))
                {
                    if (!string.Equals(System.IO.Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase)) continue;
                    nested++;
                    CreditNotice(shipped, pack + "_" + System.IO.Path.GetFileName(file), file, wrong);
                }
            }
        DemoValidation.Info("Licence notices a player build ships: " + shipped.Count + " from " + folders + " pack folders (" + nested
            + " from Licenses folders); credited packs " + (credited.Count > 0 ? string.Join(", ", credited) : "none")
            + (wrong.Count > 0 ? "; wrong: " + string.Join("; ", wrong) : ""));
        DemoValidation.Check(folders > 0 && shipped.Count > 0 && wrong.Count == 0,
            "The player build ships every pack's licence notice, the credited packs' among them");
    }

    // The notice name is shipped from that file.
    private static void CreditNotice(Dictionary<string, string> shipped, string name, string file, List<string> wrong)
    {
        if (!shipped.TryGetValue(name, out string source)) wrong.Add(name + " not shipped");
        else if (!string.Equals(System.IO.Path.GetFullPath(source), System.IO.Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))
            wrong.Add(name + " shipped from " + source);
    }

    // ---------------------------------------------------------------- presenter switches (Play mode)

    // At arrival (every Start has run, nothing toggled; ArrivalPrintChecks switches nothing):
    // every set is enabled and listed exactly once in PresenterSet.All (the Scene tab's SCENE
    // DRESSING rows), none is shown, and the underscore is off. Then each listed set's switch shows
    // its content and hides it again, each time with a monitor-only toast the headset skips, and
    // the list stays as it was (the content is another object, so the set stays enabled). The
    // underscore's own switch and toasts are SoundUnderscore's (NightStoreValidationSound). Leaves
    // the arrival state.
    private static void PresenterArrivalChecks()
    {
        PresenterSet[] scene = UnityEngine.Object.FindObjectsByType<PresenterSet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int revision = PresenterSet.Revision;
        bool live = scene.Length > 0, once = PresenterSet.All.Count == scene.Length, hidden = true;
        List<string> labels = new List<string>();
        foreach (PresenterSet set in scene)
        {
            labels.Add(set.Label + (set.Shown ? " (shown)" : "") + (set.isActiveAndEnabled ? "" : " (disabled)"));
            live &= set.isActiveAndEnabled;
            hidden &= !set.Shown && set.Content != null && !set.Content.activeInHierarchy;
            int listed = 0;
            for (int i = 0; i < PresenterSet.All.Count; i++)
                if (PresenterSet.All[i] == set) listed++;
            once &= listed == 1;
        }
        labels.Sort(StringComparer.Ordinal);
        DemoMusic music = DemoMusic.Instance;
        bool quiet = music == null || !music.BedOn;
        DemoValidation.Info("Presenter sets at arrival: " + string.Join(", ", labels) + "; listed for the Scene tab=" + PresenterSet.All.Count
            + "; underscore " + (music == null ? "absent (no Music pack)" : music.Bed == null ? "without a bed clip" : music.BedOn ? "ON" : "off"));
        DemoValidation.Check(live && once && hidden && quiet, "Presenter sets are hidden at arrival and each listed once for the Scene tab; the underscore is off");

        List<string> bad = new List<string>();
        foreach (PresenterSet set in new List<PresenterSet>(PresenterSet.All))
        {
            if (set == null || set.Content == null) { bad.Add(set != null ? set.Label + " (no content)" : "a destroyed set"); continue; }
            DemoFeedback.Clear();
            set.Toggle();
            bool shown = set.Shown && set.Content.activeInHierarchy && PresenterToast(set.Label + " shown");
            set.Toggle();
            bool off = !set.Shown && !set.Content.activeInHierarchy && PresenterToast(set.Label + " hidden");
            set.SetShown(set.ShownAtStart);
            if (!shown || !off) bad.Add(set.Label);
        }
        bool kept = PresenterSet.Revision == revision && PresenterSet.All.Count == scene.Length;
        DemoFeedback.Clear();
        DemoValidation.Info("Presenter-set switches tried: " + PresenterSet.All.Count
            + (bad.Count > 0 ? "; wrong: " + string.Join(", ", bad) : "") + (kept ? "" : "; the set list changed"));
        DemoValidation.Check(scene.Length > 0 && bad.Count == 0 && kept,
            "Each presenter-set switch shows and hides its set and confirms on the monitor only");
    }

    // The newest feed message is this text, for the monitor only: the headset toast skips it.
    private static bool PresenterToast(string text)
    {
        IReadOnlyList<DemoFeedback.Message> recent = DemoFeedback.Recent;
        if (recent.Count == 0) return false;
        DemoFeedback.Message newest = recent[recent.Count - 1];
        return newest.Text == text && newest.DesktopOnly
            && (!DemoFeedback.TryNewestForHeadset(out DemoFeedback.Message visitor) || visitor.Text != text);
    }

    // ---------------------------------------------------------------- reset (Play mode)

    // Set by PresenterDisturb just before the shared reset and read by PresenterResetChecks after
    // it (Play mode statics live until the run ends, like arrivalAmbient).
    private static int presenterStingsBefore, presenterShown;
    private static bool presenterBedBefore, presenterBarHeld;
    private static float presenterResetAt;

    // The last step of DisturbBeforeReset, in the shared photo stage right before
    // DemoSession.ResetSession: the presenter showed every set, held the light bar steady and
    // turned the underscore on; the stings played so far and the time are recorded.
    private static void PresenterDisturb()
    {
        presenterShown = 0;
        foreach (PresenterSet set in UnityEngine.Object.FindObjectsByType<PresenterSet>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            set.SetShown(true);
            if (set.Shown) presenterShown++;
        }
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        if (lighting.LightBar != null) lighting.LightBar.SetFlashing(false);
        presenterBarHeld = lighting.LightBar != null && !lighting.LightBar.Flashing;
        DemoMusic music = DemoMusic.Instance;
        presenterBedBefore = false;
        if (music != null && music.Bed != null)
        {
            music.SetBed(true);
            presenterBedBefore = music.BedOn;
        }
        presenterStingsBefore = music != null ? music.StingsPlayed : 0;
        presenterResetAt = Time.unscaledTime;
    }

    // After ResetChecks: every set is back at ShownAtStart (hidden), the light bar flashes again
    // with its washes on, and the underscore is still on (then switched off); with a sting, the
    // reset played it once and ducks the bed for DuckSeconds from the reset (DemoMusic's private
    // duckUntil, as the bed's level follows it).
    private static void PresenterResetChecks()
    {
        PresenterSet[] sets = UnityEngine.Object.FindObjectsByType<PresenterSet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int restored = 0;
        foreach (PresenterSet set in sets)
            if (set.Shown == set.ShownAtStart) restored++;
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        LightBarBlink bar = lighting.LightBar;
        bool flashing = presenterBarHeld && bar != null && bar.Flashing && lighting.LightBarFlashing;
        if (bar != null)
            foreach (Renderer wash in bar.Washes) flashing &= wash != null && wash.enabled;
        DemoMusic music = DemoMusic.Instance;
        bool bedKept = !presenterBedBefore || (music != null && music.BedOn);
        DemoValidation.Info("Reset: presenter sets shown before " + presenterShown + ", back at their arrival state after " + restored + " of " + sets.Length
            + "; light bar " + (bar == null ? "missing" : (presenterBarHeld ? "held steady" : "NOT steady") + " before, " + (bar.Flashing ? "flashing" : "STILL STEADY") + " after")
            + "; underscore " + (presenterBedBefore ? "on before, " + (bedKept ? "still on" : "OFF") + " after" : "not tried (no bed clip)"));
        if (presenterBedBefore && music != null) music.SetBed(false);
        DemoValidation.Check(sets.Length > 0 && presenterShown == sets.Length && restored == sets.Length && flashing && bedKept,
            "A session reset hides every presenter set and makes the light bar flash again; the underscore keeps its setting");

        if (music == null) { DemoValidation.Info("Music pack not present (no DemoMusic): reset sting check skipped"); return; }
        if (music.Sting == null) { DemoValidation.Info("No reset sting (" + NightAudioBuilder.MusicPack + "/" + NightAudioBuilder.ResetSting + " absent): sting check skipped"); return; }
        System.Reflection.FieldInfo field = typeof(DemoMusic).GetField("duckUntil",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        float until = field != null ? (float)field.GetValue(music) : float.NaN;
        // Set during the reset: no earlier than the reset's time plus DuckSeconds, no later than now plus DuckSeconds.
        bool ducked = until >= presenterResetAt + music.DuckSeconds - 0.01f && until <= Time.unscaledTime + music.DuckSeconds + 0.01f;
        DemoValidation.Info("Reset sting: played " + (music.StingsPlayed - presenterStingsBefore) + " time(s) by the reset; bed ducked until "
            + (float.IsNaN(until) ? "unknown (no duckUntil field)" : (until - presenterResetAt).ToString("0.00") + " s after it") + " (DuckSeconds " + music.DuckSeconds + ")");
        DemoValidation.Check(music.StingSource != null && music.StingsPlayed == presenterStingsBefore + 1 && ducked,
            "A session reset plays the sting and ducks the underscore");
    }
}
#endif
