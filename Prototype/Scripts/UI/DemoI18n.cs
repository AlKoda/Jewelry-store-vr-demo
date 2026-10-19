using UnityEngine;
using System;
using System.Collections.Generic;

// Translate once at the presentation boundary; retain English source strings for
// reliable language switching and keep keyboard bindings unchanged.
public static class DemoI18n
{
    static readonly Dictionary<GUIStyle,GUIStyle> rtl=new Dictionary<GUIStyle,GUIStyle>();
    static readonly Dictionary<Tuple<GUIStyle,int>,GUIStyle> sizes=new Dictionary<Tuple<GUIStyle,int>,GUIStyle>();
    static GUIStyle Style(GUIStyle source,string text)
    {
        if(!DemoLanguage.Arabic||!DemoLanguage.HasArabic(text))return source;
        if(rtl.TryGetValue(source,out var saved))return saved;
        var style=new GUIStyle(source);if(DemoLanguage.Font)style.font=DemoLanguage.Font;
        if(style.alignment==TextAnchor.UpperLeft)style.alignment=TextAnchor.UpperRight;
        if(style.alignment==TextAnchor.MiddleLeft)style.alignment=TextAnchor.MiddleRight;
        if(style.alignment==TextAnchor.LowerLeft)style.alignment=TextAnchor.LowerRight;
        if(rtl.Count>512)rtl.Clear();return rtl[source]=style;
    }
    static GUIStyle Fit(Rect rect,string text,GUIStyle source,bool button=false)
    {
        if(string.IsNullOrEmpty(text))return source;
        int size=source.fontSize>0?source.fontSize:14;
        float available=Mathf.Max(1,rect.width-source.padding.horizontal-4);
        float measured=source.CalcSize(new GUIContent(text)).x-source.padding.horizontal;
        // Fixed icon labels and buttons must stay inside their own hit rectangle.
        if((!source.wordWrap||button)&&measured>available)size=Mathf.Max(9,Mathf.FloorToInt(size*available/measured));
        if(size==source.fontSize&&(!button||!source.wordWrap))return source;
        var key=Tuple.Create(source,button?-size:size);if(sizes.TryGetValue(key,out var cached))return cached;
        var fit=new GUIStyle(source){fontSize=size};if(button)fit.wordWrap=false;
        if(sizes.Count>512)sizes.Clear();return sizes[key]=fit;
    }
    static GUIContent Content(GUIContent value)=>new GUIContent(DemoLanguage.Text(value.text),value.image,DemoLanguage.Text(value.tooltip));
    public static void Label(Rect r,string text,GUIStyle style){style=Fit(r,DemoLanguage.Text(text),Style(style,DemoLanguage.Logical(text)));GUI.Label(r,DemoLanguage.Wrap(text,r.width,style),style);}
    public static void Label(Rect r,GUIContent content,GUIStyle style){var c=Content(content);style=Fit(r,c.text,Style(style,c.text));c.text=DemoLanguage.Wrap(content.text,r.width,style);GUI.Label(r,c,style);}
    public static void Label(Rect r,string text)=>Label(r,text,GUI.skin.label);
    public static bool Button(Rect r,string text,GUIStyle style){string visual=DemoLanguage.Text(text);return GUI.Button(r,visual,Fit(r,visual,Style(style,visual),true));}
    public static bool Button(Rect r,GUIContent text,GUIStyle style){var c=Content(text);return GUI.Button(r,c,Fit(r,c.text,Style(style,c.text),true));}
    public static bool Button(Rect r,string text)=>Button(r,text,GUI.skin.button);
    public static void LayoutLabel(string text,GUIStyle style,params GUILayoutOption[] options){var c=new GUIContent(DemoLanguage.Text(text));style=Style(style,c.text);var rect=GUILayoutUtility.GetRect(c,style,options);Label(rect,text,style);}
    public static void LayoutLabel(GUIContent text,GUIStyle style,params GUILayoutOption[] options){style=Style(style,DemoLanguage.Logical(text.text));var rect=GUILayoutUtility.GetRect(Content(text),style,options);Label(rect,text,style);}
    public static void LayoutLabel(string text,params GUILayoutOption[] options)=>LayoutLabel(text,GUI.skin.label,options);
    public static bool LayoutButton(string text,GUIStyle style,params GUILayoutOption[] options)=>LayoutButton(new GUIContent(text),style,options);
    public static bool LayoutButton(GUIContent text,GUIStyle style,params GUILayoutOption[] options){var c=Content(text);style=Style(style,c.text);var rect=GUILayoutUtility.GetRect(c,style,options);return Button(rect,text,style);}
    public static bool LayoutButton(string text,params GUILayoutOption[] options)=>LayoutButton(text,GUI.skin.button,options);
    public static bool LayoutToggle(bool value,string text,GUIStyle style,params GUILayoutOption[] options)=>GUILayout.Toggle(value,DemoLanguage.Text(text),Style(style,DemoLanguage.Logical(text)),options);
}
