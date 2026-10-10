using UnityEngine;
using UnityEngine.Events;

public sealed class ToolStation : MonoBehaviour
{
    public DeployedTool ConePrefab;
    public DeployedTool MarkerPrefab;
    public DeployedTool TapePostPrefab;
    public DeployedTool ScalePrefab;
    public SceneTape TapePrefab;
    public Transform DeploymentRoot;
    public Transform SpawnPoint;
    public UnityEvent ToolsChanged = new UnityEvent();

    private int nextMarker = 1;
    private DeployedTool pendingPost;
    private DeployedTool lastPlacedPost;
    public bool AutoConnectTape = true;
    public DeployedTool LastPlacedPost => lastPlacedPost;
    public DeployedTool PendingPost => pendingPost;
    public int NextMarkerNumber => nextMarker;
    // Active tools only; objects pending destruction are already inactive.
    public int DeployedCount => DeploymentRoot != null ? DeploymentRoot.GetComponentsInChildren<DeployedTool>().Length : 0;

    public DeployedTool Prefab(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return ConePrefab;
            case DemoToolKind.Marker: return MarkerPrefab;
            case DemoToolKind.TapePost: return TapePostPrefab;
            default: return ScalePrefab;
        }
    }

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

    // Select an existing post twice to connect them. Cancel clears only selection.
    public void SelectTapePost(DeployedTool post)
    {
        if (post == null || post.Kind != DemoToolKind.TapePost ||
            !post.transform.IsChildOf(DeploymentRoot) || !post.gameObject.activeInHierarchy)
            return;
        if (pendingPost == null || !pendingPost.gameObject.activeInHierarchy)
        {
            pendingPost = post;
            ToolsChanged.Invoke();
            return;
        }
        if (pendingPost == post) return;
        if (TapePrefab == null || pendingPost.TapeAnchor == null || post.TapeAnchor == null)
        {
            Debug.LogError("Assign tape prefab and post anchors.", this);
            return;
        }
        foreach(SceneTape existing in DeploymentRoot.GetComponentsInChildren<SceneTape>())
            if ((existing.StartAnchor==pendingPost.TapeAnchor && existing.EndAnchor==post.TapeAnchor) ||
                (existing.EndAnchor==pendingPost.TapeAnchor && existing.StartAnchor==post.TapeAnchor))
            { pendingPost=null; ToolsChanged.Invoke(); return; }
        SceneTape tape = Instantiate(TapePrefab, DeploymentRoot);
        tape.StartAnchor = pendingPost.TapeAnchor;
        tape.EndAnchor = post.TapeAnchor;
        pendingPost = null;
        tape.Refresh();
        ToolsChanged.Invoke();
    }

    // Called only on deliberate placement, not spawn, reset or mode switching.
    public void NotifyPlaced(DeployedTool tool)
    {
        if(tool==null || tool.Kind!=DemoToolKind.TapePost || !tool.transform.IsChildOf(DeploymentRoot)) return;
        if(!tool.HasBeenPlaced && AutoConnectTape)
        {
            if(lastPlacedPost!=null && lastPlacedPost.gameObject.activeInHierarchy)
            {
                pendingPost=lastPlacedPost;
                SelectTapePost(tool);
            }
            lastPlacedPost=tool;
        }
        tool.HasBeenPlaced=true;
        ToolsChanged.Invoke();
    }

    public void StartNewTapeRun() { lastPlacedPost=null; pendingPost=null; ToolsChanged.Invoke(); }

    public void CancelTapeSelection() { pendingPost = null; ToolsChanged.Invoke(); }

    public void RemoveTool(DeployedTool tool)
    {
        if (tool == null || DeploymentRoot == null ||
            !tool.transform.IsChildOf(DeploymentRoot)) return;
        if (pendingPost == tool) pendingPost = null;
        if(lastPlacedPost==tool) lastPlacedPost=null;
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
