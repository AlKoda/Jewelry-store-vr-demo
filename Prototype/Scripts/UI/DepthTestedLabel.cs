using UnityEngine;

// Keeps world labels on the live font atlas while using depth-tested rendering.
[ExecuteAlways, RequireComponent(typeof(TextMesh))]
public sealed class DepthTestedLabel : MonoBehaviour
{
    public Material LabelMaterial;
    private void OnEnable() { Font.textureRebuilt+=OnFontRebuilt; Refresh(); }
    private void OnDisable() { Font.textureRebuilt-=OnFontRebuilt; }
    private void OnFontRebuilt(Font font)
    {
        TextMesh label=GetComponent<TextMesh>();
        if(label!=null && label.font==font) Refresh();
    }
    public void Refresh()
    {
        TextMesh label=GetComponent<TextMesh>();
        if(LabelMaterial==null || label==null || label.font==null) return;
        LabelMaterial.mainTexture=label.font.material.mainTexture;
        GetComponent<MeshRenderer>().sharedMaterial=LabelMaterial;
    }
}
