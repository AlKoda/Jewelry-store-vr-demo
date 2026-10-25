using UnityEngine;

// One bounded ray for both input adapters. Solid geometry always wins over a
// trigger target behind it. The player's own colliders and held tools are skipped.
public static class InvestigationInteraction {
 public static bool Target(Ray ray,float reach,ToolHolder holder,out RaycastHit hit){
  hit=default;float nearest=reach;bool found=false;
  foreach(var h in Physics.RaycastAll(ray,reach,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)){
   var t=h.collider.transform;if(holder&&(t.IsChildOf(holder.transform)||t.GetComponentInParent<CharacterController>()||(holder.FieldTool&&t.IsChildOf(holder.FieldTool.transform))||(holder.CarriedKit&&t.IsChildOf(holder.CarriedKit.transform))))continue;
   bool target=t.GetComponentInParent<EvidenceClue>()||t.GetComponentInParent<EvidenceContainer>()||t.GetComponentInParent<EvidenceRegister>()||t.GetComponentInParent<FieldInvestigationTool>()||t.GetComponentInParent<PortableFieldKit>()||t.GetComponentInParent<LatentMarkField>()||t.GetComponentInParent<ForensicSurface>();
   if(h.collider.isTrigger&&!target)continue;if(h.distance>nearest)continue;nearest=h.distance;hit=h;found=true;
  }return found;
 }
 public static bool Use(ToolHolder holder,Ray ray,float reach){
  if(!Target(ray,reach,holder,out var hit)){if(holder.FieldTool)holder.FieldTool.UseAt(null,default,false);return holder.FieldTool;}
  var clue=hit.collider.GetComponentInParent<EvidenceClue>();
  if(holder.FieldTool){holder.FieldTool.UseAt(clue,hit,true);return true;}
  var tool=hit.collider.GetComponentInParent<FieldInvestigationTool>();if(tool){holder.TakeFieldTool(tool);return true;}
  var kit=hit.collider.GetComponentInParent<PortableFieldKit>();if(kit&&kit.GripCollider==hit.collider){holder.TakeKit(kit);return true;}
  if(clue)return clue.Inspect();var box=hit.collider.GetComponentInParent<EvidenceContainer>();if(box){box.Toggle();return true;}
  var register=hit.collider.GetComponentInParent<EvidenceRegister>();if(register){register.NextPage();return true;}return false;
 }
 public static PortableFieldKit NearKit(HandInteractor hand){
  PortableFieldKit best=null;float bestDistance=float.MaxValue;
  foreach(var collider in Physics.OverlapSphere(hand.transform.position,.08f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)){
   var kit=collider.GetComponentInParent<PortableFieldKit>();if(!kit||kit.Holder||kit.GripCollider!=collider)continue;
   float distance=(collider.ClosestPoint(hand.transform.position)-hand.transform.position).sqrMagnitude;
   // The case sits above the cart's push bar. PhysX overlap order is not a
   // selection priority: prefer the handle actually touched by the hand.
   distance+=(kit.Grip.position-hand.transform.position).sqrMagnitude*.001f;
   if(distance<bestDistance&&Reachable(hand,collider,kit.Grip.position)){best=kit;bestDistance=distance;}
  }return best;
 }
 public static FieldInvestigationTool Near(HandInteractor hand){
  FieldInvestigationTool best=null;float distance=.18f;
  foreach(var h in Physics.OverlapSphere(hand.transform.position,.18f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide)){
   var t=h.GetComponentInParent<FieldInvestigationTool>();if(!t||t.Holder)continue;float d=Vector3.Distance(h.ClosestPoint(hand.transform.position),hand.transform.position);
   if(!Reachable(hand,h,t.transform.position))continue;
   if(d<distance){distance=d;best=t;}
  }return best;
 }
 static bool Reachable(HandInteractor hand,Collider target,Vector3 point){
  var direction=point-hand.transform.position;if(direction.sqrMagnitude<.000001f)return true;
  if(Target(new Ray(hand.transform.position,direction),direction.magnitude+.015f,hand,out var hit))return hit.collider==target;
  // PhysX rays omit a collider containing their origin. A hand already inside
  // the small trigger is a valid grip, provided the short ray found no wall.
  return (target.ClosestPoint(hand.transform.position)-hand.transform.position).sqrMagnitude<.000001f;
 }
}
