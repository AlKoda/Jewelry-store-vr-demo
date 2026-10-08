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
