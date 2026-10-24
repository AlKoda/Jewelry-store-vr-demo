using System;
using System.Collections.Generic;
using UnityEngine;

// One piece of evidence: observed in place or collected with a compatible method. RequiredTool is
// the authored primary method (kept for saved scenes); ExtraMethods are the other methods that work
// on it, so a tape lift, a hinge lifter and a gel lifter all take a developed mark. Every action is
// written to the ProcedureLog on this object (Docs/Research/FORENSIC_MECHANICS_SPEC.md §4): the
// sandbox never blocks a step, it records the order and the method so the review can judge them.
public sealed class EvidenceClue : MonoBehaviour {
 public string Id,TitleEn,TitleAr,ObservationEn,ObservationAr;
 public bool CanSample;
 public FieldInvestigationTool.Method RequiredTool;
 public FieldInvestigationTool.Method[] ExtraMethods=new FieldInvestigationTool.Method[0];
 public SurfaceClass Surface=SurfaceClass.NonPorousSmooth;
 public bool Biological; // paper packaging, dries before sealing
 public GameObject Specimen;
 public EvidenceRegister Register;
 public EvidenceContainer Container;
 public bool Observed {get;private set;}
 public bool Sampled {get;private set;}
 public FieldInvestigationTool.Method CollectedWith {get;private set;}
 public string Title=>DemoLanguage.Arabic?TitleAr:TitleEn;
 public string Observation=>DemoLanguage.Arabic?ObservationAr:ObservationEn;
 public ProcedureLog Log=>ProcedureLog.Of(this);
 readonly HashSet<FieldInvestigationTool.Method> refused=new HashSet<FieldInvestigationTool.Method>();
 public bool Accepts(FieldInvestigationTool.Method method){if(method==RequiredTool)return true;if(ExtraMethods!=null)foreach(var m in ExtraMethods)if(m==method)return true;return false;}
 public bool Available=>isActiveAndEnabled&&(!Container||Container.IsOpen)&&!Sampled&&(!GetComponent<DustablePrint>()||GetComponent<DustablePrint>().Developed)&&(!GetComponent<UVDiscoverableClue>()||GetComponent<UVDiscoverableClue>().CanInspect);
 public bool Inspect(){
  if(!Available)return false;
  if(GetComponent<UVDiscoverableClue>())GetComponent<UVDiscoverableClue>().Remember();
  if(!Observed){Observed=true;Log.Add("Observed","",StepFlag.Correct);if(Register)Register.Record(this,false);}
  DemoFeedback.Post(Title+": "+Observation);return true;
 }
 // The photo log reaches every clue the frame covered (EvidenceRegister.PhotoSaved).
 public void NotePhotographed(bool withScale){
  var log=Log;bool first=!log.Has("Photographed");
  log.Add("Photographed",withScale?"camera, scale in frame":"camera",withScale||!first?StepFlag.Correct:StepFlag.Acceptable,withScale?null:"no scale in the frame");
 }
 public bool Sample(FieldInvestigationTool tool){
  if(!Available||!tool)return false;
  if(!CanSample){return false;}
  if(!Accepts(tool.Kind)){
   if(refused.Add(tool.Kind))Log.Add("Wrong method offered",ForensicRules.MethodName(tool.Kind),StepFlag.WrongMethod,"accepted: "+AcceptedList());
   return false;
  }
  // A controller ray cannot collect across the room. Desktop also has a short
  // examination reach, independent of placement-tool distance grabbing.
  if(!tool.Holder||Vector3.Distance(tool.Holder is HandInteractor ? tool.Holder.transform.position : ((DesktopInteractor)tool.Holder).View.transform.position,transform.position)> (tool.Holder is HandInteractor ? .65f : 2f)){
   DemoFeedback.Post("Move closer to collect the sample",FeedbackTone.Warning);return false;
  }
  var cast=GetComponent<CastableMark>();if(cast&&tool.Kind==cast.Kit&&!cast.ReadyToLift)return cast.Use(this);
  var stain=GetComponent<BloodStainClue>();if(stain&&tool.Kind==FieldInvestigationTool.Method.Swab&&stain.Result==BloodStainClue.Outcome.Untested)Log.Add("Swabbed untested stain","swab",StepFlag.Acceptable,"presumptive test skipped; the laboratory decides");
  var dna=GetComponent<TouchDnaClue>();if(dna&&tool.Kind==FieldInvestigationTool.Method.Swab)dna.OnSwabbed(this);
  var print=GetComponent<DustablePrint>();if(print){if(tool.Kind==FieldInvestigationTool.Method.GelLifter)print.LiftedWithGel=true;if(print.Grade<=2)Log.Add("Low-grade lift",ForensicRules.MethodName(tool.Kind),StepFlag.Acceptable,"grade "+print.Grade+(print.Smudge?", smear":", over-brushed"));}
  bool photographed=Log.Has("Photographed");
  Log.Add("Collected",ForensicRules.MethodName(tool.Kind),photographed?StepFlag.Correct:StepFlag.OutOfSequence,photographed?ForensicRules.Container(tool.Kind,Biological):"no photograph before collection; "+ForensicRules.Container(tool.Kind,Biological));
  if(cast)cast.OnLifted();
  Observed=true;Sampled=true;CollectedWith=tool.Kind;if(Specimen)Specimen.SetActive(false);if(GetComponent<Collider>())GetComponent<Collider>().enabled=false;
  if(Register)Register.Record(this,true);DemoSounds.Play(DemoSound.Select,transform.position,.2f);
  DemoFeedback.Post(DemoLanguage.Arabic?"تم حفظ العينة وتسجيلها":"Sample sealed and recorded",FeedbackTone.Success);return true;
 }
 string AcceptedList(){var names=new List<string>{ForensicRules.MethodName(RequiredTool)};if(ExtraMethods!=null)foreach(var m in ExtraMethods)names.Add(ForensicRules.MethodName(m));return string.Join(", ",names);}
 string WrongToolMessage(){
  switch(RequiredTool){
   case FieldInvestigationTool.Method.Forceps:return "Use evidence forceps for this loose item";
   case FieldInvestigationTool.Method.LiftCard:case FieldInvestigationTool.Method.Tape:return "Lift the developed ridge mark with tape, a hinge lifter or a gel lifter";
   case FieldInvestigationTool.Method.GelLifter:return "Lift the dust mark with a gel lifter";
   case FieldInvestigationTool.Method.Swab:return "Use a sterile swab for this trace";
   case FieldInvestigationTool.Method.CastingKit:return "Cast this impression with dental stone";
   case FieldInvestigationTool.Method.MikrosilKit:return "Cast this tool mark with silicone";
   default:return "This method does not suit the item ("+AcceptedList()+")";
  }
 }
 public void ResetEvidence(){
  Observed=false;Sampled=false;refused.Clear();Log.Clear();if(Specimen)Specimen.SetActive(true);if(GetComponent<Collider>())GetComponent<Collider>().enabled=true;
  if(GetComponent<DustablePrint>())GetComponent<DustablePrint>().ResetPrint();if(GetComponent<UVDiscoverableClue>())GetComponent<UVDiscoverableClue>().ResetDiscovery();
  if(GetComponent<BloodStainClue>())GetComponent<BloodStainClue>().ResetTest();if(GetComponent<CastableMark>())GetComponent<CastableMark>().ResetCast();if(GetComponent<DustMarkClue>())GetComponent<DustMarkClue>().ResetMark();
 }
}
