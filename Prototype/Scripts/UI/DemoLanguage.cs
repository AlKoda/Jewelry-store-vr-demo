using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// UI language is independent from the permanently Arabic scene signage.
public static class DemoLanguage
{
    [Serializable] public sealed class Pair { public string en,ar; }
    [Serializable] sealed class Catalog { public Pair[] entries,fragments; }
    static readonly Dictionary<string,string> translations=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string,string> visual=new Dictionary<string,string>();
    static readonly Dictionary<string,string> logicalCache=new Dictionary<string,string>();
    static readonly Dictionary<Tuple<string,int,GUIStyle>,string> wrapped=new Dictionary<Tuple<string,int,GUIStyle>,string>();
    static Pair[] replacements;
    static Regex[] patterns;
    static Font font;
    public static event Action Changed;
    public static bool Arabic { get=>PlayerPrefs.GetInt("DemoUiArabic",1)==1; set {if(Arabic==value)return;PlayerPrefs.SetInt("DemoUiArabic",value?1:0);PlayerPrefs.Save();visual.Clear();logicalCache.Clear();wrapped.Clear();Changed?.Invoke();} }
    public static Font Font {get {if(!font)font=Resources.Load<Font>("ArabicUiFont");return font;}}
    public static bool HasArabic(string text)=>!string.IsNullOrEmpty(text)&&Regex.IsMatch(text,"[\u0600-\u06ff\ufb50-\ufeff]");
    static void Load()
    {
        if(replacements!=null)return;
        var asset=Resources.Load<TextAsset>("Arabic");if(!asset){replacements=new Pair[0];patterns=new Regex[0];return;}
        var catalog=JsonUtility.FromJson<Catalog>(asset.text);
        foreach(var p in catalog.entries)translations[p.en]=p.ar;
        replacements=catalog.entries.Concat(catalog.fragments).OrderByDescending(p=>p.en.Length).ToArray();
        patterns=replacements.Select(pair=>new Regex((char.IsLetterOrDigit(pair.en[0])?"(?<![A-Za-z])":"")+Regex.Escape(pair.en)+(char.IsLetterOrDigit(pair.en[pair.en.Length-1])?"(?![A-Za-z])":""),RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)).ToArray();
    }
    public static string Logical(string text,bool force=false)
    {
        if((!Arabic&&!force)||string.IsNullOrEmpty(text))return text??"";
        // Preserve attribution, licence identifiers and source URLs verbatim.
        if(text.Contains("https://")||text.Contains("CC0")||text.Contains("CC BY"))return text;
        if(Regex.IsMatch(text,"[\ufb50-\ufeff]"))return text;
        if(logicalCache.TryGetValue(text,out var cached))return cached;
        string original=text;Load();text=Regex.Replace(text,"<[^>]*>","");
        if(translations.TryGetValue(text,out string result)){logicalCache[original]=result;return result;}
        for(int i=0;i<replacements.Length;i++){string ar=replacements[i].ar;text=patterns[i].Replace(text,_=>ar);}
        if(logicalCache.Count>4096)logicalCache.Clear();logicalCache[original]=text;return text;
    }
    public static string MapText(string text)=>Shape(Logical(text,true));
    public static string Text(string text)
    {
        if(!Arabic||string.IsNullOrEmpty(text))return text??"";
        if(Regex.IsMatch(text,"[\ufb50-\ufeff]"))return text;
        if(visual.TryGetValue(text,out var saved))return saved;
        string result=string.Join("\n",Logical(text).Split('\n').Select(Shape));
        if(visual.Count>1024)visual.Clear();visual[text]=result;return result;
    }
    public static string Wrap(string text,float width,GUIStyle style)
    {
        if(!Arabic||!style.wordWrap||!HasArabic(Logical(text))||Regex.IsMatch(text??"","[\ufb50-\ufeff]"))return Text(text);
        var key=Tuple.Create(text,Mathf.RoundToInt(width),style);if(wrapped.TryGetValue(key,out var cached))return cached;
        var lines=new List<string>();
        foreach(string paragraph in Logical(text).Split('\n')){
            string current="";
            foreach(string word in paragraph.Split(' ')){string next=current.Length==0?word:current+" "+word;
                if(current.Length>0&&style.CalcSize(new GUIContent(Shape(next))).x>width){lines.Add(Shape(current));current=word;}else current=next;
            }lines.Add(Shape(current));
        }if(wrapped.Count>2048)wrapped.Clear();return wrapped[key]=string.Join("\n",lines);
    }
    // Joining forms are generated from Unicode decomposition data; Latin/keycap
    // runs retain their order within the right-to-left paragraph.
    static readonly Dictionary<int,int[]> Forms=new Dictionary<int,int[]> {
        {1649,new int[]{64336,64337,0,0}},
{1659,new int[]{64338,64339,64340,64341}},
{1662,new int[]{64342,64343,64344,64345}},
{1664,new int[]{64346,64347,64348,64349}},
{1658,new int[]{64350,64351,64352,64353}},
{1663,new int[]{64354,64355,64356,64357}},
{1657,new int[]{64358,64359,64360,64361}},
{1700,new int[]{64362,64363,64364,64365}},
{1702,new int[]{64366,64367,64368,64369}},
{1668,new int[]{64370,64371,64372,64373}},
{1667,new int[]{64374,64375,64376,64377}},
{1670,new int[]{64378,64379,64380,64381}},
{1671,new int[]{64382,64383,64384,64385}},
{1677,new int[]{64386,64387,0,0}},
{1676,new int[]{64388,64389,0,0}},
{1678,new int[]{64390,64391,0,0}},
{1672,new int[]{64392,64393,0,0}},
{1688,new int[]{64394,64395,0,0}},
{1681,new int[]{64396,64397,0,0}},
{1705,new int[]{64398,64399,64400,64401}},
{1711,new int[]{64402,64403,64404,64405}},
{1715,new int[]{64406,64407,64408,64409}},
{1713,new int[]{64410,64411,64412,64413}},
{1722,new int[]{64414,64415,0,0}},
{1723,new int[]{64416,64417,64418,64419}},
{1728,new int[]{64420,64421,0,0}},
{1729,new int[]{64422,64423,64424,64425}},
{1726,new int[]{64426,64427,64428,64429}},
{1746,new int[]{64430,64431,0,0}},
{1747,new int[]{64432,64433,0,0}},
{1709,new int[]{64467,64468,64469,64470}},
{1735,new int[]{64471,64472,0,0}},
{1734,new int[]{64473,64474,0,0}},
{1736,new int[]{64475,64476,0,0}},
{1655,new int[]{64477,0,0,0}},
{1739,new int[]{64478,64479,0,0}},
{1733,new int[]{64480,64481,0,0}},
{1737,new int[]{64482,64483,0,0}},
{1744,new int[]{64484,64485,64486,64487}},
{1609,new int[]{65263,65264,64488,64489}},
{1740,new int[]{64508,64509,64510,64511}},
{1569,new int[]{65152,0,0,0}},
{1570,new int[]{65153,65154,0,0}},
{1571,new int[]{65155,65156,0,0}},
{1572,new int[]{65157,65158,0,0}},
{1573,new int[]{65159,65160,0,0}},
{1574,new int[]{65161,65162,65163,65164}},
{1575,new int[]{65165,65166,0,0}},
{1576,new int[]{65167,65168,65169,65170}},
{1577,new int[]{65171,65172,0,0}},
{1578,new int[]{65173,65174,65175,65176}},
{1579,new int[]{65177,65178,65179,65180}},
{1580,new int[]{65181,65182,65183,65184}},
{1581,new int[]{65185,65186,65187,65188}},
{1582,new int[]{65189,65190,65191,65192}},
{1583,new int[]{65193,65194,0,0}},
{1584,new int[]{65195,65196,0,0}},
{1585,new int[]{65197,65198,0,0}},
{1586,new int[]{65199,65200,0,0}},
{1587,new int[]{65201,65202,65203,65204}},
{1588,new int[]{65205,65206,65207,65208}},
{1589,new int[]{65209,65210,65211,65212}},
{1590,new int[]{65213,65214,65215,65216}},
{1591,new int[]{65217,65218,65219,65220}},
{1592,new int[]{65221,65222,65223,65224}},
{1593,new int[]{65225,65226,65227,65228}},
{1594,new int[]{65229,65230,65231,65232}},
{1601,new int[]{65233,65234,65235,65236}},
{1602,new int[]{65237,65238,65239,65240}},
{1603,new int[]{65241,65242,65243,65244}},
{1604,new int[]{65245,65246,65247,65248}},
{1605,new int[]{65249,65250,65251,65252}},
{1606,new int[]{65253,65254,65255,65256}},
{1607,new int[]{65257,65258,65259,65260}},
{1608,new int[]{65261,65262,0,0}},
{1610,new int[]{65265,65266,65267,65268}},
{0xfefb,new int[]{0xfefb,0xfefc,0,0}},{0xfef7,new int[]{0xfef7,0xfef8,0,0}},{0xfef9,new int[]{0xfef9,0xfefa,0,0}},{0xfef5,new int[]{0xfef5,0xfef6,0,0}}
    };
    static bool Mark(char c)=>c>='\u064b'&&c<='\u065f';
    public static string Shape(string logical)
    {
        if(!HasArabic(logical)||Regex.IsMatch(logical,"[\ufb50-\ufeff]"))return logical;
        logical=logical.Replace("لا","\ufefb").Replace("لأ","\ufef7").Replace("لإ","\ufef9").Replace("لآ","\ufef5");
        var chars=logical.ToCharArray();var shaped=(char[])chars.Clone();
        for(int i=0;i<chars.Length;i++)if(Forms.TryGetValue(chars[i],out var f)){
            int p=i-1,n=i+1;while(p>=0&&Mark(chars[p]))p--;while(n<chars.Length&&Mark(chars[n]))n++;
            bool before=p>=0&&Forms.TryGetValue(chars[p],out var prev)&&prev[2]!=0&&f[1]!=0;
            bool after=n<chars.Length&&Forms.TryGetValue(chars[n],out var next)&&f[2]!=0&&next[1]!=0;
            int index=before?(after?3:1):(after?2:0);shaped[i]=(char)(f[index]!=0?f[index]:f[0]);
        }
        Array.Reverse(shaped);
        for(int i=0;i<shaped.Length;i++)shaped[i]=shaped[i]=='('?')':shaped[i]==')'?'(':shaped[i]=='['?']':shaped[i]==']'?'[':shaped[i];
        return Regex.Replace(new string(shaped),"[A-Za-z0-9\u0660-\u0669](?:[A-Za-z0-9\u0660-\u0669 .:/_+\\-]*[A-Za-z0-9\u0660-\u0669])?",m=>new string(m.Value.Reverse().ToArray()));
    }
}
