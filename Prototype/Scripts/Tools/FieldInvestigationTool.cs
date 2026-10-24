using UnityEngine;

// Reusable field-kit tools have their own rack rather than altering the six
// established placement-tool slots or controller wheel. No dynamic physics.
// Kinds (append only, saved scenes store the value): forceps, sterile swab, powder brush, hinge
// lifter (the original "lift card"), magnetic wand, lifting tape with backing card, gel lifter,
// presumptive blood test kit, dental-stone casting kit, silicone tool-mark casting kit.
public sealed class FieldInvestigationTool : MonoBehaviour {
 public enum Method { Forceps, Swab, Brush, LiftCard, MagneticWand, Tape, GelLifter, KMTest, CastingKit, MikrosilKit }
 public Method Kind;
 public EvidenceRegister Register;
 public Transform Tip;
 public Transform Dock;
 public Vector3 HandPosition=new Vector3(0,-.045f,.005f);
 public Vector3 HandEuler=new Vector3(0,0,0);
 public ToolHolder Holder {get;private set;}
 Vector3 home;Quaternion rotation;Transform homeParent;bool recorded;DemoLighting lighting;DustablePrint dusting;bool sweeping;Vector3 previousContact;float porousWarned;
 static readonly Collider[] contacts=new Collider[24];
 public string ToolName{get{switch(Kind){
  case Method.Forceps:return "Evidence forceps";case Method.Swab:return "Sterile swab";case Method.Brush:return "Fingerprint brush";case Method.LiftCard:return "Hinge lifter";
  case Method.MagneticWand:return "Magnetic powder wand";case Method.Tape:return "Lifting tape and backing card";case Method.GelLifter:return "Gelatin lifter";
  case Method.KMTest:return "Presumptive blood test kit";case Method.CastingKit:return "Dental-stone casting kit";default:return "Silicone casting kit";}}}
 public bool IsApplicator=>Kind==Method.Brush||Kind==Method.MagneticWand;
 // What the tool works on and how (Docs/Research/FORENSIC_MECHANICS_SPEC.md §3), shown on a card
 // over the tool on request (B/Y on the holding controller, Tab on the desktop; Koda, October 24: no
 // hint unless asked); ForensicGlow marks the compatible targets faintly while it is held.
 public static string WorksOn(Method kind){switch(kind){
  case Method.Forceps:return DemoLanguage.Arabic?"الأشياء الصغيرة والألياف والشعر":"loose items, fibres, hairs, cigarette ends";
  case Method.Swab:return DemoLanguage.Arabic?"المقابض والبقع والأشياء الملموسة":"handles, stains, touched items";
  case Method.Brush:return DemoLanguage.Arabic?"الزجاج والمعدن والأسطح الملساء":"glass, metal and painted surfaces";
  case Method.MagneticWand:return DemoLanguage.Arabic?"البلاستيك والأسطح الخشنة (ليس الفولاذ)":"textured plastic, not steel";
  case Method.LiftCard:case Method.Tape:return DemoLanguage.Arabic?"الأثر المطور بالمسحوق":"a developed powder mark";
  case Method.GelLifter:return DemoLanguage.Arabic?"الأثر المطور وآثار الأحذية في الغبار":"developed marks and dust footwear marks";
  case Method.KMTest:return DemoLanguage.Arabic?"البقع البنية المحمرة":"red-brown stains";
  case Method.CastingKit:return DemoLanguage.Arabic?"آثار الأحذية في الرمل أو التراب":"footwear impressions in sand or soil";
  default:return DemoLanguage.Arabic?"آثار الأدوات على المعدن والطلاء":"tool marks on metal and paint";}}
 public static string HowTo(Method kind){switch(kind){
  case Method.Forceps:return DemoLanguage.Arabic?"صوّر أولًا ثم اضغط الزناد على الأثر":"photograph first, then trigger on the item";
  case Method.Swab:return DemoLanguage.Arabic?"صوّر وارفع الأثر أولًا، ثم اضغط الزناد على السطح":"photograph and lift any mark first, then trigger on the surface";
  case Method.Brush:return DemoLanguage.Arabic?"اضغط الزناد باستمرار ولامس السطح بالشعيرات وامسح بخفة؛ توقف عند ظهور الخطوط":"hold the trigger with the bristles on the surface and sweep lightly; stop when ridges show";
  case Method.MagneticWand:return DemoLanguage.Arabic?"اضغط الزناد باستمرار وحرّك العصا فوق السطح بقدر إصبع دون لمسه، بحركة دائرية بطيئة":"hold the trigger and hover the wand a finger's width above the surface, circling slowly; it never touches";
  case Method.LiftCard:case Method.Tape:case Method.GelLifter:return DemoLanguage.Arabic?"صوّر الأثر بمقياس ثم اضغط الزناد عليه":"photograph the mark with a scale, then trigger on it";
  case Method.KMTest:return DemoLanguage.Arabic?"أربع ضغطات على البقعة: مسح، إيثانول، كاشف، بيروكسيد":"four presses on the stain: rub, ethanol, reagent, peroxide";
  case Method.CastingKit:return DemoLanguage.Arabic?"صوّر بمقياس، اضغط لصب القالب، انتظر، ثم اضغط لرفعه":"photograph with a scale, trigger to pour, wait, trigger to lift";
  default:return DemoLanguage.Arabic?"صوّر بمقياس، اضغط لفرد السيليكون، انتظر، ثم اضغط لرفعه":"photograph with a scale, trigger to spread, wait, trigger to peel";}}
 public string Hint=>WorksOn(Kind)+". "+HowTo(Kind);
 // Raised when a tool is taken or put back, so the glow on compatible targets can follow the hand.
 public static event System.Action<FieldInvestigationTool,bool> HeldChanged;
 public void RecordHome(){if(recorded)return;recorded=true;home=transform.position;rotation=transform.rotation;homeParent=transform.parent;}
 void Awake(){RecordHome();foreach(var c in GetComponentsInChildren<Collider>())c.isTrigger=true;}
 public bool Take(ToolHolder holder,Transform anchor){
  if(Holder||!holder||!anchor)return false;RecordHome();Holder=holder;transform.SetParent(Register?Register.transform:null,true);lighting=FindFirstObjectByType<DemoLighting>();Follow(anchor);
  ForensicFeedback.Post(DemoLanguage.Arabic?DemoLanguage.Text(ToolName+" في اليد"):ToolName+" in hand");HeldChanged?.Invoke(this,true);return true;
 }
 // A small card above the held tool (headset UI layer, faces the eyes): name, what it works on, how.
 // Shown only on request: B/Y on the holding controller or Tab on the desktop toggles it.
 Transform hintCard;TextMesh hintText;
 public bool HintShown=>hintCard;
 public void ToggleHint(){if(hintCard)HideHint();else ShowHint();}
 public void ShowHint(){
  if(!Application.isPlaying)return;HideHint();
  hintCard=new GameObject("ToolHint").transform;hintCard.SetParent(transform,false);hintCard.localPosition=new Vector3(0,.13f,.04f);
  string text=ToolName+"\n"+Wrap((DemoLanguage.Arabic?"يعمل على: ":"Works on: ")+WorksOn(Kind),38)+"\n"+Wrap((DemoLanguage.Arabic?"الطريقة: ":"How: ")+HowTo(Kind),38);
  int lines=text.Split('\n').Length;float height=.011f*lines+.012f;
  DemoUi.WorldPanel("Back",hintCard,new Vector3(0,0,.002f),.22f,height,.008f,DemoUi.WithAlpha(DemoUi.Card,.85f));
  hintText=DemoUi.WorldText("Text",hintCard,new Vector3(0,0,-.0008f),DemoLanguage.Arabic?DemoLanguage.Text(text):text,.0095f,DemoUi.Ink,TextAnchor.MiddleCenter,DemoLanguage.Arabic?DemoLanguage.Font:null);
  DemoUi.ToHeadsetUiLayer(hintCard);
 }
 public void HideHint(){if(hintCard){Destroy(hintCard.gameObject);hintCard=null;hintText=null;}}
 static string Wrap(string value,int width){var result="";int column=0;foreach(string word in value.Split(' ')){if(column+word.Length>width){result+="\n";column=0;}if(column>0){result+=" ";column++;}result+=word;column+=word.Length;}return result;}
 void FaceHint(){if(!hintCard)return;var cam=Camera.main;if(cam)DemoUi.FaceViewer(hintCard,cam.transform.position);}
 public void Follow(Transform anchor){if(!anchor)return;var offset=Holder is DesktopInteractor?new Vector3(.18f,-.14f,.38f):HandPosition;transform.SetPositionAndRotation(anchor.TransformPoint(offset),anchor.rotation*Quaternion.Euler(HandEuler));}
 void LateUpdate(){
  if(!Holder)return;
  if(Holder is HandInteractor&&lighting&&((lighting.TorchEquipped(false)&&lighting.FlashlightLight.transform.parent==Holder.transform)||(lighting.TorchEquipped(true)&&lighting.UVTorchLight.transform.parent==Holder.transform))){Holder.ReturnFieldTool();return;}
  Follow(Holder is HandInteractor?Holder.transform:((DesktopInteractor)Holder).View.transform);FaceHint();
  if(IsApplicator)ContinueDusting();
 }
 public void ReturnToRack(){if(!recorded)RecordHome();bool held=Holder;Holder=null;dusting=null;sweeping=false;HideHint();transform.SetParent(Dock?Dock.parent:homeParent,true);transform.SetPositionAndRotation(Dock?Dock.position:home,Dock?Dock.rotation:rotation);if(held)HeldChanged?.Invoke(this,false);}
 public bool Use(EvidenceClue clue)=>UseAt(clue,default,false);
 // The trigger on whatever the ray found: a clue, or (for the powder applicators) any surface.
 public bool UseAt(EvidenceClue clue,RaycastHit hit,bool hasHit){
  if(IsApplicator){
   // The surface under the working tip counts before the controller ray: the brush is used by
   // touch, the wand by hovering, and neither needs the hand aimed at the glass.
   Collider at=hasHit?hit.collider:null;Vector3 point=hasHit?hit.point:Vector3.zero;
   if(Holder is HandInteractor&&TipContact(out var tipCollider,out var tipPoint)){at=tipCollider;point=tipPoint;}
   dusting=clue?clue.GetComponent<DustablePrint>():null;
   if(!dusting&&at)dusting=LatentMarkField.MarkAt(at,point,.05f);
   if(dusting){if(!CanBrush(dusting)){dusting=null;ForensicFeedback.Post(Kind==Method.Brush?"Bring the bristles onto the surface":"Bring the wand to a finger's width above the surface",FeedbackTone.Warning);return false;}sweeping=false;previousContact=dusting.transform.position;ForensicFeedback.Post(StrokeHint());return true;}
   if(at&&(at.GetComponentInParent<LatentMarkField>()||at.GetComponentInParent<ForensicSurface>())){
    var surface=ForensicSurface.ClassOf(at);sweeping=true;previousContact=point;
    if(ForensicSurface.Porous(surface)){ForensicFeedback.Post("Powder is not the method for paper or fabric: photograph it and bag it for the laboratory (ninhydrin, DFO)",FeedbackTone.Warning);porousWarned=Time.time;}
    else ForensicFeedback.Post(StrokeHint());
    return true;
   }
   ForensicFeedback.Post(Kind==Method.Brush?"Touch the bristles to a glass, metal or painted surface":"Hover the wand just above a textured or plastic surface",FeedbackTone.Warning);return false;
  }
  if(!clue){ForensicFeedback.Post(Kind==Method.KMTest?"Hold the test over a suspected stain":Kind==Method.CastingKit||Kind==Method.MikrosilKit?"Aim the kit at a mark to cast":"Aim at a trace to sample",FeedbackTone.Warning);return false;}
  if(Kind==Method.KMTest){var stain=clue.GetComponent<BloodStainClue>();if(!stain){ForensicFeedback.Post("The presumptive test is for suspected blood stains",FeedbackTone.Warning);return false;}if(!clue.Available&&!clue.Observed){ForensicFeedback.Post("Nothing to test here yet",FeedbackTone.Warning);return false;}return stain.Advance(clue);}
  if(Kind==Method.CastingKit||Kind==Method.MikrosilKit){var cast=clue.GetComponent<CastableMark>();if(!cast||cast.Kit!=Kind){ForensicFeedback.Post(Kind==Method.CastingKit?"Dental stone casts three-dimensional impressions in soil or sand":"Silicone casts tool marks on hard surfaces",FeedbackTone.Warning);return false;}}
  return clue.Sample(this);
 }
 string StrokeHint()=>Kind==Method.Brush?"Hold the trigger, keep the bristles on the surface and sweep lightly; stop when ridges show":"Hold the trigger and circle the wand slowly just above the surface; stop when ridges show";
 // How near the working tip must be: the brush touches (5 cm allows for tracking), the wand hovers (9 cm).
 public float ContactReach=>Kind==Method.MagneticWand?.09f:.05f;
 bool CanBrush(DustablePrint print)=>Holder&&print&&Vector3.Distance(Tip.position,print.transform.position)<(Holder is HandInteractor ? ContactReach+.1f : 2);
 // The nearest brushable surface within ContactReach of the working tip (box, sphere, capsule or
 // convex colliders; the case tops, handle marks and paper are boxes), with the point of contact.
 public bool TipContact(out Collider at,out Vector3 point){
  at=null;point=Vector3.zero;if(!Tip)return false;float best=ContactReach;
  int n=Physics.OverlapSphereNonAlloc(Tip.position,ContactReach,contacts,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);
  for(int i=0;i<n;i++){
   var c=contacts[i];if(!c||c.transform.IsChildOf(transform)||(c is MeshCollider mesh&&!mesh.convex))continue;
   if(!c.GetComponentInParent<LatentMarkField>()&&!c.GetComponentInParent<ForensicSurface>()&&!c.GetComponentInParent<DustablePrint>())continue;
   var p=c.ClosestPoint(Tip.position);float d=Vector3.Distance(p,Tip.position);if(d<best&&!Occluded(c,p)){best=d;at=c;point=p;}
  }
  return at;
 }
 // Solid geometry between the hand and the contact blocks it; the surface's own object (the door
 // leaf under a handle mark, the showcase under a field) never counts as an obstruction.
 bool Occluded(Collider target,Vector3 point){
  if(!Holder)return false;Vector3 from=Holder.transform.position;Vector3 to=point-from;float length=to.magnitude;if(length<.01f)return false;
  var field=target.GetComponentInParent<LatentMarkField>();var print=target.GetComponentInParent<DustablePrint>();
  Transform owner=field?field.transform.parent:print?print.transform.parent:target.transform;
  var hits=Physics.RaycastAll(from,to/length,length-.005f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
  foreach(var h in hits){if(h.collider.transform.IsChildOf(transform)||h.collider.transform.IsChildOf(Holder.transform)||(owner&&h.collider.transform.IsChildOf(owner)))continue;return true;}
  return false;
 }
 // Where the stroke lands: the tip's own contact for tracked hands, else the pointer ray.
 bool Contact(Ray ray,out Collider at,out Vector3 point){
  if(Holder is HandInteractor){if(TipContact(out at,out point))return true;if(InvestigationInteraction.Target(ray,.65f,Holder,out var near)&&Vector3.Distance(near.point,Tip.position)<ContactReach+.06f){at=near.collider;point=near.point;return true;}return false;}
  if(InvestigationInteraction.Target(ray,2,Holder,out var hit)){at=hit.collider;point=hit.point;return true;}at=null;point=Vector3.zero;return false;
 }
 // Development per stroke: the brush works by motion (little happens standing still), the
 // magnetic wand by dwell as the powder is drawn across the surface, with motion helping less.
 float Development(float sweep,float seconds)=>Kind==Method.MagneticWand?sweep*2f+Mathf.Clamp(seconds,0,.05f)*.9f:sweep*3.5f+Mathf.Clamp(seconds,0,.05f)*.25f;
 void ContinueDusting(){
  if(!dusting&&!sweeping)return;if(dusting&&!CanBrush(dusting)&&!sweeping)return;bool pressed=false;Ray ray;
  if(Holder is HandInteractor hand){var input=hand.GetComponent<XRControllerInput>();pressed=input&&input.TriggerHeld;ray=new Ray(hand.transform.position,hand.transform.forward);}
  else{var desktop=(DesktopInteractor)Holder;pressed=desktop.ReadDesktopInput&&(!desktop.Panel||!desktop.Panel.CapturesInput)&&(Input.GetMouseButton(0)||Input.GetKey(KeyCode.G));ray=desktop.PointerRay();}
  BrushStroke(ray,Time.deltaTime,pressed);
 }
 // Shared by tracked and desktop input, and exercised against scene colliders by validation.
 // A stroke develops the mark under the contact: the designated mark the trigger started on, or
 // whichever field mark lies within 3.5 cm of the contact point while sweeping a case top.
 public bool BrushStroke(Ray ray,float seconds,bool pressed){
  if(!pressed||!IsApplicator||(!dusting&&!sweeping)||!Contact(ray,out var at,out var point))return false;
  DustablePrint target=null;
  if(dusting&&!sweeping){if(at.GetComponentInParent<DustablePrint>()!=dusting||!CanBrush(dusting))return false;target=dusting;}
  else{
   if(!at.GetComponentInParent<LatentMarkField>()&&!at.GetComponentInParent<ForensicSurface>()&&!at.GetComponentInParent<DustablePrint>())return false;
   var surface=ForensicSurface.ClassOf(at);
   if(ForensicSurface.Porous(surface)){if(Time.time-porousWarned>4){porousWarned=Time.time;ForensicFeedback.Post("Powder on paper: the mark is being lost. Paper goes to the laboratory",FeedbackTone.Warning);}previousContact=point;return false;}
   target=LatentMarkField.MarkAt(at,point,Kind==Method.MagneticWand?.045f:.035f);
   if(!target){previousContact=point;return true;} // powder on bare glass: nothing shows
  }
  float sweep=Mathf.Min(.03f,Vector3.Distance(previousContact,point));previousContact=point;
  target.Brush(Development(sweep,seconds),Kind);return true;
 }
}
