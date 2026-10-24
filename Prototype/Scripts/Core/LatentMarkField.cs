using System.Collections.Generic;
using UnityEngine;

// Latent marks are not where a designer put them: a burglar touches display cases anywhere. This
// field sits on a case top (a trigger BoxCollider over the glass) and, when the scene starts, seeds
// 0..MaxMarks hidden marks on the top face from its Seed. Some are smudges without ridge detail.
// A brush or magnetic wand swept over the glass develops the marks under its tip
// (FieldInvestigationTool.BrushStroke asks MarkAt); the marks register themselves as runtime
// clues so they can be photographed, lifted and recorded like the authored ones.
[RequireComponent(typeof(BoxCollider))]
public sealed class LatentMarkField : MonoBehaviour {
 public EvidenceRegister Register;
 public int Seed;
 public int MaxMarks=3;
 [Range(0,1)] public float SmudgeShare=.4f;
 public Material RidgeMaterial;      // CrimeScene/SurfaceDecal with the ridge atlas; a property block picks the cell
 public Vector2 AtlasCell=new Vector2(.25f,.5f);
 public int AtlasColumns=4,AtlasRows=2;
 public SurfaceClass Surface=SurfaceClass.NonPorousSmooth;
 public string IdPrefix="LM";
 public string PlaceEn="display case",PlaceAr="خزانة العرض";
 public float MarkWidth=.032f,MarkHeight=.042f;
 readonly List<DustablePrint> marks=new List<DustablePrint>();
 public IReadOnlyList<DustablePrint> Marks=>marks;
 public bool Built {get;private set;}
 static Mesh quad;
 void Start(){Build();}
 public void Build(){
  if(Built)return;Built=true;var box=GetComponent<BoxCollider>();var rng=new System.Random(Seed);int count=rng.Next(0,MaxMarks+1);
  for(int i=0;i<count;i++){
   float u=(float)rng.NextDouble()*1.7f-.85f,v=(float)rng.NextDouble()*1.7f-.85f;
   Vector3 local=box.center+new Vector3(box.size.x*.5f*u,box.size.y*.5f+.0008f,box.size.z*.5f*v);
   var g=new GameObject("LatentMark_"+i);g.transform.SetParent(transform,false);g.transform.localPosition=local;g.transform.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);g.layer=gameObject.layer;
   var filter=g.AddComponent<MeshFilter>();filter.sharedMesh=Quad();var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=RidgeMaterial;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=true;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;r.enabled=false;
   g.transform.localScale=new Vector3(MarkWidth,1,MarkHeight);
   int cell=rng.Next(0,AtlasColumns*AtlasRows);var block=new MaterialPropertyBlock();block.SetVector("_MainTex_ST",new Vector4(AtlasCell.x,AtlasCell.y,(cell%AtlasColumns)*AtlasCell.x,1-AtlasCell.y-(cell/AtlasColumns)*AtlasCell.y));r.SetPropertyBlock(block);
   var hit=g.AddComponent<BoxCollider>();hit.isTrigger=true;hit.size=new Vector3(1.6f,.02f/MarkHeight,1.4f);hit.center=Vector3.zero;
   var clue=g.AddComponent<EvidenceClue>();clue.Id=IdPrefix+"-"+i;clue.TitleEn="Developed mark, "+PlaceEn;clue.TitleAr="أثر مطور، "+PlaceAr;
   clue.ObservationEn="A powder-developed contact on the glass. Comparison is still required; it does not identify a person.";clue.ObservationAr="أثر تلامس مطور بالمسحوق على الزجاج. يحتاج إلى مقارنة ولا يحدد شخصًا بمفرده.";
   clue.Register=Register;clue.CanSample=true;clue.RequiredTool=FieldInvestigationTool.Method.LiftCard;clue.ExtraMethods=new[]{FieldInvestigationTool.Method.Tape,FieldInvestigationTool.Method.GelLifter};clue.Surface=Surface;
   var print=g.AddComponent<DustablePrint>();print.Clue=clue;print.Powder=r;print.Smudge=rng.NextDouble()<SmudgeShare;print.ResetPrint();
   if(Register)Register.AddRuntimeClue(clue);marks.Add(print);
  }
 }
 public DustablePrint Nearest(Vector3 point,float radius){
  DustablePrint best=null;float bestDistance=radius;
  foreach(var m in marks){if(!m)continue;float d=Vector3.Distance(m.transform.position,point);if(d<bestDistance){bestDistance=d;best=m;}}
  return best;
 }
 // The mark under a brush contact on any field surface, or null.
 public static DustablePrint MarkAt(RaycastHit hit,float radius){var own=hit.collider.GetComponentInParent<DustablePrint>();if(own)return own;var field=hit.collider.GetComponentInParent<LatentMarkField>();return field?field.Nearest(hit.point,radius):null;}
 static Mesh Quad(){
  if(quad)return quad;quad=new Mesh{name="LatentMarkQuad"};
  quad.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
  quad.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};quad.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};
  quad.triangles=new[]{0,2,1,0,3,2};quad.RecalculateBounds();return quad;
 }
 public void ResetField(){foreach(var m in marks)if(m){m.ResetPrint();}}
}
