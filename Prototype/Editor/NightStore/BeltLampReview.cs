#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class BeltLampReview {
 public static void Run(){GameObject test=null;try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
  test=new GameObject("Temporary belt and lamp checks");var belt=test.AddComponent<XRHolsters>();var hip=belt.HipOffset;var chest=belt.ChestOffset;
  try{typeof(XRHolsters).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(belt,null);if(Mathf.Abs(belt.HipOffset.y-(hip.y-.08f))>.0001f||Mathf.Abs(belt.ChestOffset.y-(chest.y-.08f))>.0001f)throw new Exception("Belt adjustment wrong");}finally{belt.HipOffset=hip;belt.ChestOffset=chest;}
  var station=test.AddComponent<ToolStation>();
  DeployedTool Lamp(){var g=new GameObject("Test portable lamp");g.transform.SetParent(test.transform);var t=g.AddComponent<DeployedTool>();t.Kind=DemoToolKind.Lamp;t.Lamp=g.AddComponent<Light>();t.Lamp.type=LightType.Spot;return t;}
  var a=Lamp();var b=Lamp();var c=Lamp();station.SetLampLit(a,true);station.SetLampLit(b,true);
  if(!a.LampLit||!b.LampLit||station.LitLampCount!=2||a.Lamp.shadows==LightShadows.None||b.Lamp.shadows==LightShadows.None)throw new Exception("Second lamp disables first or loses shadows");
  station.SetLampLit(c,true);if(a.LampLit||!b.LampLit||!c.LampLit||station.LitLampCount!=2)throw new Exception("Two-lamp budget broken");
  station.SetLampLit(b,false);if(!c.LampLit||station.LitLampCount!=1)throw new Exception("Lamp switching not independent");
  Object.DestroyImmediate(test);test=null;Debug.Log("BELT_LAMP_CHECKS_PASS belt +1cm; two independent shadowed lamps, bounded third-lamp replacement");EditableDelivery.Build();Debug.Log("BELT_LAMP_LOCAL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);if(test)Object.DestroyImmediate(test);EditorApplication.Exit(1);}}
}
#endif
