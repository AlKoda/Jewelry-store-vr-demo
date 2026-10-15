using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// First scene of the Windows build: a start menu that lists every map in the build,
// newest first, with the build date and source commit (Resources/DemoBuildInfo, written
// by DemoBuild.BuildLatest). Enter or 1 starts the newest map; F10 in any map returns
// here. IMGUI on the monitor, so it needs no packages. Desktop only for now: the build
// has no XR plug-in until XR Plug-in Management and OpenXR are installed. The menu theme
// (a DemoMusic bed, when DemoBuild found the Music pack) fades out before a map loads, and
// the credits summary (DemoUi.Credits' first line) sits above the build stamp.
public sealed class DemoLauncher : MonoBehaviour
{
    public struct Entry
    {
        public int BuildIndex;
        public string Scene, Title, Description;
        public int Order; // lower is newer
    }

    // Known maps, newest first. Unknown scenes in the build still appear, last.
    private static readonly (string scene, string title, string description)[] Known =
    {
        ("JewelryStoreNight", "Night store — latest", "Expanded 16 × 12 m store at night: police car, broken frontage, two open safes, flashlight and UV torch."),
        ("JewelryStoreGlass", "Daytime shop — October 10 milestone", "The first verified shop with glass, robbed stock and the forensic tools (137 checks)."),
    };

    public static Entry Describe(string scenePath, int buildIndex)
    {
        string scene = Path.GetFileNameWithoutExtension(scenePath);
        for (int i = 0; i < Known.Length; i++)
            if (Known[i].scene == scene)
                return new Entry { BuildIndex = buildIndex, Scene = scene, Title = Known[i].title, Description = Known[i].description, Order = i };
        return new Entry { BuildIndex = buildIndex, Scene = scene, Title = scene, Description = "", Order = Known.Length + buildIndex };
    }

    // Every scene in the build except this launcher, newest first.
    public static List<Entry> Entries(IList<string> scenePaths, int launcherIndex)
    {
        List<Entry> entries = new List<Entry>();
        for (int i = 0; i < scenePaths.Count; i++)
            if (i != launcherIndex) entries.Add(Describe(scenePaths[i], i));
        entries.Sort((a, b) => a.Order.CompareTo(b.Order));
        return entries;
    }

    private static readonly string[] Hints = { "Enter", "Newest map", "1-9", "Choose", "F10", "Back here from a map", "Esc", "Quit" };
    private static readonly string[] Numbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    // How long the menu theme fades before the map loads (unscaled seconds).
    public const float MusicFadeSeconds = 0.6f;

    private List<Entry> entries;
    private string buildInfo = "", credits = "";
    // Set by the first Open: a second key or click during the fade loads nothing more.
    private bool opening;
    private GUIStyle title, cardTitle, card, cardLatest;
    private Font styledWith;

