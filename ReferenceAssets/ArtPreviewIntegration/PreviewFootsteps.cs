using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public sealed class PreviewFootsteps : MonoBehaviour {
 public AudioClip[] Clips;
 CharacterController body; ShopWalkController walker; DemoDesktopPanel panel; AudioSource source; Vector3 previous;float distance;int clip;
 public int PlayedSteps {get;private set;}
 void Start(){body=GetComponent<CharacterController>();walker=GetComponent<ShopWalkController>();panel=FindFirstObjectByType<DemoDesktopPanel>();previous=transform.position;source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.16f;}
 void LateUpdate(){Vector3 delta=transform.position-previous;previous=transform.position;delta.y=0;
 if(!walker || !walker.enabled || !walker.ReadDesktopInput || !body.isGrounded || (panel&&panel.CapturesInput)||delta.magnitude>1){distance=0;return;}
 distance+=delta.magnitude;if(distance<(walker.Crouching?.55f:.72f))return;distance=0; if(Clips==null||Clips.Length==0)return;
 source.pitch=1+(clip%3-1)*.035f;source.PlayOneShot(Clips[clip++%Clips.Length],walker.Crouching?.45f:1);PlayedSteps++;}
}
