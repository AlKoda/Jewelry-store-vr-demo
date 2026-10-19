#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Original blockout scene: store, tools, desktop player, three captures and
// the shared tool/tape/camera/reset checks. Invoke with -executeMethod.
[InitializeOnLoad]
public static class JewelrySceneValidation
{
    private const string Key = "JewelryValidationStage";
    static JewelrySceneValidation() { EditorApplication.update += Tick; }

    public static void Run()
    {
        try
        {
            DemoValidation.Begin("Verification");
            JewelryStoreBuilder.CreateStore();
            DemoToolsBuilder.Create();
            Camera camera = DemoValidation.CreatePreviewCamera(-30);
            DemoValidation.SaveScene("JewelryStoreDemo");
            DemoValidation.Capture(camera, "showroom.png", new Vector3(3.6f, 1.65f, 0.8f), new Vector3(-0.2f, 1, 4.5f));
            DemoValidation.Capture(camera, "safe-room.png", new Vector3(3.4f, 1.65f, 8.8f), new Vector3(2.1f, 0.9f, 10.5f));
            DemoValidation.CaptureOverview(camera);
            DemoToolsBuilder.CreatePlayer(camera);
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, "Scene generation and 3 actual Unity rendered captures passed.");
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    private static void Tick()
    {
        if (!DemoValidation.Running(Key, out int stage)) return;
        try
        {
            if (stage == 1) { DemoValidation.ToolStage(); SessionState.SetInt(Key, 2); }
            else if (stage == 2 && DemoValidation.PhotoStage()) SessionState.SetInt(Key, 3);
            else if (stage == 3 && DemoValidation.ResetStage()) DemoValidation.Finish(Key, true, null);
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }
}
#endif
