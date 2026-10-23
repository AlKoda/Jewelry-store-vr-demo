using UnityEngine;

public sealed class EvidenceClue : MonoBehaviour {
 public string Id,TitleEn,TitleAr,ObservationEn,ObservationAr;
 public bool CanSample;
 public FieldInvestigationTool.Method RequiredTool;
 public GameObject Specimen;
 public EvidenceRegister Register;
 public EvidenceContainer Container;
 public bool Observed {get;private set;}
 public bool Sampled {get;private set;}
 public string Title=>DemoLanguage.Arabic?TitleAr:TitleEn;
 public string Observation=>DemoLanguage.Arabic?ObservationAr:ObservationEn;
 public bool Available=>isActiveAndEnabled&&(!Container||Container.IsOpen)&&!Sampled&&(!GetComponent<DustablePrint>()||GetComponent<DustablePrint>().Developed)&&(!GetComponent<UVDiscoverableClue>()||GetComponent<UVDiscoverableClue>().CanInspect);
 public bool Inspect(){
  if(!Available)return false;
  if(GetComponent<UVDiscoverableClue>())GetComponent<UVDiscoverableClue>().Remember();
  if(!Observed){Observed=true;if(Register)Register.Record(this,false);}
  DemoFeedback.Post(Title+": "+Observation);return true;
 }
 public bool Sample(FieldInvestigationTool tool){
  if(!Available||!tool)return false;
  if(!CanSample){DemoFeedback.Post("Record this item in place",FeedbackTone.Warning);return false;}
  if(tool.Kind!=RequiredTool){DemoFeedback.Post(RequiredTool==FieldInvestigationTool.Method.Forceps?"Use evidence forceps for this loose item":RequiredTool==FieldInvestigationTool.Method.LiftCard?"Use a lift card for the developed ridge mark":"Use a sterile swab for this trace",FeedbackTone.Warning);return false;}
  // A controller ray cannot collect across the room. Desktop also has a short
  // examination reach, independent of placement-tool distance grabbing.
  if(!tool.Holder||Vector3.Distance(tool.Holder is HandInteractor ? tool.Holder.transform.position : ((DesktopInteractor)tool.Holder).View.transform.position,transform.position)> (tool.Holder is HandInteractor ? .65f : 2f)){
   DemoFeedback.Post("Move closer to collect the sample",FeedbackTone.Warning);return false;
  }
  Observed=true;Sampled=true;if(Specimen)Specimen.SetActive(false);if(GetComponent<Collider>())GetComponent<Collider>().enabled=false;
  if(Register)Register.Record(this,true);DemoSounds.Play(DemoSound.Select,transform.position,.2f);DemoFeedback.Post("Sample sealed and recorded",FeedbackTone.Success);return true;
 }
 public void ResetEvidence(){Observed=false;Sampled=false;if(Specimen)Specimen.SetActive(true);if(GetComponent<Collider>())GetComponent<Collider>().enabled=true;if(GetComponent<DustablePrint>())GetComponent<DustablePrint>().ResetPrint();if(GetComponent<UVDiscoverableClue>())GetComponent<UVDiscoverableClue>().ResetDiscovery();}
}
