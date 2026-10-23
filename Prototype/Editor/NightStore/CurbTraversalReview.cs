#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DemoGeometry;
public static partial class PresentationPolish {
 static void CheckCurbTravel(string label,bool require){
  var walker=UnityEngine.Object.FindFirstObjectByType<ShopWalkController>(FindObjectsInactive.Include);var player=walker.transform;var cc=player.GetComponent<CharacterController>();var xr=player.GetComponent<XRLocomotion>();
  var pos=player.position;var rotation=player.rotation;var head=xr.Head;var hp=head.localPosition;var hr=head.localRotation;var move=typeof(ShopWalkController).GetMethod("Move",BindingFlags.Instance|BindingFlags.NonPublic);
  var routes=new[]{(V(-5,-.12f,-4),V(-5,0,-2)),(V(-5,0,-2),V(-5,-.12f,-4)),(V(20,-.12f,-4),V(20,0,-2)),(V(20,0,-2),V(20,-.12f,-4)),(V(-5,-.12f,-19),V(-5,0,-21)),(V(-5,0,-21),V(-5,-.12f,-19))};int failures=0;
  try{for(int mode=0;mode<2;mode++)for(int i=0;i<routes.Length;i++){
   var route=routes[i];var direction=(route.Item2-route.Item1);direction.y=0;direction.Normalize();XRLocomotion.Relocate(player,route.Item1);player.rotation=Quaternion.LookRotation(direction);head.localPosition=V(0,1.65f,0);head.localRotation=Quaternion.identity;Physics.SyncTransforms();
   for(int frame=0;frame<180;frame++){
    var delta=route.Item2-player.position;delta.y=0;if(delta.magnitude<.035f)break;
    if(mode==0)move.Invoke(walker,new object[]{direction*(1.4f/90)+Vector3.down/90});else xr.SmoothMove(Vector2.up,1f/90);
   }
   var remaining=route.Item2-player.position;remaining.y=0;bool pass=remaining.magnitude<.10f&&Mathf.Abs(player.position.y-route.Item2.y)<.065f;if(!pass)failures++;
   Debug.Log($"CURB_TRAVERSAL_{label} mode={(mode==0?"keyboard":"vr")} route={i} pass={pass} at={player.position:F4} target={route.Item2} step={cc.stepOffset}");
  }
  XRLocomotion.Relocate(player,V(7.1f,0,4));Physics.SyncTransforms();for(int i=0;i<180;i++)cc.Move(Vector3.right*.015f+Vector3.down*.01f);if(player.position.x>7.9f||player.position.y>.20f)throw new Exception("Curb fix permits wall climbing or escape");
  if(require&&failures>0)throw new Exception("Curb crossing failures: "+failures);Debug.Log("CURB_TRAVERSAL_"+label+" failures="+failures+" / 12; tall wall still blocks");
  }finally{XRLocomotion.Relocate(player,pos);player.rotation=rotation;head.localPosition=hp;head.localRotation=hr;Physics.SyncTransforms();}
 }
 public static void CurbTraversalRepairRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");CheckCurbTravel("BEFORE",false);
  var walker=UnityEngine.Object.FindFirstObjectByType<ShopWalkController>(FindObjectsInactive.Include);var cc=walker.GetComponent<CharacterController>();cc.stepOffset=.22f;EditorUtility.SetDirty(cc);
  var ground=oman.Find("ParkingAndPavements");foreach(var c in ground.GetComponents<BoxCollider>()){
   if(Mathf.Abs(c.center.z+1.41f)<.01f){c.center=V(0,-.15f,-1.5f);c.size=V(140,.3f,3);EditorUtility.SetDirty(c);}
   if(Mathf.Abs(c.center.z+21.59f)<.01f){c.center=V(0,-.15f,-21.5f);c.size=V(140,.3f,3);EditorUtility.SetDirty(c);}
  }
  Physics.SyncTransforms();CheckCurbTravel("PASS",true);GeneralSceneReview();ShopAtmosphereChecks();NightStoreValidation.PresentationChecks();AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditableDelivery.Build();Debug.Log("CURB_TRAVERSAL_LOCAL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
