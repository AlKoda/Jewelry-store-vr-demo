using UnityEngine;

// Attach only to scene props whose initial pose/state should be restored.
public sealed class ResettableSceneObject : MonoBehaviour
{
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 scale;
    private bool active;
    private bool recorded;

    private void Awake() { RecordInitialState(); }

    public void RecordInitialState()
    {
        position = transform.localPosition;
        rotation = transform.localRotation;
        scale = transform.localScale;
        active = gameObject.activeSelf;
        recorded = true;
    }

    public void RestoreInitialState()
    {
        if (!recorded) return;
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = scale;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            #if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
        gameObject.SetActive(active);
    }
}
