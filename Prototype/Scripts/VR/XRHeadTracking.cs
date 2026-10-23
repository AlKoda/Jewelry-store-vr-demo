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
    // Two separate corrections, both read once from PlayerPrefs (the getters run several times a frame):
    // - HeightOffset lifts the HEAD only (a comfort setting, default 0). It used to default to +12 cm and
    //   was also added to both controllers, which lifted the whole tracked space: a real hand on the real
    //   floor rendered 12 cm above the virtual floor, so nothing on the floor could be reached.
    // - FloorCorrection moves head and hands together; CalibrateFloor measures it from a controller
    //   resting on the real floor, instead of guessing. Floor-origin tracking makes it ~0 on a good setup.
    private static float height = LoadHeight();
    private static float floorCorrection = PlayerPrefs.GetFloat("VRFloorCorrection", 0f);
    private static float LoadHeight()
    {
        // Builds before October 23 saved +0.12 here and applied it to the hands too; forget that value once.
        if (PlayerPrefs.GetInt("VRHeightOffsetVersion", 0) < 2)
        {
            PlayerPrefs.DeleteKey("VRHeightOffset");
            PlayerPrefs.SetInt("VRHeightOffsetVersion", 2);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetFloat("VRHeightOffset", 0f);
    }
    public static float HeightOffset
    {
        get => height;
        set { height = Mathf.Clamp(value, -.2f, .4f); PlayerPrefs.SetFloat("VRHeightOffset", height); PlayerPrefs.Save(); }
    }
    public static float FloorCorrection
    {
        get => floorCorrection;
        set { floorCorrection = Mathf.Clamp(value, -.3f, .3f); PlayerPrefs.SetFloat("VRFloorCorrection", floorCorrection); PlayerPrefs.Save(); }
    }
    // Height of a Touch controller's tracked point above a surface it rests on.
    public const float ControllerRestHeight = 0.035f;
    // Called with the controller on the real floor: its tracked height (rig space, before any correction)
    // should then be ControllerRestHeight; whatever remains becomes the shared floor correction.
    public static void CalibrateFloor(float lowestTrackedControllerY)
    {
        FloorCorrection = ControllerRestHeight - lowestTrackedControllerY;
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
        if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position+Vector3.up*(HeightOffset+FloorCorrection);
        if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
    }
}
