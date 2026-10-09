#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ShopPresentationValidation
{
    private const string Key="ShopPresentationValidationStage";
    private static readonly List<string> results=new List<string>();
    private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/VerificationReview"));
    private static float started;
    private static int frame;
    private static EvidenceCamera evidence;
    private static string failure;
    static ShopPresentationValidation() { EditorApplication.update+=Tick; }

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            JewelryStoreBuilder.CreateStore();
            DemoToolsBuilder.Create();
            ShopCityRefinement.Apply();
            ShopPresentationExpansion.Apply();
            GameObject lightObject=new GameObject("PreviewSun");
            Light sun=lightObject.AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.intensity=0.6f;
            lightObject.transform.rotation=Quaternion.Euler(55,150,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(0.22f,0.22f,0.22f);
            GameObject viewing=new GameObject("DesktopPreviewCamera");
            Camera camera=viewing.AddComponent<Camera>();
            camera.tag="MainCamera";
            camera.nearClipPlane=0.03f;
            camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(0.2f,0.22f,0.25f);
            viewing.AddComponent<AudioListener>();
            View(camera,new Vector3(3.6f,1.65f,0.8f),new Vector3(-0.2f,1,4.5f));
            string dir="Assets/CrimeSceneDemo/Scenes";
            Directory.CreateDirectory(dir);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),dir+"/JewelryStoreReview.unity");
            EditorBuildSettings.scenes=new [] {new EditorBuildSettingsScene(dir+"/JewelryStoreReview.unity",true)};
            Render(camera,"showroom.png");
            View(camera,new Vector3(-.4f,1.6f,2),new Vector3(-2,1,3.7f));
            Render(camera,"display-details.png");
            View(camera,new Vector3(0,1.65f,1.5f),new Vector3(0,2,-18));
            Render(camera,"street-from-inside.png");
            View(camera,new Vector3(3.4f,1.65f,8.8f),new Vector3(2.1f,0.9f,10.5f));
            Render(camera,"safe-room.png");
            // Temporarily hide ceilings solely for the overview capture.
            Transform ceiling=GameObject.Find("JewelryStore_Blockout").transform.Find("Architecture/Ceiling_Optional");
            ceiling.gameObject.SetActive(false);
            View(camera,new Vector3(12,15,-12),new Vector3(0,0,4));
            Render(camera,"layout-overview.png");
            ceiling.gameObject.SetActive(true);
            View(camera,new Vector3(3.6f,1.65f,0.8f),new Vector3(-0.2f,1,4.5f));
            ShopCityRefinement.CreateWalker(camera);
            ShopPresentationExpansion.ConnectDesktop(camera);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(Output,"runtime-results.txt"),"Unity "+Application.unityVersion+"; Built-in pipeline\nExpanded scene generation and 5 actual Unity rendered captures passed.\n");
            SessionState.SetInt(Key,1);
            EditorApplication.isPlaying=true;
        }
        catch(Exception ex) { Finish(false,ex.ToString()); }
    }

    private static void Tick()
    {
        int stage=SessionState.GetInt(Key,0);
        if(stage!=0) EditorApplication.QueuePlayerLoopUpdate();
        if(stage==0 || !EditorApplication.isPlaying) return;
        try
        {
            if(stage==1)
            {
                if(Time.frameCount<10) return;
                started=Time.realtimeSinceStartup;
                ShopWalkController walker=UnityEngine.Object.FindFirstObjectByType<ShopWalkController>();
                Check(walker!=null && walker.transform.position.z>0,"Player spawns inside");
                walker.ReadDesktopInput=false;
                Vector3 start=walker.transform.position;
                walker.MovePlanar(Vector3.forward*.5f);
                Check(walker.transform.position.z>start.z+.3f,"Desktop walking moves player");
                walker.ResetPosition();
                for(int i=0;i<100;i++) walker.MovePlanar(Vector3.back*.05f);
                Check(walker.transform.position.z>=.29f,"Entrance boundary keeps player inside");
                walker.ResetPosition();
                for(int i=0;i<80;i++) walker.MovePlanar(Vector3.right*.05f);
                Check(walker.transform.position.x<4.8f,"Side wall keeps player inside");
                walker.ResetPosition();
                for(int i=0;i<19;i++) walker.MovePlanar(Vector3.left*.05f);
                for(int i=0;i<190;i++) walker.MovePlanar(Vector3.forward*.05f);
                Check(walker.transform.position.z>8.3f,"Rear safe room remains accessible");
                walker.ResetPosition();
                GameObject boundary=GameObject.Find("IndoorBoundary_Front");
                Check(boundary.GetComponent<BoxCollider>()!=null && boundary.GetComponent<Renderer>()==null,
                    "Front boundary blocks movement without blocking view");
                GameObject city=GameObject.Find("CityBackdrop");
                Check(city.GetComponentsInChildren<Collider>().Length==0,"Backdrop has no physics colliders");
                Check(city.GetComponentsInChildren<MeshRenderer>().Length<25,"Backdrop renderer budget");
                int triangles=0;
                foreach(MeshFilter filter in city.GetComponentsInChildren<MeshFilter>())
                    triangles+=filter.sharedMesh.triangles.Length/3;
                Check(triangles<20000,"Backdrop triangle budget");
                results.Add("INFO: City renderers="+city.GetComponentsInChildren<MeshRenderer>().Length+
                    "; triangles="+triangles);
                ToolStation station=UnityEngine.Object.FindFirstObjectByType<ToolStation>();
                Check(station!=null,"Tool station exists");
                DesktopToolPlacement placement=UnityEngine.Object.FindFirstObjectByType<DesktopToolPlacement>();
                placement.ReadDesktopInput=false;
                Check(placement!=null,"Point placement component is connected");
                DeployedTool cone=station.Spawn(DemoToolKind.Cone);
                DeployedTool marker1=station.Spawn(DemoToolKind.Marker);
                DeployedTool marker2=station.Spawn(DemoToolKind.Marker);
                Check(marker1.MarkerNumber==1 && marker2.MarkerNumber==2,"Sequential marker numbers");
                Check(!string.IsNullOrEmpty(marker1.NumberLabel.text),"Marker label assigned");
                cone.PlaceAt(new Vector3(-2,0,2),45);
                Check(Vector3.Distance(cone.transform.position,new Vector3(-2,0,2))<0.001f,"Tool placement");
                placement.BeginPlacement(marker1);
                Vector3 target;
                Check(placement.TryFindPlacement(new Ray(new Vector3(3.2f,1.5f,3),Vector3.down),out target),
                    "Pointing finds an interior floor surface");
                Check(placement.PlaceSelected(target,30),"Point placement commits marker");
                Check(Vector3.Distance(marker1.transform.position,target)<.001f,"Marker reaches pointed surface");
                Check(!placement.PlaceSelected(new Vector3(0,0,-4),0),"Placement rejects exterior position");
                placement.BeginPlacement(marker1);
                placement.CancelPlacement();
                Check(!placement.IsPlacing && marker1.transform.position==target,"Cancel preserves placement");
                Check(!placement.TryFindPlacement(new Ray(new Vector3(0,1,1),Vector3.back),out target),
                    "Front barrier prevents placement through storefront");
                Check(marker2.NumberLabel.font!=null,"Marker has a rendering font");
                GameObject streetDetails=GameObject.Find("StreetDetails");
                Check(streetDetails.GetComponentsInChildren<MeshRenderer>().Length<=6,
                    "Street additions stay within renderer budget");
                Check(streetDetails.GetComponentsInChildren<Collider>().Length==0,
                    "Street additions have no physics colliders");
                DeployedTool post1=station.Spawn(DemoToolKind.TapePost);
                DeployedTool post2=station.Spawn(DemoToolKind.TapePost);
                post1.PlaceAt(new Vector3(0,0,1),0);
                post2.PlaceAt(new Vector3(2,0,1),0);
                station.SelectTapePost(post1);
                station.SelectTapePost(post2);
                SceneTape tape=station.DeploymentRoot.GetComponentInChildren<SceneTape>();
                Check(tape!=null,"Tape connection");
                tape.SendMessage("LateUpdate");
                Check(Mathf.Abs(tape.Ribbon.localScale.z-2)<0.01f,"Tape length");
                post2.PlaceAt(new Vector3(3,0,1),0);
                tape.SendMessage("LateUpdate");
                Check(Mathf.Abs(tape.Ribbon.localScale.z-3)<0.01f,"Tape follows moved post");
                station.RemoveTool(post1);
                tape.SendMessage("LateUpdate");
                evidence=UnityEngine.Object.FindFirstObjectByType<EvidenceCamera>();
                evidence.CaptureFailed.AddListener(message=>failure=message);
                evidence.CaptureFromView(Camera.main.transform);
                frame=Time.frameCount;
                SessionState.SetInt(Key,2);
            }
            else if(stage==2)
            {
                if(Time.realtimeSinceStartup-started>30) throw new Exception("Photo capture timeout: "+evidence.Status);
                if(failure!=null) throw new Exception(failure);
                if(Time.frameCount<=frame+3 || evidence.IsCapturing || evidence.LastPhotoPath==null) return;
                ToolStation station=UnityEngine.Object.FindFirstObjectByType<ToolStation>();
                Check(station.DeploymentRoot.GetComponentInChildren<SceneTape>()==null,"Tape removed after endpoint removal");
                Check(File.Exists(evidence.LastPhotoPath),"Runtime photograph saved");
                Check(Vector3.Distance(evidence.transform.Find("PhotoCamera").localPosition,new Vector3(0,0,.1f))<.001f,
                    "View photograph restores dedicated camera pose");
                File.Copy(evidence.LastPhotoPath,Path.Combine(Output,"runtime-photo.png"),true);
                SessionReviewRecorder review=UnityEngine.Object.FindFirstObjectByType<SessionReviewRecorder>();
                Check(review!=null,"Instructor review connected");
                string jsonPath=Path.Combine(review.ReviewFolder,"session.json");
                Check(File.Exists(jsonPath),"Photograph writes review JSON");
                var document=JsonUtility.FromJson<SessionReviewRecorder.ReviewDocument>(File.ReadAllText(jsonPath));
                Check(document.entries.Count==1 && document.entries[0].tools.Count>=3,"Photo records deployed tools");
                Check(File.Exists(Path.Combine(review.ReviewFolder,document.entries[0].photo)),"Review contains copied photograph");
                Check(File.ReadAllText(Path.Combine(review.ReviewFolder,"review.html")).Contains(document.entries[0].photo),"HTML review references photograph");
                review.SaveSnapshot();
                evidence.transform.position=new Vector3(0,1.5f,4);
                UnityEngine.Object.FindFirstObjectByType<DemoSession>().ResetSession();
                frame=Time.frameCount;
                SessionState.SetInt(Key,3);
            }
            else if(stage==3 && Time.frameCount>frame+2)
            {
                ToolStation station=UnityEngine.Object.FindFirstObjectByType<ToolStation>();
                Check(station.DeploymentRoot.childCount==0,"Reset clears deployed objects");
                Check(station.NextMarkerNumber==1,"Reset restarts marker numbering");
                Check(File.Exists(evidence.LastPhotoPath),"Reset preserves photographs");
                Check(Vector3.Distance(evidence.transform.localPosition,new Vector3(-3.6f,1.1f,1))<0.001f,"Reset restores handheld camera");
                SessionReviewRecorder review=UnityEngine.Object.FindFirstObjectByType<SessionReviewRecorder>();
                var document=JsonUtility.FromJson<SessionReviewRecorder.ReviewDocument>(File.ReadAllText(Path.Combine(review.ReviewFolder,"session.json")));
                Check(document.entries.Count==3,"Manual snapshot and reset append review entries");
                Check(document.entries[2].reason=="Before scene reset" && document.entries[2].tools.Count>=3,"Reset records tools before clearing");
                Finish(true,null);
            }
        }
        catch(Exception ex) { Finish(false,ex.ToString()); }
    }

    private static void Check(bool condition,string label)
    {
        if(!condition) throw new Exception("FAILED: "+label);
        results.Add("PASS: "+label);
    }
    private static void Finish(bool success,string error)
    {
        SessionState.SetInt(Key,0);
        Directory.CreateDirectory(Output);
        File.AppendAllText(Path.Combine(Output,"runtime-results.txt"),
            string.Join("\n",results)+"\n"+(success?"PASS: Validation completed":"FAIL: "+error)+"\n");
        Debug.Log(success?"JEWELRY_VALIDATION_PASS":"JEWELRY_VALIDATION_FAIL "+error);
        EditorApplication.Exit(success?0:1);
    }
    private static void View(Camera camera,Vector3 position,Vector3 target)
    {
        camera.transform.position=position;
        camera.transform.LookAt(target);
    }
    private static void Render(Camera camera,string filename)
    {
        RenderTexture rt=RenderTexture.GetTemporary(1280,720,24);
        RenderTexture previous=RenderTexture.active;
        RenderTexture previousTarget=camera.targetTexture;
        Texture2D image=null;
        try
        {
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output,filename),image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;
            RenderTexture.active=previous;
            RenderTexture.ReleaseTemporary(rt);
            if(image!=null) UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
#endif
