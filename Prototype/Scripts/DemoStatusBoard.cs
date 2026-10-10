using System.IO;
using UnityEngine;

// World-space status text beside the tool rack, readable in the headset and on
// the monitor without any UI package. Updates only on events.
[RequireComponent(typeof(TextMesh))]
public sealed class DemoStatusBoard : MonoBehaviour
{
    public ToolStation Station;
    public EvidenceCamera EvidenceCamera;
    public DemoSession Session;

    private TextMesh text;
    private string photoLine = "No photographs yet";

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
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
        if (Station == null) { text.text = photoLine; return; }
        text.text = "SCENE TOOLS\n"
            + Station.DeployedCount + " deployed, next marker " + Station.NextMarkerNumber + "\n"
            + (Station.PendingPrompt ?? "Tape: select two posts or two reels") + "\n"
            + photoLine;
    }
}
