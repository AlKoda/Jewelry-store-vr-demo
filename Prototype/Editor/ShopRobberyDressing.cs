#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

public static class ShopRobberyDressing
{
    public static void Apply()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        Transform intact=store.Find("IntactOverlay");
        Transform robbed=Group("RobberyDressing",store);
        var pads=new List<Transform>(); var stands=new List<Transform>(); var jewels=new List<Transform>();
        foreach(Transform t in store.GetComponentsInChildren<Transform>(true))
        {
            if(t.name.StartsWith("JewelryPad_")) pads.Add(t);
            else if(t.name=="NecklaceBust" || t.name=="RingStand") stands.Add(t);
            else if(t.name=="JewelryLoop") jewels.Add(t);
        }
        foreach(Transform t in pads) t.SetParent(intact,true);
        foreach(Transform t in stands) t.SetParent(intact,true);
        foreach(Transform t in jewels) t.SetParent(intact,true);
        for(int i=0;i<pads.Count;i++)
        {
            Transform copy=Copy(pads[i],robbed,"EmptyPad_"+i);
            copy.Rotate(0,(i%3-1)*17,0,Space.World);
            if(i%4==2) copy.position+=new Vector3(.13f,0,.08f);
        }
        for(int i=0;i<stands.Count;i++)
        {
            if(i%3==2) continue;
            Transform copy=Copy(stands[i],robbed,"AbandonedStand_"+i);
            if(i%3==0)
            {
                copy.rotation=Quaternion.Euler(82,19+i*23,12);
                copy.position+=new Vector3(.10f,0,.06f);
                Seat(copy,.905f); copy.name="OverturnedStand_"+i;
            }
        }
        foreach(int i in new int[]{1,5,9})
        {
            Transform copy=Copy(jewels[i],robbed,"RemainingJewelry_"+i);
            copy.rotation=Quaternion.Euler(90,0,15+i*13); Seat(copy,.91f);
        }
        Vector3[] dropped={new Vector3(-.75f,0,2.35f),new Vector3(2.8f,0,5.12f)};
        for(int i=0;i<dropped.Length;i++)
        {
            Transform copy=Copy(jewels[i],robbed,"DroppedJewelry_"+i);
            copy.position=dropped[i];copy.rotation=Quaternion.Euler(90,0,35+i*63);Seat(copy,.008f);
        }
        Transform fallen=Copy(stands[0],robbed,"FallenStand");
        fallen.position=new Vector3(-.8f,0,4.95f);fallen.rotation=Quaternion.Euler(93,31,14);Seat(fallen,.008f);
        Transform fallenPad=Copy(pads[0],robbed,"FallenPad");
        fallenPad.position=new Vector3(-.68f,0,5.16f);fallenPad.rotation=Quaternion.Euler(0,28,0);Seat(fallenPad,.008f);
        Material lining=AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/InteriorGenerated/DisplayVelvet.mat");
        Material oak=AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/InteriorGenerated/SmokedOak.mat");
        Transform tray=Group("RansackedTray",robbed);tray.position=new Vector3(-.83f,.57f,3.4f);
        Box("Bottom",tray,Vector3.zero,new Vector3(.48f,.025f,.64f),oak);
        Box("EmptyInsert",tray,new Vector3(0,.018f,0),new Vector3(.43f,.008f,.59f),lining);
        Box("Front",tray,new Vector3(.23f,.055f,0),new Vector3(.035f,.10f,.64f),oak);
        for(int sign=-1;sign<=1;sign+=2)
            Box("Side",tray,new Vector3(0,.045f,sign*.305f),new Vector3(.48f,.075f,.025f),oak);
        var state=store.GetComponent<CrimeSceneState>();
        var objects=new List<GameObject>(state.RobbedOnly);objects.Add(robbed.gameObject);state.RobbedOnly=objects.ToArray();
        state.SetIntact(false);AssetDatabase.SaveAssets();
    }
    private static Transform Copy(Transform source,Transform parent,string name)
    {
        Transform copy=Object.Instantiate(source,parent);
        copy.name=name;copy.position=source.position;copy.rotation=source.rotation;copy.gameObject.SetActive(true);
        foreach(Collider collider in copy.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        return copy;
    }
    private static void Seat(Transform item,float surface)
    {
        Bounds bounds=item.GetComponentInChildren<Renderer>().bounds;
        foreach(Renderer renderer in item.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        item.position+=Vector3.up*(surface-bounds.min.y);
    }
    public static void Check()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        Transform dressing=store.Find("RobberyDressing");
        DemoValidation.Check(dressing!=null && dressing.gameObject.activeSelf,"Robbed dressing visible at startup");
        DemoValidation.Check(dressing.Find("FallenStand")!=null && dressing.Find("DroppedJewelry_1")!=null,"Fallen stand and dropped jewelry present");
        DemoValidation.Check(dressing.GetComponentsInChildren<Collider>().Length==0,"Robbery dressing adds no walking obstacles");
        int triangles=0;
        foreach(MeshFilter f in dressing.GetComponentsInChildren<MeshFilter>()) triangles+=f.sharedMesh.triangles.Length/3;
        DemoValidation.Check(triangles<3000,"Robbery dressing below 3000 triangles");
        var state=store.GetComponent<CrimeSceneState>();state.SetIntact(true);
        DemoValidation.Check(!dressing.gameObject.activeSelf && store.Find("IntactOverlay").gameObject.activeSelf,"Intact mode restores tidy stock and hides mess");
        state.ShowRobbed();
        DemoValidation.Check(dressing.gameObject.activeSelf && !store.Find("IntactOverlay").gameObject.activeSelf,"Robbed mode restores disturbed stock");
        DemoValidation.Check(!DemoValidation.Find<DemoDesktopPanel>().Visible,"Presenter menu starts closed");
        var panel=DemoValidation.Find<DemoDesktopPanel>();
        var map=DemoValidation.Find<DemoOverviewMap>();
        DemoValidation.Check(!map.ShouldRender,"Hidden overview skips automatic rendering");
        panel.Visible=true;
        DemoValidation.Check(map.ShouldRender,"Overview rendering resumes when menu opens");
        panel.Visible=false;
        DemoValidation.Info("Robbery dressing triangles="+triangles+"; jewelry remaining=3; dropped=2");
    }
}
#endif