    private void Awake()
    {
        List<string> paths = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) paths.Add(SceneUtility.GetScenePathByBuildIndex(i));
        entries = Entries(paths, SceneManager.GetActiveScene().buildIndex);
        TextAsset info = Resources.Load<TextAsset>("DemoBuildInfo");
        if (info != null) buildInfo = info.text.Trim();
        credits = FirstLine(DemoUi.Credits);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ReturnToLauncher.Ensure(SceneManager.GetActiveScene().buildIndex);
        // OnGUI and BuildStyles use GUI.* only: no Layout event before each event.
        useGUILayout = false;
    }

    private void Update()
    {
        // Esc quits even from an empty menu or during the fade.
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (entries.Count == 0 || opening) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Open(entries[0]);
        for (int i = 0; i < entries.Count && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) Open(entries[i]);
    }

    // Loads the map once: the menu theme (DemoMusic.Instance, when the launcher has one)
    // fades out over MusicFadeSeconds first, on unscaled time.
    private void Open(Entry entry)
    {
        if (opening) return;
        opening = true;
        StartCoroutine(OpenAfterFade(entry.BuildIndex));
    }

    private static IEnumerator OpenAfterFade(int buildIndex)
    {
        DemoMusic music = DemoMusic.Instance;
        if (music != null)
        {
            music.FadeOut(MusicFadeSeconds);
            yield return new WaitForSecondsRealtime(MusicFadeSeconds);
        }
        SceneManager.LoadScene(buildIndex);
    }

    // The summary line of a credits text; the attribution lines below it are in the map's
    // Settings tab and the build's ThirdPartyLicenses folder.
    private static string FirstLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int end = text.IndexOf('\n');
        return (end < 0 ? text : text.Substring(0, end)).Trim();
    }

    // Laid out for 1280 x 720 and scaled to the window, so it reads on a projector; the
    // look is DemoUi's, the same as the in-map panel and the VR controller hints.
    private void OnGUI()
    {
        DemoUi.Skin s = DemoUi.Styles;
        if (title == null || styledWith != DemoUi.Regular) BuildStyles(s);
        float scale = Mathf.Max(0.5f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, height = Screen.height / scale;
        DemoUi.Fill(new Rect(0, 0, width, height), new Color(0.035f, 0.04f, 0.055f));
        DemoUi.Fill(new Rect(0, 0, width, 3), DemoUi.Accent);
        float w = Mathf.Min(780, width - 64), x = (width - w) / 2, y = Mathf.Max(40, height * 0.09f);

        GUI.Label(new Rect(x, y, w, 20), "POLICE TRAINING  ·  CRIME SCENE DEMONSTRATION", s.Heading);
        GUI.Label(new Rect(x, y + 22, w, 52), "Jewelry Store Crime Scene", title);
        GUI.Label(new Rect(x, y + 76, w, 22), "Desktop build. VR needs the OpenXR plug-in, which is not in this build yet.", s.Dim);
        y += 122;

        if (entries.Count == 0)
        {
            DemoUi.Box(new Rect(x, y, w, 70), 14, DemoUi.Card, DemoUi.Line);
            GUI.Label(new Rect(x + 24, y, w - 48, 70), "No maps in this build. Run Start-Demo.cmd again to rebuild.", s.Body);
        }
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            bool latest = i == 0;
            float h = latest ? 104 : 84;
            Rect r = new Rect(x, y, w, h);
            if (GUI.Button(r, GUIContent.none, latest ? cardLatest : card)) Open(e);
            if (i < 9) GUI.Label(new Rect(r.x + 20, r.y + (h - 30) / 2, 30, 30), DemoUi.Content(Numbers[i]), s.Keycap);
            GUI.Label(new Rect(r.x + 66, r.y + 14, w - 220, 30), e.Title, cardTitle);
            if (e.Description.Length > 0) GUI.Label(new Rect(r.x + 66, r.y + 44, w - 100, h - 52), e.Description, s.Dim);
            if (latest)
            {
                GUIContent tag = DemoUi.Content("LATEST");
                Vector2 chip = s.ChipAccent.CalcSize(tag);
                GUI.Label(new Rect(r.xMax - chip.x - 18, r.y + 16, chip.x, chip.y), tag, s.ChipAccent);
            }
            y += h + 12;
        }

        // Bottom up: build stamp, a two-line word-wrapped credits summary, the key hints.
        DemoUi.DrawHints(new Rect(x, height - 114, w, 28), Hints);
        if (credits.Length > 0) GUI.Label(new Rect(x, height - 78, w, 34), credits, s.Small);
        if (buildInfo.Length > 0) GUI.Label(new Rect(x, height - 40, w, 20), buildInfo, s.Small);
        GUI.matrix = Matrix4x4.identity;
    }

    private void BuildStyles(DemoUi.Skin s)
    {
        styledWith = DemoUi.Regular;
        title = new GUIStyle(s.Title) { fontSize = 42 };
        cardTitle = new GUIStyle(s.Title) { fontSize = 21 };
        card = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
        card.normal.background = DemoUi.Rounded(14, DemoUi.Card, DemoUi.Line);
        card.hover.background = DemoUi.Rounded(14, DemoUi.CardHover, DemoUi.WithAlpha(DemoUi.Accent, 0.45f));
        card.active.background = DemoUi.Rounded(14, DemoUi.AccentDark, DemoUi.Accent);
        cardLatest = new GUIStyle(card);
        cardLatest.normal.background = DemoUi.Rounded(14, DemoUi.Card, DemoUi.WithAlpha(DemoUi.Accent, 0.6f), 2);
    }
}
