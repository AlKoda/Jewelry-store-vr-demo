using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// First scene of the Windows build: a start menu that lists every map in the build,
// newest first, with the build date and source commit (Resources/DemoBuildInfo, written
// by DemoBuild.BuildLatest). Enter or 1 starts the newest map; F10 in any map returns
// here. IMGUI on the monitor, so it needs no packages; a headset shows the map once one
// is chosen.
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

    private List<Entry> entries;
    private string buildInfo = "";
    private GUIStyle title, body, button, small;

    private void Awake()
    {
        List<string> paths = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) paths.Add(SceneUtility.GetScenePathByBuildIndex(i));
        entries = Entries(paths, SceneManager.GetActiveScene().buildIndex);
        TextAsset info = Resources.Load<TextAsset>("DemoBuildInfo");
        if (info != null) buildInfo = info.text.Trim();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ReturnToLauncher.Ensure(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (entries.Count == 0) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Open(entries[0]);
        for (int i = 0; i < entries.Count && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Open(entries[i]);
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    private static void Open(Entry entry) { SceneManager.LoadScene(entry.BuildIndex); }

    private void OnGUI()
    {
        if (title == null) BuildStyles();
        // Laid out for 1920 x 1080 and scaled to the window, so it reads on a projector.
        float scale = Mathf.Max(0.5f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, x = (width - 900) / 2;
        GUILayout.BeginArea(new Rect(x, 110, 900, 860));
        GUILayout.Label("Jewelry Store Crime Scene", title);
        GUILayout.Label("Police training demonstration · desktop or VR (F9 inside a map)", body);
        GUILayout.Space(30);
        if (entries.Count == 0) GUILayout.Label("No maps in this build. Run Start-Demo.cmd again to rebuild.", body);
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (GUILayout.Button((i + 1) + "   " + e.Title, button, GUILayout.Height(i == 0 ? 92 : 72))) Open(e);
            if (e.Description.Length > 0) GUILayout.Label(e.Description, small);
            GUILayout.Space(16);
        }
        GUILayout.Space(20);
        GUILayout.Label("Enter: newest map   1–9: choose   F10 in a map: back to this menu   Esc: quit", small);
        if (buildInfo.Length > 0) GUILayout.Label(buildInfo, small);
        GUILayout.EndArea();
        GUI.matrix = Matrix4x4.identity;
    }

    private void BuildStyles()
    {
        title = new GUIStyle(GUI.skin.label) { fontSize = 46, fontStyle = FontStyle.Bold };
        title.normal.textColor = new Color(0.95f, 0.78f, 0.42f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
        body.normal.textColor = new Color(0.85f, 0.87f, 0.9f);
        small = new GUIStyle(body) { fontSize = 18 };
        small.normal.textColor = new Color(0.62f, 0.66f, 0.72f);
        button = new GUIStyle(GUI.skin.button) { fontSize = 28, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(28, 20, 10, 10) };
    }
}
