using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class ShopWalkController : MonoBehaviour
{
    public Transform View;
    public float Speed = 1.7f;
    public float LookSensitivity = 2f;
    public bool ReadDesktopInput = true;
    private CharacterController body;
    private float pitch;
    private float verticalSpeed;
    private Vector3 spawn;
    private Quaternion spawnRotation;

    private void Awake()
    {
        body=GetComponent<CharacterController>();
        spawn=transform.position;
        spawnRotation=transform.rotation;
    }

    private void Update()
    {
        if(!ReadDesktopInput) return;
        if(Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState=CursorLockMode.None;
        bool looking=Input.GetMouseButton(1);
        Cursor.lockState=looking?CursorLockMode.Locked:CursorLockMode.None;
        Cursor.visible=!looking;
        if(looking && View!=null)
        {
            transform.Rotate(0,Input.GetAxis("Mouse X")*LookSensitivity,0);
            pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*LookSensitivity,-75,75);
            View.localRotation=Quaternion.Euler(pitch,0,0);
        }
        Vector3 direction=transform.right*Input.GetAxisRaw("Horizontal")
            +transform.forward*Input.GetAxisRaw("Vertical");
        // Single Move per frame: a separate planar Move clears isGrounded,
        // which made the downward speed grow without limit while walking.
        verticalSpeed=body.isGrounded?-1f:verticalSpeed-9.81f*Time.deltaTime;
        Vector3 planar=Vector3.ClampMagnitude(direction,1)*Speed;
        Move((planar+Vector3.up*verticalSpeed)*Time.deltaTime);
        if(Input.GetKeyDown(KeyCode.Home)) ResetPosition();
    }

    public void MovePlanar(Vector3 displacement)
    {
        displacement.y=0;
        Move(displacement);
    }

    private void Move(Vector3 displacement)
    {
        if(body==null) body=GetComponent<CharacterController>();
        body.Move(displacement);
        // Fallback bounds complement walls/front colliders. Safe room remains accessible.
        Vector3 p=transform.position;
        p.x=Mathf.Clamp(p.x,-4.7f,4.7f);
        p.z=Mathf.Clamp(p.z,0.3f,10.75f);
        if(p.z>8.25f) p.x=Mathf.Clamp(p.x,1.3f,3.7f);
        if((p-transform.position).sqrMagnitude>0.00001f)
        {
            body.enabled=false;
            transform.position=p;
            body.enabled=true;
        }
    }

    public void ResetPosition()
    {
        body.enabled=false;
        transform.SetPositionAndRotation(spawn,spawnRotation);
        body.enabled=true;
        verticalSpeed=0;
        pitch=0;
        if(View!=null) View.localRotation=Quaternion.identity;
    }

    private void OnDisable()
    {
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(320,12,520,30),
            "WASD / arrows: walk   |   Hold right mouse: look   |   Home: return inside");
    }
}
