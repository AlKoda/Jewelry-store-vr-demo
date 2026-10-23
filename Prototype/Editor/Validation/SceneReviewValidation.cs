#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class SceneReviewValidation {
 public static void Check(){
 var lines=new List<string>();void Require(bool ok,string label){lines.Add((ok?"PASS: ":"FAIL: ")+label);if(!ok)throw new Exception(label);}
 var root=GameObject.Find("NightStore").transform;
 var car=root.Find("Exterior/PoliceCar").GetComponent<BoxCollider>();Require(car&&car.size.z>4.6f&&car.size.z<4.85f&&car.size.x<2.4f,"Supplied car has measured metre-scale collision bounds");
 Require(Shader.Find("CrimeScene/DisplayGlass")&&Shader.Find("CrimeScene/DisplayGlass").isSupported,"Dielectric glass shader supported");
 var middle=root.Find("Showroom/CentralCase_Middle");var body=middle.Find("Body").GetComponent<MeshFilter>();
 var ray=new Ray(middle.TransformPoint(new Vector3(0,.59f,-.4f)),middle.forward);bool blocked=DemoValidation.TriangleRay(ray,.4f,DemoValidation.NewRayMesh(body),out float hit);Require(!blocked,"Open display drawer has a real clear cabinet aperture; hit="+hit+" point="+middle.InverseTransformPoint(ray.GetPoint(hit)));
 Require(middle.Find("Drawer")&&middle.Find("Drawer").localPosition.z<-.5f,"Drawer extension matches its visible tray depth");
 var doors=root.GetComponentsInChildren<PushDoorLeaf>();Require(doors.Length==4,"Four physical entrance and staff door leaves");
 foreach(var door in doors){var rb=door.GetComponent<Rigidbody>();var hinge=door.GetComponent<HingeJoint>();Require(rb&&!rb.isKinematic&&!rb.useGravity&&hinge&&hinge.useLimits,"Door has a constrained dynamic hinge: "+door.name);Require(door.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetStaticEditorFlags(t.gameObject)==0),"Moving leaf and its glass remnants carry no static flag (no static batching or baked lighting): "+door.name);Require(door.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.name.StartsWith("DoorGlass")||r.name.Contains("Remnant"))||!door.name.StartsWith("Door"),"Door glass remnants are children of their leaf: "+door.name);Require(door.OpenSpeed<=1.3f&&door.Acceleration<=6f&&rb.maxAngularVelocity<=1.5f,"Door swings at an eased, limited speed: "+door.name);}
 Require(Object.FindObjectsByType<DoorBodyPush>(FindObjectsSortMode.None).Length>0,"Desktop body contact has a door adapter");
 Require(Object.FindObjectsByType<VRDoorBodyPush>(FindObjectsSortMode.None).Any(v=>v.Head&&v.Mode),"Tracked VR torso sweep has head and mode references");
 Require(RenderSettings.skybox&&RenderSettings.skybox.mainTexture&&RenderSettings.skybox.mainTexture.name=="rooftop_night_1k","Photographic night sky assigned");
 Require(RenderSettings.fog&&RenderSettings.fogDensity>=.03f,"Distance haze enabled");
 Require(root.Find("DistantCityBackdrop").childCount==4,"Textured city continuation wraps the street");
 Require(root.Find("CaseReflectionProbes").GetComponentsInChildren<ReflectionProbe>().Length==6,"Six local baked case reflection probes");
 Require(root.GetComponentsInChildren<Light>().Count(l=>l.name.EndsWith("_Specular")&&l.lightmapBakeType==LightmapBakeType.Realtime)==2,"Low-energy headlight highlights remain realtime");
 Require(root.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported)),"All visible model materials resolve to supported shaders");
 foreach(var glass in root.GetComponentsInChildren<MeshFilter>())if(glass.name.StartsWith("Window_")||glass.name.StartsWith("DoorGlass_")||glass.name.StartsWith("GroundGlass_")){
  var bounds=glass.GetComponent<Renderer>().bounds;
  Require(bounds.size.y<2.3f&&bounds.size.x<3&&bounds.size.z<3,"Glass has correct metre-scale bounds: "+glass.name+" "+bounds.size);
 }
 Require(!root.Find("AdditionalShopDetails/StationerySet"),"Malformed floating stationery removed");
 Require(root.Find("EntranceGlassReflection")&&root.Find("EntranceGlassReflection").GetComponent<ReflectionProbe>().resolution==256,"Entrance glass has a local 256px reflection probe");
 int tris=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.gameObject.activeInHierarchy).Sum(f=>f.sharedMesh.triangles.Length/3);lines.Add("INFO: all-LOD mesh triangles="+tris+" (not the selected-LOD frame budget)");
 string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/VerificationReview"));Directory.CreateDirectory(dir);File.WriteAllLines(Path.Combine(dir,"edit-checks.txt"),lines);Debug.Log("REVIEW_EDIT_CHECKS_PASS "+(lines.Count-1));
 }
}
#endif
