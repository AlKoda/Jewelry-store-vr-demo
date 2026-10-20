using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Opt-in player verification, invoked only by -combinedCheck.
public static class ExpandedPlayerCheck
{
    public static IEnumerator Run(Camera camera,DemoDesktopPanel panel,DemoLighting lighting,ShopWalkController walker,string directory)
    {
        Directory.CreateDirectory(directory);
        var bank=UnityEngine.Object.FindFirstObjectByType<DemoSoundBank>();
        if(bank.FlashlightSwitch.name!="TorchClick_01"||bank.CameraClick.name!="CameraShutter_01")throw new Exception("Selected recorded device sounds missing");
        var root=GameObject.Find("NightStore").transform;
        if(root.Find("Exterior/Buildings/StreetContinuationBuildings").childCount!=8||!root.Find("Dressing/RebuiltCuttingTorch"))throw new Exception("Expanded dressing missing");
        if(root.Find("Exterior/Equipment").gameObject.activeInHierarchy||root.Find("ToolSystem/PhotoFrame").gameObject.activeInHierarchy||root.Find("ToolSystem/StatusBoard").gameObject.activeInHierarchy)throw new Exception("Outdoor equipment remains active");
        var staff=root.Find("StaffRoomFurnishings");if(!staff||staff.GetComponentsInChildren<Renderer>().Length<12)throw new Exception("Staff furniture missing");
        foreach(var sample in root.GetComponentsInChildren<ToolRackSample>())if(sample.transform.position.z<8.5f)throw new Exception("Physical tool left outside");
        foreach(var r in staff.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m&&m.name.StartsWith("Matte_")&&m.HasProperty("_GlossMapScale")&&m.GetFloat("_GlossMapScale")>.271f)throw new Exception("Staff metal remains polished");
        Debug.Log("STAFF_PLAYER_LAYOUT_PASS outdoor equipment hidden, indoor tools, furnished rooms and rough materials");
        var locomotion=walker.GetComponent<XRLocomotion>();
        var door=root.Find("Facade/EntranceDoors/DoorLeft").GetComponent<PushDoorLeaf>();
        camera.transform.localPosition=new Vector3(0,1.6f,0);camera.transform.localRotation=Quaternion.identity;
        door.Close();yield return new WaitForFixedUpdate();var start=door.transform.TransformPoint(new Vector3(.45f,0,-.65f));XRLocomotion.Relocate(walker.transform,start);
        for(int i=0;i<100;i++){locomotion.SmoothMove(Vector2.up,.02f);yield return new WaitForFixedUpdate();}
        if(door.OpeningDegrees<5||walker.transform.position.z-start.z<.8f)throw new Exception("Smooth VR walking did not push and pass front door");
        door.Close();XRLocomotion.Relocate(walker.transform,new Vector3(0,0,-2));yield return new WaitForFixedUpdate();
        Debug.Log("EXPANDED_SMOOTH_VR_DOOR_PASS");

        var savedPosition=walker.transform.position;var savedRotation=walker.transform.rotation;var savedView=camera.transform.localRotation;
        walker.transform.rotation=Quaternion.identity;camera.transform.localRotation=Quaternion.Euler(0,180,0);XRLocomotion.Relocate(walker.transform,new Vector3(-23,0,-2));
        for(int i=0;i<500;i++){locomotion.SmoothMove(Vector2.up,.02f);if(i%30==0)yield return new WaitForFixedUpdate();}
        if(walker.transform.position.z> -14.4f||Mathf.Abs(walker.transform.position.y)>.03f)throw new Exception("Expanded road cannot be crossed between both pavements");
        walker.transform.rotation=savedRotation;camera.transform.localRotation=savedView;XRLocomotion.Relocate(walker.transform,savedPosition);
        Debug.Log("PLAYABLE_ROAD_CROSSING_PASS continuous bounds and both kerb heights");

        var traces=UnityEngine.Object.FindObjectsByType<UVTrace>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("EvidenceUV_")).ToArray();
        if(traces.Length!=44)throw new Exception("Expected 44 attached evidence traces");
        var trace=traces.First(t=>t.name=="EvidenceUV_WipedGlassSmear");
        lighting.UVTorch(false);lighting.Flashlight(true);lighting.SettleUVTraces();
        if(traces.Any(t=>t.Visibility!=0))throw new Exception("Evidence visible without UV");
        camera.transform.position=trace.transform.position+trace.Normal*.85f+Vector3.up*.04f;camera.transform.LookAt(trace.transform.position);
        yield return null;yield return null;Capture(camera,directory,"evidence-white-light");
        lighting.Flashlight(false);lighting.UVTorch(true);yield return null;yield return null;
        lighting.UVTorchLight.transform.LookAt(trace.transform.position);lighting.SettleUVTraces();
        if(trace.Visibility<.99f)throw new Exception("UV trace did not appear in direct UV beam");
        foreach(var t in traces)t.GetComponent<Renderer>().enabled=false;
        var hidden=Pixels(camera);
        foreach(var t in traces)t.GetComponent<Renderer>().enabled=true;
        var shown=Pixels(camera);int greenPixels=0;
        for(int i=0;i<shown.Length;i++)if(shown[i].g-hidden[i].g>8&&(shown[i].g-hidden[i].g)>(shown[i].r-hidden[i].r)*1.4f)greenPixels++;
        Debug.Log("UV_RENDER_PIXEL_CHECK green pixels="+greenPixels+" globals="+Shader.GetGlobalVector(DemoLighting.UVParamsId)+" position="+trace.transform.position+" local="+trace.transform.localPosition);
        if(greenPixels<10)throw new Exception("UV evidence gating passed but the green trace was not rendered");
        Capture(camera,directory,"evidence-uv-light");
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="UV verification occluder";blocker.transform.position=(lighting.UVTorchLight.transform.position+trace.transform.position)*.5f;blocker.transform.localScale=Vector3.one*.20f;Physics.SyncTransforms();lighting.SettleUVTraces();
        if(trace.Visibility!=0)throw new Exception("UV trace visible through blocker");UnityEngine.Object.Destroy(blocker);yield return null;
        var local=trace.transform.localPosition;var previous=trace.transform.position;door.transform.localRotation*=Quaternion.Euler(0,25,0);Physics.SyncTransforms();
        if(trace.transform.localPosition!=local||Vector3.Distance(previous,trace.transform.position)<.1f)throw new Exception("Evidence did not follow the door");door.Close();lighting.UVTorch(false);lighting.SettleUVTraces();
        Debug.Log("EXPANDED_UV_GATING_PASS 44 marks, white-light hidden, UV reveals, occlusion hides, traces follow door");

