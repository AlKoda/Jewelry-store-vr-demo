using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class ToolStation : MonoBehaviour
{
    public DeployedTool ConePrefab;
    public DeployedTool MarkerPrefab;
    public DeployedTool TapePostPrefab;
    public DeployedTool ScalePrefab;
    public DeployedTool MeasurePrefab;
    public DeployedTool LampPrefab;
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
    // Each lit lamp is a realtime spot light; two keep the night scene within its
    // pixel-light budget beside the flashlight. Lit lamps in the order they were lit.
    public const int MaxLitLamps = 2;
    // Spot cookie a lamp projects once lit (the night store's NightSky LED floodlight);
    // null keeps the plain spot (the shop).
    public Texture LampCookie;
    private readonly List<DeployedTool> litLamps = new List<DeployedTool>();
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
            case DemoToolKind.Lamp: return LampPrefab;
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
        // A new lamp comes on in the hand, so it can be aimed while it is carried.
        if (kind == DemoToolKind.Lamp) LightLamp(tool, true);
        ToolsChanged.Invoke();
        return tool;
    }

    public int LitLampCount
    {
        get { PruneLamps(); return litLamps.Count; }
    }

    // Lighting a lamp beyond MaxLitLamps switches off the one lit longest ago.
    public void SetLampLit(DeployedTool lamp, bool lit)
    {
        if (lamp == null || lamp.Lamp == null) return;
        LightLamp(lamp, lit);
        ToolsChanged.Invoke();
    }

    public void ToggleLamp(DeployedTool lamp)
    {
        if (lamp == null || lamp.Lamp == null) return;
        SetLampLit(lamp, !lamp.LampLit);
        DemoFeedback.Post("Scene lamp " + (lamp.LampLit ? "on" : "off"));
    }

    private void LightLamp(DeployedTool lamp, bool lit)
    {
        litLamps.Remove(lamp);
        PruneLamps();
        if (lit)
        {
            while (litLamps.Count >= MaxLitLamps)
            {
                litLamps[0].SetLampLit(false);
                litLamps.RemoveAt(0);
                DemoFeedback.Post("Two scene lamps at most: the first one went off");
            }
            litLamps.Add(lamp);
            if (LampCookie != null && lamp.Lamp != null) lamp.Lamp.cookie = LampCookie;
        }
        lamp.SetLampLit(lit);
        UpdateLampShadows();
    }

    // Both portable lamps remain shadowed alongside whichever torch is lit (white or UV,
    // never both: DemoLighting.TorchesExclusive); no short-range
    // unshadowed substitute is permitted to illuminate through solid walls.
    public void UpdateLampShadows()
    {
        PruneLamps();
        for (int i = 0; i < litLamps.Count; i++)
        {
            Light lamp = litLamps[i].Lamp;
            if (lamp == null) continue;
            // Two portable lamps plus the torch: bounded cost, no unshadowed lamp
            // beams crossing walls. Profile the three shadow maps in actual PCVR.
            lamp.shadows = LightShadows.Soft;
            lamp.shadowCustomResolution=1024;
            lamp.shadowBias=.04f;lamp.shadowNormalBias=.3f;
            DemoPlatform.TuneShadows(lamp, 1024); // hard 512 on the Quest alone
            lamp.range=DemoToolGeometry.LampRange;
        }
    }

    // Removed lamps are inactive before Destroy lands; they no longer count.
    private void PruneLamps()
    {
        for (int i = litLamps.Count - 1; i >= 0; i--)
            if (litLamps[i] == null || !litLamps[i].gameObject.activeInHierarchy) litLamps.RemoveAt(i);
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
            DemoFeedback.Post(PendingPrompt);
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
            { pendingPost=null; ToolsChanged.Invoke(); DemoFeedback.Post("Those two are already connected"); return; }
        SceneTape tape = Instantiate(prefab, DeploymentRoot);
        tape.StartAnchor = pendingPost.TapeAnchor;
        tape.EndAnchor = post.TapeAnchor;
        pendingPost = null;
        tape.Refresh();
        ToolsChanged.Invoke();
        DemoFeedback.Post(post.Kind == DemoToolKind.Measure && tape.DistanceLabel != null
            ? "Measured " + tape.DistanceLabel.text : "Tape connected", FeedbackTone.Success);
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
        bool wasLit = litLamps.Remove(tool);
        if(tool.TapeAnchor!=null)
            foreach(SceneTape tape in DeploymentRoot.GetComponentsInChildren<SceneTape>())
                if(tape.StartAnchor==tool.TapeAnchor || tape.EndAnchor==tool.TapeAnchor)
                { tape.gameObject.SetActive(false); Destroy(tape.gameObject); }
        tool.RemoveTool();
        // The remaining lit lamp may take over the shadow.
        if (wasLit) UpdateLampShadows();
        ToolsChanged.Invoke();
    }

    public void ClearTools()
    {
        pendingPost = null;
        lastPlacedPost = null;
        lastPlacedReel = null;
        litLamps.Clear();
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
