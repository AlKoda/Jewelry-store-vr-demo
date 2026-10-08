using UnityEngine;
using UnityEngine.Events;

public sealed class DemoSession : MonoBehaviour
{
    public ToolStation Station;
    public ResettableSceneObject[] SceneObjects = new ResettableSceneObject[0];
    // Resetting fires first so adapters can release held objects; SessionReset follows.
    public UnityEvent Resetting = new UnityEvent();
    public UnityEvent SessionReset = new UnityEvent();

    private void Start()
    {
        // Also records assigned inactive props, whose Awake may not have run.
        foreach (ResettableSceneObject item in SceneObjects)
            if (item != null) item.RecordInitialState();
    }

    public void ResetSession()
    {
        Resetting.Invoke();
        if (Station != null) Station.ClearTools();
        foreach (ResettableSceneObject item in SceneObjects)
            if (item != null) item.RestoreInitialState();
        // Never delete photographs or reset the camera's photo sequence.
        SessionReset.Invoke();
    }
}
