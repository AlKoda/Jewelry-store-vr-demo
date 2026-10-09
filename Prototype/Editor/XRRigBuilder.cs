#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Package-free XR rig skeleton: a root with locomotion and bounds, a Head
// placeholder and two hands with HandInteractor. After the XR packages are
// installed, add the XR camera under Head, tracked pose drivers to Head and
// both hands, and bind input actions to the HandInteractor/XRLocomotion methods.
public static class XRRigBuilder
{
    [MenuItem("Crime Scene Demo/Create XR Rig Skeleton")]
    public static void Create()
    {
        if(GameObject.Find("XRRig")!=null)
        {
            EditorUtility.DisplayDialog("Already created","Remove the existing XRRig first.","OK");
            return;
        }
        GameObject rig=new GameObject("XRRig");
        Undo.RegisterCreatedObjectUndo(rig,"Create XR rig skeleton");
        Transform start=GameObject.Find("JewelryStore_Blockout")?.transform.Find("ReferencePoints/SuggestedPlayerStart");
        rig.transform.position=start!=null?start.position:new Vector3(3.65f,0,1.3f);
        rig.transform.rotation=Quaternion.Euler(0,-30,0);

        InteriorBounds bounds=rig.AddComponent<InteriorBounds>();
        Transform head=Child(rig.transform,"Head",new Vector3(0,1.65f,0));
        XRLocomotion locomotion=rig.AddComponent<XRLocomotion>();
        locomotion.Head=head;
        locomotion.Bounds=bounds;

        ToolStation station=Object.FindFirstObjectByType<ToolStation>();
        DemoSession session=Object.FindFirstObjectByType<DemoSession>();
        EvidenceCamera evidence=Object.FindFirstObjectByType<EvidenceCamera>();
        foreach(float side in new[] {-1f,1f})
        {
            HandInteractor hand=Child(rig.transform,side<0?"LeftHand":"RightHand",new Vector3(side*0.2f,1.1f,0.3f))
                .gameObject.AddComponent<HandInteractor>();
            hand.Station=station;
            hand.Session=session;
            hand.EvidenceCamera=evidence;
            hand.Bounds=bounds;
        }
        Selection.activeGameObject=rig;
        Debug.Log("XR rig skeleton created. Add the XR camera and tracked pose drivers after installing the XR packages.",rig);
    }

    private static Transform Child(Transform parent,string name,Vector3 localPosition)
    {
        Transform t=Group(name,parent);
        t.localPosition=localPosition;
        return t;
    }
}
#endif
