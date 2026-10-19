#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// User-supplied assets, prepared in Blender at metre scale. Plain clay materials
// are intentional. These are staged scenery, not replacements for working tools.
public static class UserPropDressing
{
    public const string Pack="UserProps";
    public static readonly string[] Names={"camera","flashlight","gloves","cone","evidence_marker","equipment_case","canister","sofa"};
    public static GameObject Place(string name,Transform parent,Vector3 position,float yaw=0)
    {
        GameObject g=DemoAssetLibrary.Place(Pack,name,parent,position,yaw,1,false);
        if(g==null) throw new InvalidOperationException("Missing prepared user prop: "+name);
        var material=Mat(StoreKit.Folder,"UserProps_Clay",new Color(.43f,.47f,.49f),.22f);
        foreach(var r in g.GetComponentsInChildren<MeshRenderer>())
        {
            var mats=new Material[r.sharedMaterials.Length];
            for(int i=0;i<mats.Length;i++) mats[i]=material;
            r.sharedMaterials=mats;
        }
        return g;
    }

    public static void Apply(Transform dressing)
    {
        // Historical snapshots without the user's pack remain usable.
        if(!AssetDatabase.IsValidFolder("Assets/CrimeSceneDemo/ThirdParty/"+Pack)) return;
        Transform staging=Group("UserProps_InteriorStaging",dressing);
        staging.localPosition=V(6.4f,0,2.5f);
        Table(staging);
        var sofa=GameObject.Find(NightStoreLayout.Root)?.transform.Find("Showroom/Sofa");
        if(sofa!=null)
        {
            // Retain the layout hierarchy, but fit collisions to the supplied sofa.
            foreach(var r in sofa.GetComponentsInChildren<MeshRenderer>()) r.enabled=false;
            foreach(var c in sofa.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            DemoAssetLibrary.AddBoundsCollider(Place("sofa",sofa,Vector3.zero,180));
        }
    }

    public static Transform Table(Transform parent)
    {
        Transform table=Group("PreparedEquipment",parent);
        var collider=table.gameObject.AddComponent<BoxCollider>();
        collider.center=V(0,.375f,0);collider.size=V(1.8f,.75f,.70f);
        var top=Mat(StoreKit.Folder,"UserProps_Table",new Color(.16f,.18f,.19f),.25f);
        Box("TableTop",table,V(0,.73f,0),V(1.8f,.04f,.70f),top);
        for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            Box("TableLeg",table,V(x*.78f,.35f,z*.25f),V(.04f,.70f,.04f),top);
        Place("camera",table,V(-.60f,.75f,-.16f),15);
        Place("equipment_case",table,V(.48f,.75f,.04f),0);
        Place("flashlight",table,V(-.32f,.75f,.08f));
        Place("gloves",table,V(.02f,.75f,.10f),0);
        Place("evidence_marker",table,V(.22f,.75f,-.20f));
        Place("cone",table,V(-.60f,0,-.68f));
        Place("canister",table,V(.58f,0,.05f));
        return table;
    }

    public static void Validate(Transform table)
    {
        string[] names={"camera","flashlight","gloves","cone","evidence_marker","equipment_case","canister"};
        float[] maximum={.15f,.24f,.25f,.45f,.12f,.34f,.42f};
        bool sizes=true, surfaces=true;
        for(int i=0;i<names.Length;i++)
        {
            Transform item=table.Find(names[i]);
            if(item==null){sizes=false;continue;}
            Bounds bounds=new Bounds();bool first=true;
            foreach(var filter in item.GetComponentsInChildren<MeshFilter>())
                foreach(Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point=item.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            float largest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            sizes &= Mathf.Abs(largest-maximum[i])<.025f;
            surfaces &= Mathf.Abs(bounds.min.y)<.002f;
        }
        DemoValidation.Check(sizes,"User props have the intended metre dimensions");
        DemoValidation.Check(surfaces,"User prop pivots rest on their support surfaces");
        DemoValidation.Check(table.GetComponent<BoxCollider>()!=null,"Interior equipment table blocks the walker");
    }
}
#endif
