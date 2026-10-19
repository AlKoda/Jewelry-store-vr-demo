#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object=UnityEngine.Object;
using static DemoGeometry;
public static class SceneReviewPass {
 const string F="Assets/CrimeSceneDemo/CombinedArt";
 public static void Apply(){
 Atmosphere();ExtendGround();Doors();Details();SpecularLighting();Reflections();
 foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))c.farClipPlane=Mathf.Max(c.farClipPlane,220);
 Debug.Log("REVIEW_PASS_APPLIED city continuation, night panorama, pushable doors, interior details, material response");
 }
 static void Atmosphere(){
 var sky=new Material(Shader.Find("Skybox/Panoramic"));sky.SetTexture("_MainTex",DemoAssetLibrary.Texture("NightSky","rooftop_night_1k.hdr"));sky.SetFloat("_Exposure",.45f);sky.SetColor("_Tint",new Color(.32f,.44f,.6f));sky.SetFloat("_Rotation",135);sky.SetFloat("_Mapping",1);sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
 string p=F+"/PhotographicNightSky.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(p);if(old){EditorUtility.CopySerialized(sky,old);Object.DestroyImmediate(sky);sky=old;}else AssetDatabase.CreateAsset(sky,p);RenderSettings.skybox=sky;
 RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.033f;RenderSettings.fogColor=new Color(.095f,.115f,.145f);
 var root=GameObject.Find("NightStore").transform;var group=Group("DistantCityBackdrop",root);
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(DemoAssetLibrary.Root+"/CityBackdrop/CityMistPanorama.png");if(!texture)throw new Exception("City backdrop missing");var m=new Material(Shader.Find("CrimeScene/CityBackdrop"));m.mainTexture=texture;var cityOld=AssetDatabase.LoadAssetAtPath<Material>(F+"/DistantCity.mat");if(cityOld){EditorUtility.CopySerialized(m,cityOld);Object.DestroyImmediate(m);m=cityOld;}else AssetDatabase.CreateAsset(m,F+"/DistantCity.mat");
 foreach(float yaw in new[]{0f,90f,180f,270f}){
  var panel=new GameObject("CityHorizon_"+yaw);panel.transform.SetParent(group,false);panel.transform.localRotation=Quaternion.Euler(0,yaw,0);
  const int segments=32;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
  for(int i=0;i<=segments;i++){float u=i/(float)segments;float a=(u-.5f)*Mathf.PI*.5f;var radial=new Vector3(Mathf.Sin(a)*58,0,Mathf.Cos(a)*58);vertices[i*2]=radial+Vector3.up*(-5);vertices[i*2+1]=radial+Vector3.up*25;uv[i*2]=new Vector2(u,0);uv[i*2+1]=new Vector2(u,1);if(i<segments){int v=i*2,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+2;triangles[t+4]=v+1;triangles[t+5]=v+3;}}
  var mesh=new Mesh{name="CurvedCityHorizon_"+yaw,vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();string meshPath=F+"/CityArc_"+yaw+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);panel.AddComponent<MeshFilter>().sharedMesh=mesh;var r=panel.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;GameObjectUtility.SetStaticEditorFlags(panel,0);
 }
 var people=root.Find("Exterior/People");if(people)people.gameObject.SetActive(false);
 }
 static void ExtendGround(){
 // Continue only the outer boundary vertices; retain near-road markings and geometry.
 var ground=GameObject.Find("NightStore/Exterior/Ground");if(!ground)throw new Exception("Ground missing");
 foreach(var filter in ground.GetComponentsInChildren<MeshFilter>()){
  var mesh=Object.Instantiate(filter.sharedMesh);mesh.name=filter.name+"_HazeContinuation";var original=mesh.vertices;var vertices=(Vector3[])original.Clone();var uv=mesh.uv;
  for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(original[i].x)>=NightStoreLayout.StreetHalfX-.001f){float dx=Mathf.Sign(original[i].x)*90-original[i].x;vertices[i].x+=dx;
   for(int j=0;j<original.Length;j++)if(Mathf.Abs(original[j].x-original[i].x)>1&&Mathf.Abs(original[j].y-original[i].y)<.001f&&Mathf.Abs(original[j].z-original[i].z)<.001f){uv[i]+=(mesh.uv[i]-mesh.uv[j])*(dx/(original[i].x-original[j].x));break;}
  }
  mesh.vertices=vertices;mesh.uv=uv;mesh.RecalculateBounds();string path=F+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;
 }
 }
 static void Doors(){
 var root=GameObject.Find("NightStore/Facade/EntranceDoors").transform;
 var leaves=new[]{root.Find("DoorLeft"),root.Find("DoorRight"),GameObject.Find("NightStore/Office/OfficeDoor/Door")?.transform,GameObject.Find("NightStore/Office/ServiceDoor/Door")?.transform};
 foreach(var leaf in leaves){
  string name=leaf ? leaf.parent.name+"/"+leaf.name : "missing";if(!leaf)throw new Exception("Missing door "+name);leaf.localRotation=Quaternion.identity;
  foreach(var t in leaf.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
  foreach(var r in leaf.GetComponentsInChildren<MeshRenderer>(true)){r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.shadowCastingMode=DemoAssetLibrary.Blended(r)?ShadowCastingMode.Off:ShadowCastingMode.On;}
  var body=leaf.gameObject.AddComponent<Rigidbody>();body.mass=8;body.useGravity=false;body.linearDamping=4;body.angularDamping=1.5f;body.maxAngularVelocity=1.8f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  var hinge=leaf.gameObject.AddComponent<HingeJoint>();hinge.axis=Vector3.up;hinge.anchor=new Vector3(0,1.15f,0);hinge.autoConfigureConnectedAnchor=false;hinge.connectedAnchor=leaf.TransformPoint(hinge.anchor);hinge.useLimits=true;var limits=hinge.limits;limits.min=-105;limits.max=105;hinge.limits=limits;hinge.useSpring=true;var spring=hinge.spring;spring.spring=1.2f;spring.damper=.8f;spring.targetPosition=0;hinge.spring=spring;
  leaf.gameObject.AddComponent<PushDoorLeaf>();
  var reset=leaf.gameObject.AddComponent<ResettableSceneObject>();var session=Object.FindFirstObjectByType<DemoSession>();if(session)session.SceneObjects=session.SceneObjects.Concat(new[]{reset}).ToArray();
 }
 // The old closed side rooms were excluded from locomotion bounds. Open leaves
 // are useful only when both desktop and XR can actually cross their thresholds.
 var left=new InteriorBounds.Region(NightStoreLayout.MinX+.3f,-NightStoreLayout.OfficeHalf-.2f,NightStoreLayout.RearZ-.25f,NightStoreLayout.MaxZ-.25f);
 var right=new InteriorBounds.Region(NightStoreLayout.OfficeHalf+.2f,NightStoreLayout.MaxX-.3f,NightStoreLayout.RearZ-.25f,NightStoreLayout.MaxZ-.25f);
 foreach(var bounds in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None))bounds.Regions=bounds.Regions.Concat(new[]{left,right}).ToArray();
 foreach(var zones in Object.FindObjectsByType<AmbientZones>(FindObjectsSortMode.None))zones.Inside=zones.Inside.Concat(new[]{new Rect(left.MinX,left.MinZ,left.MaxX-left.MinX,left.MaxZ-left.MinZ),new Rect(right.MinX,right.MinZ,right.MaxX-right.MinX,right.MaxZ-right.MinZ)}).ToArray();
 foreach(var hand in Object.FindObjectsByType<XRControllerInput>(FindObjectsInactive.Include,FindObjectsSortMode.None)) { var contact=hand.gameObject.AddComponent<VRDoorHandPush>();contact.Input=hand; }
 foreach(var controller in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None)){if(!controller.GetComponent<DoorBodyPush>())controller.gameObject.AddComponent<DoorBodyPush>();var mode=controller.GetComponent<DemoModeSwitch>();if(mode){var vr=controller.gameObject.AddComponent<VRDoorBodyPush>();vr.Mode=mode;var locomotion=controller.GetComponent<XRLocomotion>();vr.Head=locomotion?locomotion.Head:null;}}
 }
 static void Details(){
 var root=GameObject.Find("NightStore").transform;var g=Group("AdditionalShopDetails",root);
 Place("DeskPhone",g,new Vector3(5.92f,1.0f,7.85f),175);
 // Removed the malformed floating stationery; keep only convincing counter props.
 Place("PhotoFrameDesk",g,new Vector3(5.50f,1.0f,7.98f),180);
 // All mounts are grounded on the named counter's actual top, not guessed from model scale.
 foreach(var child in g.Cast<Transform>().ToArray()){
  var counter=root.Find("Showroom/Counter");if(counter){var top=counter.GetComponents<BoxCollider>().Select(c=>c.bounds.max.y).DefaultIfEmpty(1).Max();var b=DemoAssetLibrary.LocalBounds(child.gameObject);var pos=child.position;pos.y=top-b.min.y+.001f;child.position=pos;}
 }
 StoreKit.MarkStatic(g.gameObject,true);
 }
 static void Place(string model,Transform parent,Vector3 pos,float yaw){var go=DemoAssetLibrary.PlacePack("InteriorModels",model,parent,pos,yaw,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.Prop});if(!go)throw new Exception("Missing interior model "+model);}
 static void Reflections(){
 var root=GameObject.Find("NightStore").transform;var probes=Group("CaseReflectionProbes",root);
 var entrance=new GameObject("EntranceGlassReflection");entrance.transform.SetParent(root,false);entrance.transform.localPosition=new Vector3(0,1.2f,.5f);var doorProbe=entrance.AddComponent<ReflectionProbe>();doorProbe.mode=ReflectionProbeMode.Baked;doorProbe.boxProjection=true;doorProbe.size=new Vector3(5,3.5f,5);doorProbe.resolution=256;doorProbe.hdr=true;doorProbe.importance=6;
 foreach(string name in new[]{"CentralCase_Left","CentralCase_Middle","CentralCase_Right","LeftIsland_Rear","RightIsland_Rear","RightIsland_Front"}){var fixture=root.Find("Showroom/"+name);var go=new GameObject(name+"_Reflection");go.transform.SetParent(probes,false);go.transform.position=fixture.TransformPoint(new Vector3(0,.88f,0));var probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Baked;probe.boxProjection=true;probe.size=Mathf.Abs(fixture.eulerAngles.y%180)>45?new Vector3(1.15f,1.2f,2.3f):new Vector3(2.3f,1.2f,1.15f);probe.resolution=128;probe.hdr=true;probe.importance=5;}
 }
 static void SpecularLighting(){
 var group=GameObject.Find("NightStore/Lighting").transform;
 // The baked headlights provide the diffuse navigation light. Low-energy realtime
 // companions provide view-dependent highlights on metal/glass and moving leaves.
 foreach(var original in group.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Headlight_")).ToArray()){
  var go=new GameObject(original.name+"_Specular");go.transform.SetParent(group,false);go.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=original.range;light.spotAngle=original.spotAngle;light.innerSpotAngle=original.innerSpotAngle;light.color=original.color;light.intensity=.16f;light.cookie=original.cookie;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.Auto;light.lightmapBakeType=LightmapBakeType.Realtime;
 }
 }
}
#endif
