#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 const string FieldDocuments="Assets/CrimeSceneDemo/ThirdParty/InvestigationDetail/FieldDocuments/";
 static Material FieldPaper(string name){var m=Mat(Folder,"FieldPaper_"+name,Color.white,.045f);m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(FieldDocuments+name+".png");if(!m.mainTexture)throw new Exception("Missing Arabic field document: "+name);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_GlossyReflections",0);return m;}
 static GameObject PaperSheet(Transform parent,string name,Vector3 position,Vector2 size,float yaw){
  var k=new KitMesh{Lightmapped=false};k.Bar(FieldPaper(name),V(0,.0006f,0),new Vector3(size.x,.0012f,size.y),.0004f);
  // Explicit top-face artwork UVs: cube UVs are metric and unsuitable for a page.
  var g=k.Emit(name,parent,position,Quaternion.Euler(0,yaw,0),Folder,"field_paper_"+name);var mesh=g.GetComponent<MeshFilter>().sharedMesh;
  var copy=Object.Instantiate(mesh);var vertices=copy.vertices;var uv=copy.uv;for(int i=0;i<vertices.Length;i++)uv[i]=new Vector2(vertices[i].x/size.x+.5f,vertices[i].z/size.y+.5f);copy.uv=uv;
  string path=Folder+"/FieldPage_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(copy,old);Object.DestroyImmediate(copy);copy=old;}else AssetDatabase.CreateAsset(copy,path);g.GetComponent<MeshFilter>().sharedMesh=copy;return g;
 }
 static EvidenceClue Clue(Transform parent,EvidenceRegister register,string id,string en,string ar,string noteEn,string noteAr,Vector3 position,Vector3 size,GameObject specimen=null,bool sample=false,FieldInvestigationTool.Method method=FieldInvestigationTool.Method.Forceps){
  var g=Group(id+"_"+en,parent);g.position=position;var collider=g.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=size;
  var c=g.gameObject.AddComponent<EvidenceClue>();c.Id=id;c.TitleEn=en;c.TitleAr=ar;c.ObservationEn=noteEn;c.ObservationAr=noteAr;c.Register=register;c.CanSample=sample;c.RequiredTool=method;c.Specimen=specimen;return c;
 }
 static FieldInvestigationTool FieldTool(Transform parent,EvidenceRegister register,FieldInvestigationTool.Method kind,Vector3 position){
  var g=Group(kind.ToString(),parent);g.localPosition=position;g.localRotation=Quaternion.Euler(0,90,0);
  var steel=Mat(Folder,"FieldSatinSteel",new Color(.40f,.42f,.43f),.33f);steel.SetFloat("_Metallic",.72f);
  var polymer=Mat(Folder,"FieldSterilePolymer",new Color(.72f,.72f,.66f),.13f);var tip=Mat(Folder,"FieldSwabCotton",new Color(.78f,.77f,.69f),.015f);
  var k=new KitMesh{Lightmapped=false};
  if(kind==FieldInvestigationTool.Method.Forceps){
   foreach(float side in new[]{-1f,1f}){
    k.Cylinder(steel,V(0,0,-.055f),V(side*.007f,.001f,.065f),.0032f,8);k.Cylinder(steel,V(side*.007f,.001f,.065f),V(side*.0015f,-.008f,.115f),.0021f,6);
    for(int i=0;i<9;i++)k.Bar(steel,V(side*.005f,.004f,-.01f+i*.005f),V(.004f,.0015f,.0015f),.0005f);
   }
  }else if(kind==FieldInvestigationTool.Method.Swab){k.Cylinder(polymer,V(0,0,-.058f),V(0,0,.105f),.0023f,8);k.Cylinder(tip,V(0,0,.105f),V(0,0,.128f),.005f,10);}
  else if(kind==FieldInvestigationTool.Method.Brush){
   var model=DemoAssetLibrary.PlacePack("ForensicModels","FingerprintBrush",g,V(0,0,-.05f),0,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.None,ModelEuler=new Vector3(90,0,0)});
   if(!model){k.Cylinder(polymer,V(0,0,-.05f),V(0,0,.06f),.004f,10);k.Cylinder(trayFallback(),V(0,0,.06f),V(0,0,.10f),.012f,12);}
  }else{k.Bar(polymer,V(0,0,.015f),V(.067f,.002f,.10f),.001f);k.Box(steel,V(0,-.0012f,.032f),V(.043f,.0005f,.038f));}
  if(k.TriangleCount>0)k.Emit(kind.ToString()+"Body",g,Vector3.zero,Quaternion.identity,Folder,"field_tool_"+kind);
  var box=g.gameObject.AddComponent<BoxCollider>();box.size=kind==FieldInvestigationTool.Method.LiftCard?V(.074f,.035f,.115f):V(.045f,.035f,.19f);box.center=V(0,0,.035f);box.isTrigger=true;
  var tool=g.gameObject.AddComponent<FieldInvestigationTool>();tool.Kind=kind;tool.Register=register;tool.HandPosition=V(0,-.045f,.005f);
  tool.Tip=Group("WorkingTip",g);tool.Tip.localPosition=V(0,0,.125f);foreach(var t in g.GetComponentsInChildren<Transform>())t.gameObject.layer=CrowdLayer;return tool;
 }
 static Material trayFallback()=>Mat(Folder,"FieldBrushBristles",new Color(.07f,.065f,.055f),.04f);
 static EvidenceContainer FieldBox(Transform parent,string name,Vector3 position,Vector3 size,Material shell,Material lining){
  var g=Group(name,parent);g.localPosition=position;float w=size.x,h=size.y,d=size.z;var k=new KitMesh{Lightmapped=false};
  k.Bar(shell,V(0,.006f,0),V(w,.012f,d),.003f);k.Box(lining,V(0,.013f,0),V(w-.016f,.004f,d-.016f));
  foreach(float s in new[]{-1f,1f}){k.Bar(shell,V(s*(w*.5f-.005f),h*.5f,0),V(.01f,h,d),.002f);k.Bar(shell,V(0,h*.5f,s*(d*.5f-.005f)),V(w-.02f,h,.01f),.002f);}
  k.Emit("HollowBase",g,Vector3.zero,Quaternion.identity,Folder,"field_box_"+name);
  var hinge=Group("LidHinge",g);hinge.localPosition=V(0,h,d*.5f);var lid=new KitMesh{Lightmapped=false};lid.Bar(shell,V(0,.005f,-d*.5f),V(w+.004f,.010f,d+.004f),.004f);
  var metal=Mat(Folder,"FieldBoxLatch",new Color(.31f,.33f,.34f),.29f);metal.SetFloat("_Metallic",.6f);lid.Bar(metal,V(0,-.01f,-d-.005f),V(.027f,.027f,.005f),.002f);lid.Emit("Lid",hinge,Vector3.zero,Quaternion.identity,Folder,"field_lid_"+name);
  var hit=hinge.gameObject.AddComponent<BoxCollider>();hit.center=V(0,.005f,-d*.5f);hit.size=V(w,.014f,d);hit.isTrigger=true;
  var c=g.gameObject.AddComponent<EvidenceContainer>();c.Lid=hinge;c.OpenDegrees=105;
  // Positive X opens a lid extending along -Z upward and behind its rear hinge.
  return c;
 }
 static void EvidenceGameplay(){
  var group=Fresh("InvestigationFieldwork",root);var register=group.gameObject.AddComponent<EvidenceRegister>();register.Session=Object.FindFirstObjectByType<DemoSession>();register.Camera=Object.FindFirstObjectByType<EvidenceCamera>();
  var tray=Mat(Folder,"FieldKitRubber",new Color(.045f,.055f,.06f),.13f);
  var stand=Group("PortableFieldworkCart",group);stand.localPosition=V(6.3f,0,2.4f);var steel=Mat(Folder,"FieldCartSatinSteel",new Color(.22f,.25f,.26f),.24f);steel.SetFloat("_Metallic",.6f);var cart=new KitMesh{LightmapScale=.55f};
  cart.Bar(steel,V(0,.875f,0),V(1,.03f,.54f),.008f);cart.Bar(steel,V(0,.52f,0),V(.88f,.02f,.42f),.004f);
  foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.21f,.21f}){cart.Cylinder(steel,V(x,.05f,z),V(x,.86f,z),.014f,8);cart.Cylinder(tray,V(x-.013f,.04f,z),V(x+.013f,.04f,z),.04f,10);}
  cart.Emit("CartFrameAndCasters",stand,Vector3.zero,Quaternion.identity,Folder,"field_cart");StoreKit.MarkStatic(stand.gameObject,true);var standCollider=stand.gameObject.AddComponent<BoxCollider>();standCollider.center=V(0,.445f,0);standCollider.size=V(.98f,.89f,.53f);
  var kit=Group("FieldKit",group);kit.localPosition=V(6.03f,.892f,2.4f);var k=new KitMesh{Lightmapped=false};
  k.Bar(tray,V(0,.010f,0),V(.48f,.02f,.31f),.007f);
  foreach(float s in new[]{-1f,1f}){k.Bar(tray,V(s*.234f,.031f,0),V(.012f,.045f,.31f),.004f);k.Bar(tray,V(0,.031f,s*.149f),V(.46f,.045f,.012f),.004f);}
  k.Box(tray,V(0,.024f,0),V(.008f,.028f,.28f));k.Emit("PartitionedFieldTray",kit,Vector3.zero,Quaternion.identity,Folder,"field_kit_tray");
  var forceps=FieldTool(kit,register,FieldInvestigationTool.Method.Forceps,V(-.1f,.034f,-.065f));var swab=FieldTool(kit,register,FieldInvestigationTool.Method.Swab,V(-.1f,.034f,.065f));register.Tools=new[]{forceps,swab};
  var guide=PaperSheet(kit,"FieldGuide",V(.12f,.023f,0),new Vector2(.16f,.24f),0);
  var pad=Group("EvidenceRegisterClipboard",group);pad.localPosition=V(6.56f,.892f,2.4f);var clip=new KitMesh{Lightmapped=false};clip.Bar(tray,V(0,.005f,0),V(.37f,.01f,.46f),.005f);clip.Emit("ClipboardBack",pad,Vector3.zero,Quaternion.identity,Folder,"field_clipboard");
  PaperSheet(pad,"RegisterPaper",V(0,.011f,0),new Vector2(.34f,.42f),0);
  var text=Text("WrittenRegister",pad,"",V(0,.014f,0),0,.0027f,new Color(.065f,.063f,.052f));text.transform.localRotation=Quaternion.Euler(90,0,0);text.font=DemoLanguage.Font;text.fontSize=64;text.lineSpacing=1.45f;
  var ink=Mat(Folder,"FieldRegisterInk",Color.black,0);ink.shader=Shader.Find("CrimeScene/WorldText");ink.mainTexture=text.font.material.mainTexture;text.GetComponent<MeshRenderer>().sharedMaterial=ink;text.gameObject.layer=CrowdLayer;register.PageText=text;register.RefreshPage();
  // Only the paper is an interaction surface; the tray never masks its tools.
  var pageCollider=pad.gameObject.AddComponent<BoxCollider>();pageCollider.center=V(0,.02f,0);pageCollider.size=V(.36f,.025f,.45f);pageCollider.isTrigger=true;
  // Register component lives on the group, so this collider routes to it.
  var packets=Group("SealedSamplePackets",group);packets.localPosition=V(6.5f,.533f,2.4f);var kraft=Mat(Folder,"FieldSampleKraft",new Color(.56f,.46f,.30f),.04f);register.SealedPackets=new GameObject[6];
  for(int i=0;i<6;i++){var packet=new KitMesh{Lightmapped=false};packet.Bar(kraft,V(0,.003f,0),V(.11f,.006f,.15f),.002f);packet.Box(tray,V(0,.0065f,-.035f),V(.085f,.001f,.012f));var p=packet.Emit("SealedPacket_"+(i+1),packets,V(0,i*.008f,0),Quaternion.Euler(0,i*4-8,0),Folder,"field_packet_"+i);register.SealedPackets[i]=p;p.SetActive(false);}
  var clues=new System.Collections.Generic.List<EvidenceClue>();
  var dressing=root.Find("Dressing");var old=root.Find("Dressing/AdditionalPhysicalEvidence");
  var fibre=old.Find("CaughtBlueFabricFibres");clues.Add(Clue(group,register,"E03","Blue fibres","ألياف زرقاء","Several short blue threads beside the entry route.","خيوط زرقاء قصيرة بجوار مسار الدخول.",V(.685f,.012f,1.20f),V(.095f,.025f,.095f),fibre.gameObject,true));
  var clasp=old.Find("BentBrokenJewelleryClasp");clues.Add(Clue(group,register,"E04","Broken clasp","قفل سوار مكسور","An open, bent clasp. Its source has not been established.","قفل مثني ومفتوح. لم يحدد مصدره بعد.",V(-1.03f,.015f,4.06f),V(.10f,.035f,.10f),clasp.gameObject,true));
  foreach(int i in new[]{1,4}){var blood=dressing.Find("FloorBloodTraces/SmallDriedBloodDrops_"+i);clues.Add(Clue(group,register,i==1?"E01":"E02",i==1?"Small floor trace":"Separate floor trace",i==1?"أثر صغير على الأرض":"أثر منفصل على الأرض","A small reddish-brown transfer; identification requires analysis.","أثر بني محمر صغير؛ تحديد طبيعته يحتاج إلى تحليل.",blood.position+Vector3.up*.012f,V(.10f,.025f,.10f),null,true,FieldInvestigationTool.Method.Swab));}
  var chipGroup=Group("LooseGlassChip",group);chipGroup.localPosition=V(.48f,.003f,.82f);var glass=Mat(Folder,"FieldLooseChip",new Color(.77f,.83f,.85f,.18f),.86f);glass.shader=Shader.Find("CrimeScene/DisplayGlass");
  var chip=new KitMesh{Lightmapped=false};chip.Bar(glass,V(0,.002f,0),V(.023f,.004f,.016f),.001f);var chipObject=chip.Emit("ClosedGlassFragment",chipGroup,Vector3.zero,Quaternion.Euler(0,23,0),Folder,"field_glass_fragment");clues.Add(Clue(group,register,"E05","Loose glass fragment","شظية زجاج منفصلة","A small fragment close to the damaged entrance pane.","شظية صغيرة قرب لوح زجاج المدخل المتضرر.",chipGroup.position,V(.085f,.035f,.085f),chipGroup.gameObject,true));
  var tieGroup=Group("CutCableTie",group);tieGroup.localPosition=V(1.42f,.004f,10.72f);var nylon=Mat(Folder,"FieldBlackNylon",new Color(.031f,.034f,.035f),.14f);var tie=new KitMesh{Lightmapped=false};
  for(int i=0;i<12;i++){float a=(i*14-60)*Mathf.Deg2Rad,b=((i+1)*14-60)*Mathf.Deg2Rad;tie.Cylinder(nylon,V(Mathf.Cos(a)*.035f,.001f,Mathf.Sin(a)*.03f),V(Mathf.Cos(b)*.035f,.001f,Mathf.Sin(b)*.03f),.0018f,5);}tie.Bar(nylon,V(.035f,.002f,-.027f),V(.009f,.005f,.008f),.001f);tie.Emit("CutTieBody",tieGroup,Vector3.zero,Quaternion.identity,Folder,"field_cut_tie");clues.Add(Clue(group,register,"E06","Cut cable tie","رباط بلاستيكي مقطوع","A cut fastening beside the safe area. Do not assume who left it.","رباط مقطوع قرب الخزائن. لا يحدد وحده من تركه.",tieGroup.position,V(.11f,.035f,.11f),tieGroup.gameObject,true));
  var bag=Group("DroppedJewelleryPouch",group);bag.localPosition=V(-1.34f,.001f,4.24f);var cloth=Mat(Folder,"FieldPouchFabric",new Color(.54f,.41f,.28f),.055f);cloth.mainTexture=DemoAssetLibrary.Texture("PBRInterior","VelvetCharcoal_Color.jpg");cloth.SetTexture("_BumpMap",DemoAssetLibrary.Texture("PBRInterior","VelvetCharcoal_Normal.png"));cloth.EnableKeyword("_NORMALMAP");cloth.SetFloat("_BumpScale",.25f);var pouch=new KitMesh{Lightmapped=false};
  Vector3 Fabric(float u,float v,bool top){float width=.058f+.018f*Mathf.Sin(v*Mathf.PI);return V((u-.5f)*2*width,top ? .004f+Mathf.Sin(v*Mathf.PI)*(.009f+.010f*Mathf.Sin(u*Mathf.PI))+.002f*Mathf.Sin(u*Mathf.PI*8)*Mathf.Sin(v*Mathf.PI) : .001f,-.06f+v*.12f);}
  for(int i=0;i<12;i++)for(int j=0;j<8;j++)foreach(bool top in new[]{true,false}){var a=Fabric(i/12f,j/8f,top);var b=Fabric((i+1)/12f,j/8f,top);var c=Fabric((i+1)/12f,(j+1)/8f,top);var d=Fabric(i/12f,(j+1)/8f,top);pouch.Quad(cloth,top?Vector3.up:Vector3.down,a,b,c,d,new Rect(i/12f,j/8f,1/12f,1/8f));}
  foreach(float u in new[]{0f,1f})for(int j=0;j<8;j++)pouch.Quad(cloth,u==0?Vector3.left:Vector3.right,Fabric(u,j/8f,false),Fabric(u,j/8f,true),Fabric(u,(j+1)/8f,true),Fabric(u,(j+1)/8f,false),new Rect(0,0,.03f,.03f));
  foreach(float v in new[]{0f,1f})for(int j=0;j<12;j++)pouch.Quad(cloth,v==0?Vector3.back:Vector3.forward,Fabric(j/12f,v,false),Fabric((j+1)/12f,v,false),Fabric((j+1)/12f,v,true),Fabric(j/12f,v,true),new Rect(0,0,.03f,.03f));
  pouch.Cylinder(nylon,V(-.055f,.004f,.052f),V(-.095f,.002f,.025f),.0012f,5);pouch.Emit("FabricPouchWithGatheredSeam",bag,Vector3.zero,Quaternion.Euler(0,-21,0),Folder,"field_pouch");clues.Add(Clue(group,register,"E07","Dropped pouch","كيس مجوهرات ساقط","A small pouch lies away from its display. Record its position.","كيس صغير بعيد عن العرض. سجّل موضعه قبل تحريكه.",bag.position,V(.18f,.055f,.15f)));
  var footprint=old.Find("PartialDustFootwear_1");clues.Add(Clue(group,register,"E08","Partial footwear mark","أثر حذاء جزئي","Only part of a sole pattern remains; it is not an identification.","جزء من نقشة نعل؛ لا يكفي وحده لتحديد الهوية.",footprint.position+Vector3.up*.012f,V(.13f,.025f,.25f)));
  var tag=PaperSheet(group,"StockTag",V(1.80f,.001f,9.93f),new Vector2(.065f,.086f),31);clues.Add(Clue(group,register,"E09","Loose stock tag","بطاقة مخزون منفصلة","A bracelet-clasp stock tag. Compare it with the other objects.","بطاقة مخزون لقفل سوار. قارنها ببقية الأشياء.",tag.transform.position+Vector3.up*.01f,V(.09f,.025f,.10f)));
  var shell=Mat(Folder,"FieldDocumentBoxSteel",new Color(.19f,.23f,.23f),.22f);shell.SetFloat("_Metallic",.35f);var box=FieldBox(group,"OfficeDocumentBox",V(2.5f,.752f,9.3f),V(.25f,.07f,.19f),shell,tray);
  var receipt=PaperSheet(box.transform,"SalesReceipt",V(0,.017f,0),new Vector2(.11f,.145f),0);var receiptClue=Clue(box.transform,register,"E10","Service receipt","إيصال صيانة","A shop copy for bracelet-clasp servicing. It gives context, not proof of involvement.","نسخة متجر لصيانة قفل سوار. تضيف سياقًا ولا تثبت التورط.",receipt.transform.position+Vector3.up*.005f,V(.12f,.01f,.15f));receiptClue.Container=box;clues.Add(receiptClue);
  var note=PaperSheet(group,"StaffNote",V(2.5f,.752f,8.99f),new Vector2(.16f,.215f),12);clues.Add(Clue(group,register,"E11","Staff reminder","ملاحظة الموظفين","A routine reminder to separate repair items from display stock.","تذكير بفصل قطع الصيانة عن مخزون العرض.",note.transform.position+Vector3.up*.01f,V(.17f,.025f,.22f)));
  var safe=root.Find("Office/SafeLarge");var scratches=Group("FreshSafeFrameScuffs",group);scratches.position=safe.TransformPoint(V(.61f,.85f,-.456f));var scuff=new KitMesh{Lightmapped=false};var bare=Mat(Folder,"FieldExposedSafeSteel",new Color(.34f,.35f,.34f),.34f);bare.SetFloat("_Metallic",.7f);for(int i=0;i<4;i++)scuff.Cylinder(bare,V(-.006f,i*.006f,0),V(.015f,i*.006f+.003f,-.001f),.0007f,5);scuff.Emit("FineFrameScratches",scratches,Vector3.zero,Quaternion.identity,Folder,"field_safe_scuffs");clues.Add(Clue(group,register,"E12","Frame scuff marks","خدوش إطار الخزنة","Short bright scratches interrupt the finish. Their cause is unconfirmed.","خدوش قصيرة تقطع الطلاء. سببها غير مؤكد.",scratches.position,V(.065f,.065f,.045f)));
  register.Clues=clues.OrderBy(c=>c.Id).ToArray();register.Containers=new[]{box};register.RefreshPage();
  var state=Object.FindFirstObjectByType<CrimeSceneState>();if(state){var robbed=clues.Where(c=>c.Id!="E10"&&c.Id!="E11").Select(c=>c.gameObject).Concat(new[]{chipGroup.gameObject,tieGroup.gameObject,bag.gameObject,scratches.gameObject,tag});state.RobbedOnly=state.RobbedOnly.Where(g=>g&&!g.transform.IsChildOf(group)).Concat(robbed).Distinct().ToArray();}
  foreach(var r in group.GetComponentsInChildren<MeshRenderer>(true)){r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;if(r.GetComponent<TextMesh>())r.shadowCastingMode=ShadowCastingMode.Off;}
  ConfigurePortableFieldwork(register);
  AddShopSearchActivities(register);
 }
 public static void FieldworkInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;EvidenceGameplay();
  foreach(var r in root.Find("Showroom/Counter").GetComponentsInChildren<Renderer>())Debug.Log("FIELD_COUNTER "+PathOf(r.transform)+" "+r.bounds);
  foreach(var r in root.GetComponentsInChildren<Renderer>().Where(r=>r.bounds.Intersects(new Bounds(V(2.5f,.95f,9.2f),V(.65f,.5f,.85f)))))Debug.Log("FIELD_OFFICE_NEAR "+PathOf(r.transform)+" "+r.bounds);
  FieldworkPreview();Debug.Log("FIELDWORK_INSPECTION_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void FieldworkChecks(){
  if(!root)root=GameObject.Find("NightStore").transform;
  var register=root.Find("InvestigationFieldwork").GetComponent<EvidenceRegister>();bool persist=register.PersistRecords;register.PersistRecords=false;
  void Require(bool value,string message){if(!value)throw new Exception("Fieldwork: "+message);}
  Require(register.Clues.Length==18&&register.Clues.Select(c=>c.Id).Distinct().Count()==18,"eighteen stable, unique evidence IDs required");
  Require(register.Tools.Length==4&&register.Clues.Count(c=>c.CanSample)==10,"four field tools / ten sample points required");
  Require(register.GetComponentsInChildren<Light>().Length==0&&register.GetComponentsInChildren<Rigidbody>().Length==0,"fieldwork must not add lights or rigidbody forces");
  foreach(var tool in register.Tools){Require(tool.GetComponentsInChildren<Collider>().All(c=>c.isTrigger),"held/racked tool can collide with player");tool.RecordHome();}
  int triangles=register.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3);Require(triangles<15000,"fieldwork geometry exceeds budget");
  foreach(var r in register.GetComponentsInChildren<MeshRenderer>(true))Require(r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported&&!m.IsKeywordEnabled("_EMISSION")),"unavailable or glowing fieldwork material");
  var test=new GameObject("Temporary fieldwork input test");var hand=test.AddComponent<HandInteractor>();var second=new GameObject("Temporary second hand");var hand2=second.AddComponent<HandInteractor>();
  try{
   register.Begin();var forceps=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.Forceps);var swab=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.Swab);var fibre=register.Clues.First(c=>c.Id=="E03");
   var home=forceps.transform.position;test.transform.position=home+Vector3.up*.07f;Require(hand.TakeFieldTool(forceps),"controller cannot take field tool");second.transform.position=test.transform.position;Require(!hand2.TakeFieldTool(forceps),"two hands can hold the same tool");
   test.transform.position=fibre.transform.position+V(0,.3f,-.01f);test.transform.LookAt(fibre.transform.position);forceps.Follow(test.transform);Physics.SyncTransforms();
   Require(InvestigationInteraction.Target(new Ray(test.transform.position,test.transform.forward),.65f,hand,out var selected)&&selected.collider.GetComponentInParent<EvidenceClue>()==fibre,"controller ray misses fibre or hits its own held tool");
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=Vector3.Lerp(test.transform.position,fibre.transform.position,.5f);wall.transform.localScale=Vector3.one*.08f;Physics.SyncTransforms();
   Require(InvestigationInteraction.Target(new Ray(test.transform.position,test.transform.forward),.65f,hand,out selected)&&!selected.collider.GetComponentInParent<EvidenceClue>(),"evidence can be selected through opaque geometry");Object.DestroyImmediate(wall);Physics.SyncTransforms();
   hand.Trigger();Require(fibre.Sampled&&!fibre.Specimen.activeSelf&&register.SampleCount==1,"forceps did not collect / record / hide the loose specimen");int entries=register.Entries.Count;Require(!fibre.Sample(forceps)&&register.Entries.Count==entries,"duplicate collection adds records");
   hand.Release();Require(!hand.FieldTool&&!forceps.Holder&&Vector3.Distance(forceps.transform.position,home)<.001f,"released field tool floats instead of returning to its tray");
   test.transform.position=swab.transform.position+Vector3.up*.07f;Require(hand.TakeFieldTool(swab),"swab cannot be taken");var blood=register.Clues.First(c=>c.Id=="E01");test.transform.position=blood.transform.position+Vector3.up*.3f;Require(blood.Sample(swab),"swab cannot sample trace");Require(root.Find("Dressing/FloorBloodTraces").gameObject.activeInHierarchy,"sampling erased the whole blood trail");
   var clasp=register.Clues.First(c=>c.Id=="E04");test.transform.position=clasp.transform.position+Vector3.up*.3f;Require(!clasp.Sample(swab)&&!clasp.Sampled,"wrong tool collected loose metal");hand.Release();
   test.transform.position=forceps.transform.position+Vector3.up*.07f;hand.TakeFieldTool(forceps);test.transform.position=clasp.transform.position+Vector3.up*2;Require(!clasp.Sample(forceps),"controller collection works from outside reach");hand.Release();
   var receipt=register.Clues.First(c=>c.Id=="E10");Require(!receipt.Inspect(),"document can be read inside closed box");receipt.Container.Toggle();Require(receipt.Inspect(),"opening the box does not expose its document");
   var desktopObject=new GameObject("Temporary desktop field tool test");var view=desktopObject.AddComponent<Camera>();var desktop=desktopObject.AddComponent<DesktopInteractor>();desktop.ReadDesktopInput=false;desktop.View=view;
   try{Require(desktop.TakeFieldTool(forceps),"desktop cannot take field tool");desktopObject.transform.position=clasp.transform.position+Vector3.up*.5f;forceps.Follow(view.transform);Require(forceps.transform.position.y<desktopObject.transform.position.y,"desktop tool does not sit below view centre");Require(clasp.Sample(forceps),"desktop sampling fails within reach");desktop.ReleaseAll();Require(!forceps.Holder,"desktop mode exit leaves tool held");}finally{Object.DestroyImmediate(desktopObject);}
   bool language=DemoLanguage.Arabic;try{foreach(bool ar in new[]{true,false}){DemoLanguage.Arabic=ar;register.RefreshPage();Require(!string.IsNullOrEmpty(register.PageText.text),"bilingual register page missing");}}finally{DemoLanguage.Arabic=language;}
   string json=JsonUtility.ToJson(new EvidenceRegister.RecordFile{session="verification",entries=register.Entries.ToList()});Require(JsonUtility.FromJson<EvidenceRegister.RecordFile>(json).entries.Count==register.Entries.Count,"evidence log cannot round-trip");
   register.ResetInvestigation();Require(register.ObservedCount==0&&register.SampleCount==0&&register.SealedPackets.All(p=>!p.activeSelf)&&register.Clues.All(c=>!c.Specimen||c.Specimen.activeSelf)&&register.Containers.All(c=>!c.IsOpen),"reset fails to restore samples, packets or lids");
  }finally{hand.ReleaseAll();hand2.ReleaseAll();Object.DestroyImmediate(test);Object.DestroyImmediate(second);register.ResetInvestigation();register.PersistRecords=persist;}
  PortableFieldworkChecks(register);
  ShopSearchChecks(register);
  Debug.Log("FIELDWORK_BEHAVIOUR_PASS unique IDs; two-handed exclusion; controller ray; opaque occlusion; forceps/swab and wrong-tool/reach rules; single collection; tray return; container access; desktop sampling; bilingual register; JSON; reset. triangles="+triangles);
 }
 public static void FieldworkRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string car=CarGeometrySignature();EvidenceGameplay();FieldworkChecks();
  EvidenceGameplay();Physics.SyncTransforms();if(car!=CarGeometrySignature())throw new Exception("Protected car geometry changed");InvestigationChecks();NightStoreValidation.PresentationChecks();
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();FieldworkPreview();PortablePreview();Debug.Log("FIELDWORK_REFINEMENT_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void FieldworkDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");FieldworkChecks();InvestigationChecks();NightStoreValidation.PresentationChecks();EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");EditableDelivery.Build();Debug.Log("FIELDWORK_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void FieldworkRuntimeCheck(){
  root=GameObject.Find("NightStore").transform;FieldworkChecks();var register=root.Find("InvestigationFieldwork").GetComponent<EvidenceRegister>();
  register.Clues.First(c=>c.Id=="E07").Inspect();string path=register.RecordPath;if(!File.Exists(path)||!File.ReadAllText(path).Contains("E07"))throw new Exception("Runtime evidence file was not saved");
  register.Session.SessionReset.Invoke();if(register.ObservedCount!=0||!File.Exists(path))throw new Exception("Reset lost record history or failed to clear current register");
  var box=register.Containers[0];box.Toggle();box.SendMessage("Update");if(box.Lid.localEulerAngles.x<.01f)throw new Exception("Runtime container lid does not animate");box.Close();
  var testHand=new GameObject("Temporary torch replacement hand");var adapter=testHand.AddComponent<HandInteractor>();var forceps=register.Tools[0];testHand.transform.position=forceps.transform.position+Vector3.up*.07f;
  var lighting=Object.FindFirstObjectByType<DemoLighting>();Transform oldParent=null;Vector3 oldPosition=Vector3.zero;Quaternion oldRotation=Quaternion.identity;
  try{if(!adapter.TakeFieldTool(forceps))throw new Exception("Runtime field tool cannot be held");lighting.Flashlight(true);var beam=lighting.FlashlightLight.transform;oldParent=beam.parent;oldPosition=beam.localPosition;oldRotation=beam.localRotation;beam.SetParent(testHand.transform,false);forceps.SendMessage("LateUpdate");if(adapter.FieldTool||forceps.Holder)throw new Exception("Same-hand torch stacks on held field tool");beam.SetParent(oldParent,false);beam.localPosition=oldPosition;beam.localRotation=oldRotation;
   var kit=register.Kit;testHand.transform.position=kit.Grip.position;if(!adapter.TakeKit(kit))throw new Exception("Runtime portable case cannot be held");beam.SetParent(testHand.transform,false);kit.SendMessage("LateUpdate");if(adapter.CarriedKit||kit.Holder)throw new Exception("Same-hand torch stacks on portable case");beam.SetParent(oldParent,false);beam.localPosition=oldPosition;beam.localRotation=oldRotation;register.ResetInvestigation();
   Debug.Log("FIELDWORK_TORCH_REPLACEMENT_PASS field tool and portable case safely put down when same-hand torch is equipped");}
  finally{if(lighting&&lighting.FlashlightLight&&lighting.FlashlightLight.transform.parent==testHand.transform){lighting.FlashlightLight.transform.SetParent(oldParent,false);lighting.FlashlightLight.transform.localPosition=oldPosition;lighting.FlashlightLight.transform.localRotation=oldRotation;}adapter.ReleaseAll();Object.DestroyImmediate(testHand);}
  Debug.Log("FIELDWORK_RUNTIME_PASS real Start/events, disk record, session-reset subscription, completed record retained; saved test record="+path);FieldworkPreview();
 }
 static void FieldworkPreview(){
  var go=new GameObject("Temporary fieldwork review camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=48;c.farClipPlane=220;var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;
  var target=new RenderTexture(1440,1000,24);c.targetTexture=target;var previous=RenderTexture.active;
  var views=new[]{(V(-4.3f,1.55f,-2.25f),V(-4.3f,1.08f,-1.3f)),(V(1.65f,1.5f,9.4f),V(2.5f,.8f,9.2f)),(V(-1.15f,.6f,3.65f),V(-1.25f,.025f,4.17f)),(V(1.4f,1.2f,10.1f),V(1.45f,.45f,10.8f))};
  var box=root.Find("InvestigationFieldwork").GetComponent<EvidenceRegister>().Containers[0];
  try{for(int i=0;i<views.Length;i++){if(i==1)box.Lid.localRotation=Quaternion.Euler(box.OpenDegrees,0,0);c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);lighting.FlashlightLight.transform.SetPositionAndRotation(c.transform.position+c.transform.right*.10f,c.transform.rotation);c.Render();RenderTexture.active=target;var t=new Texture2D(1440,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,1000),0,0);t.Apply();File.WriteAllBytes("Builds/fieldwork-"+i+".png",t.EncodeToPNG());Object.DestroyImmediate(t);box.Close();}}
  finally{box.Close();RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
}
public static partial class NightStoreValidation {public static void FieldworkChecks()=>PresentationPolish.FieldworkChecks();}
[InitializeOnLoad] public static class FieldworkPlayVerification {
 const string Pending="FieldworkPlayVerificationPending";static int frames;
 static FieldworkPlayVerification(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Poll;}
 public static void Run(){EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");SessionState.SetBool(Pending,true);EditorApplication.update-=Poll;EditorApplication.update+=Poll;EditorApplication.EnterPlaymode();}
 static void Poll(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling||++frames<30)return;EditorApplication.update-=Poll;SessionState.SetBool(Pending,false);try{PresentationPolish.FieldworkRuntimeCheck();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
