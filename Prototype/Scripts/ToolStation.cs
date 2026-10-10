using UnityEngine;
using UnityEngine.Events;

public sealed class ToolStation : MonoBehaviour
{
    public DeployedTool ConePrefab;
    public DeployedTool MarkerPrefab;
    public DeployedTool TapePostPrefab;
    public DeployedTool ScalePrefab;
    public DeployedTool MeasurePrefab;
    public SceneTape TapePrefab;
    public SceneTape MeasureTapePrefab;
    public Transform DeploymentRoot;
    public Transform SpawnPoint;
    public UnityEvent ToolsChanged = new UnityEvent();

    private int nextMarker = 1;
    private DeployedTool pendingPost;
    // Separate chains, so a reel laid during a barrier run does not break the run.
    private DeployedTool lastPlacedPost, lastPlacedReel;
    public bool AutoConnectTape = true;
    public DeployedTool LastPlacedPost => lastPlacedPost;
    public DeployedTool LastPlacedReel => lastPlacedReel;
    public DeployedTool PendingPost => pendingPost;
    public int NextMarkerNumber => nextMarker;
    // Active tools only; objects pending destruction are already inactive.
    public int DeployedCount => DeploymentRoot != null ? DeploymentRoot.GetComponentsInChildren<DeployedTool>().Length : 0;
    // What the visitor should do while one end of a connection is selected; null otherwise.
    public string PendingPrompt => pendingPost == null ? null
        : pendingPost.Kind == DemoToolKind.Measure ? "Measure: choose the second reel" : "Tape: choose the second post";

    public DeployedTool Prefab(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return ConePrefab;
            case DemoToolKind.Marker: return MarkerPrefab;
            case DemoToolKind.TapePost: return TapePostPrefab;
            case DemoToolKind.Scale: return ScalePrefab;
            default: return MeasurePrefab;
        }
    }

    // Barrier tape joins tape posts and measuring tape joins reels; other kinds never connect.
    private SceneTape RibbonPrefab(DemoToolKind kind)
        => kind == DemoToolKind.TapePost ? TapePrefab : kind == DemoToolKind.Measure ? MeasureTapePrefab : null;

    public void SpawnCone() { Spawn(DemoToolKind.Cone); }
    public void SpawnMarker() { Spawn(DemoToolKind.Marker); }
    public void SpawnTapePost() { Spawn(DemoToolKind.TapePost); }

    public DeployedTool Spawn(DemoToolKind kind)
    {
        if (DeploymentRoot == null || SpawnPoint == null)
        {
            Debug.LogError("Assign deployment root and spawn point.", this);
            return null;
        }
        DeployedTool prefab = Prefab(kind);
        if (prefab == null)
        {
            Debug.LogError("Missing tool prefab: " + kind, this);
            return null;
        }
        DeployedTool tool = Instantiate(prefab, SpawnPoint.position,
            SpawnPoint.rotation, DeploymentRoot);
        if (kind == DemoToolKind.Marker) tool.SetMarkerNumber(nextMarker++);
        ToolsChanged.Invoke();
        return tool;
    }

    // Select two existing posts (or two reels) to connect them; selecting the other
    // kind restarts the selection with it. Cancel clears only the selection.
    public void SelectTapePost(DeployedTool post)
    {
        if (post == null || post.TapeAnchor == null ||
            !post.transform.IsChildOf(DeploymentRoot) || !post.gameObject.activeInHierarchy)
            return;
        if (pendingPost == null || !pendingPost.gameObject.activeInHierarchy || pendingPost.Kind != post.Kind)
        {
            pendingPost = post;
            ToolsChanged.Invoke();
            return;
        }
        if (pendingPost == post) return;
        SceneTape prefab = RibbonPrefab(post.Kind);
        if (prefab == null || pendingPost.TapeAnchor == null)
        {
            Debug.LogError("Assign the ribbon prefab for " + post.Kind + " and the anchors.", this);
            return;
        }
        foreach(SceneTape existing in DeploymentRoot.GetComponentsInChildren<SceneTape>())
            if ((existing.StartAnchor==pendingPost.TapeAnchor && existing.EndAnchor==post.TapeAnchor) ||
                (existing.EndAnchor==pendingPost.TapeAnchor && existing.StartAnchor==post.TapeAnchor))
            { pendingPost=null; ToolsChanged.Invoke(); return; }
        SceneTape tape = Instantiate(prefab, DeploymentRoot);
        tape.StartAnchor = pendingPost.TapeAnchor;
        tape.EndAnchor = post.TapeAnchor;
        pendingPost = null;
        tape.Refresh();
        ToolsChanged.Invoke();
    }

    // Called only on deliberate placement, not spawn, reset or mode switching. A new
    // post joins the last placed post, a new reel the last placed reel, never across.
    public void NotifyPlaced(DeployedTool tool)
    {
        if(tool==null || tool.TapeAnchor==null || !tool.transform.IsChildOf(DeploymentRoot)) return;
        if(!tool.HasBeenPlaced && AutoConnectTape)
        {
            bool reel=tool.Kind==DemoToolKind.Measure;
            DeployedTool previous=reel?lastPlacedReel:lastPlacedPost;
            if(previous!=null && previous.gameObject.activeInHierarchy)
            {
                pendingPost=previous;
                SelectTapePost(tool);
            }
            if(reel) lastPlacedReel=tool; else lastPlacedPost=tool;
        }
        tool.HasBeenPlaced=true;
        ToolsChanged.Invoke();
    }

    public void StartNewTapeRun() { lastPlacedPost=null; lastPlacedReel=null; pendingPost=null; ToolsChanged.Invoke(); }

    public void CancelTapeSelection() { pendingPost = null; ToolsChanged.Invoke(); }

    // Ribbons of either kind go with their endpoint.
    public void RemoveTool(DeployedTool tool)
    {
        if (tool == null || DeploymentRoot == null ||
            !tool.transform.IsChildOf(DeploymentRoot)) return;
        if (pendingPost == tool) pendingPost = null;
        if(lastPlacedPost==tool) lastPlacedPost=null;
        if(lastPlacedReel==tool) lastPlacedReel=null;
        if(tool.TapeAnchor!=null)
            foreach(SceneTape tape in DeploymentRoot.GetComponentsInChildren<SceneTape>())
                if(tape.StartAnchor==tool.TapeAnchor || tape.EndAnchor==tool.TapeAnchor)
                { tape.gameObject.SetActive(false); Destroy(tape.gameObject); }
        tool.RemoveTool();
        ToolsChanged.Invoke();
    }

    public void ClearTools()
    {
        pendingPost = null;
        lastPlacedPost = null;
        lastPlacedReel = null;
        if (DeploymentRoot == null) return;
        for (int i = DeploymentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = DeploymentRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        nextMarker = 1;
        ToolsChanged.Invoke();
    }
}
