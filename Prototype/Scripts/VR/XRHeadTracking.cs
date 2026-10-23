using UnityEngine;
using UnityEngine.XR;

// Moves the camera with the headset through Unity's built-in XR input API.
// No package is needed in the project; the OpenXR runtime supplies the poses
// once XR Plug-in Management is installed and enabled. Tracking is floor
// relative, so the camera's parent (the rig root) stands on the floor. The pose
// is read in Update and again just before rendering.
// Runs first, so XRLocomotion's teleport and snap-turn pivot (driven from
// XRControllerInput, order -100) read this frame's head pose.
[DefaultExecutionOrder(-110)]
public sealed class XRHeadTracking : MonoBehaviour
{
    // Read once from PlayerPrefs; the getter runs six times a frame (head and both hands, Update and
    // before render), so it must not touch PlayerPrefs each time.
    private static float height = PlayerPrefs.GetFloat("VRHeightOffset", .12f);
    public static float HeightOffset
    {
        get => height;
        set { height = Mathf.Clamp(value, -.2f, .4f); PlayerPrefs.SetFloat("VRHeightOffset", height); PlayerPrefs.Save(); }
    }
    private void OnEnable() { Application.onBeforeRender += LatePose; }
    private void OnDisable() { Application.onBeforeRender -= LatePose; }
    private void Update() { ApplyPose(); }
    // The XR input subsystem refreshes the pose just before rendering with a shorter prediction;
    // reading it again there keeps the view on the pose the frame is displayed with.
    private void LatePose() { ApplyPose(); }
    private void ApplyPose()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (!head.isValid) return;
        if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position+Vector3.up*HeightOffset;
        if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
    }
}
