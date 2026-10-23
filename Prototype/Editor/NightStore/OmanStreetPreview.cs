#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;
public static partial class PresentationPolish {
 public static void OmanInspectRun(){try{
 EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
 var lines=new List<string>();
 foreach(var t in root.GetComponentsInChildren<Transform>(true)){
 string p=PathOf(t);if(p.Split('/').Length>5)continue;
 var r=t.GetComponent<Renderer>();if(r)lines.Add(p+" active="+t.gameObject.activeInHierarchy+" bounds="+r.bounds);
 else if(t.childCount>0)lines.Add(p+" active="+t.gameObject.activeInHierarchy);
 }
 File.WriteAllLines("Builds/oman-hierarchy.txt",lines);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

 const string OmanFolder="Assets/CrimeSceneDemo/CombinedArt/OmanStreet";
 static Material[] omanWalls,omanSigns,omanAge,omanShutters;
 static Material omanFreshWall;
 static Material omanTrim,omanMetal,omanDark,omanShutter,omanStains,omanRoad,omanPaving,omanWhite,omanBlack;
 static Transform oman;
 static void OmanDisable(string path){var t=root.Find(path);if(t)t.gameObject.SetActive(false);}
 static Material OmanMat(string name,Color c,float rough=.08f){var m=Mat(OmanFolder,name,c,rough);m.SetFloat("_Metallic",0);return m;}
 static void OmanMaterials(){
  if(!AssetDatabase.IsValidFolder(OmanFolder))AssetDatabase.CreateFolder(Folder,"OmanStreet");
  omanWalls=new[]{OmanMat("Ivory",new Color(.88f,.85f,.77f)),OmanMat("Sand",new Color(.76f,.71f,.61f)),OmanMat("Chalk",new Color(.93f,.92f,.86f)),OmanMat("WarmRender",new Color(.84f,.77f,.65f))};
  var plaster=DemoAssetLibrary.Texture("OmanExterior","IvoryPlasterTile.png");if(!plaster)throw new Exception("Oman plaster absent");
  foreach(var m in omanWalls){m.mainTexture=plaster;m.mainTextureScale=Vector2.one*.35f;}
  omanTrim=OmanMat("ConcreteTrim",new Color(.72f,.7f,.64f));omanMetal=OmanMat("DustyPaintedSteel",new Color(.18f,.20f,.19f),.22f);
  omanDark=OmanMat("DarkRecess",new Color(.052f,.067f,.069f),.2f);omanShutter=OmanMat("FadedShutter",new Color(.27f,.30f,.27f),.13f);
  omanShutter.mainTexture=DemoAssetLibrary.Texture("PBRExterior","MetalShutter_Color.jpg");omanShutter.mainTextureScale=Vector2.one;
  omanShutter.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("PBRExterior","MetalShutter_Normal.png"));omanShutter.EnableKeyword("_NORMALMAP");omanShutter.SetFloat("_BumpScale",.35f);omanShutter.color=new Color(.73f,.77f,.71f);
  omanMetal.mainTexture=DemoAssetLibrary.Texture("StreetModels","StreetLamp_LampMetal_Color.jpg");omanMetal.mainTextureScale=Vector2.one*2;
  omanWhite=OmanMat("KerbWhite",new Color(.68f,.68f,.63f));omanWhite.mainTexture=plaster;omanWhite.mainTextureScale=Vector2.one*1.4f;
  omanBlack=OmanMat("KerbBlack",new Color(.065f,.071f,.071f));omanBlack.mainTexture=plaster;
  omanRoad=OmanMat("DryRoad",new Color(.69f,.68f,.65f),.16f);omanRoad.mainTexture=DemoAssetLibrary.Texture("PBRExterior","AsphaltDry_Color.jpg");omanRoad.mainTextureScale=Vector2.one/3;
  omanRoad.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("PBRExterior","AsphaltDry_Normal.png"));omanRoad.EnableKeyword("_NORMALMAP");omanRoad.SetFloat("_BumpScale",.3f);
  omanPaving=OmanMat("DustyPaving",new Color(.81f,.77f,.69f),.12f);omanPaving.mainTexture=DemoAssetLibrary.Texture("PBRExterior","PavementSlab_Color.jpg");omanPaving.mainTextureScale=Vector2.one/2;
  omanStains=OmanMat("ParapetRunoff",new Color(.65f,.58f,.45f,.20f));omanStains.mainTexture=DemoAssetLibrary.Texture("OmanExterior","ParapetRunoff.png");Fade(omanStains,3001);

  omanFreshWall=OmanMat("MaintainedFreshPlaster",new Color(.92f,.91f,.85f),.08f);omanFreshWall.mainTexture=DemoAssetLibrary.Texture("PBRExterior","Render_Color.jpg");omanFreshWall.mainTextureScale=Vector2.one*.5f;
  omanShutters=new Material[3];omanAge=new Material[3];
  for(int grade=0;grade<3;grade++){
   var sh=OmanMat("ShutterCondition_"+grade,grade==0?new Color(.48f,.57f,.54f):grade==1?new Color(.73f,.77f,.71f):new Color(.52f,.48f,.40f),grade==0?.22f:.1f);sh.mainTexture=omanShutter.mainTexture;sh.SetTexture("_BumpMap",omanShutter.GetTexture("_BumpMap"));sh.EnableKeyword("_NORMALMAP");sh.SetFloat("_BumpScale",.28f);omanShutters[grade]=sh;
   var grime=OmanMat("RunoffCondition_"+grade,new Color(.65f,.58f,.45f,grade==0?.025f:grade==1?.16f:.34f));grime.mainTexture=omanStains.mainTexture;Fade(grime,3001);omanAge[grade]=grime;
  }
  omanSigns=new Material[9];for(int i=0;i<9;i++){var m=OmanMat("Fascia_"+i,Color.white,.14f);m.mainTexture=DemoAssetLibrary.Texture("OmanExterior","Sign"+i+".png");omanSigns[i]=m;}
 }
 static GameObject OmanEmit(KitMesh k,string name,Transform parent,float scale=.12f){var g=k.Emit(name,parent,Vector3.zero,Quaternion.identity,OmanFolder,"oman_"+name);StoreKit.MarkStatic(g,true);var r=g.GetComponent<MeshRenderer>();if(r)r.scaleInLightmap=scale;return g;}
 static void OmanSolid(Transform p,Vector3 center,Vector3 size){var c=p.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;}
 // Arches are actual open masonry strips, with front/back and a closed intrados, not dark stickers.
 static void OmanArch(KitMesh k,Material m,float x,float spring,float radius,float rise,float z,float depth,float top){
  int n=18;for(int i=0;i<n;i++){
   float a=Mathf.PI-i*Mathf.PI/n,b=Mathf.PI-(i+1)*Mathf.PI/n;
   var p=V(x+Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*rise,z);var q=V(x+Mathf.Cos(b)*radius,spring+Mathf.Sin(b)*rise,z);
   var pp=p+Vector3.forward*depth;var qq=q+Vector3.forward*depth;
   k.Face(m,Vector3.back,p,q,V(q.x,top,z),V(p.x,top,z));k.Face(m,Vector3.forward,pp,V(pp.x,top,pp.z),V(qq.x,top,qq.z),qq);
   k.Face(m,new Vector3(x-(p.x+q.x)*.5f,-1,0),p,pp,qq,q);
  }
 }
 static void OmanSign(KitMesh k,int index,float x,float y,float z,float width){
  float h=width/4;k.Box(omanMetal,V(x,y,z+.035f),V(width+.07f,h+.065f,.07f));
  k.Quad(omanSigns[index],Vector3.back,V(x-width/2,y-h/2,z-.004f),V(x+width/2,y-h/2,z-.004f),V(x+width/2,y+h/2,z-.004f),V(x-width/2,y+h/2,z-.004f),new Rect(0,0,1,1));
 }
 static void OmanAC(KitMesh k,float x,float y,float z){
  var shell=omanWalls[2];k.Bar(shell,V(x,y,z-.18f),V(.82f,.55f,.36f),.025f);
  k.Cylinder(omanMetal,V(x-.12f,y,z-.368f),V(x-.12f,y,z-.378f),.205f,16);
  for(int j=-3;j<=3;j++)k.Box(shell,V(x-.12f+j*.047f,y,z-.384f),V(.012f,.37f,.012f));
  for(int j=0;j<6;j++)k.Box(omanMetal,V(x+.27f,y-.18f+j*.067f,z-.369f),V(.15f,.014f,.012f));
  foreach(float dx in new[]{-.27f,.27f}){k.Box(omanMetal,V(x+dx,y-.32f,z-.12f),V(.035f,.045f,.48f));k.Box(omanMetal,V(x+dx,y-.21f,z+.008f),V(.035f,.3f,.025f));}
  k.Cylinder(omanTrim,V(x+.43f,y-.1f,z-.12f),V(x+.54f,y-.38f,z-.12f),.025f,6);k.Cylinder(omanTrim,V(x+.54f,y-.38f,z-.12f),V(x+.54f,3.55f,z-.12f),.025f,6);
 }
 static void OmanWindow(KitMesh k,float x,float y,float z,bool arched){
  float w=1.1f,h=1.5f;k.Box(omanTrim,V(x,y,z-.045f),V(w+.16f,h+.16f,.15f));k.Box(omanDark,V(x,y,z-.126f),V(w,h,.012f));
  for(int i=-2;i<=2;i++)k.Box(omanMetal,V(x+i*.205f,y,z-.145f),V(.022f,h,.025f));
  foreach(float dy in new[]{-.45f,0f,.45f})k.Box(omanMetal,V(x,y+dy,z-.145f),V(w,.025f,.025f));
  k.Box(omanTrim,V(x,y-h*.5f-.09f,z-.09f),V(w+.27f,.10f,.26f));
  if(arched){OmanArch(k,omanWalls[2],x,y+.3f,.53f,.47f,z-.16f,.18f,y+.81f);}
 }
 static void OmanShop(Transform row,float x,float z,float width,float height,int style,int sign,int seed){
  
  int grade=seed%5<2?0:seed%5==4?2:1;bool low=seed%7==3;if(low){height=4.15f;style=3;}
  var t=Group("Shop_"+seed+"_"+new[]{"Maintained","Weathered","Neglected"}[grade],row);t.localPosition=V(x,0,z);var k=new KitMesh{LightmapScale=.12f};var wall=grade==0?omanFreshWall:omanWalls[seed%4];var shutter=omanShutters[grade];
  if(grade==0)sign=4+seed%5;
  // Setback building mass leaves genuine recesses beneath beams and in the upper gallery.
  k.Box(wall,V(0,height/2,4.5f),V(width-.07f,height,8));
  float edge=width/2-.19f;
  k.Box(wall,V(-edge,1.65f,.16f),V(.35f,3.3f,.7f));k.Box(wall,V(edge,1.65f,.16f),V(.35f,3.3f,.7f));
  k.Box(omanTrim,V(0,.07f,.02f),V(width-.2f,.14f,1.2f));
  float open=width-.85f;
  k.Box(omanDark,V(0,1.46f,.405f),V(open,2.7f,.07f));
  if(style!=2){
   k.Box(shutter,V(0,1.47f,.32f),V(open-.04f,2.63f,.10f));
   for(float y=.2f;y<2.8f;y+=.09f)k.Box(omanMetal,V(0,y,.258f),V(open-.05f,.012f,.018f));
   k.Box(omanMetal,V(0,.23f,.235f),V(.2f,.045f,.045f));
  }else{
   for(int j=-1;j<=1;j++)k.Box(omanMetal,V(j*open/3,1.47f,.30f),V(.045f,2.7f,.08f));
   for(float y=.4f;y<2.8f;y+=.28f)k.Box(omanMetal,V(0,y,.30f),V(open,.018f,.022f));
  }
  k.Box(wall,V(0,3.3f,.10f),V(width+.04f,.40f,1.0f));
  var board=new KitMesh{LightmapScale=.12f};OmanSign(board,sign,0,3.02f,-.52f,Mathf.Min(width-.7f,4.35f));OmanEmit(board,"ShopSign_"+seed,t,.12f);
  // Shallow concrete eaves and small projecting corbels rather than western cornices.
  k.Box(omanTrim,V(0,height-.06f,.14f),V(width+.10f,.16f,1.05f));
  k.Box(wall,V(0,height+.24f,4.5f),V(width,.6f,8));
  k.Box(omanTrim,V(0,height+.57f,4.5f),V(width+.09f,.08f,8.08f));
  for(float xx=-edge;xx<=edge;xx+=1.8f)k.Box(omanTrim,V(xx,height-.22f,.31f),V(.14f,.25f,.62f));
  if(style==0){
   int bays=Mathf.Max(2,Mathf.FloorToInt(width/2.1f));float step=(width-.45f)/bays;
   k.Box(omanTrim,V(0,3.73f,-.02f),V(width,.18f,1.15f));
   for(int i=0;i<=bays;i++){float xx=-width/2+.225f+i*step;k.Box(wall,V(xx,4.88f,.01f),V(.22f,2.23f,.38f));k.Box(omanTrim,V(xx,5.76f,-.04f),V(.35f,.12f,.5f));}
   for(int i=0;i<bays;i++){float xx=-width/2+.225f+(i+.5f)*step;OmanArch(k,wall,xx,5.4f,(step-.22f)/2,.72f,-.18f,.38f,height-.1f);}
   k.Box(omanMetal,V(0,4.66f,-.12f),V(width-.35f,.045f,.04f));k.Box(omanMetal,V(0,3.96f,-.12f),V(width-.35f,.04f,.04f));
   for(float xx=-edge;xx<=edge;xx+=.19f)k.Box(omanMetal,V(xx,4.31f,-.12f),V(.018f,.7f,.025f));
  }else if(!low){
   int count=Mathf.Max(2,Mathf.FloorToInt(width/2.4f));for(int i=0;i<count;i++)OmanWindow(k,-width/2+(i+.5f)*width/count,4.95f,.46f,style==1);
  }

  if(low){OmanAC(k,width*.29f,height+.38f,1.0f);k.Box(wall,V(-width*.3f,height+.38f,1.3f),V(1.2f,.7f,1.1f));}
  else OmanAC(k,width*.29f,4.08f,.04f);
  if(style==2){
   // Shallow closed-shop canopy: head clearance is 2.25 m and fascia remains unobstructed.
   var cloth=grade==0?omanShutters[0]:omanShutters[2];k.Box(omanMetal,V(0,2.35f,-.05f),V(width-.7f,.08f,.08f));
   for(int rib=0;rib<9;rib++){float xx=-(width-.8f)/2+rib*(width-.8f)/8;k.Cylinder(omanMetal,V(xx,2.36f,.03f),V(xx,2.28f,-.93f),.012f,6);}
   k.Face(cloth,Vector3.up,V(-open/2,2.36f,.03f),V(open/2,2.36f,.03f),V(open/2,2.28f,-.93f),V(-open/2,2.28f,-.93f));k.Face(cloth,Vector3.down,V(-open/2,2.352f,.03f),V(open/2,2.352f,.03f),V(open/2,2.272f,-.93f),V(-open/2,2.272f,-.93f));
  }

  k.Cylinder(omanMetal,V(-edge+.18f,3.6f,-.08f),V(-edge+.18f,height-.45f,-.08f),.018f,6);
  // Weathered roof stains on separate geometry; texture supplied below.
  var wear=new KitMesh{Lightmapped=false};if(omanStains!=null&&grade>0)wear.Quad(omanAge[grade],Vector3.back,V(-width/2+.04f,height-1.15f,.493f),V(width/2-.04f,height-1.15f,.493f),V(width/2-.04f,height+.48f,.493f),V(-width/2+.04f,height+.48f,.493f),new Rect((seed%4)*.25f,0,.25f,1));
  OmanEmit(wear,"ParapetAge_"+seed,t,0);var g=OmanEmit(k,"Facade_"+seed,t);OmanSolid(t,V(0,height/2,4.5f),V(width-.07f,height,8));
 }
 static void OmanMainUpper(){
  var t=Group("StoreUpperGallery",oman);var k=new KitMesh{LightmapScale=.18f};var wall=omanWalls[0];
  k.Box(wall,V(0,5.25f,3.6f),V(16,3.0f,5.9f));k.Box(wall,V(0,7.01f,3.6f),V(16.12f,.6f,6));
  k.Box(omanTrim,V(0,7.35f,3.6f),V(16.22f,.10f,6.08f));
  for(int i=0;i<7;i++){float x=-7.8f+i*2.6f;k.Box(wall,V(x,4.83f,.03f),V(.24f,2.2f,.5f));k.Box(omanTrim,V(x,5.75f,-.04f),V(.4f,.14f,.62f));
   if(i<6){OmanArch(k,wall,x+1.3f,5.62f,1.18f,.75f,-.22f,.52f,6.86f);OmanWindow(k,x+1.3f,4.95f,.64f,false);}
  }
  k.Box(omanMetal,V(0,4.56f,-.18f),V(15.7f,.045f,.045f));k.Box(omanMetal,V(0,3.93f,-.18f),V(15.7f,.045f,.045f));
  for(float x=-7.7f;x<7.8f;x+=.20f)k.Box(omanMetal,V(x,4.24f,-.18f),V(.018f,.63f,.025f));
  OmanAC(k,-5.9f,4.05f,.26f);OmanAC(k,5.7f,4.08f,.26f);
  k.Box(omanTrim,V(0,3.76f,.19f),V(16.1f,.12f,.84f));
  // Keep the accepted fascia sign and its clearance completely untouched.
  OmanEmit(k,"MainUpperMasonry",t,.22f);
 }
 static void OmanGround(){
  var t=Fresh("ParkingAndPavements",oman);
  ContinuousRoad(t);
  for(int segment=-7;segment<7;segment++){
   float x=segment*10+5;var k=new KitMesh{LightmapScale=.45f};
   k.Box(omanPaving,V(x,-.15f,-1.41f),V(10,.3f,2.82f));k.Box(omanPaving,V(x,-.15f,-21.59f),V(10,.3f,2.82f));
   for(int j=0;j<10;j++)foreach(float z in new[]{-2.91f,-20.09f})k.Box(j%2==0?omanWhite:omanBlack,V(x-4.5f+j,-.13f,z),V(.99f,.26f,.18f));
   OmanEmit(k,"Surface_"+(segment+7),t,.45f);
  }
  // Include the kerbs in the pavement collision surface: no 18 cm hole between slabs.
  OmanSolid(t,V(0,-.27f,-11.5f),V(140,.3f,17));OmanSolid(t,V(0,-.15f,-1.5f),V(140,.3f,3));OmanSolid(t,V(0,-.15f,-21.5f),V(140,.3f,3));
  // Parking paint is composited into the opaque road material by RoadMaterials.
  // Asphalt continues into haze rather than terminating at the playable limit.
  var far=new KitMesh{Lightmapped=false};far.Box(omanRoad,V(0,-.43f,-11.5f),V(600,.02f,17));far.Box(omanWalls[1],V(0,-.45f,0),V(600,.02f,600));OmanEmit(far,"HorizonGround",t,0);
  var safety=Fresh("LocalPreviewBoundary",oman);foreach(float x in new[]{-29.5f,29.5f})OmanSolid(safety,V(x,1.4f,-11.5f),V(.2f,3.3f,23));OmanSolid(safety,V(0,1.4f,-22.9f),V(59,3.3f,.2f));
  foreach(var b in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   b.Regions=b.Regions.Where(r=>r.MinZ>=-.36f).Concat(new[]{new InteriorBounds.Region(-29,29,-3,-.35f,0),new InteriorBounds.Region(-29,29,-20,-3,NightStoreLayout.StreetY),new InteriorBounds.Region(-29,29,-22.65f,-20,0)}).ToArray();EditorUtility.SetDirty(b);
  }
 }
 static void OmanGateway(){
  var t=Group("GoldMarketGateway",oman);t.localPosition=V(25,0,-11.5f);t.localRotation=Quaternion.Euler(0,90,0);var k=new KitMesh{LightmapScale=.1f};var wall=omanWalls[0];
  // Wide road arch plus two narrower pedestrian portals, inspired by the supplied Omani examples.
  foreach(float x in new[]{-10.1f,10.1f}){k.Box(wall,V(x,4.7f,.0f),V(1.3f,9.4f,.85f));OmanSolid(t,V(x,4.7f,0),V(1.3f,9.4f,.85f));}
  OmanArch(k,wall,0,4.3f,9.45f,3.35f,-.425f,.85f,9.4f);
  for(int i=0;i<24;i++){
   float a=Mathf.PI-i*Mathf.PI/24,b=Mathf.PI-(i+1)*Mathf.PI/24;
   var p=V(Mathf.Cos(a)*9.45f,4.3f+Mathf.Sin(a)*3.35f,-.465f);var q=V(Mathf.Cos(b)*9.45f,4.3f+Mathf.Sin(b)*3.35f,-.465f);
   var pp=V(Mathf.Cos(a)*9.70f,4.3f+Mathf.Sin(a)*3.65f,-.465f);var qq=V(Mathf.Cos(b)*9.70f,4.3f+Mathf.Sin(b)*3.65f,-.465f);k.Face(omanTrim,Vector3.back,p,q,qq,pp);
  }
  foreach(float side in new[]{-1f,1f}){
   k.Box(wall,V(side*13.1f,3.2f,0),V(.75f,6.4f,.85f));OmanSolid(t,V(side*13.1f,3.2f,0),V(.75f,6.4f,.85f));
   OmanArch(k,wall,side*11.73f,2.4f,1.0f,1.2f,-.425f,.85f,6.5f);
   for(int j=0;j<4;j++)k.Box(wall,V(side*(10.1f+j*.28f),9.55f-j*.35f,0),V(.3f,.6f,.85f));
  }
  OmanSign(k,3,0,8.65f,-.52f,5.1f);
  var back=Group("GatewayReverseSign",t);back.localRotation=Quaternion.Euler(0,180,0);var bsign=new KitMesh{LightmapScale=.1f};OmanSign(bsign,3,0,8.65f,-.52f,5.1f);OmanEmit(bsign,"GatewaySignReverse",back);
  OmanEmit(k,"GatewayMasonry",t,.12f);
 }
 static void OmanLighting(){
  // Old visual lanterns and their halos are replaced together; no realtime light budget added.
  foreach(var l in root.Find("Lighting").GetComponentsInChildren<Light>(true))if(l.name.StartsWith("Streetlight_"))l.enabled=false;
  foreach(var halo in root.GetComponentsInChildren<Transform>(true).Where(h=>h.name.StartsWith("Halo_Streetlight_")))halo.gameObject.SetActive(false);
  var t=Group("StreetPracticalLighting",oman);var k=new KitMesh{LightmapScale=.08f};var lens=OmanMat("StreetLens",new Color(.8f,.71f,.51f));lens.EnableKeyword("_EMISSION");lens.SetColor("_EmissionColor",new Color(2.2f,1.7f,1.0f));
  foreach(var p in new[]{V(-16,0,-2.5f),V(13,0,-2.5f),V(-12,0,-20.6f),V(14,0,-20.6f),V(35,0,-20.6f),V(-37,0,-2.5f)}){
   float dir=p.z<-10?1:-1;
   k.Lathe(omanMetal,p,Quaternion.identity,new[]{.105f,.09f,.057f,.052f},new[]{0f,1.0f,4.8f,5.6f},12,true,true,true);
   k.Bar(omanTrim,p+V(0,.035f,0),V(.36f,.07f,.36f),.025f);k.Bar(omanMetal,p+V(0,.086f,0),V(.29f,.035f,.29f),.016f);
   foreach(float dx in new[]{-.11f,.11f})foreach(float dz in new[]{-.11f,.11f})k.Cylinder(omanMetal,p+V(dx,.105f,dz),p+V(dx,.145f,dz),.018f,6);
   k.Bar(omanMetal,p+V(0,.63f,-.091f),V(.105f,.29f,.014f),.012f);
   for(int arc=0;arc<7;arc++){float a=arc/7f,b=(arc+1)/7f;k.Cylinder(omanMetal,p+V(0,5.6f+.2f*Mathf.Sin(a*Mathf.PI/2),dir*.82f*a),p+V(0,5.6f+.2f*Mathf.Sin(b*Mathf.PI/2),dir*.82f*b),.045f,8);}

   k.Bar(omanMetal,p+V(0,5.71f,dir*.9f),V(.30f,.16f,.66f),.035f);k.Box(omanTrim,p+V(0,5.63f,dir*.9f),V(.255f,.015f,.56f));k.Box(lens,p+V(0,5.618f,dir*.9f),V(.22f,.009f,.49f));
   for(int fin=0;fin<6;fin++)k.Box(omanMetal,p+V(0,5.81f,dir*(.67f+fin*.08f)),V(.23f,.035f,.022f));
   var g=new GameObject("OmanStreetLamp");g.transform.SetParent(t,false);g.transform.localPosition=p+V(0,5.58f,dir*.9f);g.transform.rotation=Quaternion.Euler(90,0,0);var l=g.AddComponent<Light>();l.type=LightType.Spot;l.spotAngle=115;l.range=17;l.intensity=5.0f;l.color=new Color(1,.82f,.57f);l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;l.shadowRadius=.18f;
  }
  OmanEmit(k,"SimpleMunicipalPosts",t,.1f);
 }

