using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ArtPreviewCapture:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Init(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-artPreviewSmoke")<0)return;Application.runInBackground=true;var g=new GameObject("ArtPreviewCapture");DontDestroyOnLoad(g);g.AddComponent<ArtPreviewCapture>();}
 IEnumerator Start(){foreach(string scene in new[]{"DisplayCaseMaterialReview","UserPropsReview","JewelryStoreNight"}){yield return SceneManager.LoadSceneAsync(scene);for(int i=0;i<90;i++)yield return null;var c=Camera.main??UnityEngine.Object.FindFirstObjectByType<Camera>();
 if(scene=="JewelryStoreNight"){
 var panel=UnityEngine.Object.FindFirstObjectByType<DemoDesktopPanel>();panel.HudVisible=true; panel.Scale=1.25f;var light=UnityEngine.Object.FindFirstObjectByType<DemoLighting>(); light.Flashlight(false);light.UVTorch(false);var use=typeof(DemoDesktopPanel).GetMethod("UseSlot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);use.Invoke(panel,new object[]{7});if(!light.FlashlightOn)throw new Exception("White click-on failed");use.Invoke(panel,new object[]{7});if(light.FlashlightOn)throw new Exception("White click-off failed");use.Invoke(panel,new object[]{8});if(!light.UVTorchOn)throw new Exception("UV click-on failed");use.Invoke(panel,new object[]{8});if(light.UVTorchOn)throw new Exception("UV click-off failed");if(DemoSounds.Staged(DemoSound.Switch)==null)throw new Exception("Switch audio missing");Debug.Log("ART_TOGGLE_PASS white on/off UV on/off with switch clip");

 foreach(var b in c.GetComponentsInParent<MonoBehaviour>())b.enabled=false;
 c.transform.position=new Vector3(.3f,1.4f,3.2f);c.transform.LookAt(new Vector3(0,.75f,5.2f));Capture(c,"night-interior");
 c.transform.position=new Vector3(2.7f,1.65f,-3.3f);c.transform.LookAt(new Vector3(.3f,1.15f,0));Capture(c,"entrance-glass");
 c.transform.position=new Vector3(.6f,.9f,-1.7f);c.transform.LookAt(new Vector3(.2f,.02f,-.45f));Capture(c,"ground-glass");
 c.transform.position=new Vector3(-2.8f,1.35f,4.7f);c.transform.LookAt(new Vector3(-4.6f,.8f,5.8f));Capture(c,"rear-case");light.ToggleFlashlight();yield return null;Capture(c,"rear-case-flashlight-on");light.ToggleFlashlight();
 c.transform.position=new Vector3(0,1.6f,-2);c.transform.LookAt(new Vector3(9,3,-20));Capture(c,"exterior-atmosphere");
 panel.enabled=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/VerificationArtPreview/hud-revision2.png")));yield return null;

 }else Capture(c,scene);Debug.Log("ART_PLAYER_PASS "+scene);}Application.Quit(0);}
 static void Capture(Camera c,string name){string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/VerificationArtPreview"));Directory.CreateDirectory(dir);var rt=new RenderTexture(1920,1080,24){antiAliasing=4};var prev=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=prev;rt.Release();Destroy(rt);Destroy(t);}
}
