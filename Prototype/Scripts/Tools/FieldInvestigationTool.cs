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
 public void RecordHome(){if(recorded)return;recorded=true;home=transform.position;rotation=transform.rotation;homeParent=transform.parent;}
 void Awake(){RecordHome();foreach(var c in GetComponentsInChildren<Collider>())c.isTrigger=true;}
 public bool Take(ToolHolder holder,Transform anchor){
  if(Holder||!holder||!anchor)return false;RecordHome();Holder=holder;transform.SetParent(Register?Register.transform:null,true);lighting=FindFirstObjectByType<DemoLighting>();Follow(anchor);return true;
 }
 public void Follow(Transform anchor){if(!anchor)return;var offset=Holder is DesktopInteractor?new Vector3(.18f,-.14f,.38f):HandPosition;transform.SetPositionAndRotation(anchor.TransformPoint(offset),anchor.rotation*Quaternion.Euler(HandEuler));}
 void LateUpdate(){
  if(!Holder)return;
  if(Holder is HandInteractor&&lighting&&((lighting.TorchEquipped(false)&&lighting.FlashlightLight.transform.parent==Holder.transform)||(lighting.TorchEquipped(true)&&lighting.UVTorchLight.transform.parent==Holder.transform))){Holder.ReturnFieldTool();return;}
  Follow(Holder is HandInteractor?Holder.transform:((DesktopInteractor)Holder).View.transform);
  if(IsApplicator)ContinueDusting();
 }
 public void ReturnToRack(){if(!recorded)RecordHome();Holder=null;dusting=null;sweeping=false;transform.SetParent(Dock?Dock.parent:homeParent,true);transform.SetPositionAndRotation(Dock?Dock.position:home,Dock?Dock.rotation:rotation);}
 public bool Use(EvidenceClue clue)=>UseAt(clue,default,false);
 // The trigger on whatever the ray found: a clue, or (for the powder applicators) any surface.
 public bool UseAt(EvidenceClue clue,RaycastHit hit,bool hasHit){
  if(IsApplicator){
   dusting=clue?clue.GetComponent<DustablePrint>():null;
   if(!dusting&&hasHit)dusting=LatentMarkField.MarkAt(hit,.05f);
   if(dusting){if(!CanBrush(dusting)){dusting=null;DemoFeedback.Post("Bring the "+(Kind==Method.Brush?"brush":"wand")+" closer to the surface",FeedbackTone.Warning);return false;}sweeping=false;previousContact=dusting.transform.position;DemoFeedback.Post("Hold the trigger and sweep lightly over the surface; stop when ridges show");return true;}
   if(hasHit&&(hit.collider.GetComponentInParent<LatentMarkField>()||hit.collider.GetComponentInParent<ForensicSurface>())){
    var surface=ForensicSurface.ClassOf(hit.collider);sweeping=true;previousContact=hit.point;
    if(ForensicSurface.Porous(surface)){DemoFeedback.Post("Powder is not the method for paper or fabric: photograph it and bag it for the laboratory (ninhydrin, DFO)",FeedbackTone.Warning);porousWarned=Time.time;}
    else DemoFeedback.Post("Hold the trigger and sweep lightly over the surface; stop when ridges show");
    return true;
   }
   DemoFeedback.Post(Kind==Method.Brush?"Sweep the brush over a glass, metal or painted surface":"Sweep the wand over a textured or plastic surface",FeedbackTone.Warning);return false;
  }
  if(!clue){DemoFeedback.Post(Kind==Method.KMTest?"Hold the test over a suspected stain":Kind==Method.CastingKit||Kind==Method.MikrosilKit?"Aim the kit at a mark to cast":"Aim at a trace to sample",FeedbackTone.Warning);return false;}
  if(Kind==Method.KMTest){var stain=clue.GetComponent<BloodStainClue>();if(!stain){DemoFeedback.Post("The presumptive test is for suspected blood stains",FeedbackTone.Warning);return false;}if(!clue.Available&&!clue.Observed){DemoFeedback.Post("Nothing to test here yet",FeedbackTone.Warning);return false;}return stain.Advance(clue);}
  if(Kind==Method.CastingKit||Kind==Method.MikrosilKit){var cast=clue.GetComponent<CastableMark>();if(!cast||cast.Kit!=Kind){DemoFeedback.Post(Kind==Method.CastingKit?"Dental stone casts three-dimensional impressions in soil or sand":"Silicone casts tool marks on hard surfaces",FeedbackTone.Warning);return false;}}
  return clue.Sample(this);
 }
 bool CanBrush(DustablePrint print)=>Holder&&print&&Vector3.Distance(Tip.position,print.transform.position)<(Holder is HandInteractor ? .28f : 2);
 void ContinueDusting(){
  if(!dusting&&!sweeping)return;if(dusting&&(dusting.Developed||!CanBrush(dusting))&&!sweeping)return;bool pressed=false;Ray ray;
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
   if(ForensicSurface.Porous(surface)){if(Time.time-porousWarned>4){porousWarned=Time.time;DemoFeedback.Post("Powder on paper: the mark is being lost. Paper goes to the laboratory",FeedbackTone.Warning);}previousContact=hit.point;return false;}
   target=LatentMarkField.MarkAt(hit,.035f);
   if(!target){previousContact=hit.point;return true;} // powder on bare glass: nothing shows
  }
  float sweep=Mathf.Min(.03f,Vector3.Distance(previousContact,hit.point));previousContact=hit.point;
  target.Brush(sweep*3.5f+Mathf.Clamp(seconds,0,.05f)*.34f,Kind);return true;
 }
}
