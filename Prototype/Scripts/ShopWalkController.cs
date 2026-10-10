using UnityEngine;

// Desktop first-person walking. Walls and the front barrier collide normally;
// InteriorBounds (added with the shop defaults if unassigned) keeps the player
// inside everywhere else.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
public sealed class ShopWalkController : MonoBehaviour
{
    public Transform View;
    public InteriorBounds Bounds;
    public float Speed = 1.7f;
    public float SprintMultiplier = 1.8f;
    public float LookSensitivity = 2f;
    public bool ReadDesktopInput = true;
    public bool Looking { get; private set; }

    private DemoDesktopPanel panel;
    private CharacterController body;
    private float pitch;
    private float verticalSpeed;
    private Vector3 spawn;
    private Quaternion spawnRotation;

    private void Awake()
    {
        body = GetComponent<CharacterController>();
        panel = FindFirstObjectByType<DemoDesktopPanel>();
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
        spawn = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (!ReadDesktopInput) return;
        if (panel != null && panel.Visible || Input.GetKeyDown(KeyCode.Escape)) Looking = false;
        else if (Input.GetMouseButtonDown(1)) Looking = !Looking;
        Cursor.lockState = Looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !Looking;
        if (Looking && View != null)
        {
            transform.Rotate(0, Input.GetAxisRaw("Mouse X") * LookSensitivity, 0);
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * LookSensitivity, -75, 75);
            View.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (panel != null && panel.Visible) return;
        Vector3 direction = transform.right * Input.GetAxisRaw("Horizontal")
            + transform.forward * Input.GetAxisRaw("Vertical");
        float speed = Speed * (Input.GetKey(KeyCode.LeftShift) ? SprintMultiplier : 1);
        // One Move per frame keeps isGrounded valid, so fall speed cannot accumulate.
        verticalSpeed = body.isGrounded ? -1f : Mathf.Max(verticalSpeed - 9.81f * Time.deltaTime, -8f);
        Move((Vector3.ClampMagnitude(direction, 1) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Home)) ResetPosition();
    }

    // Validation and future adapters move horizontally without reading input.
    public void MovePlanar(Vector3 displacement)
    {
        displacement.y = 0;
        Move(displacement);
    }

    public void ResetPosition()
    {
        XRLocomotion.Relocate(transform, spawn);
        transform.rotation = spawnRotation;
        verticalSpeed = 0;
        pitch = 0;
        if (View != null) View.localRotation = Quaternion.identity;
    }

    private void Move(Vector3 displacement)
    {
        if (body == null) body = GetComponent<CharacterController>();
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
        // Clamp the intended target first so the one Move per frame stays grounded;
        // relocating only when the controller still slid out of bounds.
        Vector3 target = Bounds.Clamp(transform.position + displacement);
        body.Move(target - transform.position);
        Vector3 inside = Bounds.Clamp(transform.position);
        if ((inside - transform.position).sqrMagnitude > 0.0025f) XRLocomotion.Relocate(transform, inside);
    }

    public Vector3 EyeHeight = new Vector3(0, 1.65f, 0);

    private void OnEnable()
    {
        pitch = 0;
        if (View == null) return;
        View.localPosition = EyeHeight;
        View.localRotation = Quaternion.identity;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { Looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    private void OnDisable()
    {
        Looking = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
