using UnityEngine;

// A two-dimensional footwear mark in dust (impressions_toolmarks_glass.md §1): found with oblique
// light, photographed with a scale at 90 degrees, lifted with a gelatin lifter or electrostatic
// film. Walking over it before it is recorded is the classic scene-discipline slip, so the player's
// feet are watched while the mark is unrecorded.
public sealed class DustMarkClue : MonoBehaviour {
 public float Radius=.3f;
 EvidenceClue clue;bool trodden;float next;Transform feet;
 void Update(){if(trodden||Time.time<next)return;next=Time.time+.25f;var p=Feet();if(p.HasValue)Check(p.Value);}
 public bool Check(Vector3 playerPosition){
  if(trodden)return false;if(!clue)clue=GetComponent<EvidenceClue>();if(!clue||clue.Sampled||ProcedureLog.Of(clue).Has("Photographed"))return false;
  var d=playerPosition-transform.position;d.y=0;if(d.sqrMagnitude>Radius*Radius)return false;
  trodden=true;ProcedureLog.Of(clue).Add("Mark trodden on","",StepFlag.Discipline,"walked over an unrecorded footwear mark");
  DemoFeedback.Post("You have walked over an unrecorded footwear mark",FeedbackTone.Warning);return true;
 }
 Vector3? Feet(){
  if(!feet){var controller=FindFirstObjectByType<CharacterController>();if(controller)feet=controller.transform;else if(Camera.main)feet=Camera.main.transform;}
  return feet?feet.position:(Vector3?)null;
 }
 public void ResetMark(){trodden=false;}
}
