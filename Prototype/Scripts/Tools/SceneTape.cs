using System.Globalization;
using UnityEngine;

// Visible ribbon geometry; no collision, physics or maximum-span restriction.
// Barrier tape stands on edge between posts; the measuring tape lies flat
// between two reels, with end caps and a label reading the span.
public sealed class SceneTape : MonoBehaviour
{
    public Transform StartAnchor;
    public Transform EndAnchor;
    public Transform Ribbon;
    public TextMesh FrontWarning;
    public TextMesh BackWarning;
    // Measuring tape only: caps at the anchors and the distance text above the middle.
    public Transform StartCap;
    public Transform EndCap;
    public TextMesh DistanceLabel;
    // Lies on its width like a measuring tape instead of standing on edge like barrier tape.
    public bool Flat;
    [Min(0.01f)] public float RibbonHeight = 0.08f;
    [Min(0.001f)] public float Thickness = 0.004f;

    public bool ShowsDistance => DistanceLabel != null;

    private MaterialPropertyBlock block;
    private Vector3 lastStart,lastEnd;
    private bool refreshed;
    private void LateUpdate() { Refresh(); FaceViewer(); }

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
        bool visible = length > 0.001f;
        Ribbon.gameObject.SetActive(visible);
        if (StartCap != null) StartCap.gameObject.SetActive(visible);
        if (EndCap != null) EndCap.gameObject.SetActive(visible);
        if (DistanceLabel != null) DistanceLabel.gameObject.SetActive(visible);
        if (!visible) return;
        Ribbon.position = (StartAnchor.position + EndAnchor.position) * 0.5f;
        Vector3 forward = delta / length;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f
            ? Vector3.right : Vector3.up;
        Ribbon.rotation = Quaternion.LookRotation(forward, up);
        Ribbon.localScale = Flat ? new Vector3(RibbonHeight, Thickness, length) : new Vector3(Thickness, RibbonHeight, length);
        if(block==null) block=new MaterialPropertyBlock();
        block.SetFloat("_Span",length);
        Ribbon.GetComponent<Renderer>().SetPropertyBlock(block);
        if (StartCap != null) StartCap.SetPositionAndRotation(StartAnchor.position + forward * 0.01f, Ribbon.rotation);
        if (EndCap != null) EndCap.SetPositionAndRotation(EndAnchor.position - forward * 0.01f, Ribbon.rotation);
        if (DistanceLabel != null)
        {
            // Invariant culture: the check and the review read "1.50 m" on any PC locale.
            DistanceLabel.text = length.ToString("F2", CultureInfo.InvariantCulture) + " m";
            DistanceLabel.transform.position = Ribbon.position + Ribbon.up * ((Flat ? Thickness : RibbonHeight) * 0.5f + 0.015f);
        }
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

    // The single-sided distance text turns (yaw only, so it stays upright) toward
    // the main camera, which is the desktop view or the headset.
    private void FaceViewer()
    {
        if (DistanceLabel == null || !DistanceLabel.gameObject.activeInHierarchy) return;
        Camera view = Camera.main;
        if (view == null) return;
        Vector3 toLabel = DistanceLabel.transform.position - view.transform.position;
        toLabel.y = 0;
        if (toLabel.sqrMagnitude > 0.0001f) DistanceLabel.transform.rotation = Quaternion.LookRotation(toLabel);
    }
}
