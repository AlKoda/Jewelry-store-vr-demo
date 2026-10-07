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
        if (NumberLabel != null) NumberLabel.text = number.ToString();
        if (BackNumberLabel != null) BackNumberLabel.text = number.ToString();
        name = Kind == DemoToolKind.Marker ? "EvidenceMarker_" + number : Kind.ToString();
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
