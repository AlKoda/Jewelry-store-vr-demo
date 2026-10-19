#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static partial class ArtPreviewBuild {
 static Material Exterior(string name,Vector2 tiling){
 var m=new Material(Shader.Find("Standard"));m.color=Color.white;
 foreach(var pair in new[]{"_MainTex:_Color.jpg","_BumpMap:_Normal.png","_MetallicGlossMap:_MetallicSmoothness.png","_OcclusionMap:_AO.jpg"}){var s=pair.Split(':');m.SetTexture(s[0],AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Exterior/"+name+s[1]));m.SetTextureScale(s[0],tiling);}
 m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_BumpScale",.6f);m.SetFloat("_GlossMapScale",.8f);return Save(name,m);}
 static void EnhanceScene(){
 foreach(string p in Directory.GetFiles(A+"/Exterior")){if(!(AssetImporter.GetAtPath(p.Replace('\\','/')) is TextureImporter t))continue;t.sRGBTexture=p.Contains("_Color");if(p.Contains("_Normal"))t.textureType=TextureImporterType.NormalMap;t.maxTextureSize=1024;t.anisoLevel=4;t.SaveAndReimport();}
 var map=new System.Collections.Generic.Dictionary<string,Material>{{"KitAsphalt",Exterior("AsphaltWet",Vector2.one/3)},{"KitPavement",Exterior("PavementSlab",Vector2.one/2)},{"KitConcrete",Exterior("Concrete",new Vector2(.5f,1))},{"KitRender",Exterior("BrickFacade",Vector2.one/3)},{"KitCladding",Exterior("FacadeStone",Vector2.one/1.2f)}};
 var tape=Save("LitPoliceTape",new Material(Shader.Find("ArtPreview/LitTape")));tape.color=new Color(.75f,.50f,.015f);
 foreach(var r in Required("NightStore").GetComponentsInChildren<Renderer>(true)){
 r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;if(map.ContainsKey(m.name))return map[m.name];if(m.shader.name=="CrimeScene/Tape")return tape;if(m.shader.name.Contains("Glass") || m.name=="KitGlass")return m.name.Contains("Edge")?mats["FreshCutEdges"]:mats["LowIronGlass"];if(m.name=="NightHeadlampLens"){var lens=new Material(m);lens.color=new Color(1,.83f,.63f);lens.SetColor("_EmissionColor",new Color(1,.83f,.63f)*.7f);return Save("DimHeadlampLens",lens);}return m;}).ToArray();}
 var station=Object.FindFirstObjectByType<ToolStation>();if(station&&station.TapePrefab){var path=AssetDatabase.GetAssetPath(station.TapePrefab);var g=PrefabUtility.LoadPrefabContents(path);foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.shader.name=="CrimeScene/Tape"?tape:m).ToArray();var prefab=PrefabUtility.SaveAsPrefabAsset(g,A+"/Generated/LitSceneTape.prefab");PrefabUtility.UnloadPrefabContents(g);station.TapePrefab=prefab.GetComponent<SceneTape>();}
 int headlights=0;foreach(var l in Required("NightStore/Lighting").GetComponentsInChildren<Light>())if(l.name.StartsWith("Headlight_")){l.intensity=2.5f;l.color=new Color(1,.83f,.63f);headlights++;}
 if(headlights!=2)throw new Exception("Expected two headlights");
 var sky=new Material(Shader.Find("Skybox/Panoramic"));sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture>(A+"/Sky/city_night_sky_overcast_1k.hdr"));sky.SetFloat("_Exposure",.35f);RenderSettings.skybox=Save("OvercastNightSky",sky);
 RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.045f,.052f,.065f);RenderSettings.fogDensity=.038f;
 var bank=Object.FindFirstObjectByType<DemoSoundBank>();if(!bank || !bank.FlashlightSwitch)throw new Exception("Switch audio missing");bank.AmbienceVolume=.10f;
 foreach(var w in Object.FindObjectsByType<ShopWalkController>(FindObjectsSortMode.None)){var steps=w.gameObject.AddComponent<PreviewFootsteps>();steps.Clips=Directory.GetFiles(A+"/Audio","footstep_concrete*.ogg").Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p.Replace('\\','/'))).ToArray();if(steps.Clips.Length<3)throw new Exception("Footsteps missing");}
 Debug.Log("ART_REV2_CHECK headlights=2 intensity=2.5 warm tint; neutral glass; lit tape; fog; sky; switch audio; footsteps");
 }
}
#endif
