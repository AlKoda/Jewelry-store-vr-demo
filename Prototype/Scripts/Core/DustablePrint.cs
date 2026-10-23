using UnityEngine;

// A designated latent mark, separate from the pre-existing UV traces. Powder
// reveals a ridge pattern only here; it does not identify a person.
public sealed class DustablePrint : MonoBehaviour {
 public Renderer Powder;
 public EvidenceClue Clue;
 public float Coverage {get;private set;}
 public bool Developed=>Coverage>=1;
 MaterialPropertyBlock block;
 public void Brush(float amount){
  if(Developed||amount<=0)return;Coverage=Mathf.Clamp01(Coverage+amount);Refresh();
  if(Developed){Clue.Inspect();DemoSounds.Play(DemoSound.Select,transform.position,.15f);DemoFeedback.Post("Ridge mark developed: photograph it before lifting",FeedbackTone.Success);}
 }
 public void Refresh(){if(!Powder)return;Powder.enabled=Coverage>0;if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",new Color(.075f,.068f,.058f,Coverage*.76f));Powder.SetPropertyBlock(block);}
 public void ResetPrint(){Coverage=0;Refresh();}
}
