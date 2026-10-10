using UnityEngine;

// Visible ribbon geometry; no collision, physics or maximum-span restriction.
public sealed class SceneTape : MonoBehaviour
{
    public Transform StartAnchor;
    public Transform EndAnchor;
    public Transform Ribbon;
    public TextMesh FrontWarning;
    public TextMesh BackWarning;
    [Min(0.01f)] public float RibbonHeight = 0.08f;
    [Min(0.001f)] public float Thickness = 0.004f;

    private MaterialPropertyBlock block;
    private Vector3 lastStart,lastEnd;
    private bool refreshed;
    private void LateUpdate() { Refresh(); }

    // Re-stretch between the anchors now; LateUpdate does this every frame.
    public void Refresh()
    {
        if (StartAnchor == null || EndAnchor == null ||
            !StartAnchor.gameObject.activeInHierarchy || !EndAnchor.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }
        if (Ribbon == null) return;
        if(refreshed && lastStart==StartAnchor.position && lastEnd==EndAnchor.position) return;
        refreshed=true;lastStart=StartAnchor.position;lastEnd=EndAnchor.position;
        Vector3 delta = EndAnchor.position - StartAnchor.position;
        float length = delta.magnitude;
        Ribbon.gameObject.SetActive(length > 0.001f);
        if (length <= 0.001f) return;
        Ribbon.position = (StartAnchor.position + EndAnchor.position) * 0.5f;
        Vector3 forward = delta / length;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f
            ? Vector3.right : Vector3.up;
        Ribbon.rotation = Quaternion.LookRotation(forward, up);
        Ribbon.localScale = new Vector3(Thickness, RibbonHeight, length);
        if(block==null) block=new MaterialPropertyBlock();
        block.SetFloat("_Span",length);
        Ribbon.GetComponent<Renderer>().SetPropertyBlock(block);
        foreach(TextMesh label in new[]{FrontWarning,BackWarning})
        {
            if(label==null) continue;
            label.gameObject.SetActive(length>.9f);
            float side=label==FrontWarning?-1:1;
            label.transform.position=Ribbon.position+Ribbon.right*(side*Thickness*.65f)-Ribbon.up*Mathf.Min(.035f,length*.012f);
            label.transform.rotation=Ribbon.rotation*Quaternion.Euler(0,side<0?90:-90,0);
            label.characterSize=Mathf.Min(.009f,length*.005f);
        }
    }
}