 static void OmanNeighbourLamps(){
  var lamps=Group("NeighbourPracticalLights",oman);var fixtures=new KitMesh{LightmapScale=.08f};var lens=OmanMat("ShopLampLens",new Color(.6f,.48f,.3f));lens.EnableKeyword("_EMISSION");lens.SetColor("_EmissionColor",new Color(.6f,.42f,.17f));
  foreach(var shop in oman.Find("NearShopRow").Cast<Transform>().Concat(oman.Find("OppositeShopRow").Cast<Transform>())){
   if(Mathf.Abs(shop.position.x)>36)continue;
   var pos=shop.TransformPoint(V(-2.3f,3.85f,-.53f));var target=shop.TransformPoint(V(-.5f,1.1f,-1.15f));
   var g=new GameObject("ClosedShopExteriorLight");g.transform.SetParent(lamps,false);g.transform.position=pos;g.transform.LookAt(target);
   var l=g.AddComponent<Light>();l.type=LightType.Spot;l.range=7;l.spotAngle=105;l.intensity=1.65f;l.color=new Color(1,.86f,.64f);l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;l.shadowRadius=.12f;
   var p=lamps.InverseTransformPoint(pos);fixtures.Box(omanMetal,p,V(.3f,.09f,.24f));fixtures.Box(lens,p+Vector3.down*.048f,V(.23f,.009f,.16f));
  }
  // Warm wall grazing makes the market gateway readable without flooding the store interior.
  foreach(float z in new[]{-1.4f,-21.6f}){
   var g=new GameObject("GatewayWallGrazer");g.transform.SetParent(lamps,false);g.transform.localPosition=V(23.9f,1.0f,z);g.transform.LookAt(V(25,7.3f,-11.5f));var l=g.AddComponent<Light>();l.type=LightType.Spot;l.range=17;l.spotAngle=100;l.intensity=2.0f;l.color=new Color(1,.82f,.56f);l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;
   fixtures.Box(omanMetal,V(23.9f,.92f,z),V(.18f,.22f,.16f));
  }
  OmanEmit(fixtures,"ExteriorShopFixtureHousings",lamps,.08f);
 }

