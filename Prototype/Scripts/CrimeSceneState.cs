using UnityEngine;
using UnityEngine.Events;

// Robbed (default) or intact view of the same shop, for the instructor to show
// visitors what changed. Robbed-only objects hide and intact-only objects show
// while intact; the safe door closes. A session reset returns to robbed.
public sealed class CrimeSceneState : MonoBehaviour
{
    public GameObject[] RobbedOnly = new GameObject[0];
    public GameObject[] IntactOnly = new GameObject[0];
    public Transform SafeDoorHinge;
    public float RobbedDoorYaw = 110f;
    public DemoSession Session;
    public bool ReadDesktopInput = true;
    public UnityEvent StateChanged = new UnityEvent();
    public bool Intact { get; private set; }

    private void OnEnable()
    {
        if (Session != null) Session.SessionReset.AddListener(ShowRobbed);
    }

    private void OnDisable()
    {
        if (Session != null) Session.SessionReset.RemoveListener(ShowRobbed);
    }

    private void Update()
    {
        if (ReadDesktopInput && Input.GetKeyDown(KeyCode.I)) Toggle();
    }

    public void Toggle() { SetIntact(!Intact); }
    public void ShowRobbed() { SetIntact(false); }

    public void SetIntact(bool intact)
    {
        Intact = intact;
        foreach (GameObject g in RobbedOnly) if (g != null) g.SetActive(!intact);
        foreach (GameObject g in IntactOnly) if (g != null) g.SetActive(intact);
        if (SafeDoorHinge != null) SafeDoorHinge.localRotation = Quaternion.Euler(0, intact ? 0 : RobbedDoorYaw, 0);
        StateChanged.Invoke();
    }
}
