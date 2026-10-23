using UnityEngine;
using UnityEngine.Rendering;

// Soft ambient contact only. Actual white/UV torch shadows still come from the
// skinned meshes. All ten foot patches share one small dynamic mesh/draw.
public sealed class CrowdGroundContacts : MonoBehaviour {
 public Transform[] Feet=new Transform[0],Roots=new Transform[0];
 public Material ContactMaterial;
 Mesh mesh;Vector3[] vertices;Color32[] colors;
 void Start(){RefreshContacts();}
 void LateUpdate(){RefreshContacts();}
 public void RefreshContacts(){
  if(Feet.Length==0||Feet.Length!=Roots.Length||!ContactMaterial)return;
  if(!mesh){
   mesh=new Mesh{name="Crowd foot contact shading"};mesh.MarkDynamic();vertices=new Vector3[Feet.Length*4];colors=new Color32[vertices.Length];
   var uv=new Vector2[vertices.Length];var triangles=new int[Feet.Length*6];
   for(int i=0;i<Feet.Length;i++){
    int k=i*4,t=i*6;uv[k]=new Vector2(0,0);uv[k+1]=new Vector2(1,0);uv[k+2]=new Vector2(1,1);uv[k+3]=new Vector2(0,1);
    triangles[t]=k;triangles[t+1]=k+2;triangles[t+2]=k+1;triangles[t+3]=k;triangles[t+4]=k+3;triangles[t+5]=k+2;
   }
   mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
   var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
   var renderer=GetComponent<MeshRenderer>();if(!renderer)renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=ContactMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
  }
  for(int i=0;i<Feet.Length;i++){
   if(!Feet[i]||!Roots[i])continue;
   var p=Feet[i].position;float height=p.y-Roots[i].position.y;p.y=Roots[i].position.y+.0008f;
   float strength=Mathf.Clamp01(1-Mathf.Max(0,height-.11f)/.12f)*.23f;
   var right=Roots[i].right*.10f;var forward=Roots[i].forward*.16f;int k=i*4;
   vertices[k]=transform.InverseTransformPoint(p-right-forward);vertices[k+1]=transform.InverseTransformPoint(p+right-forward);vertices[k+2]=transform.InverseTransformPoint(p+right+forward);vertices[k+3]=transform.InverseTransformPoint(p-right+forward);
   for(int c=0;c<4;c++)colors[k+c]=new Color32(255,255,255,(byte)(strength*255));
  }
  mesh.vertices=vertices;mesh.colors32=colors;mesh.RecalculateBounds();
 }
 void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
}
