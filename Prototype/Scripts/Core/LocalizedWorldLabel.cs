using UnityEngine;
// XR hints retain their source string when the UI language is switched in place.
[RequireComponent(typeof(TextMesh))]
public sealed class LocalizedWorldLabel:MonoBehaviour
{
    TextMesh label;string source,last;
    void Awake(){label=GetComponent<TextMesh>();source=label.text;}
    bool arabic;
    // Re-shapes only when the text or the language changed; TextMesh rebuilds its mesh on every assignment.
    void LateUpdate()
    {
        if(!label)return;
        bool changed=label.text!=last;
        if(changed)source=label.text;
        if(!changed&&arabic==DemoLanguage.Arabic)return;
        arabic=DemoLanguage.Arabic;
        last=DemoLanguage.Text(source);
        if(label.text!=last)label.text=last;
        var font=DemoLanguage.Font;
        if(font&&label.font!=font){label.font=font;var r=label.GetComponent<MeshRenderer>();if(r)r.sharedMaterial=font.material;}
    }
}