        foreach(string name in new[]{"EvidenceUV_BackgroundPrint_0","EvidenceUV_FloorDrops_0"}){
            var mark=traces.First(t=>t.name==name);
            camera.transform.position=mark.transform.position+mark.Normal*.42f;camera.transform.LookAt(mark.transform.position);
            lighting.UVTorch(false);lighting.Flashlight(true);yield return null;lighting.SettleUVTraces();Capture(camera,directory,name+"-white");
            if(mark.Visibility!=0)throw new Exception("Background mark fluoresces in white light");
            lighting.Flashlight(false);lighting.UVTorch(true);yield return null;yield return null;lighting.UVTorchLight.transform.LookAt(mark.transform.position);lighting.SettleUVTraces();
            if(mark.Visibility<.99f)throw new Exception("Distributed trace blocked by its own surface: "+name);
            mark.GetComponent<Renderer>().enabled=false;var before=Pixels(camera);mark.GetComponent<Renderer>().enabled=true;var after=Pixels(camera);int green=0;
            for(int i=0;i<after.Length;i++)if(after[i].g-before[i].g>5&&(after[i].g-before[i].g)>(after[i].r-before[i].r)*1.4f)green++;
            if(green<5)throw new Exception("Distributed UV trace failed rendered pixel check: "+name+" pixels="+green);
            Capture(camera,directory,name+"-uv");Debug.Log("STAFF_UV_RENDER_PASS "+name+" green pixels="+green);
        }
        lighting.UVTorch(false);lighting.Flashlight(true);lighting.SettleUVTraces();
        foreach(var sample in new[]{new[]{new Vector3(-4.1f,1.6f,9.1f),new Vector3(-5.5f,1,10.7f)},new[]{new Vector3(3.6f,1.6f,9.1f),new Vector3(6.6f,1,10.6f)}}){camera.transform.position=sample[0];camera.transform.LookAt(sample[1]);yield return null;Capture(camera,directory,sample[0].x<0?"staff-security-room":"staff-break-room");}
        var station=UnityEngine.Object.FindFirstObjectByType<ToolStation>();var tape=UnityEngine.Object.Instantiate(station.TapePrefab);
        if(tape.FrontWarning||tape.BackWarning||tape.Ribbon.GetComponent<Renderer>().sharedMaterial.mainTexture.name!="SceneTape")throw new Exception("Tape print is not integrated Arabic material");
        var a=new GameObject("Tape check A");var b=new GameObject("Tape check B");a.transform.position=new Vector3(-1.28f,1.05f,-1.8f);b.transform.position=new Vector3(1.28f,1.05f,-1.8f);tape.StartAnchor=a.transform;tape.EndAnchor=b.transform;tape.Refresh();
        lighting.Flashlight(true);camera.transform.position=new Vector3(0,1.22f,-3.05f);camera.transform.LookAt(new Vector3(0,1.04f,-1.8f));yield return null;Capture(camera,directory,"arabic-tape-front");
        camera.transform.position=new Vector3(0,1.2f,-.55f);camera.transform.LookAt(new Vector3(0,1.04f,-1.8f));yield return null;Capture(camera,directory,"arabic-tape-back");
        UnityEngine.Object.Destroy(tape.gameObject);UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);lighting.Flashlight(false);
        Debug.Log("EXPANDED_PRINTED_TAPE_PASS no floating labels, Arabic ink texture, two lit faces");

        Vector3[] positions={new Vector3(-9,2,-8),new Vector3(15,2,-12),new Vector3(5.4f,1.6f,9.2f),new Vector3(-5.4f,1.5f,9.3f),new Vector3(-.1f,1.2f,7.7f),new Vector3(1,2,-7)};
        Vector3[] targets={new Vector3(-11.8f,5,-.12f),new Vector3(22,5,-16.7f),new Vector3(6.9f,1.15f,11.3f),new Vector3(-6.8f,1,11.3f),new Vector3(-.3f,.35f,9),new Vector3(0,3.2f,.1f)};
        string[] names={"near-fire-escape","far-fire-escape","stock-room","office-shelves","cutting-torch","arabic-store-sign"};
        for(int i=0;i<names.Length;i++){camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);yield return null;Capture(camera,directory,names[i]);}

        DemoLanguage.Arabic=true;
        if(!DemoLanguage.Font||!DemoLanguage.HasArabic(DemoLanguage.Text("Settings"))||DemoLanguage.Shape("العربية")=="العربية")throw new Exception("Arabic UI resource/shaping missing");
        var printed=GameObject.Find("NightStore").GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).First(m=>m&&m.HasProperty("_MainTex")&&m.mainTexture&&m.mainTexture.name=="ShopFascia_Designed").mainTexture;
        panel.HudVisible=true;panel.Layout=PanelLayout.Full;panel.Tab=PanelTab.Settings;panel.Scale=1.25f;
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-arabic-settings.png"));yield return null;
        panel.HelpOpen=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-arabic-help.png"));yield return null;panel.HelpOpen=false;
        foreach(float scale in new[]{1f,1.25f,2f}){
            panel.Scale=scale;panel.Layout=PanelLayout.Hidden;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-arabic-hotbar-"+scale.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return null;
            panel.Layout=PanelLayout.Full;panel.Tab=PanelTab.Tools;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-arabic-tools-"+scale.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return null;
        }
        panel.Scale=1.25f;panel.Tab=PanelTab.Settings;
        DemoLanguage.Arabic=false;if(DemoLanguage.Text("Settings")!="Settings"||printed.name!="ShopFascia_Designed")throw new Exception("UI language switch changed map signs");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-english-settings.png"));yield return null;
        foreach(float scale in new[]{1f,1.25f,2f}){
            panel.Scale=scale;panel.Layout=PanelLayout.Hidden;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-english-hotbar-"+scale.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return null;
            panel.Layout=PanelLayout.Full;panel.Tab=PanelTab.Tools;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ui-english-tools-"+scale.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return null;
        }
        panel.Scale=1.25f;
        DemoLanguage.Arabic=true;panel.Layout=PanelLayout.Hidden;panel.HudVisible=false;
        Debug.Log("EXPANDED_LANGUAGE_PASS Arabic shaping/font, Arabic-English roundtrip, permanent Arabic map");
    }
    static void Capture(Camera c,string directory,string name)
    {
        var rt=new RenderTexture(1600,900,24){antiAliasing=4};var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());c.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
    }
    static Color32[] Pixels(Camera c)
    {
        var rt=new RenderTexture(800,450,24);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(800,450,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,800,450),0,0);t.Apply();var pixels=t.GetPixels32();c.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(t);return pixels;
    }
}

