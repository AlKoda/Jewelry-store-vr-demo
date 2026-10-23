#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 static Material GlassFinish(string name,float opacity,float polish,float wear){
  var m=RefinedGlassMaterial(name,opacity,polish);m.SetFloat("_MicroVariation",.009f);m.SetFloat("_WearStrength",wear);
  if(wear>0){m.SetTexture("_WearMap",DemoAssetLibrary.Texture("PBRInterior","GlassSmudge_ColorAlpha.png"));m.SetTextureScale("_WearMap",Vector2.one/.6f);m.EnableKeyword("_GLASS_WEAR");}else m.DisableKeyword("_GLASS_WEAR");
  EditorUtility.SetDirty(m);return m;
 }
 static void RefineShopGlassFamily(){
  var clean=GlassFinish("PolishedRetailGlass",.13f,.968f,.32f);
  var window=GlassFinish("WeatheredWindowGlass",.14f,.935f,.24f);
  var broken=GlassFinish("FreshFracturedGlassFaces",.14f,.94f,.12f);
  var edge=GlassFinish("PolishedGlassCutEdges",.235f,.84f,0);edge.SetFloat("_DetailFade",1);
  var floor=GlassFinish("LooseGlassShardFaces",.105f,.935f,0);
  var small=GlassFinish("SmallPropClearGlass",.13f,.96f,0);
  var fractureEdge=Mat(Folder,"LooseGlassShardEdges",new Color(.85f,.87f,.88f,.18f),.74f);fractureEdge.shader=Shader.Find("CrimeScene/FractureGlint");fractureEdge.SetFloat("_Smoothness",.74f);fractureEdge.SetFloat("_DiffuseScatter",.35f);
  var seam=Mat(Folder,"CaseGlassFractureSeams",new Color(.72f,.75f,.76f,.28f),.59f);seam.shader=Shader.Find("CrimeScene/FractureGlint");seam.SetFloat("_Smoothness",.59f);
  int bevelled=0;
  foreach(var f in root.Find("Showroom").GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Glass"||f.name.StartsWith("Glass_")).ToArray()){
   var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!r.sharedMaterials.Any(m=>m&&m.shader.name=="CrimeScene/DisplayGlass"))continue;
   f.sharedMesh=RecordingGlass.PolishedPaneAssembly("PolishedRetailPane_"+bevelled,f.sharedMesh,out int panes);r.sharedMaterials=new[]{clean,edge};r.shadowCastingMode=ShadowCastingMode.Off;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;bevelled+=panes;
  }
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
   if(PathOf(r.transform).Contains("/Exterior/PoliceCar/"))continue;
   var mats=r.sharedMaterials;bool changed=false;for(int i=0;i<mats.Length;i++){
    var m=mats[i];if(!m)continue;
    if(m.shader.name=="CrimeScene/DisplayGlass"){
     if(m.name.Contains("Edges")||m.name.Contains("Cut"))continue;
     mats[i]=m.name=="SmallPropClearGlass"?small:PathOf(r.transform).Contains("/Facade/")?window:PathOf(r.transform).Contains("GlassDebris")||r.name.StartsWith("GroundGlass")?floor:clean;changed=true;
    }else if(m.shader.name=="Standard"&&m.name.StartsWith("Glass_")&&r.transform.position.z>0&&Mathf.Abs(r.transform.position.x)<8.2f){mats[i]=small;changed=true;}
   }if(changed)r.sharedMaterials=mats;
  }
  int caseIndex=0,paneCount=0;
  foreach(string name in new[]{"CentralCase_Left","CentralCase_Right","RightIsland_Front"}){
   var fixture=root.Find("Showroom/"+name);var group=Fresh("RefinedCaseGlass",fixture);int side=0;
   var originalShards=fixture.Find("Shards");if(originalShards)foreach(var r in originalShards.GetComponentsInChildren<Renderer>(true))r.enabled=false;
   foreach(var f in fixture.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="Window_Broken"&&f.transform.parent==fixture).ToArray()){
    var b=f.sharedMesh.bounds;var scale=f.transform.localScale;var position=f.transform.localPosition+f.transform.localRotation*Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),scale);
    var pane=RecordingGlass.ArchitecturalPane("NativeCaseFracture_"+side,group,position,b.size.x*scale.x,b.size.y*scale.y,4510+caseIndex*23+side*3,3,broken,edge,seam);pane.transform.localRotation=f.transform.localRotation;f.gameObject.SetActive(false);side++;paneCount++;
   }
   foreach(var old in fixture.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==fixture&&(t.name=="GroundGlass_Sparse"||t.name=="GroundGlass_WindowFall")))old.gameObject.SetActive(false);
   RecordingGlass.ArchitecturalDebris("CaseDeckChips_"+caseIndex,group,V(0,.713f,-.15f),4611+caseIndex,70,.62f,.05f,floor,fractureEdge,false,2);
   RecordingGlass.ArchitecturalDebris("CaseFloorFragments_"+caseIndex,group,V(0,.0015f,-.60f),4712+caseIndex,140,.60f,.24f,floor,fractureEdge,false,caseIndex==1?1:2);
   foreach(var r in group.GetComponentsInChildren<MeshRenderer>()){r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
   caseIndex++;
  }
  FineGlassPiles();
  var granules=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/FineGranuleFaces.mat");granules.SetFloat("_DiffuseScatter",.15f);granules.color=new Color(.83f,.85f,.86f,.18f);EditorUtility.SetDirty(granules);
  var granuleEdges=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/FineGranuleEdges.mat");granuleEdges.SetFloat("_DiffuseScatter",.35f);EditorUtility.SetDirty(granuleEdges);
  var crumbs=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DenseFineGlassContact.mat");crumbs.SetFloat("_Density",.24f);EditorUtility.SetDirty(crumbs);
  Debug.Log("GLASS_FAMILY_AUTHORED beveled intact panes="+bevelled+" native case fractures="+paneCount+"; separate polished/weathered/fractured/loose glass finishes; compact fine chips; no new lights or physics");
 }
 static void GlassFamilyChecks(){
  int intact=0;foreach(var f in root.Find("Showroom").GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Glass"||f.name.StartsWith("Glass_"))){var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!r.sharedMaterials.Any(m=>m&&m.name=="PolishedRetailGlass"))continue;if(f.sharedMesh.subMeshCount!=2||f.sharedMesh.triangles.Length<=36)throw new Exception("Intact pane has no beveled thickness");intact++;}
  if(intact<10)throw new Exception("Intact retail glass missing");
  foreach(string name in new[]{"CentralCase_Left","CentralCase_Right","RightIsland_Front"}){
   var fixture=root.Find("Showroom/"+name);var g=fixture.Find("RefinedCaseGlass");if(!g||g.GetComponentsInChildren<Collider>().Length>0||g.GetComponentsInChildren<Light>().Length>0)throw new Exception("Case glass missing or adds physics/lights");
   if(fixture.GetComponentsInChildren<Transform>().Any(t=>t.name=="Window_Broken"||t.name=="GroundGlass_Sparse"||t.name=="GroundGlass_WindowFall"))throw new Exception("Legacy stretched case glass still visible");
   var old=fixture.Find("Shards");if(old&&old.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled))throw new Exception("Original flat case shards remain visible");
   foreach(var f in g.GetComponentsInChildren<MeshFilter>())if(!f.GetComponent<MeshRenderer>().sharedMaterials.All(m=>m.shader.name=="CrimeScene/GlassContact")&&f.sharedMesh.normals.Any(n=>n.sqrMagnitude<.5f))throw new Exception("Invalid new glass normal: "+f.name);
  }
  var polished=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PolishedRetailGlass.mat");if(!polished.IsKeywordEnabled("_GLASS_WEAR")||!polished.GetTexture("_WearMap")||polished.color.a<.1f)throw new Exception("Photographic wear or stronger intact glass missing");
  Debug.Log("GLASS_FAMILY_CHECKS_PASS beveled intact panes="+intact+"; three independently fractured cases, native proportions, closed chips/splinters; original stretched panes inactive; map-driven non-emissive dielectric finishes");
 }
}
#endif