 static void OmanContinuity(){
  var t=Group("OmaniCityContinuity360",oman);var rng=new System.Random(102126);
  var chunks=Enumerable.Range(0,8).Select(i=>new KitMesh{Lightmapped=false}).ToArray();
  // Mid/far scenery is eight bounded chunks, without interiors, physics, individual scripts or lights.
  for(int i=0;i<40;i++){
   float angle=i*Mathf.PI*2/40;float radius=68+(float)rng.NextDouble()*18;
   var p=V(Mathf.Sin(angle)*radius,0,-11.5f+Mathf.Cos(angle)*radius);
   if(Mathf.Abs(p.z+11.5f)<18)continue;var k=chunks[i/5];float w=7+(float)rng.NextDouble()*7,d=8,h=5+(float)rng.NextDouble()*6;var wall=omanWalls[i%4];
   k.Box(wall,p+V(0,h/2,0),V(w,h,d));k.Box(omanTrim,p+V(0,h+.12f,0),V(w+.14f,.24f,d+.14f));
   if(i%4==0){k.Box(wall,p+V(1,h+.7f,0),V(2.2f,1.4f,2));k.Cylinder(omanWhite,p+V(-1,h+.2f,0),p+V(-1,h+1.2f,0),.45f,8);}
   for(float y=2;y<h-1;y+=2.8f)for(int j=-1;j<=1;j++)foreach(float f in new[]{-1f,1f}){
    float x=p.x+j*w*.24f,z=p.z+f*(d/2+.012f);k.Face(omanDark,Vector3.forward*f,V(x-.35f,y-.5f,z),V(x+.35f,y-.5f,z),V(x+.35f,y+.5f,z),V(x-.35f,y+.5f,z));
   }
  }
  foreach(float side in new[]{-1f,1f})for(int bank=0;bank<2;bank++){
   var k=chunks[(side<0?0:4)+bank];float z=bank==0?0:-23,facing=bank==0?-1:1;
   for(int i=0;i<5;i++){
    if(i==1&&bank==1)continue;float x=side*(58+i*8.7f),h=6.4f+(i%3)*.5f;var wall=omanWalls[(i+bank)%4];
    k.Box(wall,V(x,h/2,z-facing*4),V(8.65f,h,8));k.Box(omanTrim,V(x,h+.12f,z-facing*4),V(8.7f,.24f,8.1f));
    k.Box(omanShutter,V(x,1.4f,z+facing*.015f),V(7.6f,2.7f,.025f));k.Box(omanTrim,V(x,3.3f,z+facing*.12f),V(8.55f,.65f,.25f));
    for(int j=-1;j<=1;j++){float wx=x+j*2.5f,zz=z+facing*.022f;k.Face(omanDark,Vector3.forward*facing,V(wx-.5f,4.1f,zz),V(wx+.5f,4.1f,zz),V(wx+.5f,5.6f,zz),V(wx-.5f,5.6f,zz));}
   }
  }
  // A T-junction/dogleg beyond each playable end closes the horizon naturally, rather than a fog wall.
  foreach(float side in new[]{-1f,1f}){
   var k=chunks[side<0?2:6];float x=side*78;var wall=omanWalls[side<0?1:2];
   k.Box(wall,V(x,3.8f,-11.5f),V(7,7.6f,29));k.Box(omanTrim,V(x,7.78f,-11.5f),V(7.15f,.2f,29.15f));
   float front=x-side*3.515f;
   for(int j=0;j<8;j++){float z=-24+j*3.45f;k.Face(omanDark,Vector3.left*side,V(front,4.1f,z-.5f),V(front,4.1f,z+.5f),V(front,5.7f,z+.5f),V(front,5.7f,z-.5f));k.Box(omanShutter,V(front,1.4f,z),V(.025f,2.7f,2.9f));}
   // Asphalt physically turns through the omitted rear shop bay into a short side street.
   k.Box(omanRoad,V(side*66.7f,-.28f,-35),V(8.6f,.30f,30));
   k.Box(omanPaving,V(side*60.9f,-.16f,-35),V(2.8f,.30f,30));k.Box(omanPaving,V(side*72.5f,-.16f,-35),V(2.8f,.30f,30));
   // A darker neighboring roof and setback service block break the outline of the terminating street.
   k.Box(omanWalls[3],V(x+side*5,4.6f,8),V(8,9.2f,10));
  }
  int triangles=0;
  for(int i=0;i<8;i++){triangles+=chunks[i].TriangleCount;var g=OmanEmit(chunks[i],"ScenerySector_"+i,t,0);var r=g.GetComponent<MeshRenderer>();if(r){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}}
  if(triangles>6500)throw new Exception("Distant scenery budget exceeded: "+triangles);
  Debug.Log("OMAN_CONTINUITY_BUDGET_PASS eight scenery renderers; triangles="+triangles+"; no colliders, scripts, interiors or lights; two road doglegs");
 }
 static string OmanInteriorSignature(){
  var lines=new List<string>();foreach(string part in new[]{"Architecture","Facade","Showroom","Office","Dressing"}){var t=root.Find(part);if(!t)continue;foreach(var x in t.GetComponentsInChildren<Transform>(true))lines.Add(PathOf(x)+"|"+x.gameObject.activeSelf+"|"+x.localPosition.ToString("F6")+"|"+x.localRotation.ToString("F6")+"|"+x.localScale.ToString("F6")+"|"+(x.GetComponent<MeshFilter>()?AssetDatabase.GetAssetPath(x.GetComponent<MeshFilter>().sharedMesh):""));}return string.Join("\n",lines);
 }
 public static void OmanReconstructRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;string before=OmanInteriorSignature();var car=root.Find("Exterior/PoliceCar").localToWorldMatrix;
  foreach(string path in new[]{"Exterior/Buildings","Exterior/NeighbourhoodDepth","Exterior/MainStreetArchitecture","Exterior/FacadeAttachments","Exterior/LivingStreetCanopy","Exterior/Trees","Exterior/Streetlights","Exterior/Ground","Exterior/StreetSafetyBoundary","DistantCityBackdrop","CitySkyline360"})OmanDisable(path);
  foreach(string n in new[]{"BusShelter","BikeRack_0","BikeRack_1","StreetGraphics"})OmanDisable("Exterior/Street/"+n);
  foreach(var t in root.Find("LivedInDetails").GetComponentsInChildren<Transform>(true).Where(t=>t.parent==root.Find("LivedInDetails")&&t.position.z<0))t.gameObject.SetActive(false);
  oman=Fresh("OmanStreet",root.Find("Exterior"));OmanMaterials();OmanGround();OmanMainUpper();
  var near=Group("NearShopRow",oman);int id=0;foreach(float side in new[]{-1f,1f})for(int i=0;i<7;i++){float x=side*(12.35f+i*6.65f);OmanShop(near,x,.0f,6.6f,6.65f+(i%3)*.55f,i%3,new[]{0,1,2,4,5,6,7,8}[(i+(side>0?2:0))%8],id++);}
  var across=Group("OppositeShopRow",oman);across.localPosition=V(0,0,-23);across.localRotation=Quaternion.Euler(0,180,0);
  for(int i=0;i<15;i++)OmanShop(across,-49+i*7,0,6.94f,6.45f+(i%4)*.45f,(i+1)%3,new[]{0,1,2,4,5,6,7,8}[(i+4)%8],id++);
  OmanGateway();OmanLighting();OmanContinuity();OmanNeighbourLamps();OmanLegacyLeafCleanup();OmanSurfacePolish();RoadPrepareMaps();RoadMaterials();RoadSealWalls();OmanBakeQuality();BroaderPracticalLights();
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.023f;RenderSettings.fogColor=new Color(.095f,.115f,.145f);
  if(before!=OmanInteriorSignature()||car!=root.Find("Exterior/PoliceCar").localToWorldMatrix)throw new Exception("Protected interior/car transform changed");
  OmanChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();RawCarChecks();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();OmanPreview();EditableDelivery.Build();Debug.Log("OMAN_LOCAL_PREVIEW_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void OmanChecks(){
  var t=root.Find("Exterior/OmanStreet");if(!t||!t.gameObject.activeInHierarchy)throw new Exception("Oman exterior missing");
  if(t.Find("NearShopRow").childCount!=14||t.Find("OppositeShopRow").childCount!=15)throw new Exception("Incomplete street rows");
  foreach(string p in new[]{"Exterior/Buildings","Exterior/MainStreetArchitecture","Exterior/Ground"})if(root.Find(p).gameObject.activeInHierarchy)throw new Exception("Legacy exterior overlaps replacement");
  foreach(var b in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var p in new[]{V(0,0,-11),V(-23,0,-7),V(23,0,-18),V(0,0,-21.5f)})if(!b.Contains(p))throw new Exception("New parking/road inaccessible "+p);
  foreach(var l in root.Find("Lighting").GetComponentsInChildren<Light>()){var p=root.InverseTransformPoint(l.transform.position);if(l.enabled&&l.type!=LightType.Directional&&p.z>.25f&&p.z<8.25f&&Mathf.Abs(p.x)<7.7f)throw new Exception("Store fixtures must remain off");}
  var distant=t.Find("OmaniCityContinuity360");if(!distant||distant.GetComponentsInChildren<MeshRenderer>().Length!=8||distant.GetComponentsInChildren<Collider>().Length!=0||distant.GetComponentsInChildren<Light>().Length!=0)throw new Exception("Distant scenery must remain eight lightweight chunks without physics/lights");

  var shops=t.Find("NearShopRow").Cast<Transform>().Concat(t.Find("OppositeShopRow").Cast<Transform>()).ToArray();
  foreach(string condition in new[]{"Maintained","Weathered","Neglected"})if(shops.Count(s=>s.name.EndsWith(condition))<5)throw new Exception("Insufficient maintenance variety: "+condition);
  if(shops.Count(s=>s.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Facade_")).Max(r=>r.bounds.max.y)<5.7f)<3)throw new Exception("Low shop-unit variation missing");
  foreach(var shop in t.Find("NearShopRow").Cast<Transform>().Concat(t.Find("OppositeShopRow").Cast<Transform>())){
   var sign=shop.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.StartsWith("ShopSign_"));var box=sign.GetComponent<MeshFilter>().sharedMesh.bounds;
   if(box.max.y>=3.62f||box.min.y<2.4f||box.max.z>-.449f)throw new Exception("Shop sign clips overhang/wall: "+PathOf(sign.transform)+" "+box);
   float aspect=sign.sharedMaterials.Single(m=>m.name.StartsWith("Fascia_")).mainTexture.width/(float)sign.sharedMaterials.Single(m=>m.name.StartsWith("Fascia_")).mainTexture.height;
   if(Mathf.Abs(aspect-4)>.01f)throw new Exception("Distorted sign artwork");
  }
  foreach(var lamp in t.GetComponentsInChildren<Light>())if(lamp.lightmapBakeType!=LightmapBakeType.Baked)throw new Exception("Exterior must not add realtime lights");
  if(!RenderSettings.fog||Mathf.Abs(RenderSettings.fogDensity-.023f)>.0001f)throw new Exception("Oman distance haze changed");
  int faces=t.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);if(faces>75000)throw new Exception("Oman exterior geometry budget exceeded "+faces);
  Debug.Log("OMAN_EXTERIOR_CHECKS_PASS 29 shops, actual open gallery/gateway arches, 5m parking bays, 7m lane, expanded walking bounds, protected shop/car, exterior triangles="+faces);
 }
 static void OmanPreview(){
  var go=new GameObject("Temporary Oman review camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=67;c.farClipPlane=220;
  var target=new RenderTexture(1600,1000,24){antiAliasing=4};c.targetTexture=target;var old=RenderTexture.active;
  var views=new[]{(V(-12,1.7f,-11),V(0,3.4f,0)),(V(-21,1.7f,-8.8f),V(25,4,-12)),(V(9,1.7f,-10),V(-6,3.1f,-23)),(V(0,1.7f,4),V(2,2,-23)),(V(18,1.7f,-17),V(25,4.8f,-11.5f)),(V(0,1.7f,-11.5f),V(-55,2.5f,-11.5f)),(V(-27,1.7f,-21.5f),V(-58,3,-24)),(V(27,1.7f,-2),V(58,3,0)),(V(0,1.7f,-11.5f),V(0,3,-50)),(V(0,1.7f,-11.5f),V(0,5,30)),(V(-12.2f,1.7f,-2.6f),V(-12.3f,2.7f,0)),(V(10.7f,1.7f,-4.3f),V(13,3.8f,-2.5f))};
  try{for(int i=0;i<views.Length;i++){c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);c.Render();RenderTexture.active=target;var img=new Texture2D(1600,1000,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,1000),0,0);img.Apply();File.WriteAllBytes("Builds/oman-street-"+i+".png",img.EncodeToPNG());Object.DestroyImmediate(img);}}
  finally{RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);}
 }

 static void OmanLegacyLeafCleanup(){
  // Old tree decals were placed at pavement level even over the lower road.
  foreach(var child in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="DryLeavesAtTree"))child.gameObject.SetActive(false);
 }
 public static void ValidateOmanExterior(){root=GameObject.Find("NightStore").transform;OmanChecks();}


 static void OmanSurfacePolish(){
  // Reuse restrained real PBR micro-relief; do not fake wear with luminous surfaces.
  var render=DemoAssetLibrary.NormalMap("PBRExterior","Render_Normal.png");var paving=DemoAssetLibrary.NormalMap("PBRExterior","PavementSlab_Normal.png");int edited=0;
  foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{OmanFolder})){
   var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
   if(new[]{"Ivory","Sand","Chalk","WarmRender","ConcreteTrim","MaintainedFreshPlaster","KerbWhite","KerbBlack"}.Contains(m.name)){
    m.SetTexture("_BumpMap",render);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",m.name=="MaintainedFreshPlaster"?.16f:.32f);edited++;EditorUtility.SetDirty(m);
   }else if(m.name=="DustyPaving"&&m.shader.name!="CrimeScene/OmanGround"){m.SetTexture("_BumpMap",paving);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.30f);edited++;EditorUtility.SetDirty(m);}
  }
  Debug.Log("OMAN_SURFACE_REFINEMENT_PASS normal-relief on "+edited+" plaster, kerb and paving finishes; restrained smoothness and nonmetallic paint preserved");
 }
 static void OmanTextureBudget(){
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{DemoAssetLibrary.Root+"/OmanExterior"})){
   string path=AssetDatabase.GUIDToAssetPath(guid);var imp=(TextureImporter)AssetImporter.GetAtPath(path);bool changed=false;
   if(imp.maxTextureSize!=1024){imp.maxTextureSize=1024;changed=true;}if(!imp.mipmapEnabled){imp.mipmapEnabled=true;changed=true;}
   if(imp.anisoLevel!=4){imp.anisoLevel=4;changed=true;}
   var a=imp.GetPlatformTextureSettings("Android");if(!a.overridden||a.maxTextureSize!=1024||a.format!=TextureImporterFormat.ASTC_6x6){a.overridden=true;a.maxTextureSize=1024;a.format=TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(a);changed=true;}
   if(changed)imp.SaveAndReimport();
  }
  Debug.Log("OMAN_TEXTURE_BUDGET_PASS mipmapped 1024px exterior artwork, Android ASTC 6x6");
 }
 public static void OmanReviewRun(){try{EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;OmanTextureBudget();OmanLegacyLeafCleanup();EditorSceneManager.SaveOpenScenes();OmanChecks();OmanPreview();GeneralSceneReview();ShopReviewChecks();RefinementChecks();NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();RawCarChecks();EditableDelivery.Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
