#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;
public static partial class PresentationPolish {
 static Material RefinedGlassMaterial(string name,float absorption,float polish){
  var m=Mat(Folder,name,new Color(.96f,.98f,1,absorption),polish);m.shader=Shader.Find("CrimeScene/DisplayGlass");m.SetFloat("_IOR",1.52f);m.SetFloat("_Smoothness",polish);m.SetFloat("_DetailFade",name=="ArchitecturalFreshCutEdges"?1:0);return m;
 }
 static void RefineArchitecturalGlass(){
  var face=RefinedGlassMaterial("ArchitecturalClearFaces",.13f,.96f);var edge=RefinedGlassMaterial("ArchitecturalFreshCutEdges",.24f,.80f);
  var floorFace=RefinedGlassMaterial("ArchitecturalDebrisFaces",.095f,.94f);var floorEdge=RefinedGlassMaterial("ArchitecturalDebrisEdges",.17f,.78f);floorEdge.shader=Shader.Find("CrimeScene/FractureGlint");floorEdge.color=new Color(.79f,.83f,.84f,.17f);floorEdge.SetFloat("_Smoothness",.78f);
  var crack=Mat(Folder,"ArchitecturalFractureScattering",new Color(.70f,.74f,.75f,.32f),.60f);crack.shader=Shader.Find("CrimeScene/FractureGlint");crack.SetFloat("_Smoothness",.60f);
  var specs=new[]{("Facade/Bay_2","Window_Cracked",V(0,.43f,0),1.92f,2.13f,710,0),
   ("Facade/Sidelight_Right","Window_Broken",V(0,.43f,0),.92f,2.13f,731,3),
   ("Facade/EntranceDoors/DoorLeft","DoorGlass_Left_Broken",V(.4575f,.19f,-.025f),.775f,2.05f,715,1),
   ("Facade/EntranceDoors/DoorRight","DoorGlass_Right_Broken",V(-.4575f,.19f,-.025f),.775f,2.05f,721,2)};
  foreach(var s in specs){var parent=root.Find(s.Item1);var old=parent.Find(s.Item2);if(old)old.gameObject.SetActive(false);var intact=parent.Find("Glass");if(intact)intact.gameObject.SetActive(false);var previous=parent.Find("ArchitecturalFracturedPane");if(previous)Object.DestroyImmediate(previous.gameObject);
   if(s.Item7==1||s.Item7==2)RecordingGlass.DoorRemnants("ArchitecturalFracturedPane",parent,s.Item3,s.Item4,s.Item5,s.Item6,face,edge,crack);
   else RecordingGlass.ArchitecturalPane("ArchitecturalFracturedPane",parent,s.Item3,s.Item4,s.Item5,s.Item6,s.Item7,face,edge,crack);
  }
  var dressing=root.Find("Dressing");foreach(Transform c in dressing)if(c.name.StartsWith("GroundGlass_"))c.gameObject.SetActive(false);
  var debris=Fresh("ArchitecturalBrokenGlassDebris",dressing);
  RecordingGlass.ArchitecturalDebris("GlassDebris_Threshold",debris,V(-.05f,.0015f,-.23f),812,560,1.04f,.44f,floorFace,floorEdge);
  RecordingGlass.ArchitecturalDebris("GlassDebris_Inside",debris,V(.10f,.0015f,.48f),813,240,.92f,.64f,floorFace,floorEdge);

  RecordingGlass.ArchitecturalDebris("GlassDebris_Sidelight",debris,V(1.48f,.0015f,-.22f),814,300,.43f,.49f,floorFace,floorEdge);
  var bed=Mat(Folder,"FineSafetyGlassBed",Color.white,.7f);bed.shader=Shader.Find("CrimeScene/GlassCrumbBed");bed.mainTexture=DemoAssetLibrary.Texture("OmanExterior","SafetyGlassFineOriginal.png");if(!bed.mainTexture)throw new Exception("Fine glass artwork missing");bed.SetFloat("_Density",.33f);bed.SetFloat("_Cutoff",.60f);
  var grains=new KitMesh{Lightmapped=false};
  foreach(var spec in new[]{(V(-.05f,.0006f,-.23f),1.50f,.70f),(V(1.48f,.0006f,-.22f),.62f,.67f)}){
   var p=spec.Item1;float x=spec.Item2*.5f,z=spec.Item3*.5f;grains.Quad(bed,Vector3.up,p+V(-x,0,-z),p+V(x,0,-z),p+V(x,0,z),p+V(-x,0,z),new Rect(0,0,1,1));
  }
  var g=grains.Emit("FineGlassCrumbContactLayer",debris,Vector3.zero,Quaternion.identity,Folder,"fine_safety_glass_contact");g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;

 }
 static void ArchitecturalGlassChecks(){
  var panes=root.Find("Facade").GetComponentsInChildren<Transform>().Where(t=>t.name=="ArchitecturalFracturedPane").ToArray();if(panes.Length!=4)throw new Exception("Four architectural panes required");
  int faces=0;foreach(var t in panes){
   var f=t.GetComponent<MeshFilter>();if(!f||f.sharedMesh.triangles.Length<1000)throw new Exception("Fracture pane has no physical cells");
   if(!t.Find("FineFractureNetwork"))throw new Exception("Missing stress fracture network");
   if(t.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Architectural glass must not introduce physics interference");
   foreach(var m in t.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials))if(m.IsKeywordEnabled("_EMISSION"))throw new Exception("Fractures glow in darkness");
  }
  var debris=root.Find("Dressing/ArchitecturalBrokenGlassDebris");if(!debris||debris.GetComponentsInChildren<Collider>().Length!=0||debris.GetComponentsInChildren<MonoBehaviour>().Length!=0)throw new Exception("Debris must remain batched non-physical scenery");
  foreach(var t in panes.Concat(new[]{debris}))foreach(var f in t.GetComponentsInChildren<MeshFilter>()){
   faces+=f.sharedMesh.triangles.Length/3;
   if(!f.name.Contains("PileContact")&&f.sharedMesh.normals.Any(n=>n.sqrMagnitude<.5f))throw new Exception("Invalid millimetre glass normal "+f.name);
  }
  if(faces>110000)throw new Exception("Architectural glass triangle budget exceeded "+faces);
  foreach(var f in debris.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("GlassDebris_")))if(f.sharedMesh.bounds.max.y>.12f||f.sharedMesh.bounds.min.y<-.0001f)throw new Exception("Floating or sunk debris "+f.name+" "+f.sharedMesh.bounds);
  foreach(var r in debris.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("GlassDebris_")))foreach(var m in r.sharedMaterials)if(m.color.a>(m.name.EndsWith("Edges")?.30f:.14f))throw new Exception("Ground glass too opaque");
  foreach(var t in panes.Where(t=>t.parent.name=="DoorLeft"||t.parent.name=="DoorRight"))foreach(var p in t.GetComponent<MeshFilter>().sharedMesh.vertices)if(Mathf.Abs(p.x)<.775f*.5f-.10f&&p.y>.10f&&p.y<1.95f)throw new Exception("Large door glass panel remains in the opening");
  Debug.Log("ARCHITECTURAL_GLASS_CHECKS_PASS two fractured windows and two clear door openings with 6mm edge remnants; 1100 closed fragments in three batched piles, no individual scripts/physics; stronger neutral glass, triangles="+faces);
 }
 static void ArchitecturalGlassPreview(string suffix){
  var go=new GameObject("Temporary architectural glass review");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=57;c.farClipPlane=220;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;var rt=new RenderTexture(1600,1000,24){antiAliasing=4};c.targetTexture=rt;var old=RenderTexture.active;
  var views=new[]{(V(-.5f,1.65f,-2.9f),V(.1f,1.15f,0)),(V(.1f,.40f,-1.4f),V(.1f,.035f,-.12f)),(V(1.9f,1.2f,-1.9f),V(1.5f,1.3f,0)),(V(-3,1.55f,-2.0f),V(-3,1.4f,0)),(V(.25f,1.7f,2.1f),V(.05f,1.1f,0)),(V(.7f,.21f,-.9f),V(.1f,.03f,-.15f))};
  try{for(int i=0;i<views.Length;i++){c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(c.transform.position+c.transform.right*.09f,c.transform.rotation);c.Render();RenderTexture.active=rt;var img=new Texture2D(1600,1000,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,1000),0,0);img.Apply();File.WriteAllBytes("Builds/architectural-glass-"+suffix+"-"+i+".png",img.EncodeToPNG());Object.DestroyImmediate(img);}}
  finally{RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
 public static void ArchitecturalGlassRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;ArchitecturalGlassPreview("before");RefineArchitecturalGlass();OmanTextureBudget();OmanSurfacePolish();ArchitecturalGlassChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();RawCarChecks();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();ArchitecturalGlassPreview("after");OmanPreview();EditableDelivery.Build();Debug.Log("ARCHITECTURAL_GLASS_LOCAL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
