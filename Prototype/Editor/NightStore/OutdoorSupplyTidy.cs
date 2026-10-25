#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 static Bounds SupplyBounds(Transform t){var r=t.GetComponentsInChildren<Renderer>();var b=r[0].bounds;foreach(var item in r)b.Encapsulate(item.bounds);return b;}
 static void TidyOutdoorSupplies(){
  var supplies=root.Find("ForensicConsumables");if(!supplies)return;
  supplies.position=V(-6.5f,0,-.95f);supplies.rotation=Quaternion.identity;
  var items=supplies.Cast<Transform>().Where(t=>t.name!="GroundSheet"&&t.GetComponentsInChildren<Renderer>().Length>0).ToArray();
  foreach(var item in items)item.localRotation=Quaternion.identity;
  const float width=2.4f,depth=1.4f,gap=.055f;
  float x=-width*.5f+gap,z=-depth*.5f+gap,rowDepth=0;
  foreach(var item in items.OrderByDescending(t=>SupplyBounds(t).size.z).ThenByDescending(t=>SupplyBounds(t).size.x)){
   var b=SupplyBounds(item);
   if(x+b.size.x>width*.5f-gap){x=-width*.5f+gap;z+=rowDepth+gap;rowDepth=0;}
   if(z+b.size.z>depth*.5f-gap)throw new Exception("Outdoor supplies exceed their clear staging area");
   Vector3 target=supplies.TransformPoint(V(x+b.size.x*.5f,.006f,z+b.size.z*.5f));
   item.position+=V(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
   x+=b.size.x+gap;rowDepth=Mathf.Max(rowDepth,b.size.z);
  }
  var old=supplies.Find("GroundSheet");if(old)Object.DestroyImmediate(old.gameObject);
  var material=Mat(Folder,"ForensicGroundSheet",new Color(.16f,.19f,.23f),.08f);var sheet=new KitMesh{Lightmapped=false};sheet.Bar(material,V(0,.002f,0),V(width,.004f,depth),.001f);
  sheet.Emit("GroundSheet",supplies,Vector3.zero,Quaternion.identity,Folder,"tidy_forensic_ground_sheet");
  foreach(var t in supplies.GetComponentsInChildren<Transform>(true))t.gameObject.layer=CrowdLayer;
  for(int a=0;a<items.Length;a++)for(int b=a+1;b<items.Length;b++)if(SupplyBounds(items[a]).Intersects(SupplyBounds(items[b])))throw new Exception("Outdoor supply overlap: "+items[a].name+" / "+items[b].name);
  var cart=root.GetComponentInChildren<EvidenceRegister>().transform.Find("ExteriorFieldTable");var cartBounds=SupplyBounds(cart);
  foreach(var item in items)if(SupplyBounds(item).Intersects(cartBounds))throw new Exception("Supply intersects trolley: "+item.name);
  Debug.Log("OUTDOOR_SUPPLIES_CLEAR_PASS separated="+items.Length+"; no supply or trolley intersections");
 }
 static void OutdoorSupplyPreview(){
  var go=new GameObject("Temporary outdoor staging camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=59;c.nearClipPlane=.03f;
  c.transform.position=V(-5.8f,2.15f,-3.9f);c.transform.LookAt(V(-5.7f,.25f,-1.0f));var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=10;light.spotAngle=88;light.intensity=2;light.color=new Color(1,.94f,.83f);light.shadows=LightShadows.Soft;
  var rt=new RenderTexture(1440,900,24){antiAliasing=4};c.targetTexture=rt;var previous=RenderTexture.active;
  try{c.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Builds/outdoor-staging-tidy.png",image.EncodeToPNG());Object.DestroyImmediate(image);}
  finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 }
 public static void OutdoorSupplyInspectRun(){try{FinishOpen();OutdoorSupplyPreview();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void OutdoorSupplyTidyRun(){try{
  FinishOpen();string car=CarGeometrySignature();TidyOutdoorSupplies();
  if(car!=CarGeometrySignature())throw new Exception("Protected car changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);EditorSceneManager.SaveOpenScenes();
  FinishCapture("tidy");OutdoorSupplyPreview();EditableDelivery.Build();Debug.Log("OUTDOOR_SUPPLY_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
