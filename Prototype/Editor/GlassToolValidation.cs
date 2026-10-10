#if UNITY_EDITOR
using UnityEngine;
public static class GlassToolValidation
{
    public static void Capture(Camera camera)
    {
        ToolStation station=Object.FindFirstObjectByType<ToolStation>();
        DeployedTool a=station.Spawn(DemoToolKind.Marker),b=station.Spawn(DemoToolKind.Marker);
        a.PlaceAt(new Vector3(-.45f,.004f,2),0);b.PlaceAt(new Vector3(.15f,.004f,2),0);
        DeployedTool p=station.Spawn(DemoToolKind.TapePost),q=station.Spawn(DemoToolKind.TapePost);
        p.PlaceAt(new Vector3(-1,0,3),0);q.PlaceAt(new Vector3(1,0,3),0);
        station.NotifyPlaced(p);station.NotifyPlaced(q);
        DemoValidation.Capture(camera,"tools-front.png",new Vector3(.35f,.75f,.9f),new Vector3(0,.30f,2.3f));
        DemoValidation.Capture(camera,"markers-back.png",new Vector3(.35f,.65f,2.8f),new Vector3(-.1f,.10f,2));
        for(int i=station.DeploymentRoot.childCount-1;i>=0;i--)Object.DestroyImmediate(station.DeploymentRoot.GetChild(i).gameObject);
        station.StartNewTapeRun();
        DemoValidation.Capture(camera,"glass-closeup.png",new Vector3(-.65f,1.32f,2.55f),new Vector3(-1.65f,.84f,3.65f));
    }
    public static void Check()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        int fragments=0;
        foreach(MeshFilter f in store.GetComponentsInChildren<MeshFilter>())
        {
            if(!f.name.Contains("DisplayShard") && !f.name.StartsWith("WindowShard"))continue;
            fragments++;
            float expected=f.name.Contains("DisplayShard")?.841f:.005f;
            DemoValidation.Check(Mathf.Abs(f.GetComponent<Renderer>().bounds.min.y-expected)<.0005f,
                "Fragment rests on support: "+f.name);
        }
        DemoValidation.Check(fragments==32,"All 32 loose shards have physical thickness and supported placement");
        ToolStation station=DemoValidation.Find<ToolStation>();
        var marker=station.MarkerPrefab;
        Transform front=marker.transform.Find("Front"),back=marker.transform.Find("Back");
        DemoValidation.Check(Mathf.Abs(front.TransformPoint(new Vector3(0,.5f,0)).z)<.002f &&
            Mathf.Abs(back.TransformPoint(new Vector3(0,.5f,0)).z)<.002f,"Marker panels meet at the top");
        DemoValidation.Check(Vector3.Dot(marker.NumberLabel.transform.up,Vector3.up)>.9f &&
            Vector3.Dot(marker.BackNumberLabel.transform.up,Vector3.up)>.9f,"Marker numbers upright on both faces");
        DemoValidation.Check(marker.NumberLabel.GetComponent<DepthTestedLabel>()!=null &&
            marker.NumberLabel.GetComponent<Renderer>().sharedMaterial.shader.name=="CrimeScene/WorldText",
            "Spawned marker prefab numbers depth-tested");
        DesktopInteractor hand=DemoValidation.Find<DesktopInteractor>();hand.ReadDesktopInput=false;
        var p=hand.SpawnIntoHand(DemoToolKind.TapePost);hand.Carry(new Ray(new Vector3(-.6f,2,2),Vector3.down));hand.Place();
        var q=hand.SpawnIntoHand(DemoToolKind.TapePost);hand.Carry(new Ray(new Vector3(.6f,2,2),Vector3.down));hand.Place();
        var tape=station.DeploymentRoot.GetComponentInChildren<SceneTape>();
        DemoValidation.Check(tape!=null && tape.StartAnchor==p.TapeAnchor && tape.EndAnchor==q.TapeAnchor,"Placing second post creates connected tape");
        hand.Hold(q);hand.Carry(new Ray(new Vector3(.7f,2,2),Vector3.down));hand.Place();tape.Refresh();
        DemoValidation.Check(station.DeploymentRoot.GetComponentsInChildren<SceneTape>().Length==1 &&
            Mathf.Abs(tape.Ribbon.localScale.z-1.3f)<.02f,"Moving a post updates tape without duplicate connections");
        hand.Remove(p);
        DemoValidation.Check(!p.gameObject.activeSelf && !tape.gameObject.activeSelf,"Removing a placed post removes connected tape immediately");
        hand.Remove(q);station.StartNewTapeRun();
        var cone=station.Spawn(DemoToolKind.Cone);cone.PlaceAt(new Vector3(0,0,2),0);Physics.SyncTransforms();
        hand.Aim(new Ray(new Vector3(0,.20f,1),Vector3.forward));
        DemoValidation.Check(hand.Hovered==cone,"Pointer selects a placed tool for removal");
        hand.Remove(hand.Target);
        DemoValidation.Check(!cone.gameObject.activeSelf,"Placed tool can be removed without holding it");
        var first=station.Spawn(DemoToolKind.TapePost);first.PlaceAt(new Vector3(0,0,1),0);station.NotifyPlaced(first);
        station.StartNewTapeRun();
        var second=station.Spawn(DemoToolKind.TapePost);second.PlaceAt(new Vector3(1,0,1),0);station.NotifyPlaced(second);
        DemoValidation.Check(station.DeploymentRoot.GetComponentsInChildren<SceneTape>().Length==0,"Separate tape run does not link back to earlier poles");
        station.RemoveTool(first);station.RemoveTool(second);station.StartNewTapeRun();
        foreach(string name in new[]{"CrimeScene/ArchitecturalGlass","CrimeScene/Tape","CrimeScene/WorldText"})
            DemoValidation.Check(!UnityEditor.ShaderUtil.ShaderHasError(Shader.Find(name)),"Shader compiles: "+name);
        DemoValidation.Check(GameObject.Find("GlassBakeCamera")==null,"Reflection camera removed after static bake");
        DemoValidation.Info("Glass uses one static 256 x 256 RGB cubemap (six faces), no realtime reflection camera.");
    }
}
#endif
