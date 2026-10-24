using UnityEngine;

// Touch DNA on a handle that may also carry a latent mark (biological_trace.md §4, HOSDB 2007,
// Staffordshire trials): a wet nylon swab wipes an undeveloped mark away, so the examiner either
// photographs and lifts first (powder and tape reduce the DNA yield) or gel-lifts, which keeps
// both. The clue on this object is the DNA sample point; Mark is the ridge mark sharing the surface.
public sealed class TouchDnaClue : MonoBehaviour {
 public DustablePrint Mark;
 public void OnSwabbed(EvidenceClue clue){
  var log=ProcedureLog.Of(clue);
  if(!Mark||Mark.Destroyed){log.Add("Swabbed",ForensicRules.MethodName(FieldInvestigationTool.Method.Swab),StepFlag.Correct);return;}
  if(!Mark.Developed){
   Mark.DestroyMark("wet swab wiped the undeveloped latent mark");
   log.Add("Swabbed before powder","swab",StepFlag.Acceptable,"DNA recovered first; the latent mark on this surface is gone (photograph the area, then swab, or gel-lift first)");
   DemoFeedback.Post("Swab taken. The wet swab has wiped any latent mark here: photograph before swabbing next time, or gel-lift first",FeedbackTone.Warning);
  }else if(Mark.Clue&&!Mark.Clue.Sampled){
   bool photographed=ProcedureLog.Of(Mark.Clue).Has("Photographed");
   log.Add("Swabbed over developed mark","swab",photographed?StepFlag.Acceptable:StepFlag.OutOfSequence,photographed?"mark photographed first (HOSDB 2007)":"developed mark not photographed before swabbing");
  }else log.Add("Swabbed after lift","swab",Mark.LiftedWithGel?StepFlag.Correct:StepFlag.Acceptable,Mark.LiftedWithGel?"gel lift leaves DNA recoverable":"powder and tape lift reduce the DNA yield");
 }
}
