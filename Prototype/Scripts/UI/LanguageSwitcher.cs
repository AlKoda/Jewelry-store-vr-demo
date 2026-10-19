using UnityEngine;
public sealed class LanguageSwitcher:MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Init(){if(FindAnyObjectByType<LanguageSwitcher>())return;var go=new GameObject("UI language");DontDestroyOnLoad(go);go.AddComponent<LanguageSwitcher>();}
    void Update(){if(Input.GetKeyDown(KeyCode.F7))DemoLanguage.Arabic=!DemoLanguage.Arabic;}
    void OnGUI(){var panel=FindAnyObjectByType<DemoDesktopPanel>();if(panel&&((!panel.HudVisible&&panel.Layout==PanelLayout.Hidden)||panel.Layout==PanelLayout.Full||panel.HelpOpen))return;
        var previous=GUI.matrix;GUI.matrix=Matrix4x4.identity;
        var style=new GUIStyle(GUI.skin.button){font=DemoLanguage.Font,fontSize=17,alignment=TextAnchor.MiddleCenter};
        string label=DemoLanguage.Arabic?"English  ·  F7":DemoLanguage.Shape("العربية")+"  ·  F7";
        if(GUI.Button(new Rect(Screen.width-165,12,150,34),label,style))DemoLanguage.Arabic=!DemoLanguage.Arabic;GUI.matrix=previous;
    }
}
