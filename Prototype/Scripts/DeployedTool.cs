using UnityEngine;

public enum DemoToolKind { Cone, Marker, TapePost }

[RequireComponent(typeof(Rigidbody))]
public sealed class DeployedTool : MonoBehaviour
{
    public DemoToolKind Kind;
    public Transform TapeAnchor;
    public TextMesh NumberLabel;
    public TextMesh BackNumberLabel;
    public int MarkerNumber { get; private set; }

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

    public bool Highlighted { get; private set; }
    private static MaterialPropertyBlock block;

    // Lightens every mesh of the tool (not its number labels) so the pointed or
    // nearest tool stands out; a property block leaves the shared materials alone.
    public void SetHighlight(bool on)
    {
        if (Highlighted == on) return;
        Highlighted = on;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.GetComponent<TextMesh>() != null || renderer.sharedMaterial == null) continue;
            block.Clear();
            if (on) block.SetColor("_Color", Color.Lerp(renderer.sharedMaterial.color, Color.white, 0.45f));
            renderer.SetPropertyBlock(block);
        }
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
