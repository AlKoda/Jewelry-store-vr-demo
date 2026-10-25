using UnityEngine;

// A small spectator group, not a general NPC/navmesh system. Real skeletal clips
// supply all limb/weight movement. Only the walker root travels, on its authored
// pavement segment; it waits if the visitor is close. No rigidbody forces.
public sealed class CrowdBystander : MonoBehaviour {
 public enum Behaviour {Walker,Conversation,Phone,Observer}
 public Behaviour Role;
 public Animation CharacterAnimation;
 public Transform Head,Nose,Storefront;
 public CrowdBystander Partner;
 public Vector3 WalkOffset=new Vector3(5,0,0);
 public float WalkSpeed=.75f,NativeWalkSpeed=.8209517f,PhaseSeconds,PlaybackSpeed=1;
 public float PauseSeconds=5,TurnSpeed=85;
 public AudioClip[] FootstepClips=new AudioClip[0];
 public bool IsWalking {get;private set;}
 public bool IsSpeaking {get;private set;}
 Vector3 home;float clock,wait,walked,footDistance;bool initialized,towardsEnd=true;
 string currentClip;AnimationState motionState;
 Transform viewer;AudioSource footSource;
 Quaternion authoredHead,appliedHead,lookOffset=Quaternion.identity;bool headApplied;

 void Start(){Initialize();}
 public void Initialize(){
  if(initialized)return;initialized=true;home=transform.localPosition;clock=0;wait=Role==Behaviour.Walker?1.2f:0;
  viewer=Camera.main?Camera.main.transform:null;
  if(Role==Behaviour.Walker&&FootstepClips.Length>0){
   footSource=gameObject.AddComponent<AudioSource>();footSource.playOnAwake=false;footSource.spatialBlend=1;footSource.volume=.12f;footSource.minDistance=1;footSource.maxDistance=11;footSource.rolloffMode=AudioRolloffMode.Logarithmic;footSource.dopplerLevel=0;footSource.priority=180;
  }
  if(CharacterAnimation){CharacterAnimation.playAutomatically=false;CharacterAnimation.cullingType=AnimationCullingType.BasedOnRenderers;}
  SelectClip(Role==Behaviour.Conversation||Role==Behaviour.Walker?"Idle":"Motion",true);
 }
 void Update(){Tick(Time.deltaTime);}
 public void Tick(float deltaTime){
  Initialize();float dt=Mathf.Clamp(deltaTime,0,.1f);clock+=dt;
  if(!viewer&&Camera.main)viewer=Camera.main.transform;
  IsSpeaking=Role==Behaviour.Conversation&&Mathf.Repeat(clock+PhaseSeconds,18)<7.2f;
  IsWalking=false;float stridePlayback=PlaybackSpeed;
  if(Role==Behaviour.Walker){
   var destination=home+(towardsEnd?WalkOffset:Vector3.zero);var delta=destination-transform.localPosition;delta.y=0;
   bool visitorClose=viewer&&Vector3.ProjectOnPlane(viewer.position-transform.position,Vector3.up).sqrMagnitude<1.15f*1.15f;
   if(wait>0||visitorClose){
    if(!visitorClose)wait-=dt;
    if(Storefront){var d=Storefront.position-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),TurnSpeed*.4f*dt);}
   }else if(delta.magnitude<.015f){towardsEnd=!towardsEnd;wait=PauseSeconds;}
   else{
    var heading=Quaternion.LookRotation(delta);transform.localRotation=Quaternion.RotateTowards(transform.localRotation,heading,TurnSpeed*dt);
    if(Quaternion.Angle(transform.localRotation,heading)<18){
     float speed=WalkSpeed*Mathf.Clamp(delta.magnitude/.3f,.22f,1);var step=Vector3.ClampMagnitude(delta,speed*dt);transform.localPosition+=step;IsWalking=true;walked+=step.magnitude;footDistance+=step.magnitude;
     stridePlayback=speed/Mathf.Max(.1f,NativeWalkSpeed);
     if(footSource&&footDistance>.60f){footDistance=0;int index=Mathf.FloorToInt(walked/.6f)%FootstepClips.Length;footSource.pitch=.97f+Mathf.Sin(walked)*.025f;footSource.PlayOneShot(FootstepClips[index]);}
    }
   }
  }
  SelectClip(Role==Behaviour.Walker?(IsWalking?"Motion":"Idle"):Role==Behaviour.Conversation?(IsSpeaking?"Motion":"Idle"):"Motion");
  if(IsWalking&&motionState!=null)motionState.speed=stridePlayback;
 }
 void SelectClip(string name,bool initial=false){
  if(!CharacterAnimation||(!initial&&name==currentClip))return;
  var state=CharacterAnimation[name];if(state==null)return;
  if(name=="Motion")motionState=state;
  currentClip=name;state.wrapMode=WrapMode.Loop;state.speed=PlaybackSpeed;
  if(initial){CharacterAnimation.Play(name);state.time=Mathf.Repeat(PhaseSeconds,state.length);}
  else CharacterAnimation.CrossFade(name,.35f);
 }
 void LateUpdate(){LookTowardsScene(Time.deltaTime);}
 public void LookTowardsScene(float deltaTime){
  if(!Head||!Nose||!Storefront)return;
  // Legacy animation can be culled while a spectator is offscreen. Remove our
  // previous additive turn before measuring the next one, even when no new
  // animation sample has arrived. Otherwise a small turn accumulates each frame.
  if(headApplied&&Quaternion.Angle(Head.localRotation,appliedHead)<.001f)Head.localRotation=authoredHead;
  authoredHead=Head.localRotation;
  Vector3 target=Storefront.position+Vector3.up*1.5f;
  if(Role==Behaviour.Conversation&&Partner&&Mathf.Repeat(clock+PhaseSeconds,11)<7)target=Partner.transform.position+Vector3.up*1.55f;
  var forward=Nose.position-Head.position;var desired=target-Head.position;
  if(forward.sqrMagnitude<.000001f||desired.sqrMagnitude<.01f)return;
  var delta=Quaternion.FromToRotation(forward,desired);float limit=Role==Behaviour.Phone?12:28;
  var limited=Quaternion.RotateTowards(Quaternion.identity,delta,limit);
  var parent=Head.parent?Head.parent.rotation:Quaternion.identity;
  var localOffset=Quaternion.Inverse(parent)*limited*parent;
  lookOffset=Quaternion.Slerp(lookOffset,localOffset,1-Mathf.Exp(-3*Mathf.Clamp(deltaTime,0,.1f)));
  Head.localRotation=lookOffset*authoredHead;appliedHead=Head.localRotation;headApplied=true;
 }
}
