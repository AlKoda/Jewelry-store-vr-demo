using UnityEngine;

// A three-dimensional footwear impression (dental stone, impressions_toolmarks_glass.md §1: pour,
// let it set 25-30 min before lifting) or a tool mark (silicone casting compound, §2: mix, spread,
// sets in 6-10 min; never offer the suspect tool to the mark). The kit's trigger pours; the cast
// sets over SetSeconds (compressed time, stated to the trainee); the same kit then lifts it, which
// collects the exhibit through EvidenceClue.Sample. CastVisual is the cast object shown while setting.
public sealed class CastableMark : MonoBehaviour {
 public enum Kind { Impression, ToolMark }
 public Kind Type=Kind.Impression;
 public float SetSeconds=30;
 public GameObject CastVisual;
 public enum Stage { Open, Setting, Set, Lifted }
 public Stage State {get;private set;}
 float poured;
 public FieldInvestigationTool.Method Kit=>Type==Kind.Impression?FieldInvestigationTool.Method.CastingKit:FieldInvestigationTool.Method.MikrosilKit;
 public bool ReadyToLift=>State==Stage.Set;
 public float Remaining=>State==Stage.Setting?Mathf.Max(0,SetSeconds-(Time.time-poured)):0;
 public bool Use(EvidenceClue clue){
  switch(State){
   case Stage.Open:
    State=Stage.Setting;poured=Time.time;if(CastVisual)CastVisual.SetActive(true);
    bool photographed=ProcedureLog.Of(clue).Has("Photographed");
    ProcedureLog.Of(clue).Add(Type==Kind.Impression?"Cast poured":"Silicone applied",ForensicRules.MethodName(Kit),photographed?StepFlag.Correct:StepFlag.OutOfSequence,photographed?null:"no photograph with scale before casting");
    ForensicFeedback.Post(Type==Kind.Impression?"Dental stone poured: let it set ("+Mathf.RoundToInt(SetSeconds)+" s here stands for 30 minutes)":"Silicone spread over the mark: let it cure ("+Mathf.RoundToInt(SetSeconds)+" s here stands for 8 minutes)",FeedbackTone.Success);return true;
   case Stage.Setting:Tick();if(State==Stage.Setting)ForensicFeedback.Post("Still setting: "+Mathf.CeilToInt(Remaining)+" s");return true;
   default:return false; // Set: the caller collects it; Lifted: nothing left
  }
 }
 void Update(){Tick();}
 public void Tick(){if(State==Stage.Setting&&Time.time-poured>=SetSeconds){State=Stage.Set;ForensicFeedback.Post(Type==Kind.Impression?"The cast has set: lift it and mark the back":"The silicone has cured: peel it off",FeedbackTone.Success);}}
 public void ForceSet(){if(State==Stage.Setting)State=Stage.Set;}
 public void OnLifted(){State=Stage.Lifted;if(CastVisual)CastVisual.SetActive(false);}
 public void ResetCast(){State=Stage.Open;if(CastVisual)CastVisual.SetActive(false);}
}
