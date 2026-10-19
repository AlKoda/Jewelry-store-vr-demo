#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static partial class ArtPreviewBuild {
 const string A="Assets/CrimeSceneDemo/ArtPreview";
 static Dictionary<string,Material> mats=new Dictionary<string,Material>();
 static Material Save(string name,Material m){m.name=name;string p=A+"/Generated/"+name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(p);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,p);return m;}
 static Material Pbr(string name){if(mats.ContainsKey(name))return mats[name];var m=new Material(Shader.Find("Standard"));m.color=Color.white;m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Materials/"+name+"_Color.jpg");m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Materials/"+name+"_Normal.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Materials/"+name+"_MetallicSmoothness.png"));m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);m.SetFloat("_BumpScale",.5f);float tile=name=="FloorTile"?1f/2.4f:(new[]{"SuedeCream","VelvetCharcoal","BronzeBrushed","BrushedSteel","SafeSteel","BrassPolished"}.Contains(name)?2f:1f); var tiling=name=="PlasterWarm"?new Vector2(.5f,1):Vector2.one*(name=="CeilingPaint"?.5f:tile); foreach(var slot in new[]{"_MainTex","_BumpMap","_MetallicGlossMap","_OcclusionMap"})m.SetTextureScale(slot,tiling); var ao=AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Materials/"+name+"_AO.jpg");if(ao)m.SetTexture("_OcclusionMap",ao); return mats[name]=Save(name,m);}
 static void Setup(){
 AssetDatabase.Refresh();
 foreach(string p in Directory.GetFiles(A+"/Materials")){if(!(AssetImporter.GetAtPath(p.Replace('\\','/')) is TextureImporter t))continue;t.sRGBTexture=p.Contains("_Color");if(p.Contains("_Normal"))t.textureType=TextureImporterType.NormalMap;t.maxTextureSize=1024;t.SaveAndReimport();}
 foreach(string p in Directory.GetFiles(A+"/Models","*.fbx",SearchOption.AllDirectories)){var i=(ModelImporter)AssetImporter.GetAtPath(p.Replace('\\','/'));i.isReadable=true;i.generateSecondaryUV=true;i.SaveAndReimport();}
 var neutral=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/DisplayCaseGenerated/LowIronGlass.mat"));neutral.color=new Color(1,1,1,.004f);mats["LowIronGlass"]=Save("LowIronGlass",neutral);
 var glass=new Material(mats["LowIronGlass"]);glass.color=new Color(1,1,1,.006f);mats["ClearFractureFaces"]=Save("ClearFractureFaces",glass);
 glass=new Material(mats["ClearFractureFaces"]);glass.color=new Color(.86f,.87f,.88f,.025f);glass.SetFloat("_Smoothness",.87f);mats["FreshCutEdges"]=Save("FreshCutEdges",glass);
 foreach(var pair in new[]{"WalnutVeneer:Walnut","ChampagneBrushed:BronzeBrushed","IvoryMicrosuede:SuedeCream","CharcoalVelvet:VelvetCharcoal","DarkStone:StoneTopDark","JewelryGold:BrassPolished"}){var s=pair.Split(':');mats[s[0]]=Pbr(s[1]);}
 var seal=new Material(Shader.Find("Standard"));seal.color=new Color(.012f,.014f,.016f);seal.SetFloat("_Glossiness",.25f);mats["GraphiteGasket"]=Save("GraphiteGasket",seal);
 mats["Gemstone"]=mats["LowIronGlass"];
 }
 static GameObject Model(string folder,string name,Transform parent,Vector3 pos,Vector3 scale){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(A+"/Models/"+folder+"/"+name+".fbx");if(!prefab)throw new Exception("Missing "+name);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=Vector3.Scale(go.transform.localScale,scale); Debug.Log("ART_TRANSFORM "+name+" scale="+go.transform.localScale+" bounds="+go.GetComponentInChildren<MeshRenderer>().bounds);
 foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){if(r.GetComponent<MeshFilter>().sharedMesh.vertexCount==0 || r.GetComponent<MeshFilter>().sharedMesh.triangles.Length==0){r.enabled=false;continue;} Debug.Log("ART_MESH "+name+"/"+r.name+" slots="+r.sharedMaterials.Length+" submeshes="+r.GetComponent<MeshFilter>().sharedMesh.subMeshCount+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m.name))); if(name.StartsWith("BezelRing")){ if(r.GetComponent<MeshFilter>().sharedMesh.subMeshCount!=2)throw new Exception("Unexpected ring submesh count vertices="+r.GetComponent<MeshFilter>().sharedMesh.vertexCount);r.sharedMaterials=new[]{mats["JewelryGold"],mats["Gemstone"]}; } r.sharedMaterials=r.sharedMaterials.Select(m=>{string n=m.name.Replace(" (Instance)","");if(!mats.ContainsKey(n))throw new Exception("Unmapped material "+n);return mats[n];}).ToArray();bool clear=r.sharedMaterials.Any(m=>m.shader.name.Contains("DisplayGlass"));r.shadowCastingMode=clear?ShadowCastingMode.Off:ShadowCastingMode.On;GameObjectUtility.SetStaticEditorFlags(r.gameObject,clear?0:StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);}
 return go;}
 static Transform Required(string path){var g=GameObject.Find(path);if(!g)throw new Exception("Missing scene object "+path);return g.transform;}
 static void Hide(Transform t){if(t)foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;}
 static void Pane(string path,string model,Vector3 pos,float xscale=1){var t=Required(path);Hide(t.Find("Glass"));Hide(t.Find("Shards"));Hide(t.Find("Cracks"));Model("Glass",model,t,pos,new Vector3(xscale,1,1));}
 public static void Run(){try{
 Setup();
 EditorSceneManager.OpenScene("Assets/CrimeSceneDemo/Scenes/JewelryStoreNight.unity");
 EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),A+"/Scenes/JewelryStoreNight.unity");
 var map=new Dictionary<string,string>{{"KitWalnut","Walnut"},{"CaseWalnut","Walnut"},{"KitSuede","SuedeCream"},{"IvoryMicrosuede","SuedeCream"},{"KitVelvet","VelvetCharcoal"},{"KitBronze","BronzeBrushed"},{"SatinChampagne","BronzeBrushed"},{"KitStone","StoneTop"},{"KitSafeSteel","SafeSteel"},{"KitFloorTile","FloorTile"},{"KitPlaster","PlasterWarm"},{"KitCeiling","CeilingPaint"},{"KitStainless","BrushedSteel"}};
 foreach(var r in Required("NightStore").GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&map.ContainsKey(m.name)?Pbr(map[m.name]):m).ToArray();
 foreach(var n in new[]{"LeftIsland_Rear","RightIsland_Rear"}){var t=Required("NightStore").GetComponentsInChildren<Transform>().First(x=>x.name==n && !x.parent.name.Contains("Dressing") && x.GetComponentsInChildren<Collider>().Length>0);Hide(t);Model("Cases","DisplayCase_Hero",t,Vector3.zero,Vector3.one);Debug.Log("ART_CASE "+t.parent.name+"/"+n);}
 Pane("NightStore/Facade/Bay_2","Window_Cracked",new Vector3(0,.43f,0));
 Pane("NightStore/Facade/Sidelight_Right","Window_Broken",new Vector3(0,.43f,0),.92f/1.92f);
 Pane("NightStore/Facade/EntranceDoors/DoorLeft","DoorGlass_Left_Broken",new Vector3(.4575f,.19f,-.025f));
 Pane("NightStore/Facade/EntranceDoors/DoorRight","DoorGlass_Right_Broken",new Vector3(-.4575f,.19f,-.025f));
 Hide(Required("NightStore/Facade/EntranceDoors").Find("Shards"));
 var d=Required("NightStore/Dressing");Hide(d.Find("EntranceGlass_Outside"));Hide(d.Find("EntranceGlass_Inside"));
 Model("Glass","GroundGlass_Threshold",d,new Vector3(0,.003f,-.65f),Vector3.one);
 Model("Glass","GroundGlass_Sparse",d,new Vector3(0,.003f,.65f),Vector3.one*.8f);
 Model("Glass","GroundGlass_WindowFall",d,new Vector3(1.5f,.003f,-.15f),new Vector3(.65f,1,.8f));
 NightLighting.UseBaked(Required("NightStore"));EnhanceScene();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);EditorSceneManager.SaveOpenScenes();
 EditorSceneManager.OpenScene("Assets/CrimeSceneDemo/Scenes/DisplayCaseMaterialReview.unity");
 foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(root.name=="display_case")Object.DestroyImmediate(root);
 var hero=Model("Cases","DisplayCase_Hero",null,Vector3.zero,Vector3.one);
 Model("Cases","RingTray_Hero",hero.transform,new Vector3(-.47f,.711f,0),Vector3.one);
 Model("Cases","BraceletStand_Hero",hero.transform,new Vector3(.48f,.711f,0),Vector3.one);

 var reviewCamera=Object.FindFirstObjectByType<Camera>();reviewCamera.transform.position=new Vector3(2.3f,1.65f,-2.85f);reviewCamera.transform.LookAt(new Vector3(0,.58f,0)); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),A+"/Scenes/DisplayCaseMaterialReview.unity");
 File.WriteAllText("Assets/CrimeSceneDemo/Resources/DemoBuildInfo.txt","Art preview 2026-10-14 | neutral glass, dim warm headlights, atmosphere, footsteps | revision 2"); AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
 string[] scenes={"Assets/CrimeSceneDemo/Scenes/DemoLauncher.unity",A+"/Scenes/JewelryStoreNight.unity",A+"/Scenes/DisplayCaseMaterialReview.unity","Assets/CrimeSceneDemo/Scenes/UserPropsReview.unity"};
 var result=BuildPipeline.BuildPlayer(scenes,Path.GetFullPath("../Builds/ArtPreview2/JewelryStoreDemo.exe"),BuildTarget.StandaloneWindows64,BuildOptions.None);
 if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed "+result.summary.result);
 Debug.Log("ART_PREVIEW_BUILD_SUCCESS "+result.summary.totalSize);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
