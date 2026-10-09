using UnityEngine;
using UnityEngine.Events;

public sealed class ToolStation : MonoBehaviour
{
    public DeployedTool ConePrefab;
    public DeployedTool MarkerPrefab;
    public DeployedTool TapePostPrefab;
    public SceneTape TapePrefab;
    public Transform DeploymentRoot;
    public Transform SpawnPoint;
    public UnityEvent ToolsChanged = new UnityEvent();

    private int nextMarker = 1;
    private DeployedTool pendingPost;
    public DeployedTool PendingPost => pendingPost;
    public int NextMarkerNumber => nextMarker;
    // Active tools only; objects pending destruction are already inactive.
    public int DeployedCount => DeploymentRoot != null ? DeploymentRoot.GetComponentsInChildren<DeployedTool>().Length : 0;

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
        DeployedTool prefab = kind == DemoToolKind.Cone ? ConePrefab :
            kind == DemoToolKind.Marker ? MarkerPrefab : TapePostPrefab;
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
            return;
        }
        if (pendingPost == post) return;
        if (TapePrefab == null || pendingPost.TapeAnchor == null || post.TapeAnchor == null)
        {
            Debug.LogError("Assign tape prefab and post anchors.", this);
            return;
        }
        SceneTape tape = Instantiate(TapePrefab, DeploymentRoot);
        tape.StartAnchor = pendingPost.TapeAnchor;
        tape.EndAnchor = post.TapeAnchor;
        pendingPost = null;
        ToolsChanged.Invoke();
    }

    public void CancelTapeSelection() { pendingPost = null; }

    public void RemoveTool(DeployedTool tool)
    {
        if (tool == null || DeploymentRoot == null ||
            !tool.transform.IsChildOf(DeploymentRoot)) return;
        if (pendingPost == tool) pendingPost = null;
        tool.RemoveTool();
        ToolsChanged.Invoke();
    }

    public void ClearTools()
    {
        pendingPost = null;
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
