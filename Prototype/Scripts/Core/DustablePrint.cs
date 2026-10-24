using UnityEngine;

// A latent mark that powder develops: designated handle marks (PortableFieldwork) and the random
// marks a LatentMarkField spreads over display-case glass. Coverage grows with each brush stroke;
// at 1.0 the ridges are developed (grade 4). Brushing on past that fills the ridges in (fingerprints.md
// §3: "less is best", stop when developed): the grade drops to 1 at OverBrushLimit and the log
// records "Over-brushed". A Smudge develops into a smear with no ridge detail (grade 0), which is
// what most contacts on real glass look like. A nylon swab over an undeveloped mark destroys it
// (TouchDnaClue). Nothing here identifies a person.
public sealed class DustablePrint : MonoBehaviour {
 public Renderer Powder;
 public EvidenceClue Clue;
 public bool Smudge;
 public const float OverBrushLimit=1.6f;
 public float Coverage {get;private set;}
 public bool Developed=>Coverage>=1;
 public bool Destroyed {get;private set;}
 public bool LiftedWithGel {get;set;}
 // CAST 0-4 quality grades (fingerprints.md §7): 4 and 3 are of value, 0 has no ridge detail.
 public int Grade=>Destroyed||Smudge||!Developed?0:Coverage<1.2f?4:Coverage<1.4f?3:Coverage<OverBrushLimit?2:1;
 MaterialPropertyBlock block;bool overLogged;
 public void Brush(float amount)=>Brush(amount,FieldInvestigationTool.Method.Brush);
 public void Brush(float amount,FieldInvestigationTool.Method applicator){
  if(Destroyed||amount<=0||Coverage>=OverBrushLimit)return;bool was=Developed;Coverage=Mathf.Min(OverBrushLimit,Coverage+amount);Refresh();
  if(!was&&Developed){
   var log=Clue?ProcedureLog.Of(Clue):null;var surface=Clue?Clue.Surface:SurfaceClass.NonPorousSmooth;if(Clue&&Clue.Register)Clue.Register.GlowRefresh();
   if(log)log.Add("Developed",ForensicRules.MethodName(applicator),ForensicRules.Applicator(applicator,surface),Smudge?"smear, no ridge detail":"ridge detail, grade 4");
   if(Smudge){ForensicFeedback.Post("Powder shows a smear with no ridge detail",FeedbackTone.Info);if(Clue)Clue.Inspect();return;}
   if(Clue)Clue.Inspect();DemoSounds.Play(DemoSound.Select,transform.position,.15f);
   ForensicFeedback.Post(applicator==FieldInvestigationTool.Method.MagneticWand&&surface==SurfaceClass.Steel?"Ridge mark developed, but magnetic powder clings to steel: a glass-fibre brush suits this surface":"Ridge mark developed: photograph it before lifting",FeedbackTone.Success);
  }else if(Developed&&!Smudge&&Coverage>=1.3f&&!overLogged){
   overLogged=true;var log=Clue?ProcedureLog.Of(Clue):null;if(log)log.Add("Over-brushed",ForensicRules.MethodName(applicator),StepFlag.Discipline,"ridge detail filling in, grade "+Grade);
   ForensicFeedback.Post("Too much powder: the ridges are filling in",FeedbackTone.Warning);
  }
 }
 // A wet swab or a careless wipe takes an undeveloped mark away for good.
 public void DestroyMark(string why){if(Destroyed)return;Destroyed=true;Coverage=0;Refresh();if(Clue)ProcedureLog.Of(Clue).Add("Mark lost","",StepFlag.Acceptable,why);}
 public void Refresh(){
  if(!Powder)return;Powder.enabled=Coverage>0&&!Destroyed;if(block==null)block=new MaterialPropertyBlock();Powder.GetPropertyBlock(block);
  float fill=Mathf.InverseLerp(1f,OverBrushLimit,Coverage); // over-brushing darkens and flattens the pattern
  Color ink=Smudge?new Color(.11f,.10f,.09f,Mathf.Clamp01(Coverage)*.45f):Color.Lerp(new Color(.075f,.068f,.058f,Mathf.Clamp01(Coverage)*.76f),new Color(.05f,.045f,.04f,.92f),fill);
  block.SetColor("_Color",ink);Powder.SetPropertyBlock(block);
 }
 public void ResetPrint(){Coverage=0;Destroyed=false;LiftedWithGel=false;overLogged=false;Refresh();}
}
