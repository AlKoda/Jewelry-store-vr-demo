#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class PresentationPolish {
 public static void IntegrationRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
  root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  string car=CarGeometrySignature();
  EvidenceGameplay();FieldworkChecks();
  // Checks deliberately collect evidence; recreate only fieldwork for the delivered scene.
  EvidenceGameplay();
  foreach(var guide in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FieldGuide").ToArray())Object.DestroyImmediate(guide.gameObject);
  int moving=0;foreach(var door in root.GetComponentsInChildren<PushDoorLeaf>(true))
   foreach(var t in door.GetComponentsInChildren<Transform>(true)){GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);moving++;}
  var materials=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="CrimeScene/DisplayGlass").Distinct();
  foreach(var m in materials){
   if(m.name.Contains("PolishedRetail")||m.name.Contains("WeatheredWindow")){
    var c=m.color;c.a=m.name.Contains("Weathered")?.26f:.23f;m.color=c;m.SetFloat("_WearStrength",.32f);EditorUtility.SetDirty(m);
   }
  }
  // Update only the existing lamp prefab; retain its GUID and the other station tools.
  var station=Object.FindFirstObjectByType<ToolStation>();
  var flags=BindingFlags.Static|BindingFlags.NonPublic;
  typeof(DemoToolsBuilder).GetField("Folder",flags).SetValue(null,Path.GetDirectoryName(AssetDatabase.GetAssetPath(station.LampPrefab)).Replace('\\','/'));
  station.LampPrefab=(DeployedTool)typeof(DemoToolsBuilder).GetMethod("SaveTool",flags).Invoke(null,new object[]{DemoToolKind.Lamp,false});
  Physics.SyncTransforms();
  if(car!=CarGeometrySignature())throw new Exception("Protected car changed");
  foreach(var door in root.GetComponentsInChildren<PushDoorLeaf>(true))foreach(var t in door.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetStaticEditorFlags(t.gameObject)!=0)throw new Exception("Static door child");
  var register=root.GetComponentInChildren<EvidenceRegister>();register.ResetInvestigation();register.RefreshPage();
  if(register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.CastingKit).Dock.IsChildOf(register.Kit.transform))throw new Exception("Invalid casting dock");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  ShopDetailPreview("integration");
  Debug.Log("INTEGRATION_SCENE_PASS moving door objects="+moving+" clues="+register.Clues.Length+" tools="+register.Tools.Length);
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing saved component on "+t.name);FieldworkChecks();EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");EditableDelivery.Build();Debug.Log("INTEGRATION_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

 // October 24 follow-up to Koda's test: re-authors the fieldwork (arranged tools with slot labels
 // and the lid guide, glow, hints), rebuilds the light probes so the far-pavement spectators take
 // the streetlight, rebakes, saves, renders the previews and builds the Windows player.
 public static void SandboxRefreshRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
  root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  string car=CarGeometrySignature();
  EvidenceGameplay();FieldworkChecks();
  EvidenceGameplay();
  foreach(var guide in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FieldGuide").ToArray())Object.DestroyImmediate(guide.gameObject);
  foreach(var door in root.GetComponentsInChildren<PushDoorLeaf>(true))foreach(var t in door.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
  NightLighting.UseLinearColorSpace();NightLighting.UseBaked(root);NightLighting.RebuildProbes(root);
  int probes=root.GetComponentInChildren<LightProbeGroup>(true).probePositions.Length;
  var register=root.GetComponentInChildren<EvidenceRegister>();register.ResetInvestigation();register.RefreshPage();
  Physics.SyncTransforms();if(car!=CarGeometrySignature())throw new Exception("Protected car changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  if(!NightLighting.Bake(out string report))throw new Exception("Bake failed: "+report);Debug.Log(report);
  EditorSceneManager.SaveOpenScenes();
  ShopDetailPreview("sandbox");
  Debug.Log("SANDBOX_REFRESH_PASS probes="+probes+" clues="+register.Clues.Length+" tools="+register.Tools.Length+" bake="+NightLighting.LastBakeSeconds.ToString("0.0")+"s");
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
  foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing saved component on "+t.name);
  EditableDelivery.Build();
  Debug.Log("SANDBOX_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