public sealed class ExpandedVerificationRunner:MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Init(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-expandedOnly")<0)return;Application.runInBackground=true;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)Application.Quit(1);};var g=new GameObject("Expanded verification");UnityEngine.Object.DontDestroyOnLoad(g);g.AddComponent<ExpandedVerificationRunner>();}
    IEnumerator Start(){yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("JewelryStoreNight");for(int i=0;i<30;i++)yield return null;var camera=Camera.main;var panel=UnityEngine.Object.FindFirstObjectByType<DemoDesktopPanel>();panel.Layout=PanelLayout.Hidden;panel.HudVisible=false;panel.HelpOpen=false;var walker=UnityEngine.Object.FindFirstObjectByType<ShopWalkController>();walker.ReadDesktopInput=false;
        foreach(var x in UnityEngine.Object.FindObjectsByType<XRLocomotion>(FindObjectsSortMode.None))x.enabled=false;
        foreach(var x in UnityEngine.Object.FindObjectsByType<DemoModeSwitch>(FindObjectsSortMode.None))x.enabled=false;
        foreach(var x in UnityEngine.Object.FindObjectsByType<VRDoorBodyPush>(FindObjectsSortMode.None))x.enabled=false;
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/VerificationStaffRooms20261018"));yield return ExpandedPlayerCheck.Run(camera,panel,UnityEngine.Object.FindFirstObjectByType<DemoLighting>(),walker,directory);Debug.Log("EXPANDED_ONLY_PASS");Application.Quit(0);}
}
