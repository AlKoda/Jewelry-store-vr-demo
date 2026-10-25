#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DemoGeometry;
public static partial class PresentationPolish {
 const string OmaniCrowdFolder="Assets/CrimeSceneDemo/ThirdParty/PeopleModels/OmaniCrowd";
 static Material OmaniCrowdMaterial(string name){
  Color color=name.Contains("Sand")?new Color(.76f,.72f,.64f):name.Contains("Embroidery")?new Color(.62f,.57f,.46f):name.Contains("Mussar")?new Color(.70f,.66f,.57f):new Color(.94f,.94f,.91f);
  var mat=Mat(Folder,name,color,.12f);mat.shader=Shader.Find("Standard");mat.SetFloat("_Metallic",0);mat.DisableKeyword("_EMISSION");
  mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(OmaniCrowdFolder+(name.Contains("Kumma")?"/KummaEmbroidery.png":"/CottonWeave.png"));
  mat.mainTextureScale=name.Contains("Kumma")?Vector2.one:new Vector2(5,9);
  mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CrimeSceneDemo/ThirdParty/InteriorModels/Shared_FabricCharcoal_Normal.jpg"));mat.SetFloat("_BumpScale",.14f);mat.EnableKeyword("_NORMALMAP");return mat;
 }
 public static void OmaniCrowdRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string car=CarGeometrySignature();
  foreach(string role in new[]{"Talker","Phone"}){var i=(ModelImporter)AssetImporter.GetAtPath(OmaniCrowdFolder+"/"+role+".fbx");i.animationType=ModelImporterAnimationType.Legacy;i.isReadable=true;i.importAnimation=true;i.SaveAndReimport();}
  CrowdGathering();CrowdStructureChecks();CrowdPoseAudit();CrowdBehaviourChecks();CrowdGathering();
  if(car!=CarGeometrySignature())throw new Exception("Car changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();CrowdPreview();
  Debug.Log("OMANI_CROWD_SCENE_PASS two traditional outfits; same five actors and animation behaviours");
  EditableDelivery.Build();Debug.Log("OMANI_CROWD_BUILD_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
