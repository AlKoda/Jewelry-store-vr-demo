using UnityEngine;

// Append only: kinds are stored by name in reviews and by value in prefabs and scenes.
public enum DemoToolKind { Cone, Marker, TapePost, Scale, Measure, Lamp }

[RequireComponent(typeof(Rigidbody))]
public sealed class DeployedTool : MonoBehaviour
{
    public DemoToolKind Kind;
    public Transform TapeAnchor;
    public TextMesh NumberLabel;
    public TextMesh BackNumberLabel;
    // Scene lamp only: its spot light and emissive panel face. ToolStation decides
    // which lamps are lit (at most two at once).
    public Light Lamp;
    public MeshRenderer LampFace;
    public int MarkerNumber { get; private set; }
    // The adapter currently carrying this tool; null when it rests in the scene.
    public ToolHolder Holder { get; set; }
    public bool HasBeenPlaced { get; set; }
    public bool Free => Holder == null;

    public void SetMarkerNumber(int number)
    {
        MarkerNumber = number;
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

    // Capitalised name for prompts and messages: "Marker 3", "Tape post".
    public static string Title(DemoToolKind kind)
    {
        string name = KindName(kind);
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public string DisplayName => Kind == DemoToolKind.Marker && MarkerNumber > 0 ? "Marker " + MarkerNumber : Title(Kind);

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
