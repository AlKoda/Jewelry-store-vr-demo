using UnityEngine;

// Reusable field-kit tools have their own rack rather than altering the six
// established placement-tool slots or controller wheel. No dynamic physics.
public sealed class FieldInvestigationTool : MonoBehaviour {
 public enum Method { Forceps, Swab, Brush, LiftCard }
 public Method Kind;
 public EvidenceRegister Register;
 public Transform Tip;
 public Transform Dock;
 public Vector3 HandPosition=new Vector3(0,-.045f,.005f);
 public Vector3 HandEuler=new Vector3(0,0,0);
 public ToolHolder Holder {get;private set;}
 Vector3 home;Quaternion rotation;Transform homeParent;bool recorded;DemoLighting lighting;DustablePrint dusting;Vector3 previousContact;
 public string ToolName=>Kind==Method.Forceps?"Evidence forceps":Kind==Method.Swab?"Sterile swab":Kind==Method.Brush?"Fingerprint brush":"Fingerprint lift card";
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
  if(Kind==Method.Brush)ContinueDusting();
 }
 public void ReturnToRack(){if(!recorded)RecordHome();Holder=null;dusting=null;transform.SetParent(Dock?Dock.parent:homeParent,true);transform.SetPositionAndRotation(Dock?Dock.position:home,Dock?Dock.rotation:rotation);}
 public bool Use(EvidenceClue clue){
  if(!clue){DemoFeedback.Post("Aim at a trace to sample",FeedbackTone.Warning);return false;}
  if(Kind==Method.Brush){
   dusting=clue.GetComponent<DustablePrint>();if(!dusting){DemoFeedback.Post("Use the brush on a designated handle or glass edge",FeedbackTone.Warning);return false;}
   if(!CanBrush(dusting)){DemoFeedback.Post("Bring the brush closer to the surface",FeedbackTone.Warning);return false;}
   previousContact=dusting.transform.position;DemoFeedback.Post("Hold the trigger and sweep the brush over the surface");return true;
  }
  return clue.Sample(this);
 }
 bool CanBrush(DustablePrint print)=>Holder&&print&&Vector3.Distance(Tip.position,print.transform.position)<(Holder is HandInteractor ? .28f : 2);
 void ContinueDusting(){
  if(!dusting||dusting.Developed||!CanBrush(dusting))return;bool pressed=false;Ray ray;
  if(Holder is HandInteractor hand){var input=hand.GetComponent<XRControllerInput>();pressed=input&&input.TriggerHeld;ray=new Ray(hand.transform.position,hand.transform.forward);}
  else{var desktop=(DesktopInteractor)Holder;pressed=desktop.ReadDesktopInput&&(!desktop.Panel||!desktop.Panel.CapturesInput)&&(Input.GetMouseButton(0)||Input.GetKey(KeyCode.G));ray=desktop.PointerRay();}
  BrushStroke(ray,Time.deltaTime,pressed);
 }
 // Shared by tracked and desktop input, and exercised against scene colliders by validation.
 public bool BrushStroke(Ray ray,float seconds,bool pressed){
  if(!pressed||Kind!=Method.Brush||!dusting||dusting.Developed||!CanBrush(dusting)||!InvestigationInteraction.Target(ray,Holder is HandInteractor ? .65f : 2,Holder,out var hit)||hit.collider.GetComponentInParent<DustablePrint>()!=dusting)return false;
  float sweep=Mathf.Min(.03f,Vector3.Distance(previousContact,hit.point));previousContact=hit.point;
  dusting.Brush(sweep*3.5f+Mathf.Clamp(seconds,0,.05f)*.34f);return true;
 }
}
