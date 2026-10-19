using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in player smoke test; never runs during normal use.
public sealed class DisplayReviewSmoke : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-displayReviewSmoke")<0)return;
        Application.runInBackground=true;
        var host=new GameObject("DisplayReviewSmoke");
        DontDestroyOnLoad(host);
        host.AddComponent<DisplayReviewSmoke>();
    }
    IEnumerator Start()
    {
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/VerificationDisplayCases"));
        Directory.CreateDirectory(output);
        foreach(string scene in new[]{"DisplayCaseMaterialReview","UserPropsReview","JewelryStoreNight"})
        {
            yield return SceneManager.LoadSceneAsync(scene);
            for(int i=0;i<90;i++)yield return null;
            Camera camera=Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(scene=="JewelryStoreNight")
            {
                camera.transform.position=new Vector3(.3f,1.4f,3.2f);
                camera.transform.LookAt(new Vector3(0,.75f,5.2f));
            }
            var target=new RenderTexture(1920,1080,24){antiAliasing=4};
            var previous=RenderTexture.active;
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            var capture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1920,1080),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(output,"player-"+scene+".png"),capture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;
            target.Release();Destroy(target);Destroy(capture);
            Debug.Log("DISPLAY_PLAYER_PASS "+scene);
        }
        Application.Quit(0);
    }
}
