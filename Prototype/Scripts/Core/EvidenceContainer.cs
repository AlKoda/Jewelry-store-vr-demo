using UnityEngine;

// Kinematic lid: never applies forces to the visitor or clips through its base.
public sealed class EvidenceContainer : MonoBehaviour {
 public Transform Lid;
 public float OpenDegrees=-105;
 public bool IsOpen {get;private set;}
 float angle;
 public void Toggle(){IsOpen=!IsOpen;DemoSounds.Play(DemoSound.Click,transform.position,.18f);}
 void Update(){angle=Mathf.MoveTowards(angle,IsOpen?OpenDegrees:0,Time.deltaTime*145);if(Lid)Lid.localRotation=Quaternion.Euler(angle,0,0);}
 public void Close(){IsOpen=false;angle=0;if(Lid)Lid.localRotation=Quaternion.identity;}
}
