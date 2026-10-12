using UnityEngine;
using UnityEngine.SceneManagement;

// Survives scene loads: F10 in any map goes back to the launcher scene.
public sealed class ReturnToLauncher : MonoBehaviour
{
    private static ReturnToLauncher instance;
    private int launcherIndex;

    public static void Ensure(int launcherIndex)
    {
        if (instance != null) { instance.launcherIndex = launcherIndex; return; }
        GameObject go = new GameObject("ReturnToLauncher");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ReturnToLauncher>();
        instance.launcherIndex = launcherIndex;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10) && SceneManager.GetActiveScene().buildIndex != launcherIndex)
            SceneManager.LoadScene(launcherIndex);
    }
}
