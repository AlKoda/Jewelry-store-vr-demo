using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

// Mode selection is independent of rig switching. XR itself must be configured in
// project settings; this class never pretends that component toggles start a loader.
public sealed class DemoLaunchOptions:MonoBehaviour
{
    public static DemoModeSwitch.DemoMode Requested { get; private set; }=DemoModeSwitch.DemoMode.Auto;
    static string failure;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init(){
        Requested=DemoModeSwitch.DemoMode.Auto;failure=null;
        // The Quest 3 app (Android build) is VR only: straight into the night scene, no monitor menu.
        if(Application.platform==RuntimePlatform.Android)Requested=DemoModeSwitch.DemoMode.VR;
        var args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++)if(args[i]=="-demo-mode"){
            if(args[i+1].Equals("desktop",StringComparison.OrdinalIgnoreCase))Requested=DemoModeSwitch.DemoMode.Desktop;
            else if(args[i+1].Equals("vr",StringComparison.OrdinalIgnoreCase))Requested=DemoModeSwitch.DemoMode.VR;
        }
        var g=new GameObject("Delivery launch mode");DontDestroyOnLoad(g);g.AddComponent<DemoLaunchOptions>();
    }
    IEnumerator Start(){
        if(Requested!=DemoModeSwitch.DemoMode.VR)yield break;
        var manager=XRGeneralSettings.Instance!=null?XRGeneralSettings.Instance.Manager:null;
        if(manager!=null){
            if(manager.activeLoader==null)yield return manager.InitializeLoader();
            if(manager.activeLoader!=null)manager.StartSubsystems();
        }
        float deadline=Time.realtimeSinceStartup+20;
        while(!XRSettings.isDeviceActive && Time.realtimeSinceStartup<deadline)yield return null;
        if(!XRSettings.isDeviceActive){
            failure="VR did not start. Connect Quest Link/Air Link and enable the OpenXR runtime. Desktop controls remain available.";
            Debug.LogWarning(failure);Requested=DemoModeSwitch.DemoMode.Desktop;yield break;
        }
        // Headset entry bypasses monitor-only IMGUI. Load once, not again on return.
        for(int i=1;i<SceneManager.sceneCountInBuildSettings;i++){
            string path=SceneUtility.GetScenePathByBuildIndex(i);
            if(path.Contains("JewelryStoreNight")){yield return SceneManager.LoadSceneAsync(i);yield break;}
        }
        failure="The VR player has no night-store scene. Use the complete tested player folder.";
        Debug.LogWarning(failure);
    }
    void OnDestroy(){if(Requested==DemoModeSwitch.DemoMode.VR&&XRGeneralSettings.Instance!=null){var manager=XRGeneralSettings.Instance.Manager;if(manager!=null){manager.StopSubsystems();manager.DeinitializeLoader();}}}
    void OnGUI(){if(!string.IsNullOrEmpty(failure))DemoI18n.Label(new Rect(20,Screen.height-110,Mathf.Max(200,Screen.width-40),90),failure,GUI.skin.box);}
}
