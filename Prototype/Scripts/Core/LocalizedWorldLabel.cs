using UnityEngine;
// XR hints retain their source string when the UI language is switched in place.
[RequireComponent(typeof(TextMesh))]
public sealed class LocalizedWorldLabel:MonoBehaviour
{
    TextMesh label;string source,last;
    void Awake(){label=GetComponent<TextMesh>();source=label.text;}
    void LateUpdate(){if(!label)return;if(label.text!=last)source=label.text;last=DemoLanguage.Text(source);label.text=last;if(DemoLanguage.Font)label.font=DemoLanguage.Font;}
}
