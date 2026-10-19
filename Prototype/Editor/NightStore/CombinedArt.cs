#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class CombinedArt {
 const string F="Assets/CrimeSceneDemo/CombinedArt";
 static Material face,edge;
 static Material Save(string name,Material m){m.name=name;string path=F+"/"+name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,path);return m;}
 static void Hide(Transform t){if(t)foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;}
 static Transform At(string path){var g=GameObject.Find(path);if(!g)throw new Exception("Missing "+path);return g.transform;}
 static GameObject Glass(string model,Transform parent,Vector3 position,Vector3 scale){
 return RecordingGlass.Create(model,parent,position,scale,face,edge);
 }
 static void Pane(string path,string model,Vector3 position,float scaleX=1){var t=At(path);Hide(t.Find("Glass"));Hide(t.Find("Shards"));Glass(model,t,position,new Vector3(scaleX,1,1));}
 public static void ImproveCaseShards(){
 foreach(string name in new[]{"CentralCase_Left","CentralCase_Right","RightIsland_Front"}){
  var t=At("NightStore/Showroom/"+name);Hide(t.Find("Shards"));
  Glass("GroundGlass_Sparse",t,new Vector3(0,.711f,0),new Vector3(.80f,1,.32f));
  Glass("GroundGlass_WindowFall",t,new Vector3(0,.002f,-.6f),new Vector3(.8f,1,.5f));
  Glass("Window_Broken",t,new Vector3(0,.71f,-.3075f),new Vector3(1.69f/1.92f,.29f/2.13f,1));
  if(name=="RightIsland_Front"){
   Glass("Window_Broken",t,new Vector3(0,.71f,.3075f),new Vector3(1.69f/1.92f,.29f/2.13f,1));
   foreach(float side in new[]{-1f,1f}){var g=Glass("Window_Broken",t,new Vector3(side*.8825f,.71f,0),new Vector3(.54f/1.92f,.29f/2.13f,1));g.transform.localRotation=Quaternion.Euler(0,90,0)*g.transform.localRotation;}
  }
 }
 Debug.Log("CASE_SHARDS_PASS closed-thickness fragments on decks, in frames and on floor");
 }
 public static void Apply(){
 if(!AssetDatabase.IsValidFolder(F))AssetDatabase.CreateFolder("Assets/CrimeSceneDemo","CombinedArt");
 face=new Material(Shader.Find("CrimeScene/DisplayGlass"));face.color=new Color(.96f,.98f,1,.018f);face.SetFloat("_IOR",1.52f);face.SetFloat("_Smoothness",.97f);face=Save("NeutralGlassFaces",face);
 edge=new Material(face);edge.color=new Color(.78f,.84f,.89f,.18f);edge.SetFloat("_Smoothness",.9f);edge=Save("NeutralGlassEdges",edge);
 var rim=new Material(Shader.Find("Standard"));rim.color=new Color(.14f,.15f,.16f);rim.SetFloat("_Glossiness",.72f);rim=Save("GlassCutRim",rim);
 foreach(string zone in new[]{"Facade","Showroom","Dressing"})foreach(var r in At("NightStore/"+zone).GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;if(m.name=="KitGlassEdge")return rim;if(m==StoreKit.Glass || m==StoreKit.ShardGlass)return face;return m;}).ToArray();
 Pane("NightStore/Facade/Bay_2","Window_Cracked",new Vector3(0,.43f,0));
 Pane("NightStore/Facade/Sidelight_Right","Window_Broken",new Vector3(0,.43f,0),.92f/1.92f);
 Pane("NightStore/Facade/EntranceDoors/DoorLeft","DoorGlass_Left_Broken",new Vector3(.4575f,.19f,-.025f));
 Pane("NightStore/Facade/EntranceDoors/DoorRight","DoorGlass_Right_Broken",new Vector3(-.4575f,.19f,-.025f));
 Hide(At("NightStore/Facade/EntranceDoors").Find("Shards"));var d=At("NightStore/Dressing");Hide(d.Find("EntranceGlass_Outside"));Hide(d.Find("EntranceGlass_Inside"));
 Glass("GroundGlass_Threshold",d,new Vector3(0,.003f,-.65f),Vector3.one);
 Glass("GroundGlass_Sparse",d,new Vector3(0,.003f,.65f),Vector3.one*.8f);
 Glass("GroundGlass_WindowFall",d,new Vector3(1.5f,.003f,-.15f),new Vector3(.65f,1,.8f));
 int panes=At("NightStore/Facade").GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Window_")||t.name.StartsWith("DoorGlass_"));if(panes!=4)throw new Exception("Expected four new fracture meshes, found "+panes);
 Debug.Log("COMBINED_ART_PASS neutral dielectric glass, four fracture panes and three ground scatters");
 }
}
public static class CombinedBuild {
 public static UnityEditor.Build.Reporting.BuildReport GenerateAndBuild(string output){
 if(File.Exists(Path.Combine(Application.dataPath,"../.user-editable-project")))throw new Exception("Use EditableDelivery.Build to preserve manual scene edits.");
 NightStoreBuilder.BuildScene(false,false);CombinedArt.Apply();var root=GameObject.Find("NightStore").transform;
 NightLighting.UseBaked(root);SceneReviewPass.Apply();CombinedArt.ImproveCaseShards();UrbanDetailPass.Apply();StaffRoomPolish.Apply();ArabicScenePass.Apply();SceneReviewValidation.Check();DemoAssetLibrary.Credit("NightSky","rooftop_night_1k");DemoAssetLibrary.WriteCredits("Jewelry store combined review — third-party credits; user-supplied car and project-owned city backdrop.");
 EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
 DemoBuild.CreateLauncherScene();
 File.WriteAllText(DemoBuild.BuildInfoAsset,"October 18 | Arabic store signs, worn printed tape, fitted bilingual UI, expanded buildings, UV evidence | baked");AssetDatabase.Refresh();AssetDatabase.SaveAssets();
 string[] scenes={DemoBuild.LauncherScene,DemoValidation.ScenesFolder+"/JewelryStoreNight.unity"};EditorBuildSettings.scenes=scenes.Select(s=>new EditorBuildSettingsScene(s,true)).ToArray();
 Directory.CreateDirectory(Path.GetDirectoryName(output));DemoBuild.CopyLicenses(Path.GetDirectoryName(output));var result=BuildPipeline.BuildPlayer(scenes,output,BuildTarget.StandaloneWindows64,BuildOptions.StrictMode);
 if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed "+result.summary.result);
 Debug.Log("COMBINED_BUILD_PASS "+output+" bytes="+result.summary.totalSize);return result;
 }
 public static void Run(){try{
 string output=Environment.GetEnvironmentVariable("JEWELRY_COMBINED_OUTPUT");if(string.IsNullOrEmpty(output))output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Review20261017/JewelryStoreDemo.exe"));
 GenerateAndBuild(output);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
