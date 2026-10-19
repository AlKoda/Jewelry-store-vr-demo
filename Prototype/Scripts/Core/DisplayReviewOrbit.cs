using UnityEngine;

// Desktop inspection camera for the controlled material and untextured prop scenes.
public sealed class DisplayReviewOrbit : MonoBehaviour
{
    public Vector3 Target = new Vector3(0, .65f, 0);
    float yaw, pitch, distance;
    void Start()
    {
        Vector3 offset=transform.position-Target;
        distance=offset.magnitude;
        pitch=Mathf.Asin(offset.y/distance)*Mathf.Rad2Deg;
        yaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg;
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }
    void LateUpdate()
    {
        if(Input.GetMouseButton(0))
        {
            yaw+=Input.GetAxis("Mouse X")*3;
            pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,3,75);
        }
        distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.15f,.65f,4.5f);
        transform.position=Target+Quaternion.Euler(-pitch,yaw,0)*Vector3.forward*distance;
        transform.LookAt(Target);
    }
    void OnGUI()
    {
        GUI.Box(new Rect(16,16,390,30),"Drag to orbit    •    Scroll to zoom    •    F10 menu");
    }
}
