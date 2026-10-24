#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

// The forensic sandbox (October 24, 2026): the research in Docs/Research turned into tools,
// surfaces and traces. Six more case tools (magnetic wand, lifting tape and card, gel lifter,
// presumptive blood test, dental-stone and silicone casting kits), latent-mark fields on every
// display case, touch DNA beside the handle marks, a paint stain that tests negative, a dust
// footwear mark that can be trodden on, a sand impression to cast, pry marks to cast, dropped
// items to photograph and bag, and the consumables laid out beside the field table. Nothing guides
// the trainee; every clue keeps a ProcedureLog for the review. Unverified in Unity.
public static partial class PresentationPolish {
 static readonly string[] SandboxCases={"CentralCase_Left","CentralCase_Middle","CentralCase_Right","WallCase_Left0","WallCase_Left1","WallCase_Left2","WallCase_Right0","WallCase_Right1","WallCase_Rear"};
 static void ConfigureForensicSandbox(EvidenceRegister register){
  var kit=register.Kit;var caseRoot=kit.transform;var tray=caseRoot.Find("FieldKit");var lid=caseRoot.Find("RaisedLid");
  foreach(var old in register.Tools.Where(t=>t&&t.Kind>=FieldInvestigationTool.Method.MagneticWand).ToArray())Object.DestroyImmediate(old.gameObject);
  var tools=register.Tools.Where(t=>t&&t.Kind<FieldInvestigationTool.Method.MagneticWand).ToList();
  // ---- case tools: three more tray rows and two spots on the case base (positions in Docs/Research/FORENSIC_MECHANICS_SPEC.md §3)
  tools.Add(FieldTool(tray,register,FieldInvestigationTool.Method.MagneticWand,V(-.1f,.034f,-.122f)));
  tools.Add(FieldTool(tray,register,FieldInvestigationTool.Method.Tape,V(.105f,.034f,-.112f)));
  tools.Add(FieldTool(tray,register,FieldInvestigationTool.Method.MikrosilKit,V(-.1f,.034f,.118f)));
  var gel=FieldTool(caseRoot,register,FieldInvestigationTool.Method.GelLifter,V(.36f,.025f,-.17f));gel.transform.localRotation=Quaternion.identity;tools.Add(gel);
  var km=FieldTool(caseRoot,register,FieldInvestigationTool.Method.KMTest,V(.36f,.025f,0));km.transform.localRotation=Quaternion.Euler(0,90,0);tools.Add(km);
  // The casting kit works outside: its bowl rests on the pavement beside the table (its dock is a child of the case, like every tool's).
  var casting=FieldTool(caseRoot,register,FieldInvestigationTool.Method.CastingKit,V(.7f,-.892f+.002f,-.75f));casting.transform.localRotation=Quaternion.identity;tools.Add(casting);
  register.Tools=tools.ToArray();
  foreach(var tool in register.Tools){if(tool.Dock)continue;var dock=Group("Dock_"+tool.Kind,caseRoot);dock.SetPositionAndRotation(tool.transform.position,tool.transform.rotation);tool.Dock=dock;}
  foreach(var tool in register.Tools)foreach(var t in tool.GetComponentsInChildren<Transform>(true))t.gameObject.layer=CrowdLayer;
  // ---- consumables and protective items on a ground sheet beside the field table
  var consumables=Fresh("ForensicConsumables",root);consumables.position=V(-5.55f,0,-1.3f);
  var sheet=Mat(Folder,"ForensicGroundSheet",new Color(.16f,.19f,.23f),.08f);var tarp=new KitMesh{Lightmapped=false};tarp.Bar(sheet,V(0,.002f,0),V(1.3f,.004f,.9f),.001f);tarp.Emit("GroundSheet",consumables,Vector3.zero,Quaternion.identity,Folder,"forensic_ground_sheet");
  (string model,Vector3 at,float yaw,Vector3 fallback)[] items={
   ("EvidenceBagPaper",V(-.5f,0,-.3f),12,V(.2f,.022f,.33f)),("EvidenceBagPlastic",V(-.27f,0,-.3f),-8,V(.23f,.008f,.3f)),("EvidenceBox",V(.4f,0,-.25f),20,V(.39f,.34f,.52f)),
   ("EvidenceTag",V(-.05f,0,-.35f),30,V(.06f,.002f,.12f)),("EvidenceLabelRoll",V(.05f,0,-.2f),0,V(.07f,.07f,.07f)),("BiohazardBinSmall",V(.45f,0,.25f),-15,V(.2f,.3f,.2f)),
   ("SteppingPlate",V(-.45f,0,.25f),0,V(.35f,.01f,.35f)),("SteppingPlate",V(-.45f,.012f,.3f),6,V(.35f,.01f,.35f)),("OrangeGoggles",V(-.1f,0,.3f),40,V(.16f,.06f,.08f)),
   ("FaceMask",V(.05f,0,.32f),-20,V(.17f,.02f,.09f)),("ShoeCoverPair",V(.2f,0,.3f),10,V(.3f,.05f,.14f)),("NitrileGloveBox",V(-.25f,0,.05f),0,V(.235f,.103f,.125f)),
   ("DentalStoneBag",V(1.0f,0,-.1f),25,V(.46f,.18f,.7f)),("CastingFrame",V(.95f,0,.35f),0,V(.18f,.03f,.46f)),("ReagentSprayBottle",V(.2f,0,.0f),0,V(.16f,.32f,.12f)),
   ("SwabBox",V(-.05f,0,.05f),15,V(.03f,.03f,.17f)),("DistilledWaterAmpoule",V(.05f,0,.08f),0,V(.03f,.08f,.03f)),("HingeLifterBlack",V(-.3f,0,-.05f),-35,V(.05f,.002f,.1f)),("BackingCardBlack",V(-.15f,0,-.1f),10,V(.076f,.001f,.127f))};
  var fallbackMat=Mat(Folder,"ForensicConsumableFallback",new Color(.55f,.56f,.52f),.2f);int placed=0;
  foreach(var item in items){
   var g=DemoAssetLibrary.PlacePack("ForensicModels",item.model,consumables,item.at,item.yaw,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.None});
   if(g){placed++;continue;}
   var f=new KitMesh{Lightmapped=false};f.Bar(fallbackMat,V(0,item.fallback.y*.5f,0),item.fallback,.003f);f.Emit(item.model+"_Fallback",consumables,item.at,Quaternion.Euler(0,item.yaw,0),Folder,"forensic_fallback_"+item.model.ToLowerInvariant());
  }
  foreach(var t in consumables.GetComponentsInChildren<Transform>(true))t.gameObject.layer=CrowdLayer;StoreKit.MarkStatic(consumables.gameObject,true);
  // ---- latent-mark fields on every display-case top
  foreach(var old in root.GetComponentsInChildren<LatentMarkField>(true))Object.DestroyImmediate(old.gameObject);
  var ridge=Mat(Folder,"LatentFieldRidge",new Color(.075f,.068f,.058f,.76f),.025f);ridge.shader=Shader.Find("CrimeScene/SurfaceDecal");ridge.mainTexture=DemoAssetLibrary.Texture("SurfaceDecals","Fingerprints_Atlas.png");ridge.renderQueue=2465;
  int fields=0;
  for(int i=0;i<SandboxCases.Length;i++){
   var showcase=root.Find("Showroom/"+SandboxCases[i]);if(!showcase)continue;var renderers=showcase.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.GetComponent<TextMesh>()).ToArray();if(renderers.Length==0)continue;
   var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   var g=Group("LatentMarkField",showcase);g.position=new Vector3(b.center.x,b.max.y,b.center.z);g.rotation=Quaternion.identity;
   var box=g.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(Mathf.Max(.2f,b.size.x-.04f),.012f,Mathf.Max(.2f,b.size.z-.04f));box.center=V(0,-.006f,0);
   var field=g.gameObject.AddComponent<LatentMarkField>();field.Register=register;field.Seed=7919+i*131;field.RidgeMaterial=ridge;field.IdPrefix="LM"+(i+1);field.PlaceEn=SandboxCases[i].Replace('_',' ').ToLowerInvariant();field.PlaceAr="خزانة العرض "+(i+1);
   g.gameObject.AddComponent<ForensicSurface>().Class=SurfaceClass.NonPorousSmooth;g.gameObject.layer=CrowdLayer;fields++;
  }
  if(fields<6)throw new Exception("Forensic sandbox: expected the display cases under Showroom, found "+fields);
  // ---- surfaces where powder is the wrong method
  foreach(var id in new[]{"E09","E10","E11"}){var paper=register.Clues.FirstOrDefault(c=>c&&c.Id==id);if(!paper)continue;paper.Surface=SurfaceClass.Porous;if(!paper.GetComponent<ForensicSurface>())paper.gameObject.AddComponent<ForensicSurface>().Class=SurfaceClass.Porous;}
  // ---- traces
  var clues=register.Clues.ToList();var group=register.transform;var robbed=new List<GameObject>();
  EvidenceClue Add(EvidenceClue c){clues.Add(c);return c;}
  // Touch DNA on the lower half of each pull bar, under the developable ridge marks (E13, E14).
  for(int i=0;i<2;i++){
   var mark=register.Clues.FirstOrDefault(c=>c&&c.Id=="E"+(13+i));if(!mark)continue;var print=mark.GetComponent<DustablePrint>();mark.Surface=SurfaceClass.Steel;mark.ExtraMethods=new[]{FieldInvestigationTool.Method.Tape,FieldInvestigationTool.Method.GelLifter};
   var old=mark.transform.Find("TouchDna");if(old)Object.DestroyImmediate(old.gameObject);
   var g=Group("TouchDna",mark.transform);var hit=g.gameObject.AddComponent<BoxCollider>();hit.isTrigger=true;hit.size=V(.075f,.045f,.05f);hit.center=V(0,-.055f,-.013f);
   var dna=g.gameObject.AddComponent<EvidenceClue>();dna.Id="E"+(19+i);dna.TitleEn=i==0?"Touch DNA, left pull bar":"Touch DNA, right pull bar";dna.TitleAr=i==0?"حمض نووي تلامسي، مقبض الباب الأيسر":"حمض نووي تلامسي، مقبض الباب الأيمن";
   dna.ObservationEn="Skin cells where the bar is gripped. A wet swab here wipes any latent mark: photograph and lift first, or gel-lift.";dna.ObservationAr="خلايا جلدية حيث يُمسك المقبض. المسحة الرطبة تمحو أي أثر كامن: صوّر وارفع الأثر أولًا أو استخدم رافع الجيلاتين.";
   dna.Register=register;dna.CanSample=true;dna.RequiredTool=FieldInvestigationTool.Method.Swab;dna.Surface=SurfaceClass.Steel;dna.Biological=true;g.gameObject.AddComponent<TouchDnaClue>().Mark=print;g.gameObject.layer=CrowdLayer;Add(dna);
  }
  // The two floor traces can be tested before they are swabbed.
  foreach(var id in new[]{"E01","E02"}){var stain=register.Clues.FirstOrDefault(c=>c&&c.Id==id);if(!stain)continue;stain.Biological=true;stain.ExtraMethods=new[]{FieldInvestigationTool.Method.KMTest};if(!stain.GetComponent<BloodStainClue>())stain.gameObject.AddComponent<BloodStainClue>().IsBlood=true;}
  // A paint drip by the office door: red-brown, tests negative.
  var paintGroup=Group("PaintDripStain",group);paintGroup.position=V(1.92f,.003f,8.72f);var paint=Mat(Folder,"FieldPaintDrip",new Color(.42f,.11f,.09f),.35f);var drip=new KitMesh{Lightmapped=false};
  drip.Quad(paint,Vector3.up,V(-.03f,.001f,-.04f),V(.03f,.001f,-.04f),V(.025f,.001f,.045f),V(-.02f,.001f,.045f),new Rect(0,0,1,1));drip.Quad(paint,Vector3.up,V(.02f,.001f,.045f),V(.035f,.001f,.05f),V(.03f,.001f,.08f),V(.022f,.001f,.075f),new Rect(0,0,1,1));
  var dripObject=drip.Emit("PaintDrip",paintGroup,Vector3.zero,Quaternion.Euler(0,27,0),Folder,"field_paint_drip");robbed.Add(paintGroup.gameObject);
  var paintClue=Add(Clue(group,register,"E21","Red-brown drip","قطرة بنية محمرة","A red-brown drip near the office door. Its nature is unknown until tested.","قطرة بنية محمرة قرب باب المكتب. طبيعتها غير معروفة حتى تُختبر.",paintGroup.position+Vector3.up*.012f,V(.12f,.025f,.14f),dripObject,true,FieldInvestigationTool.Method.Swab));
  paintClue.ExtraMethods=new[]{FieldInvestigationTool.Method.KMTest};paintClue.gameObject.AddComponent<BloodStainClue>().IsBlood=false;
  // The partial dust footwear mark (E08) can now be lifted, and trodden on.
  var dust=register.Clues.FirstOrDefault(c=>c&&c.Id=="E08");if(dust){dust.CanSample=true;dust.RequiredTool=FieldInvestigationTool.Method.GelLifter;dust.Surface=SurfaceClass.Dusty;dust.ObservationEn="Only part of a sole pattern remains in the glass dust; photograph it with a scale at 90 degrees before lifting. It is not an identification.";dust.ObservationAr="جزء من نقشة نعل في غبار الزجاج؛ صوّره بمقياس وبزاوية قائمة قبل رفعه. لا يكفي وحده لتحديد الهوية.";if(!dust.GetComponent<DustMarkClue>())dust.gameObject.AddComponent<DustMarkClue>();var old=root.Find("Dressing/AdditionalPhysicalEvidence/PartialDustFootwear_1");if(old&&!dust.Specimen)dust.Specimen=old.gameObject;}
  // A three-dimensional impression in a sand drift on the pavement, cast with dental stone.
  var sandGroup=Group("PavementSandDrift",group);sandGroup.position=V(-3.1f,0,-2.05f);var sand=Mat(Folder,"FieldSandDrift",new Color(.62f,.55f,.42f),.03f);var sandMesh=new KitMesh{Lightmapped=false};
  for(int i=0;i<8;i++){float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;sandMesh.Quad(sand,Vector3.up,V(0,.012f,0),V(Mathf.Cos(a)*.42f,.0f,Mathf.Sin(a)*.3f),V(Mathf.Cos(b)*.42f,0,Mathf.Sin(b)*.3f),V(Mathf.Cos(b)*.42f,0,Mathf.Sin(b)*.3f),new Rect(0,0,1,1));}
  sandMesh.Emit("SandDrift",sandGroup,Vector3.zero,Quaternion.Euler(0,15,0),Folder,"field_sand_drift");
  var impressionMat=Mat(Folder,"FieldSoleImpression",new Color(.42f,.36f,.26f,1),.02f);var impressionTexture=DemoAssetLibrary.Texture("ForensicModels","FootwearDustMark_ColorAlpha.png");
  if(impressionTexture){impressionMat.shader=Shader.Find("CrimeScene/SurfaceDecal");impressionMat.mainTexture=impressionTexture;impressionMat.renderQueue=2465;}
  var impression=new KitMesh{Lightmapped=false};impression.Quad(impressionMat,Vector3.up,V(-.065f,.013f,-.15f),V(.065f,.013f,-.15f),V(.065f,.013f,.15f),V(-.065f,.013f,.15f),new Rect(0,0,1,1));
  var impressionObject=impression.Emit("SoleImpression",sandGroup,V(.02f,0,0),Quaternion.Euler(0,-20,0),Folder,"field_sole_impression");robbed.Add(sandGroup.gameObject);
  var castVisual=DemoAssetLibrary.PlacePack("ForensicModels","ShoePrintCast",sandGroup,V(.02f,.012f,0),-20,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.None});
  if(!castVisual){var cv=new KitMesh{Lightmapped=false};cv.Bar(Mat(Folder,"FieldDentalStone",new Color(.86f,.85f,.8f),.08f),V(0,.016f,0),V(.14f,.03f,.31f),.004f);castVisual=cv.Emit("ShoePrintCastFallback",sandGroup,V(.02f,.012f,0),Quaternion.Euler(0,-20,0),Folder,"field_cast_fallback");}
  castVisual.SetActive(false);
  var impressionClue=Add(Clue(group,register,"E22","Footwear impression in sand","أثر حذاء في الرمل","A three-dimensional sole impression in the drift. Photograph it with a scale, then cast it; the cast sets before it is lifted.","أثر نعل ثلاثي الأبعاد في الرمل. صوّره بمقياس ثم اصببه؛ يتصلب القالب قبل رفعه.",sandGroup.position+V(.02f,.015f,0),V(.2f,.03f,.36f),impressionObject,true,FieldInvestigationTool.Method.CastingKit));
  impressionClue.Surface=SurfaceClass.Soil;var castMark=impressionClue.gameObject.AddComponent<CastableMark>();castMark.Type=CastableMark.Kind.Impression;castMark.SetSeconds=30;castMark.CastVisual=castVisual;
  // The safe-frame scuffs (E12) become a castable tool mark.
  var scuffs=register.Clues.FirstOrDefault(c=>c&&c.Id=="E12");
  if(scuffs){
   scuffs.CanSample=true;scuffs.RequiredTool=FieldInvestigationTool.Method.MikrosilKit;scuffs.Surface=SurfaceClass.Steel;scuffs.ObservationEn="Short bright scratches interrupt the finish: a tool mark. Photograph it with a scale, then cast it in silicone. Never offer a tool to the mark.";scuffs.ObservationAr="خدوش قصيرة لامعة تقطع الطلاء: أثر أداة. صوّره بمقياس ثم اصببه بالسيليكون. لا تقرّب أي أداة من الأثر.";
   var old=scuffs.transform.Find("ToolMarkCast");if(old)Object.DestroyImmediate(old.gameObject);
   var visual=DemoAssetLibrary.PlacePack("ForensicModels","ToolMarkCast",scuffs.transform,V(0,.005f,-.004f),0,new DemoAssetLibrary.PackOptions{Shadows=false,Lod=DemoAssetLibrary.LodClass.None});
   if(!visual){var cv=new KitMesh{Lightmapped=false};cv.Bar(Mat(Folder,"FieldSiliconeBrown",new Color(.36f,.22f,.14f),.2f),V(0,.01f,-.004f),V(.06f,.004f,.025f),.001f);visual=cv.Emit("ToolMarkCastFallback",scuffs.transform,Vector3.zero,Quaternion.identity,Folder,"field_toolmark_cast_fallback");}
   visual.name="ToolMarkCast";visual.SetActive(false);var cm=scuffs.GetComponent<CastableMark>();if(!cm)cm=scuffs.gameObject.AddComponent<CastableMark>();cm.Type=CastableMark.Kind.ToolMark;cm.SetSeconds=8;cm.CastVisual=visual;
  }
  // Pry marks on the office door frame, lock side, 1.05 m up.
  var officeDoor=root.Find("Office/OfficeDoor/Door");
  if(officeDoor){
   var frame=officeDoor.parent;var leafBounds=officeDoor.GetComponentsInChildren<MeshRenderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
   var pryGroup=Group("PryMarks",group);pryGroup.position=new Vector3(leafBounds.max.x+.01f,1.05f,leafBounds.center.z);pryGroup.rotation=Quaternion.LookRotation(Vector3.left);
   var pryMat=Mat(Folder,"FieldPryMark",new Color(.5f,.46f,.4f),.3f);var pryTexture=DemoAssetLibrary.Texture("ForensicModels","PryMark_ColorAlpha.png");if(pryTexture){pryMat.shader=Shader.Find("CrimeScene/SurfaceDecal");pryMat.mainTexture=pryTexture;pryMat.renderQueue=2465;}
   var pry=new KitMesh{Lightmapped=false};pry.Quad(pryMat,Vector3.back,V(-.03f,-.045f,-.001f),V(.03f,-.045f,-.001f),V(.03f,.045f,-.001f),V(-.03f,.045f,-.001f),new Rect(0,0,1,1));var pryObject=pry.Emit("PryMarkDecal",pryGroup,Vector3.zero,Quaternion.identity,Folder,"field_pry_mark");robbed.Add(pryGroup.gameObject);
   var pryClue=Add(Clue(group,register,"E23","Pry marks, office door frame","آثار عتلة على إطار باب المكتب","Two overlapping flat-blade impressions with paint loss beside the lock. Photograph with a scale and oblique light, then cast.","أثران متداخلان لنصل مسطح مع تقشر الطلاء بجوار القفل. صوّرهما بمقياس وإضاءة مائلة ثم اصببهما.",pryGroup.position,V(.08f,.12f,.08f),pryObject,true,FieldInvestigationTool.Method.MikrosilKit));
   pryClue.Surface=SurfaceClass.Steel;var pcm=pryClue.gameObject.AddComponent<CastableMark>();pcm.Type=CastableMark.Kind.ToolMark;pcm.SetSeconds=8;
   var pryCast=DemoAssetLibrary.PlacePack("ForensicModels","ToolMarkCast",pryClue.transform,V(0,0,-.006f),0,new DemoAssetLibrary.PackOptions{Shadows=false,Lod=DemoAssetLibrary.LodClass.None,ModelEuler=new Vector3(-90,0,0)});
   if(pryCast){pryCast.SetActive(false);pcm.CastVisual=pryCast;}
  }
  // Dropped items: photograph in place, then forceps into paper.
  var butt=Dropped(group,"CigaretteButt",V(1.1f,0,-1.45f),63,V(.025f,.008f,.008f),new Color(.9f,.86f,.78f));robbed.Add(butt);
  Add(Clue(group,register,"E24","Cigarette end","عقب سيجارة","A cigarette end on the pavement by the entrance: a saliva source. Photograph it in place, then forceps into a paper fold.","عقب سيجارة على الرصيف قرب المدخل: مصدر لعاب. صوّره في مكانه ثم التقطه بالملقط إلى طية ورقية.",butt.transform.position+Vector3.up*.01f,V(.08f,.03f,.08f),butt,true)).Biological=true;
  var lighter=Dropped(group,"Lighter",V(-.62f,0,3.42f),-30,V(.082f,.014f,.066f),new Color(.2f,.2f,.22f));robbed.Add(lighter);
  Add(Clue(group,register,"E25","Dropped lighter","قداحة ساقطة","A lighter on the showroom floor, away from any display. Photograph it, then forceps into a bag: a touch-DNA and fingerprint item.","قداحة على أرض صالة العرض بعيدًا عن الخزائن. صوّرها ثم التقطها بالملقط إلى كيس: قطعة لآثار اللمس والبصمات.",lighter.transform.position+Vector3.up*.01f,V(.11f,.03f,.1f),lighter,true));
  var spectacles=Dropped(group,"Spectacles",V(1.25f,0,4.72f),110,V(.14f,.03f,.14f),new Color(.3f,.25f,.2f));robbed.Add(spectacles);
  Add(Clue(group,register,"E26","Spectacles","نظارة","Spectacles on the floor by the central case. Their owner is not established; photograph, then forceps into a box.","نظارة على الأرض قرب الخزانة الوسطى. لم يُحدد صاحبها؛ صوّرها ثم ضعها بالملقط في صندوق.",spectacles.transform.position+Vector3.up*.015f,V(.17f,.04f,.17f),spectacles,true));
  var doorLeft=root.Find("Facade/EntranceDoors/DoorLeft");
  if(doorLeft){
   var tuft=DemoAssetLibrary.PlacePack("ForensicModels","FibreTuft",doorLeft,V(.16f,1.2f,-.03f),0,new DemoAssetLibrary.PackOptions{Shadows=false,Lod=DemoAssetLibrary.LodClass.None});
   if(!tuft){var tm=new KitMesh{Lightmapped=false};tm.Bar(Mat(Folder,"FieldRedFibre",new Color(.6f,.1f,.08f),.05f),V(0,0,0),V(.008f,.004f,.006f),.001f);tuft=tm.Emit("FibreTuftFallback",doorLeft,V(.16f,1.2f,-.03f),Quaternion.identity,Folder,"field_fibre_tuft_fallback");}
   tuft.name="FibreTuft";foreach(var t in tuft.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);robbed.Add(tuft);
   var tuftClue=Add(Clue(doorLeft,register,"E27","Fibres on the door glass edge","ألياف على حافة زجاج الباب","Red fibres snagged on a glass remnant at the breach: clothing contact. Photograph, then forceps into a paper fold.","ألياف حمراء عالقة بشظية زجاج عند موضع الاقتحام: تلامس ملابس. صوّرها ثم التقطها بالملقط إلى طية ورقية.",tuft.transform.position,V(.05f,.05f,.04f),tuft,true));
   tuftClue.Surface=SurfaceClass.Fabric;tuftClue.Biological=true;
  }
  register.Clues=clues.Where(c=>c).OrderBy(c=>c.Id).ToArray();register.RefreshPage();
  var state=Object.FindFirstObjectByType<CrimeSceneState>();if(state)state.RobbedOnly=state.RobbedOnly.Where(g=>g).Concat(robbed).Concat(clues.Where(c=>c&&int.Parse(c.Id.Substring(1))>=19).Select(c=>c.gameObject)).Distinct().ToArray();
  foreach(var r in group.GetComponentsInChildren<MeshRenderer>(true)){r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
  foreach(var t in group.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
  Debug.Log("FORENSIC_SANDBOX_AUTHORED tools="+register.Tools.Length+" clues="+register.Clues.Length+" fields="+fields+" consumables placed from pack="+placed+"/"+items.Length);
 }
 // A pack model on the floor, or a small fallback block of the same footprint.
 static GameObject Dropped(Transform parent,string model,Vector3 at,float yaw,Vector3 size,Color fallback){
  var g=DemoAssetLibrary.PlacePack("ForensicModels",model,parent,at,yaw,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.None});
  if(!g){var k=new KitMesh{Lightmapped=false};k.Bar(Mat(Folder,"FieldDropped_"+model,fallback,.2f),V(0,size.y*.5f,0),size,.001f);g=k.Emit(model+"_Fallback",parent,at,Quaternion.Euler(0,yaw,0),Folder,"field_dropped_"+model.ToLowerInvariant());}
  g.name=model;foreach(var t in g.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=CrowdLayer;GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);}return g;
 }

 // ------------------------------------------------------------------ checks
 static void ForensicSandboxChecks(EvidenceRegister register){
  void Require(bool value,string message){if(!value)throw new Exception("Forensic sandbox: "+message);}
  bool persist=register.PersistRecords;register.PersistRecords=false;var kit=register.Kit;
  foreach(FieldInvestigationTool.Method kind in Enum.GetValues(typeof(FieldInvestigationTool.Method)))Require(register.Tools.Count(t=>t&&t.Kind==kind)==1,"one tool of each kind required, missing "+kind);
  Require(register.Tools.All(t=>t.Dock&&t.Dock.IsChildOf(kit.transform)&&t.GetComponentsInChildren<Collider>().All(c=>c.isTrigger)),"every tool docks to the case and never collides with the player");
  var fields=root.GetComponentsInChildren<LatentMarkField>(true);Require(fields.Length>=6,"latent-mark fields missing from the display cases");
  foreach(var f in fields)f.Build();int marks=fields.Sum(f=>f.Marks.Count);Require(marks>=4&&fields.Any(f=>f.Marks.Any(m=>!m.Smudge)),"the seeded fields hold too few marks ("+marks+")");
  Require(register.RuntimeClues.Count==marks,"field marks are not registered as runtime clues");
  var a=new GameObject("Temporary forensic hand");var hand=a.AddComponent<HandInteractor>();Physics.SyncTransforms();
  try{
   FieldInvestigationTool Tool(FieldInvestigationTool.Method kind)=>register.Tools.First(t=>t.Kind==kind);
   void Take(FieldInvestigationTool tool){hand.ReleaseAll();a.transform.position=tool.transform.position+Vector3.up*.05f;Require(hand.TakeFieldTool(tool),"cannot take "+tool.Kind);}
   // -- sweep a case top: the mark under the contact develops, then over-brushing is recorded
   var mark=fields.SelectMany(f=>f.Marks).First(m=>!m.Smudge);var brush=Tool(FieldInvestigationTool.Method.Brush);Take(brush);
   a.transform.position=mark.transform.position+Vector3.up*.16f;a.transform.LookAt(mark.transform.position);brush.Follow(a.transform);Physics.SyncTransforms();
   Require(Physics.Raycast(new Ray(mark.transform.position+Vector3.up*.3f,Vector3.down),out var top,1,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)&&top.collider.GetComponentInParent<LatentMarkField>(),"case top is not hit by a ray");
   Require(brush.UseAt(null,top,true),"brush cannot start sweeping a case top");var ray=new Ray(a.transform.position,a.transform.forward);
   for(int i=0;i<120&&!mark.Developed;i++)brush.BrushStroke(ray,.05f,true);Require(mark.Developed&&mark.Grade==4&&mark.Clue.Observed,"sweeping the case does not develop the mark under the brush");
   for(int i=0;i<200&&mark.Grade>2;i++)brush.BrushStroke(ray,.05f,true);Require(mark.Grade<=2&&mark.Clue.Log.Has("Over-brushed"),"over-brushing is not recorded");
   // -- paper: powder is the wrong method
   var receipt=register.Clues.First(c=>c.Id=="E11");a.transform.position=receipt.transform.position+Vector3.up*.2f;a.transform.LookAt(receipt.transform.position);brush.Follow(a.transform);Physics.SyncTransforms();
   Require(Physics.Raycast(new Ray(a.transform.position,a.transform.forward),out var paperHit,1,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)&&ForensicSurface.ClassOf(paperHit.collider)==SurfaceClass.Porous,"staff note is not a porous surface");
   hand.ReleaseAll();
   // -- touch DNA before powder destroys the handle mark; after a photographed lift it is acceptable
   var handle=register.Clues.First(c=>c.Id=="E13");var print=handle.GetComponent<DustablePrint>();var dna=register.Clues.First(c=>c.Id=="E19");var swab=Tool(FieldInvestigationTool.Method.Swab);
   Take(swab);a.transform.position=dna.transform.position-dna.transform.forward*.12f;Require(dna.Sample(swab)&&print.Destroyed&&dna.Log.Has("Swabbed before powder"),"swab over an undeveloped mark should take the DNA and wipe the mark");
   register.ResetInvestigation();Require(!print.Destroyed&&dna.Log.Steps.Count==0&&!dna.Sampled,"reset does not restore the wiped mark and clear the log");
   print.Brush(1);Require(print.Developed,"direct development failed");
   int photographed=register.LogPhotograph(handle.transform.position-handle.transform.forward*.4f,Quaternion.LookRotation(handle.transform.forward));Require(photographed>=1&&handle.Log.Has("Photographed"),"photo log does not reach the mark in frame");
   var tape=Tool(FieldInvestigationTool.Method.Tape);Take(tape);a.transform.position=handle.transform.position-handle.transform.forward*.12f;Require(handle.Sample(tape)&&handle.Sampled&&handle.CollectedWith==FieldInvestigationTool.Method.Tape&&handle.Log.Last("Collected").flag==StepFlag.Correct,"tape lift of a photographed mark is not recorded as correct");
   Take(swab);a.transform.position=dna.transform.position-dna.transform.forward*.12f;Require(dna.Sample(swab)&&dna.Log.Has("Swabbed after lift"),"swab after the lift not recorded");
   var entry=register.Entries.Last();Require(entry.method=="swab"&&entry.container.Contains("swab box")&&entry.procedure.Contains("Swabbed after lift"),"register entry lacks method, container or procedure");
   string json=JsonUtility.ToJson(new EvidenceRegister.RecordFile{session="verification",entries=register.Entries.ToList()});Require(JsonUtility.FromJson<EvidenceRegister.RecordFile>(json).entries.Last().procedure==entry.procedure,"procedure does not round-trip through JSON");
   // -- lifting without a photograph is out of sequence
   register.ResetInvestigation();print.Brush(1);Take(tape);a.transform.position=handle.transform.position-handle.transform.forward*.12f;Require(handle.Sample(tape)&&handle.Log.Last("Collected").flag==StepFlag.OutOfSequence,"lift before photograph should be out of sequence");
   // -- presumptive blood test: positive on blood, negative on paint, invalid when the reagent stands too long
   register.ResetInvestigation();var blood=register.Clues.First(c=>c.Id=="E01");var bloodTest=blood.GetComponent<BloodStainClue>();var paintClue=register.Clues.First(c=>c.Id=="E21");var paintTest=paintClue.GetComponent<BloodStainClue>();
   for(int i=0;i<4;i++)Require(bloodTest.Advance(blood,10+i),"test stage "+i+" refused");Require(bloodTest.Result==BloodStainClue.Outcome.Positive&&blood.Observed&&blood.Log.Has("KM test positive"),"blood does not test positive");
   for(int i=0;i<4;i++)paintTest.Advance(paintClue,10+i);Require(paintTest.Result==BloodStainClue.Outcome.Negative,"paint does not test negative");
   paintTest.ResetTest();paintTest.Advance(paintClue,0);paintTest.Advance(paintClue,1);paintTest.Advance(paintClue,2);paintTest.Advance(paintClue,50);Require(paintTest.Result==BloodStainClue.Outcome.Invalid&&paintClue.Log.Has("KM test invalid"),"stale reagent should invalidate the test");
   var kmTool=Tool(FieldInvestigationTool.Method.KMTest);Take(kmTool);a.transform.position=blood.transform.position+Vector3.up*.3f;Require(!kmTool.Use(register.Clues.First(c=>c.Id=="E04")),"test kit accepted a metal clasp");
   Take(swab);a.transform.position=blood.transform.position+Vector3.up*.3f;Require(blood.Sample(swab)&&blood.Sampled,"tested blood cannot be swabbed");
   // -- casting: pour, set, lift
   var sandClue=register.Clues.First(c=>c.Id=="E22");var cast=sandClue.GetComponent<CastableMark>();var castKit=Tool(FieldInvestigationTool.Method.CastingKit);Take(castKit);a.transform.position=sandClue.transform.position+Vector3.up*.3f;
   Require(castKit.Use(sandClue)&&cast.State==CastableMark.Stage.Setting&&cast.CastVisual.activeSelf&&!sandClue.Sampled,"casting kit does not pour");Require(castKit.Use(sandClue)&&!sandClue.Sampled,"cast lifted before it set");
   cast.ForceSet();Require(castKit.Use(sandClue)&&sandClue.Sampled&&cast.State==CastableMark.Stage.Lifted&&!cast.CastVisual.activeSelf&&sandClue.CollectedWith==FieldInvestigationTool.Method.CastingKit,"set cast cannot be lifted and recorded");
   var scuffs=register.Clues.First(c=>c.Id=="E12");var mikrosil=Tool(FieldInvestigationTool.Method.MikrosilKit);Take(mikrosil);a.transform.position=scuffs.transform.position-scuffs.transform.forward*.2f;
   Require(mikrosil.Use(scuffs)&&scuffs.GetComponent<CastableMark>().State==CastableMark.Stage.Setting,"silicone kit does not apply to the tool mark");Take(castKit);Require(!castKit.Use(scuffs),"dental stone accepted on a tool mark");
   // -- dust mark discipline
   var dustClue=register.Clues.First(c=>c.Id=="E08");var dustMark=dustClue.GetComponent<DustMarkClue>();Require(dustMark.Check(dustClue.transform.position+Vector3.right*.1f)&&dustClue.Log.Has("Mark trodden on"),"walking over the dust mark is not recorded");
   register.LogPhotograph(dustClue.transform.position+Vector3.up*.8f,Quaternion.LookRotation(Vector3.down));dustMark.ResetMark();Require(!dustMark.Check(dustClue.transform.position),"photographed mark still counts as trodden");
   var gelTool=Tool(FieldInvestigationTool.Method.GelLifter);Take(gelTool);a.transform.position=dustClue.transform.position+Vector3.up*.3f;Require(dustClue.Sample(gelTool)&&dustClue.CollectedWith==FieldInvestigationTool.Method.GelLifter,"gel lifter cannot lift the dust mark");
   // -- dropped item: forceps without a photograph is out of sequence, wrong method is recorded
   var butt=register.Clues.First(c=>c.Id=="E24");var forceps=Tool(FieldInvestigationTool.Method.Forceps);Take(swab);a.transform.position=butt.transform.position+Vector3.up*.3f;Require(!butt.Sample(swab)&&butt.Log.Has("Wrong method offered"),"wrong method on the cigarette end not recorded");
   Take(forceps);Require(butt.Sample(forceps)&&butt.Log.Last("Collected").flag==StepFlag.OutOfSequence&&register.Entries.Last().container.Contains("paper"),"cigarette end not bagged in paper or sequence not recorded");
   register.ResetInvestigation();Require(register.AllClues.All(c=>!c.Sampled&&c.Log.Steps.Count==0)&&cast.State==CastableMark.Stage.Open&&bloodTest.Result==BloodStainClue.Outcome.Untested,"reset leaves sandbox state behind");
   Debug.Log("FORENSIC_SANDBOX_PASS ten tools; "+fields.Length+" latent fields with "+marks+" marks; sweep develops and over-brushes; porous paper; DNA-before-powder wipes the mark; photograph-then-lift correct; lift-first out of sequence; KM positive/negative/invalid; dental-stone and silicone casting; dust-mark discipline; forceps and paper packaging; JSON; reset");
  }finally{hand.ReleaseAll();Object.DestroyImmediate(a);foreach(var f in fields)f.ResetField();register.ResetInvestigation();register.PersistRecords=persist;Physics.SyncTransforms();}
 }
}
#endif
