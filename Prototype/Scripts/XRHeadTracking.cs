using UnityEngine;
using UnityEngine.XR;

// Moves the camera with the headset through Unity's built-in XR input API.
// No package is needed in the project; the OpenXR runtime supplies the poses
// once XR Plug-in Management is installed and enabled. Tracking is floor
// relative, so the camera's parent (the rig root) stands on the floor.
public sealed class XRHeadTracking : MonoBehaviour
{
    private void Update()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (!head.isValid) return;
        if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
        if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
    }
}
