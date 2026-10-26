using UnityEngine;

// Desktop first-person walking. Walls and the front barrier collide normally;
// InteriorBounds (added with the shop defaults if unassigned) keeps the player
// inside everywhere else. C crouches (the eye drops to look under counters), Z held
// zooms (the look slows with it) and the view settings the presenter panel changes
// (sensitivity, inverted Y, field of view) are remembered between runs.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
public sealed class ShopWalkController : MonoBehaviour
{
    public Transform View;
    public InteriorBounds Bounds;
    public float Speed = 1.7f;
    public float SprintMultiplier = 1.8f;
    public float CrouchSpeedMultiplier = 0.5f;
    public float LookSensitivity = DefaultSensitivity;
    public bool InvertY;
    public float FieldOfView = 60f;
    public float ZoomFieldOfView = 30f;
    public float CrouchEyeHeight = 0.95f;
    public bool ReadDesktopInput = true;
    public bool Looking { get; private set; }
    public bool Crouching { get; private set; }
    public bool Zooming { get; private set; }

    public const string SensitivityKey = "DemoLookSensitivity", InvertKey = "DemoInvertY", FieldOfViewKey = "DemoFieldOfView";
    public const float DefaultSensitivity = 2f;

    private DemoDesktopPanel panel;
    private CharacterController body;
    private Camera viewCamera;
    private float pitch;
    private float verticalSpeed;
    private float eyeY;
    private float sceneFieldOfView;
    private Vector3 spawn;
    private Quaternion spawnRotation;

    private void Awake()
    {
        body = GetComponent<CharacterController>();
        // The street is 12 cm below the pavement; leave clearance for the skin
        // around the capsule so ordinary kerbs do not behave like solid walls.
        body.stepOffset = Mathf.Max(body.stepOffset, Mathf.Min(.22f, body.height * .45f));
        panel = FindFirstObjectByType<DemoDesktopPanel>();
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
        spawn = transform.position;
        spawnRotation = transform.rotation;
        viewCamera = View != null ? View.GetComponent<Camera>() : null;
        if (viewCamera != null) FieldOfView = viewCamera.fieldOfView;
        sceneFieldOfView = FieldOfView;
        LoadSettings();
    }

    public void LoadSettings()
    {
        LookSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, LookSensitivity), 0.5f, 5f);
        InvertY = PlayerPrefs.GetInt(InvertKey, InvertY ? 1 : 0) == 1;
        FieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat(FieldOfViewKey, FieldOfView), 50, 90);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(SensitivityKey, LookSensitivity);
        PlayerPrefs.SetInt(InvertKey, InvertY ? 1 : 0);
        PlayerPrefs.SetFloat(FieldOfViewKey, FieldOfView);
    }

    public void ResetSettings()
    {
        LookSensitivity = DefaultSensitivity;
        InvertY = false;
        FieldOfView = sceneFieldOfView;
        SaveSettings();
    }

    public void SetFieldOfView(float degrees)
    {
        FieldOfView = Mathf.Clamp(degrees, 50, 90);
        SaveSettings();
    }

    // The eye drops to CrouchEyeHeight over a moment; instant for resets and validation.
    public void SetCrouch(bool on, bool instant = false)
    {
        Crouching = on;
        if (instant) PlaceEye(on ? CrouchEyeHeight : EyeHeight.y);
    }

    public void SetZoom(bool on, bool instant = false)
    {
        Zooming = on;
        if (instant && viewCamera != null) viewCamera.fieldOfView = on ? ZoomFieldOfView : FieldOfView;
    }

    private void Update()
    {
        EaseView(Time.unscaledDeltaTime);
        if (!ReadDesktopInput) return;
        bool paused = panel != null && panel.CapturesInput;
        if (paused || Input.GetKeyDown(KeyCode.Escape)) Looking = false;
        else if (Input.GetMouseButtonDown(1)) Looking = !Looking;
        Cursor.lockState = Looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !Looking;
        if (!paused && Input.GetKeyDown(KeyCode.C)) SetCrouch(!Crouching);
        SetZoom(!paused && Input.GetKey(KeyCode.Z));
        if (Looking && View != null)
        {
            // Zoomed in, the same mouse movement turns the view less, so aiming stays steady.
            float sensitivity = LookSensitivity * (Zooming ? ZoomFieldOfView / FieldOfView : 1);
            transform.Rotate(0, Input.GetAxisRaw("Mouse X") * sensitivity, 0);
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity * (InvertY ? -1 : 1), -75, 75);
            View.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (paused) return;
        Vector3 direction = transform.right * Input.GetAxisRaw("Horizontal")
            + transform.forward * Input.GetAxisRaw("Vertical");
        float speed = Speed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? SprintMultiplier : 1)
            * (Crouching ? CrouchSpeedMultiplier : 1);
        speed=PortableFieldKit.LimitWalkingSpeed(transform,speed);
        // One Move per frame keeps isGrounded valid, so fall speed cannot accumulate.
        verticalSpeed = body.isGrounded ? -1f : Mathf.Max(verticalSpeed - 9.81f * Time.deltaTime, -8f);
        Move((Vector3.ClampMagnitude(direction, 1) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Home)) ResetPosition();
    }

    // Moves the eye and the field of view towards their targets; writes nothing once
    // there, so other code may move the view between frames.
    private void EaseView(float deltaTime)
    {
        if (View == null) return;
        float eyeTarget = Crouching ? CrouchEyeHeight : EyeHeight.y;
        if (!Mathf.Approximately(eyeY, eyeTarget)) PlaceEye(Mathf.MoveTowards(eyeY, eyeTarget, deltaTime * 3.5f));
        if (viewCamera == null) return;
        float fovTarget = Zooming ? ZoomFieldOfView : FieldOfView;
        if (!Mathf.Approximately(viewCamera.fieldOfView, fovTarget))
            viewCamera.fieldOfView = Mathf.MoveTowards(viewCamera.fieldOfView, fovTarget, deltaTime * 200f);
    }

    private void PlaceEye(float height)
    {
        eyeY = height;
        if (View != null) View.localPosition = new Vector3(EyeHeight.x, height, EyeHeight.z);
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
        SetCrouch(false, true);
        SetZoom(false, true);
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
        Crouching = Zooming = false;
        eyeY = EyeHeight.y;
        if (View == null) return;
        View.localPosition = EyeHeight;
        View.localRotation = Quaternion.identity;
        if (viewCamera == null) viewCamera = View.GetComponent<Camera>();
        if (viewCamera != null) viewCamera.fieldOfView = FieldOfView;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { Looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    private void OnDisable()
    {
        Looking = false;
        Crouching = Zooming = false;
        if (viewCamera != null) viewCamera.fieldOfView = FieldOfView;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
