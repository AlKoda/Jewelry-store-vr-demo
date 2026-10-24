using UnityEngine;

// A red-brown stain and its Kastle-Meyer presumptive test (biological_trace.md §1): rub a filter
// paper on the stain, one drop of ethanol, one drop of the phenolphthalin reagent, watch (a colour
// now means a false positive), then one drop of peroxide; pink within about ten seconds is a
// presumptive positive, never a confirmation. Leaving the reagent longer than about 30 s before the
// peroxide lets it auto-oxidise, which the test records as invalid. Each trigger press of the test
// kit advances one stage. IsBlood false stands for paint, rust or dye: the test stays white.
public sealed class BloodStainClue : MonoBehaviour {
 public bool IsBlood=true;
 public float WindowSeconds=30;
 public Renderer TestPaper; // tinted pink on a positive (optional)
 public enum Stage { None, Rubbed, Ethanol, Reagent, Peroxide }
 public enum Outcome { Untested, Positive, Negative, Invalid }
 public Stage Step {get;private set;}
 public Outcome Result {get;private set;}
 float reagentTime;MaterialPropertyBlock block;
 public bool Advance(EvidenceClue clue)=>Advance(clue,Time.time);
 public bool Advance(EvidenceClue clue,float now){
  var log=ProcedureLog.Of(clue);
  switch(Step){
   case Stage.None:Step=Stage.Rubbed;DemoFeedback.Post(DemoLanguage.Arabic?"نقلت عينة إلى ورقة الاختبار":"Sample transferred to test paper");return true;
   case Stage.Rubbed:Step=Stage.Ethanol;DemoFeedback.Post(DemoLanguage.Arabic?"أضيف الإيثانول":"Ethanol applied");return true;
   case Stage.Ethanol:Step=Stage.Reagent;reagentTime=now;DemoFeedback.Post(DemoLanguage.Arabic?"أضيف الكاشف، لا تغير في اللون":"Reagent applied; no colour change");return true;
   case Stage.Reagent:
    Step=Stage.Peroxide;float waited=now-reagentTime;
    if(waited>WindowSeconds){Result=Outcome.Invalid;log.Add("KM test invalid","presumptive blood test",StepFlag.OutOfSequence,"reagent left "+Mathf.RoundToInt(waited)+" s before the peroxide; repeat with a fresh paper");DemoFeedback.Post(DemoLanguage.Arabic?"نتيجة الاختبار غير صالحة":"Test result invalid",FeedbackTone.Warning);Step=Stage.None;Tint(false);return true;}
    Result=IsBlood?Outcome.Positive:Outcome.Negative;Tint(IsBlood);
    log.Add(IsBlood?"KM test positive":"KM test negative","presumptive blood test",StepFlag.Correct,IsBlood?"pink within the window; presumptive only, not confirmatory":"no colour change");
    clue.Inspect();DemoSounds.Play(DemoSound.Select,transform.position,.15f);
    DemoFeedback.Post(IsBlood?(DemoLanguage.Arabic?"ظهر لون وردي؛ نتيجة أولية وليست تأكيدًا":"Pink colour; presumptive result, not confirmation"):(DemoLanguage.Arabic?"لم يتغير اللون":"No colour change"),IsBlood?FeedbackTone.Success:FeedbackTone.Info);return true;
   default:DemoFeedback.Post(DemoLanguage.Arabic?"الاختبار مسجل":"Test recorded");return false;
  }
 }
 void Tint(bool pink){if(!TestPaper)return;if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",pink?new Color(.95f,.45f,.62f):Color.white);TestPaper.SetPropertyBlock(block);}
 public void ResetTest(){Step=Stage.None;Result=Outcome.Untested;Tint(false);}
}
