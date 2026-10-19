#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using Object=UnityEngine.Object;

// Scene signage is permanently Arabic. Only interface language is switchable.
public static class ArabicScenePass
{
    const string Folder="Assets/CrimeSceneDemo/CombinedArt";
    static readonly Dictionary<string,string> Signs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
        {"ShopFascia","ShopFascia"},{"FasciaSign","ShopFascia"},{"Closed","Closed"},{"StaffOnly","StaffOnly"},
        {"CollectionRings","CollectionRings"},{"CollectionNecklaces","CollectionNecklaces"},{"SceneTape","SceneTape"},
        {"NightStoreInterior_Color","NightStoreInterior_Color"},{"NightStoreStreet_Color","NightStoreStreet_Color"},
        {"Lightbox_AurelFinchWide","ShopFascia"},{"BrandWordmark","ShopFascia"},{"FireEvacuationPlan","FireEvacuationPlan"},
        {"Calendar_October2026","Calendar"},{"NoticeBoard_StaffCork","NoticeBoard"},{"Certificate_GemmologyDiplomaFramed","Certificate"},
        {"Sign_CCTV","CCTV"},{"Sticker_AlarmMonitored","Alarm"},{"Sticker_InsurerApproved","Insured"},
        {"Flyer_LostCat","MissingCat"},{"Notice_PoliceLine","SceneTape"},{"Newspaper_FrontPage","StoreNotice"}
        ,{"Proc_Signs_Color","StreetSymbols"}
    };
    public static void Apply()
    {
        var root=GameObject.Find("NightStore");var font=DemoLanguage.Font;if(!font)throw new Exception("Arabic UI font resource missing");
        DemoAssetLibrary.Credit("Fonts","Arabic");
        int count=0;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++){
                var old=materials[i];if(!old||!old.HasProperty("_MainTex")||!old.mainTexture||!Signs.TryGetValue(old.mainTexture.name,out var sign))continue;
                var texture=DemoAssetLibrary.Texture("ArabicSceneSigns",sign+".png");if(!texture)throw new Exception("Arabic sign missing: "+sign);
                string path=Folder+"/Arabic_"+old.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(old);AssetDatabase.CreateAsset(material,path);}
                material.mainTexture=texture;if(material.HasProperty("_EmissionMap")&&old.GetTexture("_EmissionMap"))material.SetTexture("_EmissionMap",texture);
                if(material.HasProperty("_BumpMap")){material.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("ArabicSceneSigns","SignWear_Normal.png"));material.SetFloat("_BumpScale",.18f);material.EnableKeyword("_NORMALMAP");}
                materials[i]=material;count++;
            }renderer.sharedMaterials=materials;
        }
        var exit=Mat(Folder,"ArabicExit",Color.white,.35f);exit.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns","Exit.png");exit.EnableKeyword("_EMISSION");exit.SetTexture("_EmissionMap",exit.mainTexture);exit.SetColor("_EmissionColor",Color.white*.4f);exit.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterials.Any(m=>m&&m.name=="KitExitLetters")).ToArray()){
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="ArabicExitFace";go.transform.SetParent(renderer.transform,false);go.transform.localPosition=new Vector3(0,.075f,-.0525f);go.transform.localScale=new Vector3(.33f,.13f,1);Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=exit;r.shadowCastingMode=ShadowCastingMode.Off;StoreKit.MarkStatic(go,true);
        }
        foreach(var label in root.GetComponentsInChildren<TextMesh>(true))MapLabel(label);
        var tape=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ArabicPrintedTape.mat");
        if(!tape){tape=new Material(Shader.Find("CrimeScene/Tape"));AssetDatabase.CreateAsset(tape,Folder+"/ArabicPrintedTape.mat");}
        tape.color=Color.white;tape.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns","SceneTape.png");
        var tapeTexture=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tape.mainTexture));
        if(tapeTexture.wrapMode!=TextureWrapMode.Repeat){tapeTexture.wrapMode=TextureWrapMode.Repeat;tapeTexture.SaveAndReimport();}
        foreach(var station in Object.FindObjectsByType<ToolStation>(FindObjectsSortMode.None)){
            if(!station.TapePrefab)continue;string path=AssetDatabase.GetAssetPath(station.TapePrefab);var contents=PrefabUtility.LoadPrefabContents(path);
            PrintTape(contents.GetComponent<SceneTape>(),tape);
            PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
        }
        foreach(var instance in Object.FindObjectsByType<SceneTape>(FindObjectsSortMode.None))if(!instance.Flat)PrintTape(instance,tape);
        if(count<5)throw new Exception("Arabic scene sign replacement incomplete: "+count);
        Debug.Log("ARABIC_SCENE_PASS permanently Arabic signage/material slots="+count+"; Arabic exits and tool labels; UI independent");
    }
    public static void RebuildPlayer(){DemoAssetLibrary.NormalMap("ArabicSceneSigns","SignWear_Normal.png");ReviewPlayerBuild.Run();}
    static void PrintTape(SceneTape ribbon,Material material)
    {
        if(!ribbon||ribbon.Flat)return;
        if(ribbon.FrontWarning)Object.DestroyImmediate(ribbon.FrontWarning.gameObject);
        if(ribbon.BackWarning)Object.DestroyImmediate(ribbon.BackWarning.gameObject);
        ribbon.FrontWarning=null;ribbon.BackWarning=null;
        ribbon.Ribbon.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void MapLabel(TextMesh label)
    {
        string translated=DemoLanguage.MapText(label.text);if(!DemoLanguage.HasArabic(translated))return;
        label.text=translated;var oldSize=label.characterSize;label.font=DemoLanguage.Font;label.fontSize=64;label.characterSize=oldSize;
        var depth=label.GetComponent<DepthTestedLabel>();if(depth){depth.LabelMaterial=DemoUi.TextMaterial(DemoLanguage.Font);depth.Refresh();}
        else label.GetComponent<MeshRenderer>().sharedMaterial=DemoUi.TextMaterial(DemoLanguage.Font);
    }
}
#endif
