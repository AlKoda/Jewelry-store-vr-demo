using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EvidenceRegister : MonoBehaviour {
 [Serializable] public sealed class Entry {public string id,title,observation,action,utc;public Vector3 position;}
 [Serializable] public sealed class RecordFile {public string session;public List<Entry> entries=new List<Entry>();public List<string> photographs=new List<string>();}
 public EvidenceClue[] Clues=new EvidenceClue[0];
 public FieldInvestigationTool[] Tools=new FieldInvestigationTool[0];
 public EvidenceContainer[] Containers=new EvidenceContainer[0];
 public GameObject[] SealedPackets=new GameObject[0];
 public DemoSession Session;public EvidenceCamera Camera;
 public TextMesh PageText;public int Page {get;private set;}
 public PortableFieldKit Kit;
 public bool PersistRecords=true;
 RecordFile document=new RecordFile();string output;int visiblePackets;Material runtimeInk;
 public int ObservedCount=>Clues.Count(c=>c&&c.Observed);
 public int SampleCount=>Clues.Count(c=>c&&c.Sampled);
 public IReadOnlyList<Entry> Entries=>document.entries;
 public string RecordPath=>output;
 void OnEnable(){if(Session)Session.SessionReset.AddListener(ResetInvestigation);if(Camera)Camera.PhotoSaved.AddListener(PhotoSaved);DemoLanguage.Changed+=RefreshPage;}
 void OnDisable(){if(Session)Session.SessionReset.RemoveListener(ResetInvestigation);if(Camera)Camera.PhotoSaved.RemoveListener(PhotoSaved);DemoLanguage.Changed-=RefreshPage;}
 void Start(){
  Begin();if(PageText){runtimeInk=new Material(PageText.GetComponent<MeshRenderer>().sharedMaterial);var label=PageText.GetComponent<DepthTestedLabel>();if(!label)label=PageText.gameObject.AddComponent<DepthTestedLabel>();label.LabelMaterial=runtimeInk;label.Refresh();}RefreshPage();
 }
 void OnDestroy(){if(runtimeInk)Destroy(runtimeInk);}
 public void Begin(){if(!string.IsNullOrEmpty(document.session))return;document.session=Guid.NewGuid().ToString("N");output=Path.Combine(Application.persistentDataPath,"EvidenceRecords",document.session+".json");foreach(var t in Tools)if(t)t.RecordHome();}
 public void Record(EvidenceClue clue,bool sampled){Begin();document.entries.Add(new Entry{id=clue.Id,title=clue.TitleEn,observation=clue.ObservationEn,action=sampled?"sample sealed":"observed in place",utc=DateTime.UtcNow.ToString("o"),position=clue.transform.position});
  if(sampled&&visiblePackets<SealedPackets.Length)SealedPackets[visiblePackets++].SetActive(true);Page=Array.IndexOf(Clues.Where(c=>c&&c.Observed).ToArray(),clue)+1;RefreshPage();Save();}
 void PhotoSaved(string path){Begin();document.photographs.Add(path);Save();}
 void Save(){if(!PersistRecords||!Application.isPlaying)return;try{Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(document,true));}catch(Exception e){Debug.LogWarning("Evidence record could not be saved: "+e.Message);DemoFeedback.Post("Evidence record could not be saved",FeedbackTone.Warning);}}
 public void NextPage(){Page++;RefreshPage();DemoSounds.Play(DemoSound.Select,transform.position,.15f);}
 public void RefreshPage(){
  if(!PageText)return;var seen=Clues.Where(c=>c&&c.Observed).ToArray();string text;
  Page%=seen.Length+1;
  if(Page==0)text=DemoLanguage.Arabic?"سجل فحص الموقع\nالملاحظات: "+ObservedCount+" / "+Clues.Length+"\nالعينات المغلقة: "+SampleCount+" / "+Clues.Count(c=>c.CanSample)+"\nافحص آثار المقبض والسطح بضوء UV\nافتح علب الصيانة وافحص محتوياتها\nانقر لمراجعة الملاحظات المسجلة":"FIELD REGISTER\nObservations: "+ObservedCount+" / "+Clues.Length+"\nSealed samples: "+SampleCount+" / "+Clues.Count(c=>c.CanSample)+"\nUV: examine handle / counter traces\nSearch the repair and document boxes\nClick to review recorded notes";
  else {var clue=seen[Page-1];text=(DemoLanguage.Arabic?"سجل الأدلة":"EVIDENCE REGISTER")+"  "+Page+" / "+seen.Length+"\n"+clue.Id+" · "+clue.Title+"\n"+Wrap(clue.Observation)+"\n"+(clue.Sampled?(DemoLanguage.Arabic?"العينة مغلقة ومسجلة":"SAMPLE SEALED & RECORDED"):(DemoLanguage.Arabic?"تمت المعاينة في الموقع":"OBSERVED IN PLACE"));}
  PageText.characterSize=.0027f*Mathf.Min(1,6f/text.Split('\n').Length);PageText.text=DemoLanguage.Text(text);if(DemoLanguage.Font)PageText.font=DemoLanguage.Font;
 }
 static string Wrap(string value){var result="";int column=0;foreach(string word in value.Split(' ')){if(column+word.Length>35){result+="\n";column=0;}if(column>0){result+=" ";column++;}result+=word;column+=word.Length;}return result;}
 public void ResetInvestigation(){
  // Completed record files and photos survive reset; a new register starts here.
  if(Kit)Kit.ResetToHome();foreach(var c in Clues)if(c)c.ResetEvidence();foreach(var t in Tools)if(t){if(t.Holder)t.Holder.ReturnFieldTool();else t.ReturnToRack();}foreach(var c in Containers)if(c)c.Close();
  foreach(var p in SealedPackets)if(p)p.SetActive(false);visiblePackets=0;Page=0;document=new RecordFile();Begin();RefreshPage();
 }
}
