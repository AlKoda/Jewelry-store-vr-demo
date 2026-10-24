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

    // Marker numbers in use; a new marker takes the lowest free number, so a removed
    // marker's number is reused instead of leaving a gap in the sequence.
    private readonly System.Collections.Generic.HashSet<int> markerNumbers = new System.Collections.Generic.HashSet<int>();
    private DeployedTool pendingPost;
    // Separate chains, so a reel laid during a barrier run does not break the run.
    private DeployedTool lastPlacedPost, lastPlacedReel;
    public bool AutoConnectTape = true;
    // Every placed lamp stays lit (Koda, October 23). Each lit lamp is a realtime spot light;
    // only the newest MaxShadowedLamps carry shadow maps and the full 8 m beam, older ones fall
    // back to a short unshadowed pool (LampUnshadowedRange) that cannot reach through a wall.
    // The Quest alone shadows one lamp while a torch is on (two shadow maps in total).
    public const int MaxShadowedLamps = 2;
    // Spot cookie a lamp projects once lit (the night store's NightSky LED floodlight);
    // null keeps the plain spot (the shop).
    public Texture LampCookie;
    private readonly List<DeployedTool> litLamps = new List<DeployedTool>();
    // The continuous tape run: the last placed post of the run and the linked post in a hand, joined
    // by a live ribbon that follows the hand (SceneTape re-stretches every frame).
    private DeployedTool runHead, runHeldPost;
    private SceneTape runTape;
    public DeployedTool RunHead => runHead;
    public DeployedTool RunHeldPost => runHeldPost;
    public bool TapeRunActive => runHead != null && runHead.gameObject.activeInHierarchy && runHeldPost != null && runHeldPost.gameObject.activeInHierarchy;
    // A free post whose anchor lies within this of a held post's anchor is a join candidate.
    public float TapeSnapRadius = 0.12f;
    public DeployedTool LastPlacedPost => lastPlacedPost;
    public DeployedTool LastPlacedReel => lastPlacedReel;
    public DeployedTool PendingPost => pendingPost;
    public int NextMarkerNumber { get { int n = 1; while (markerNumbers.Contains(n)) n++; return n; } }
    // Active tools only; objects pending destruction are already inactive.
    private static readonly List<DeployedTool> countScratch = new List<DeployedTool>();
    public int DeployedCount
    {
        get
        {
            if (DeploymentRoot == null) return 0;
            DeploymentRoot.GetComponentsInChildren(false, countScratch); // reused list: the HUD asks four times a second
            return countScratch.Count;
        }
    }
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
        if (kind == DemoToolKind.Marker) { int number = NextMarkerNumber; markerNumbers.Add(number); tool.SetMarkerNumber(number); }
        // A new lamp comes on in the hand, so it can be aimed while it is carried.
        if (kind == DemoToolKind.Lamp) LightLamp(tool, true);
        ToolsChanged.Invoke();
        return tool;
    }

    public int LitLampCount
    {
        get { PruneLamps(); return litLamps.Count; }
    }

    // Lit lamps are independent; lighting one never switches another off.
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
            litLamps.Add(lamp);
            if (LampCookie != null && lamp.Lamp != null) lamp.Lamp.cookie = LampCookie;
        }
        lamp.SetLampLit(lit);
        UpdateLampShadows();
    }

    private DemoLighting lightingCache;
    private DemoLighting Lighting => lightingCache != null ? lightingCache : lightingCache = FindFirstObjectByType<DemoLighting>();

    // How many lit lamps may shadow right now: two beside whichever torch is lit on the PC
    // (white or UV, never both: DemoLighting.TorchesExclusive), one on the headset alone while
    // a torch is on, so the mobile GPU renders at most two shadow maps a frame.
    public int ShadowedLampBudget
    {
        get
        {
            if (!DemoPlatform.Standalone) return MaxShadowedLamps;
            DemoLighting lighting = Lighting;
            bool torch = lighting != null && (lighting.FlashlightOn || lighting.UVTorchOn);
            return torch ? 1 : MaxShadowedLamps;
        }
    }

    // The newest lit lamps (the ones the presenter is working with) keep soft shadows and the
    // full beam; the rest stay lit as short unshadowed pools, vertex-lit on the headset alone so
    // they cost no extra forward pass. DemoLighting calls this when a torch switches.
    public void UpdateLampShadows()
    {
        PruneLamps();
        int budget = ShadowedLampBudget;
        for (int i = 0; i < litLamps.Count; i++)
        {
            Light lamp = litLamps[i].Lamp;
            if (lamp == null) continue;
            bool shadowed = i >= litLamps.Count - budget;
            if (shadowed)
            {
                lamp.shadows = LightShadows.Soft;
                lamp.shadowCustomResolution = 1024;
                lamp.shadowBias = .015f; lamp.shadowNormalBias = .15f; // small tools were losing contact with their shadows at .04
                DemoPlatform.TuneShadows(lamp, 1024); // hard 512 on the Quest alone
                lamp.range = DemoToolGeometry.LampRange;
                lamp.renderMode = LightRenderMode.ForcePixel;
            }
            else
            {
                lamp.shadows = LightShadows.None;
                lamp.range = DemoToolGeometry.LampUnshadowedRange;
                lamp.renderMode = DemoPlatform.Standalone ? LightRenderMode.ForceVertex : LightRenderMode.Auto;
            }
        }
    }

    // Shadowed lit lamps (validators).
    public int ShadowedLampCount
    {
        get { PruneLamps(); int n = 0; foreach (DeployedTool lamp in litLamps) if (lamp.Lamp != null && lamp.Lamp.shadows != LightShadows.None) n++; return n; }
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

    // Called only on deliberate placement, not spawn, reset or mode switching. firstPlacement is the
    // value of HasBeenPlaced before the holder settled the tool (ToolHolder.Place reads it first; the
    // older single-argument form is for validators placing tools directly).
    //
    // Tape posts form a continuous run: placing a post leaves a linked post in the same hand with a
    // live ribbon back to it, and so on until that held post is removed (HandInteractor.RemoveHeld /
    // the desktop's remove key), which ends the run; the next post then starts a new run that does not
    // join the old one. A held post set down within TapeSnapRadius of another run's free post joins
    // that post instead: the held post merges into it and the run continues from there.
    // Measuring reels keep the simpler pairing: a new reel joins the last placed reel.
    public void NotifyPlaced(DeployedTool tool) => NotifyPlaced(tool, true, null);

    public void NotifyPlaced(DeployedTool tool, bool firstPlacement, ToolHolder holder)
    {
        if(tool==null || tool.TapeAnchor==null || !tool.transform.IsChildOf(DeploymentRoot)) return;
        bool reel=tool.Kind==DemoToolKind.Measure;
        if(firstPlacement && AutoConnectTape && reel)
        {
            DeployedTool previous=lastPlacedReel;
            if(previous!=null && previous.gameObject.activeInHierarchy) { pendingPost=previous; SelectTapePost(tool); }
            lastPlacedReel=tool;
        }
        else if(firstPlacement && AutoConnectTape && tool.Kind==DemoToolKind.TapePost)
        {
            DeployedTool joined = SnapTarget(tool);
            if(tool==runHeldPost && runTape!=null && runHead!=null && runHead.gameObject.activeInHierarchy)
            {
                // The linked post of a run: its ribbon is already there. Joining another post hands the
                // ribbon's end to that post and retires the held one.
                if(joined!=null)
                {
                    runTape.EndAnchor=joined.TapeAnchor;
                    runTape.Refresh();
                    tool.RemoveTool();
                    tool=joined;
                    DemoFeedback.Post("Tape joined", FeedbackTone.Success);
                }
                runHead=tool;
            }
            else
            {
                // A post placed on its own starts a run; next to another free post it first connects to it.
                if(joined!=null) { pendingPost=joined; SelectTapePost(tool); }
                runHead=tool;
            }
            lastPlacedPost=tool;
            runHeldPost=null; runTape=null;
            ContinueRun(holder);
        }
        tool.HasBeenPlaced=true;
        ToolsChanged.Invoke();
    }

    // Puts the next linked post in the hand that just placed one, with the live ribbon from the run head.
    private void ContinueRun(ToolHolder holder)
    {
        if(holder==null || runHead==null || TapePrefab==null) return;
        DeployedTool next=holder.SpawnIntoHand(DemoToolKind.TapePost);
        if(next==null || next.TapeAnchor==null) return;
        runHeldPost=next;
        runTape=Instantiate(TapePrefab, DeploymentRoot);
        runTape.StartAnchor=runHead.TapeAnchor;
        runTape.EndAnchor=next.TapeAnchor;
        runTape.Refresh();
        DemoFeedback.Post("Tape run: place the next post, or remove it to finish");
    }

    // The nearest other placed post whose anchor is within TapeSnapRadius of this post's anchor, or null.
    public DeployedTool SnapTarget(DeployedTool post)
    {
        if(post==null || post.TapeAnchor==null || DeploymentRoot==null) return null;
        DeployedTool best=null; float bestDistance=TapeSnapRadius;
        foreach(Transform child in DeploymentRoot)
        {
            DeployedTool other=child.GetComponent<DeployedTool>();
            if(other==null || other==post || other==runHead || other.Kind!=DemoToolKind.TapePost || other.TapeAnchor==null
               || !other.gameObject.activeInHierarchy || !other.Free || !other.HasBeenPlaced) continue;
            float d=Vector3.Distance(other.TapeAnchor.position, post.TapeAnchor.position);
            if(d<bestDistance) { bestDistance=d; best=other; }
        }
        return best;
    }

    // Ends the run when its held post is removed (or the hand lets go of it some other way).
    public void EndTapeRun()
    {
        runHead=null; runHeldPost=null; runTape=null;
        ToolsChanged.Invoke();
    }

    public void StartNewTapeRun() { lastPlacedPost=null; lastPlacedReel=null; pendingPost=null; EndTapeRun(); }

    public void CancelTapeSelection() { pendingPost = null; ToolsChanged.Invoke(); }

    // Ribbons of either kind go with their endpoint.
    public void RemoveTool(DeployedTool tool)
    {
        if (tool == null || DeploymentRoot == null ||
            !tool.transform.IsChildOf(DeploymentRoot)) return;
        if (pendingPost == tool) pendingPost = null;
        if(lastPlacedPost==tool) lastPlacedPost=null;
        if(lastPlacedReel==tool) lastPlacedReel=null;
        // Removing the held linked post finishes the run (its ribbon goes with it below); removing the
        // run head also ends it, so the next post starts fresh.
        if(tool==runHeldPost || tool==runHead) { runHead=null; runHeldPost=null; runTape=null; }
        bool wasLit = litLamps.Remove(tool);
        if (tool.Kind == DemoToolKind.Marker) markerNumbers.Remove(tool.MarkerNumber);
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
        runHead = null; runHeldPost = null; runTape = null;
        litLamps.Clear();
        if (DeploymentRoot == null) return;
        for (int i = DeploymentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = DeploymentRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        markerNumbers.Clear();
        ToolsChanged.Invoke();
    }
}
