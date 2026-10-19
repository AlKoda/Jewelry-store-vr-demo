using System.IO;
using UnityEngine;

// World-space status text beside the tool rack, readable in the headset and on
// the monitor without any UI package: the interface font on a dark rounded panel
// sized to the text, with a brass title. Updates only on events.
[RequireComponent(typeof(TextMesh))]
public sealed class DemoStatusBoard : MonoBehaviour
{
    public ToolStation Station;
    public EvidenceCamera EvidenceCamera;
    public DemoSession Session;
    public float LineHeight = 0.075f;

    private TextMesh text;
    private MeshFilter panel;
    private Material panelMaterial;
    private string photoLine = "No photographs yet";

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        DemoUi.Configure(text, DemoUi.Medium, LineHeight, DemoUi.Ink, TextAnchor.MiddleCenter);
        MeshRenderer panelRenderer = DemoUi.WorldPanel("BoardPanel", transform, new Vector3(0, 0, 0.01f), 1f, 0.4f, 0.05f,
            new Color(0.05f, 0.06f, 0.08f, 0.9f));
        panel = panelRenderer.GetComponent<MeshFilter>();
        // DemoUi materials are DontSave, so no scene load frees them: the board owns this one.
        panelMaterial = panelRenderer.sharedMaterial;
    }

    private void OnDestroy()
    {
        if (panelMaterial != null) Destroy(panelMaterial);
    }

    private void OnEnable()
    {
        if (Station != null) Station.ToolsChanged.AddListener(Refresh);
        if (Session != null) Session.SessionReset.AddListener(Refresh);
        if (EvidenceCamera != null)
        {
            EvidenceCamera.PhotoSaved.AddListener(OnPhotoSaved);
            EvidenceCamera.CaptureFailed.AddListener(OnCaptureFailed);
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (Station != null) Station.ToolsChanged.RemoveListener(Refresh);
        if (Session != null) Session.SessionReset.RemoveListener(Refresh);
        if (EvidenceCamera != null)
        {
            EvidenceCamera.PhotoSaved.RemoveListener(OnPhotoSaved);
            EvidenceCamera.CaptureFailed.RemoveListener(OnCaptureFailed);
        }
    }

    private void OnPhotoSaved(string path)
    {
        photoLine = "Saved " + Path.GetFileName(path);
        Refresh();
    }

    private void OnCaptureFailed(string message)
    {
        photoLine = "Photo failed: " + message;
        Refresh();
    }

    public void Refresh()
    {
        if (Station == null) text.text = photoLine;
        else
        {
            int lamps = Station.LitLampCount;
            string accent = DemoUi.Hex(DemoUi.Accent);
            text.text = "<color=" + accent + ">SCENE TOOLS</color>\n"
                + Station.DeployedCount + " deployed, next marker " + Station.NextMarkerNumber + "\n"
                + (Station.PendingPrompt != null ? "<color=" + accent + ">" + Station.PendingPrompt + "</color>" : "Tape: select two posts or two reels") + "\n"
                + (lamps > 0 ? "Scene lamps lit: " + lamps + " of " + ToolStation.MaxLitLamps + "\n" : "")
                + photoLine;
        }
        FitPanel();
    }

    private void FitPanel()
    {
        if (panel == null) return;
        int lines = text.text.Split('\n').Length;
        float width = DemoUi.TextWidth(text.font, text.text, LineHeight) + 0.14f;
        float height = lines * LineHeight * 1.12f + 0.09f;
        panel.sharedMesh = DemoUi.RoundedQuad(Mathf.Round(width * 20) / 20, Mathf.Round(height * 20) / 20, 0.05f);
    }
}
