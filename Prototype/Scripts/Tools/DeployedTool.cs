using UnityEngine;

// Append only: kinds are stored by name in reviews and by value in prefabs and scenes.
public enum DemoToolKind { Cone, Marker, TapePost, Scale, Measure, Lamp }

[RequireComponent(typeof(Rigidbody))]
public sealed class DeployedTool : MonoBehaviour
{
    public DemoToolKind Kind;
    void Awake(){ApplyCollisionPolicy();}
    // Tools are positioned by the presenter, never thrown by a physics solver.
    // Small evidence tools remain ray/grab targets but cannot displace the player.
    public void ApplyCollisionPolicy(){
        var body=GetComponent<Rigidbody>();
        if(body){body.isKinematic=true;body.useGravity=false;}
        foreach(var collider in GetComponentsInChildren<Collider>(true))
            collider.isTrigger=Kind!=DemoToolKind.Lamp || Holder!=null || collider.name=="Tool selection volume";
    }
    void Start(){BuildSelectionVolume();}
    // One trigger box round every renderer, so pointing and grabbing need no exact hit; rebuilt
    // when a lamp's telescoping column changes its height.
    private BoxCollider selectionVolume;
    private void BuildSelectionVolume(){
        var renderers=GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)return;
        Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
        if(selectionVolume==null){
            var target=new GameObject("Tool selection volume");target.transform.SetParent(transform,false);
            selectionVolume=target.AddComponent<BoxCollider>();selectionVolume.isTrigger=true;
        }
        selectionVolume.center=transform.InverseTransformPoint(b.center);
        selectionVolume.size=Vector3.Scale(b.size,new Vector3(1/Mathf.Abs(transform.lossyScale.x),1/Mathf.Abs(transform.lossyScale.y),1/Mathf.Abs(transform.lossyScale.z)))+Vector3.one*.035f;
    }
    public Transform TapeAnchor;
    public TextMesh NumberLabel;
    public TextMesh BackNumberLabel;
    // Scene lamp only: its spot light and emissive panel face. Every placed lamp stays lit;
    // ToolStation decides which of them shadow (the two newest).
    public Light Lamp;
    public MeshRenderer LampFace;

    // ---- scene lamp telescoping column -------------------------------------------------
    // The light stand's upper column slides: LampHeight is the head pivot's height above the
    // feet (DemoToolGeometry.LampPanelY when built). Grip moves the whole lamp; use/place near
    // the pole drives SetLampHeight (HandInteractor, DesktopInteractor). Lamps built before the
    // column became a separate part get a runtime extension tube and only extend upwards.
    public const float LampMaxHeight = 2.1f;
    private Transform lampHead, lampColumn, lampYoke;
    private CapsuleCollider lampColumnCollider;
    private bool lampPartsSearched;
    public bool IsLamp => Kind == DemoToolKind.Lamp;
    private void FindLampParts()
    {
        if (lampPartsSearched || !IsLamp) return;
        lampPartsSearched = true;
        lampHead = transform.Find("Head");
        lampYoke = transform.Find("Yoke");
        lampColumn = transform.Find("Column");
        Transform stand = transform.Find("Stand");
        if (stand != null) lampColumnCollider = stand.GetComponent<CapsuleCollider>();
        if (lampHead != null && lampColumn == null) lampColumn = DemoToolGeometry.LampExtension(this);
    }
    public float LampHeight { get { FindLampParts(); return lampHead != null ? lampHead.localPosition.y : DemoToolGeometry.LampPanelY; } }
    // The head cannot drop below the fixed column top plus the yoke; a legacy prefab's fixed
    // column already reaches the built head height, so it only extends.
    public float LampMinHeight
    {
        get
        {
            FindLampParts();
            if (lampColumn == null) return DemoToolGeometry.LampPanelY;
            return lampColumn.localPosition.y + DemoToolGeometry.LampYokeDrop + 0.05f;
        }
    }
    public bool LampTelescopes { get { FindLampParts(); return lampHead != null; } }
    // Moves the head (light, housing, yoke) to height metres above the feet and stretches the
    // column to meet it; returns the height actually applied.
    public float SetLampHeight(float height)
    {
        FindLampParts();
        if (lampHead == null) return DemoToolGeometry.LampPanelY;
        height = Mathf.Clamp(height, LampMinHeight, LampMaxHeight);
        Vector3 p = lampHead.localPosition; p.y = height; lampHead.localPosition = p;
        if (lampYoke != null) { Vector3 y = lampYoke.localPosition; y.y = height; lampYoke.localPosition = y; }
        if (lampColumn != null)
        {
            Vector3 scale = lampColumn.localScale;
            scale.y = Mathf.Max(0.001f, height - DemoToolGeometry.LampYokeDrop - lampColumn.localPosition.y);
            lampColumn.localScale = scale;
        }
        if (lampColumnCollider != null)
        {
            float bottom = 0.3f, top = height + 0.05f;
            lampColumnCollider.center = new Vector3(0, (bottom + top) / 2, 0);
            lampColumnCollider.height = top - bottom;
        }
        if (selectionVolume != null) BuildSelectionVolume();
        return height;
    }
    // True when a point (a hand) is within reach of the column itself, not the feet or the
    // panel: the telescoping grip. The column is the tool's local y axis.
    public bool NearLampColumn(Vector3 point, float reach = 0.12f)
    {
        if (!IsLamp) return false;
        Vector3 local = transform.InverseTransformPoint(point);
        float radial = new Vector2(local.x, local.z).magnitude;
        return radial <= reach && local.y > 0.25f && local.y < LampHeight + 0.05f;
    }
    public int MarkerNumber { get; private set; }
    // The adapter currently carrying this tool; null when it rests in the scene.
    private ToolHolder holder;
    public ToolHolder Holder { get=>holder; set{holder=value;ApplyCollisionPolicy();} }
    public bool HasBeenPlaced { get; set; }
    public bool Free => Holder == null;

    public void SetMarkerNumber(int number)
    {
        MarkerNumber = number;
        markerName = null;
        // Prefabs generated before the font fix have no font and render no digits.
        EnsureFont(NumberLabel);
        EnsureFont(BackNumberLabel);
        if (NumberLabel != null) NumberLabel.text = number.ToString();
        if (BackNumberLabel != null) BackNumberLabel.text = number.ToString();
        name = Kind == DemoToolKind.Marker ? "EvidenceMarker_" + number : Kind.ToString();
    }

    // Lower-case display name shared by the panel, the hand labels and prompts.
    public static string KindName(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return "cone";
            case DemoToolKind.Marker: return "marker";
            case DemoToolKind.TapePost: return "tape post";
            case DemoToolKind.Scale: return "scale";
            case DemoToolKind.Lamp: return "scene lamp";
            default: return "measuring tape";
        }
    }

    // Capitalised name for prompts and messages: "Marker 3", "Tape post". Built once per kind,
    // so the panel and the carousel can show it every frame without allocating.
    private static readonly string[] titles = MakeTitles();
    private static string[] MakeTitles()
    {
        string[] result = new string[System.Enum.GetValues(typeof(DemoToolKind)).Length];
        for (int i = 0; i < result.Length; i++) { string name = KindName((DemoToolKind)i); result[i] = char.ToUpperInvariant(name[0]) + name.Substring(1); }
        return result;
    }
    public static string Title(DemoToolKind kind)
    {
        int i = (int)kind;
        if (i >= 0 && i < titles.Length) return titles[i];
        string name = KindName(kind);
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    private string markerName; // not serialized, so Instantiate never copies it
    public string DisplayName => Kind == DemoToolKind.Marker && MarkerNumber > 0 ? (markerName ?? (markerName = "Marker " + MarkerNumber)) : Title(Kind);

    // One request per holder that points at, is nearest to, or holds this tool, so one
    // hand letting go of the highlight does not clear it for the other.
    private int highlightRequests;
    public bool Highlighted => highlightRequests > 0;
    public bool LampLit => Lamp != null && Lamp.enabled;
    // Panel face emission while lit; the saved face material's own emission is black,
    // so an unlit lamp (and the table sample) never glows.
    public static readonly Color LampGlow = new Color(1f, 0.97f, 0.92f) * 2f;
    private static MaterialPropertyBlock block;

    // Lightens every mesh of the tool (not its number labels) so the pointed or
    // nearest tool stands out; a property block leaves the shared materials alone.
    public void SetHighlight(bool on)
    {
        bool was = Highlighted;
        highlightRequests = Mathf.Max(0, highlightRequests + (on ? 1 : -1));
        if (Highlighted == was) return;
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.GetComponent<TextMesh>() != null) continue;
            Tint(renderer);
        }
    }

    // Switches the lamp's light and its glowing face; call through ToolStation.SetLampLit,
    // which keeps the number of lit lamps within its limit.
    public void SetLampLit(bool lit)
    {
        if (Lamp == null) return;
        Lamp.enabled = lit;
        Tint(LampFace);
    }

    // One property block per renderer carries both the highlight and the lamp glow.
    private void Tint(MeshRenderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        block.Clear();
        if (Highlighted) block.SetColor("_Color", Color.Lerp(renderer.sharedMaterial.color, new Color(1f, 0.85f, 0.25f), 0.5f));
        if (renderer == LampFace && LampLit) block.SetColor("_EmissionColor", LampGlow);
        renderer.SetPropertyBlock(block);
    }

    public static void EnsureFont(TextMesh text)
    {
        if (text == null || text.font != null) return;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MeshRenderer renderer = text.GetComponent<MeshRenderer>();
        if (renderer != null && text.font != null) renderer.sharedMaterial = text.font.material;
    }

    // Desktop placement or an XR adapter can call this. No evidence rules enforced.
    public void PlaceAt(Vector3 basePosition, float yawDegrees)
    {
        transform.SetPositionAndRotation(basePosition, Quaternion.Euler(0, yawDegrees, 0));
    }

    public void RemoveTool()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
