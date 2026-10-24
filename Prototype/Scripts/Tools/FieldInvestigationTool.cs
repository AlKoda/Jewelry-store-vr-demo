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
 public string ToolName{get{switch(Kind){
  case Method.Forceps:return "Evidence forceps";case Method.Swab:return "Sterile swab";case Method.Brush:return "Fingerprint brush";case Method.LiftCard:return "Hinge lifter";
  case Method.MagneticWand:return "Magnetic powder wand";case Method.Tape:return "Lifting tape and backing card";case Method.GelLifter:return "Gelatin lifter";
  case Method.KMTest:return "Presumptive blood test kit";case Method.CastingKit:return "Dental-stone casting kit";default:return "Silicone casting kit";}}}
 public bool IsApplicator=>Kind==Method.Brush||Kind==Method.MagneticWand;
 // What the tool works on and how (Docs/Research/FORENSIC_MECHANICS_SPEC.md §3), shown when it is
 // taken and printed on the case lid; ForensicGlow lights the compatible targets while it is held.
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
  case Method.Brush:case Method.MagneticWand:return DemoLanguage.Arabic?"اضغط الزناد باستمرار وامسح بخفة؛ توقف عند ظهور الخطوط":"hold the trigger, sweep lightly, stop when ridges show";
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
  ForensicFeedback.Post((DemoLanguage.Arabic?"":ToolName+": ")+Hint);HeldChanged?.Invoke(this,true);return true;
 }
 public void Follow(Transform anchor){if(!anchor)return;var offset=Holder is DesktopInteractor?new Vector3(.18f,-.14f,.38f):HandPosition;transform.SetPositionAndRotation(anchor.TransformPoint(offset),anchor.rotation*Quaternion.Euler(HandEuler));}
 void LateUpdate(){
  if(!Holder)return;
  if(Holder is HandInteractor&&lighting&&((lighting.TorchEquipped(false)&&lighting.FlashlightLight.transform.parent==Holder.transform)||(lighting.TorchEquipped(true)&&lighting.UVTorchLight.transform.parent==Holder.transform))){Holder.ReturnFieldTool();return;}
  Follow(Holder is HandInteractor?Holder.transform:((DesktopInteractor)Holder).View.transform);
  if(IsApplicator)ContinueDusting();
 }
 public void ReturnToRack(){if(!recorded)RecordHome();bool held=Holder;Holder=null;dusting=null;sweeping=false;transform.SetParent(Dock?Dock.parent:homeParent,true);transform.SetPositionAndRotation(Dock?Dock.position:home,Dock?Dock.rotation:rotation);if(held)HeldChanged?.Invoke(this,false);}
 public bool Use(EvidenceClue clue)=>UseAt(clue,default,false);
 // The trigger on whatever the ray found: a clue, or (for the powder applicators) any surface.
 public bool UseAt(EvidenceClue clue,RaycastHit hit,bool hasHit){
  if(IsApplicator){
   dusting=clue?clue.GetComponent<DustablePrint>():null;
   if(!dusting&&hasHit)dusting=LatentMarkField.MarkAt(hit,.05f);
   if(dusting){if(!CanBrush(dusting)){dusting=null;ForensicFeedback.Post("Bring the "+(Kind==Method.Brush?"brush":"wand")+" closer to the surface",FeedbackTone.Warning);return false;}sweeping=false;previousContact=dusting.transform.position;ForensicFeedback.Post("Hold the trigger and sweep lightly over the surface; stop when ridges show");return true;}
   if(hasHit&&(hit.collider.GetComponentInParent<LatentMarkField>()||hit.collider.GetComponentInParent<ForensicSurface>())){
    var surface=ForensicSurface.ClassOf(hit.collider);sweeping=true;previousContact=hit.point;
    if(ForensicSurface.Porous(surface)){ForensicFeedback.Post("Powder is not the method for paper or fabric: photograph it and bag it for the laboratory (ninhydrin, DFO)",FeedbackTone.Warning);porousWarned=Time.time;}
    else ForensicFeedback.Post("Hold the trigger and sweep lightly over the surface; stop when ridges show");
    return true;
   }
   ForensicFeedback.Post(Kind==Method.Brush?"Sweep the brush over a glass, metal or painted surface":"Sweep the wand over a textured or plastic surface",FeedbackTone.Warning);return false;
  }
  if(!clue){ForensicFeedback.Post(Kind==Method.KMTest?"Hold the test over a suspected stain":Kind==Method.CastingKit||Kind==Method.MikrosilKit?"Aim the kit at a mark to cast":"Aim at a trace to sample",FeedbackTone.Warning);return false;}
  if(Kind==Method.KMTest){var stain=clue.GetComponent<BloodStainClue>();if(!stain){ForensicFeedback.Post("The presumptive test is for suspected blood stains",FeedbackTone.Warning);return false;}if(!clue.Available&&!clue.Observed){ForensicFeedback.Post("Nothing to test here yet",FeedbackTone.Warning);return false;}return stain.Advance(clue);}
  if(Kind==Method.CastingKit||Kind==Method.MikrosilKit){var cast=clue.GetComponent<CastableMark>();if(!cast||cast.Kit!=Kind){ForensicFeedback.Post(Kind==Method.CastingKit?"Dental stone casts three-dimensional impressions in soil or sand":"Silicone casts tool marks on hard surfaces",FeedbackTone.Warning);return false;}}
  return clue.Sample(this);
 }
 bool CanBrush(DustablePrint print)=>Holder&&print&&Vector3.Distance(Tip.position,print.transform.position)<(Holder is HandInteractor ? .28f : 2);
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
  if(!pressed||!IsApplicator||(!dusting&&!sweeping)||!InvestigationInteraction.Target(ray,Holder is HandInteractor ? .65f : 2,Holder,out var hit))return false;
  DustablePrint target=null;
  if(dusting&&!sweeping){if(hit.collider.GetComponentInParent<DustablePrint>()!=dusting||!CanBrush(dusting))return false;target=dusting;}
  else{
   if(!hit.collider.GetComponentInParent<LatentMarkField>()&&!hit.collider.GetComponentInParent<ForensicSurface>()&&!hit.collider.GetComponentInParent<DustablePrint>())return false;
   var surface=ForensicSurface.ClassOf(hit.collider);
   if(ForensicSurface.Porous(surface)){if(Time.time-porousWarned>4){porousWarned=Time.time;ForensicFeedback.Post("Powder on paper: the mark is being lost. Paper goes to the laboratory",FeedbackTone.Warning);}previousContact=hit.point;return false;}
   target=LatentMarkField.MarkAt(hit,.035f);
   if(!target){previousContact=hit.point;return true;} // powder on bare glass: nothing shows
  }
  float sweep=Mathf.Min(.03f,Vector3.Distance(previousContact,hit.point));previousContact=hit.point;
  target.Brush(sweep*3.5f+Mathf.Clamp(seconds,0,.05f)*.34f,Kind);return true;
 }
}
