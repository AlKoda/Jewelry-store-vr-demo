using System.Collections.Generic;
using UnityEngine;

// While a field tool is held, everything it can work on glows faintly (Koda, October 24): a soft
// pulsing disc over each compatible clue, and a dim sheet over each display-case top for the
// powder applicators (the marks themselves stay hidden: finding them is the work). Sits on the
// EvidenceRegister; listens to FieldInvestigationTool.HeldChanged. Unlit sprites, no lights.
public sealed class ForensicGlow : MonoBehaviour {
 public EvidenceRegister Register;
 public float Radius=.07f, Pulse=1.6f, MinAlpha=.10f, MaxAlpha=.30f;
 readonly List<Renderer> glows=new List<Renderer>();
 readonly List<Transform> follow=new List<Transform>();
 FieldInvestigationTool held;Material material;static Texture2D disc;static Mesh quad;Transform eye;
 public int ActiveGlows=>glows.Count;
 void OnEnable(){FieldInvestigationTool.HeldChanged+=OnHeld;if(!Register)Register=GetComponent<EvidenceRegister>();}
 void OnDisable(){FieldInvestigationTool.HeldChanged-=OnHeld;Clear();}
 void OnHeld(FieldInvestigationTool tool,bool taken){if(taken){held=tool;Rebuild();}else if(held==tool){held=null;Clear();}}
 // Colour by what the tool does: warm for collection, cool for powder, violet for chemistry.
 public static Color Tint(FieldInvestigationTool.Method kind){switch(kind){
  case FieldInvestigationTool.Method.Brush:case FieldInvestigationTool.Method.MagneticWand:return new Color(.55f,.8f,1f);
  case FieldInvestigationTool.Method.KMTest:case FieldInvestigationTool.Method.Swab:return new Color(.95f,.55f,.95f);
  case FieldInvestigationTool.Method.CastingKit:case FieldInvestigationTool.Method.MikrosilKit:return new Color(1f,.85f,.45f);
  default:return new Color(1f,.95f,.6f);}}
 // Whether this clue is something the held tool can act on right now.
 public static bool Compatible(EvidenceClue clue,FieldInvestigationTool.Method kind){
  if(!clue||!clue.isActiveAndEnabled||clue.Sampled)return false;
  var print=clue.GetComponent<DustablePrint>();
  if(kind==FieldInvestigationTool.Method.Brush||kind==FieldInvestigationTool.Method.MagneticWand)return print&&!print.Developed&&!print.Destroyed&&!print.Smudge;
  if(print&&!print.Developed)return false;
  if(clue.Container&&!clue.Container.IsOpen)return false;
  if(kind==FieldInvestigationTool.Method.KMTest){var stain=clue.GetComponent<BloodStainClue>();return stain&&stain.Result==BloodStainClue.Outcome.Untested;}
  var cast=clue.GetComponent<CastableMark>();if(cast)return cast.Kit==kind&&cast.State!=CastableMark.Stage.Lifted;
  return clue.CanSample&&clue.Accepts(kind);
 }
 public void Rebuild(){
  Clear();if(!held||!Register)return;
  foreach(var clue in Register.AllClues)if(Compatible(clue,held.Kind)&&!clue.GetComponent<LatentMarkField>()&&!clue.transform.IsChildOf(LatentMarkFieldRoot(clue)))Add(clue.transform,clue.transform.position+Vector3.up*.015f,Radius,false);
  if(held.IsApplicator)foreach(var field in FindObjectsByType<LatentMarkField>(FindObjectsSortMode.None)){var box=field.GetComponent<BoxCollider>();if(!box)continue;var sheet=Add(field.transform,field.transform.TransformPoint(box.center+Vector3.up*(box.size.y*.5f+.004f)),1,true);sheet.transform.localScale=new Vector3(box.size.x,box.size.z,1);}
 }
 // Field marks are found by sweeping, so they are never pointed out one by one.
 static Transform LatentMarkFieldRoot(EvidenceClue clue){var field=clue.GetComponentInParent<LatentMarkField>();return field?field.transform:null;}
 Renderer Add(Transform target,Vector3 at,float radius,bool flat){
  var g=new GameObject("Glow");g.transform.SetParent(transform,false);g.transform.position=at;g.layer=target.gameObject.layer;
  g.AddComponent<MeshFilter>().sharedMesh=Quad();var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=Material();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
  if(flat)g.transform.rotation=Quaternion.LookRotation(Vector3.down,target.forward);else g.transform.localScale=Vector3.one*radius*2;
  glows.Add(r);follow.Add(flat?null:target);return r;
 }
 void LateUpdate(){
  if(glows.Count==0)return;if(!eye){var cam=Camera.main;eye=cam?cam.transform:null;}
  float a=Mathf.Lerp(MinAlpha,MaxAlpha,(Mathf.Sin(Time.time*Pulse*2*Mathf.PI)+1)*.5f);var tint=Tint(held?held.Kind:FieldInvestigationTool.Method.Forceps);tint.a=a;
  if(material)material.color=tint;
  for(int i=0;i<glows.Count;i++){if(!glows[i])continue;if(follow[i]){if(!follow[i].gameObject.activeInHierarchy){glows[i].enabled=false;continue;}glows[i].enabled=true;glows[i].transform.position=follow[i].position+Vector3.up*.015f;if(eye)glows[i].transform.rotation=Quaternion.LookRotation(glows[i].transform.position-eye.position);}}
 }
 // A collected or developed target stops glowing without waiting for the tool to change hands.
 public void Refresh(){if(held)Rebuild();}
 void Clear(){foreach(var r in glows)if(r)Destroy(r.gameObject);glows.Clear();follow.Clear();}
 Material Material(){if(material)return material;material=DemoUi.UnlitMaterial(Color.white,3005);material.mainTexture=Disc();return material;}
 static Texture2D Disc(){
  if(disc)return disc;disc=new Texture2D(64,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};var px=new Color32[64*64];
  for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Mathf.Sqrt((x-31.5f)*(x-31.5f)+(y-31.5f)*(y-31.5f))/31.5f;byte a=(byte)(255*Mathf.Clamp01(1-d)*Mathf.Clamp01(1-d));px[y*64+x]=new Color32(255,255,255,a);}
  disc.SetPixels32(px);disc.Apply();return disc;
 }
 static Mesh Quad(){
  if(quad)return quad;quad=new Mesh{name="GlowQuad"};quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
  quad.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};quad.triangles=new[]{0,2,1,0,3,2,0,1,2,0,2,3};quad.RecalculateBounds();return quad;
 }
 void OnDestroy(){if(material)Destroy(material);}
}
