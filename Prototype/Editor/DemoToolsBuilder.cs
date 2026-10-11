#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

public static class DemoToolsBuilder
{
    private const string Folder="Assets/CrimeSceneDemo/GeneratedTools";

    [MenuItem("Crime Scene Demo/Create Tool Station")]
    public static void Create() { Create(true); }

    public static void Create(bool useImportedAssets)
    {
        GameObject store=GameObject.Find("JewelryStore_Blockout");
        if(store==null)
        {
            EditorUtility.DisplayDialog("Store required","Create the store blockout first.","OK");
            return;
        }
        if(store.transform.Find("ToolSystem")!=null)
        {
            EditorUtility.DisplayDialog("Already created","Remove the existing ToolSystem before creating another.","OK");
            return;
        }
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);

        GameObject root=new GameObject("ToolSystem");
        root.transform.SetParent(store.transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Create tool station");
        ToolStation station=root.AddComponent<ToolStation>();
        station.DeploymentRoot=store.transform.Find("DeployedTools");
        if(station.DeploymentRoot==null)
        {
            GameObject deployed=new GameObject("DeployedTools");
            deployed.transform.SetParent(store.transform,false);
            station.DeploymentRoot=deployed.transform;
        }
        station.ConePrefab=SaveTool(DemoToolKind.Cone,useImportedAssets);
        station.MarkerPrefab=SaveTool(DemoToolKind.Marker,useImportedAssets);
        station.TapePostPrefab=SaveTool(DemoToolKind.TapePost,useImportedAssets);
        station.ScalePrefab=SaveTool(DemoToolKind.Scale,useImportedAssets);
        station.MeasurePrefab=SaveTool(DemoToolKind.Measure,useImportedAssets);
        // Half-size samples spaced 26 cm apart along the 1.4 m rack top (x -4.3 to -2.9);
        // clicking or grabbing one spawns a new tool. The flat L-scale takes the middle
        // slot because the handheld camera rests 15 cm above it.
        Transform rack=root.transform;
        Sample(station.ConePrefab,rack,new Vector3(-4.12f,0.9f,1));
        Sample(station.MarkerPrefab,rack,new Vector3(-3.86f,0.9f,1));
        Sample(station.ScalePrefab,rack,new Vector3(-3.64f,0.9f,0.96f));
        Sample(station.TapePostPrefab,rack,new Vector3(-3.34f,0.9f,1));
        Sample(station.MeasurePrefab,rack,new Vector3(-3.08f,0.9f,1));
        station.TapePrefab=SaveTape(DemoToolGeometry.CreateTape(),"SceneTape");
        station.MeasureTapePrefab=SaveTape(DemoToolGeometry.CreateMeasureTape(),"MeasureTape");

        GameObject spawn=new GameObject("SpawnPoint");
        spawn.transform.SetParent(root.transform,false);
        spawn.transform.localPosition=new Vector3(-3.6f,0.9f,1);
        station.SpawnPoint=spawn.transform;

        DemoSession session=root.AddComponent<DemoSession>();
        session.Station=station;
        GameObject cameraBody=new GameObject("HandheldCamera");
        cameraBody.transform.SetParent(root.transform,false);
        cameraBody.transform.localPosition=new Vector3(-3.6f,1.1f,1);
        cameraBody.transform.localRotation=Quaternion.Euler(0,90,0);
        // Procedural unbranded DSLR ("Body" and "Lens" keep the grab colliders).
        DemoToolGeometry.CreateCameraBody(cameraBody.transform);
        Persist(cameraBody,"HandheldCamera");
        Rigidbody rigidbody=cameraBody.AddComponent<Rigidbody>();
        rigidbody.isKinematic=true;
        rigidbody.useGravity=false;
        GameObject view=new GameObject("PhotoCamera");
        view.transform.SetParent(cameraBody.transform,false);
        view.transform.localPosition=new Vector3(0,0,0.1f);
        Camera camera=view.AddComponent<Camera>();
        camera.enabled=false;
        camera.stereoTargetEye=StereoTargetEyeMask.None;
        camera.nearClipPlane=0.03f;
        camera.farClipPlane=100;
        camera.fieldOfView=60;
        EvidenceCamera evidence=cameraBody.AddComponent<EvidenceCamera>();
        SerializedObject serialized=new SerializedObject(evidence);
        serialized.FindProperty("photoCamera").objectReferenceValue=camera;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        cameraBody.GetComponentInChildren<CameraScreen>().Evidence=evidence;

        ResettableSceneObject resettable=cameraBody.AddComponent<ResettableSceneObject>();
        session.SceneObjects=new ResettableSceneObject[] {resettable};
        DemoDesktopPanel panel=root.AddComponent<DemoDesktopPanel>();
        panel.Station=station;
        panel.Session=session;
        panel.EvidenceCamera=evidence;

        // Local instructor records: photos, tool positions, before-reset snapshots.
        SessionReviewRecorder review=root.AddComponent<SessionReviewRecorder>();
        review.Station=station;
        review.Session=session;
        review.EvidenceCamera=evidence;
        panel.Review=review;

        // Last photograph on the wall above the rack, 16:9, facing the showroom.
        GameObject frame=GameObject.CreatePrimitive(PrimitiveType.Quad);
        frame.name="PhotoFrame";
        Object.DestroyImmediate(frame.GetComponent<Collider>());
        frame.transform.SetParent(root.transform,false);
        frame.transform.localPosition=new Vector3(-4.95f,2.1f,2.6f);
        frame.transform.localRotation=Quaternion.Euler(0,-90,0);
        frame.transform.localScale=new Vector3(1.6f,0.9f,1);
        PhotoFrame photoFrame=frame.AddComponent<PhotoFrame>();
        photoFrame.EvidenceCamera=evidence;

        // Street ambience heard through the broken window.
        GameObject ambience=new GameObject("StreetAmbience");
        ambience.transform.SetParent(root.transform,false);
        ambience.transform.localPosition=new Vector3(-1,1.5f,-0.5f);
        ambience.AddComponent<AudioSource>().playOnAwake=false;
        ambience.AddComponent<AmbientSound>();

        // Top-down map for the presenter panel; ceilings are hidden only for its render.
        DemoOverviewMap map=root.AddComponent<DemoOverviewMap>();
        foreach(Transform t in store.transform.Find("Architecture/Ceiling_Optional").GetComponentsInChildren<Transform>(true))
            t.gameObject.layer=map.HiddenLayer;
        panel.Map=map;

        // Wall-mounted status text above the rack, facing into the showroom.
        GameObject boardObject=new GameObject("StatusBoard");
        boardObject.transform.SetParent(root.transform,false);
        boardObject.transform.localPosition=new Vector3(-4.9f,1.9f,1);
        boardObject.transform.localRotation=Quaternion.Euler(0,-90,0);
        TextMesh boardText=boardObject.AddComponent<TextMesh>();
        boardText.anchor=TextAnchor.MiddleCenter;
        boardText.alignment=TextAlignment.Center;
        boardText.fontSize=48;
        boardText.characterSize=0.02f;
        boardText.color=new Color(0.92f,0.86f,0.62f);
        DeployedTool.EnsureFont(boardText);
        DemoStatusBoard board=boardObject.AddComponent<DemoStatusBoard>();
        board.Station=station;
        board.Session=session;
        board.EvidenceCamera=evidence;

        // Day/night presets with the flashlight, and the instructor's floor beacon;
        // both return to their defaults on a session reset. The beacon's bounds come
        // from the player, which CreatePlayer wires.
        DemoLighting lighting=root.AddComponent<DemoLighting>();
        lighting.Session=session;
        lighting.FlashlightCookie=FlashlightCookie();
        DemoBeacon beacon=root.AddComponent<DemoBeacon>();
        beacon.Session=session;
        panel.Lighting=lighting;
        panel.Beacon=beacon;
        // A player created before the station is wired up here.
        foreach(ToolHolder existing in Object.FindObjectsByType<ToolHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Wire(existing);
        foreach(XRControllerInput hand in Object.FindObjectsByType<XRControllerInput>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            WireHand(hand);
        Selection.activeGameObject=root;
        Debug.Log("Tool station created. Save scene; XR grabbing/activation and lighting still require integration.",root);
    }

    [MenuItem("Crime Scene Demo/Create Player (desktop and VR)")]
    public static void CreatePlayerFromMenu()
    {
        if(Camera.main==null)
        {
            EditorUtility.DisplayDialog("Camera required","Add a camera tagged MainCamera first.","OK");
            return;
        }
        Undo.RegisterCreatedObjectUndo(CreatePlayer(Camera.main).gameObject,"Create player");
    }

    // One rig for both modes: walker and pointer for the desktop, head tracking and
    // two controller-driven hands for VR. DemoModeSwitch enables one set at a time.
    public static ShopWalkController CreatePlayer(Camera camera)
    {
        GameObject player=new GameObject("Player");
        Transform start=GameObject.Find("JewelryStore_Blockout")?.transform.Find("ReferencePoints/SuggestedPlayerStart");
        player.transform.position=start!=null?start.position:new Vector3(3.65f,0,1.3f);
        player.transform.rotation=Quaternion.Euler(0,-30,0);
        CharacterController cc=player.AddComponent<CharacterController>();
        cc.height=1.75f; cc.radius=.23f; cc.center=new Vector3(0,.875f,0);
        cc.stepOffset=.15f; cc.skinWidth=.025f;
        camera.transform.SetParent(player.transform,false);
        camera.transform.localPosition=new Vector3(0,1.65f,0);
        camera.transform.localRotation=Quaternion.identity;
        camera.clearFlags=CameraClearFlags.SolidColor;
        if(RenderSettings.fog) { camera.backgroundColor=RenderSettings.fogColor; camera.farClipPlane=120; }

        InteriorBounds bounds=player.AddComponent<InteriorBounds>();
        // The review sketches the shop, and the beacon is clamped, by the regions that bound the player.
        SessionReviewRecorder review=Object.FindFirstObjectByType<SessionReviewRecorder>();
        if(review!=null) review.Bounds=bounds;
        DemoBeacon beacon=Object.FindFirstObjectByType<DemoBeacon>();
        if(beacon!=null) beacon.Bounds=bounds;
        XRLocomotion locomotion=player.AddComponent<XRLocomotion>();
        locomotion.Head=camera.transform;
        locomotion.Bounds=bounds;
        ShopWalkController walker=player.AddComponent<ShopWalkController>();
        walker.View=camera.transform;
        walker.Bounds=bounds;
        DesktopInteractor interactor=player.AddComponent<DesktopInteractor>();
        interactor.View=camera;
        interactor.Walker=walker;
        interactor.Bounds=bounds;
        Wire(interactor);
        if(interactor.Panel!=null) interactor.Panel.Player=player.transform;

        XRHeadTracking head=camera.gameObject.AddComponent<XRHeadTracking>();
        // Comfort blink on the head camera; VR-only, so the desktop view never shows it.
        XRComfortFade fade=camera.gameObject.AddComponent<XRComfortFade>();
        locomotion.ComfortFade=fade;
        XRControllerInput left=CreateHand(player.transform,"LeftHand",UnityEngine.XR.XRNode.LeftHand,-1,locomotion,bounds);
        XRControllerInput right=CreateHand(player.transform,"RightHand",UnityEngine.XR.XRNode.RightHand,1,locomotion,bounds);
        left.Teleports=true; left.SnapTurns=false;
        right.Teleports=false; right.SnapTurns=true;
        WireHand(right);

        DemoModeSwitch mode=player.AddComponent<DemoModeSwitch>();
        mode.DesktopOnly=new Behaviour[] {walker,interactor};
        mode.DesktopColliders=new Collider[] {cc};
        mode.VROnly=new Behaviour[] {head,fade};
        mode.VRObjects=new [] {left.gameObject,right.gameObject};
        return walker;
    }

    private static XRControllerInput CreateHand(Transform rig,string name,UnityEngine.XR.XRNode node,float side,
        XRLocomotion locomotion,InteriorBounds bounds)
    {
        Transform hand=Group(name,rig);
        hand.localPosition=new Vector3(side*0.2f,1.1f,0.3f);
        Box("Visual",hand,Vector3.zero,new Vector3(.04f,.04f,.1f));
        TextMesh label=Text("Label",hand,"",new Vector3(0,.04f,-.02f),0,.004f,Color.white);
        label.transform.localRotation=Quaternion.Euler(45,0,0);
        label.anchor=TextAnchor.LowerCenter;
        HandInteractor interactor=hand.gameObject.AddComponent<HandInteractor>();
        interactor.Bounds=bounds;
        Wire(interactor);
        XRControllerInput input=hand.gameObject.AddComponent<XRControllerInput>();
        input.Node=node;
        input.Label=label;
        input.Locomotion=locomotion;
        hand.gameObject.SetActive(false);
        return input;
    }

    private static void Wire(ToolHolder holder)
    {
        holder.Station=Object.FindFirstObjectByType<ToolStation>();
        holder.Session=Object.FindFirstObjectByType<DemoSession>();
        holder.EvidenceCamera=Object.FindFirstObjectByType<EvidenceCamera>();
        DesktopInteractor desktop=holder as DesktopInteractor;
        if(desktop==null) return;
        desktop.Panel=Object.FindFirstObjectByType<DemoDesktopPanel>();
        if(desktop.Panel!=null) desktop.Panel.Interactor=desktop;
        desktop.Lighting=Object.FindFirstObjectByType<DemoLighting>();
        desktop.Beacon=Object.FindFirstObjectByType<DemoBeacon>();
    }

    // Only the right hand's stick click toggles the flashlight.
    private static void WireHand(XRControllerInput hand)
    {
        if(hand.Node==UnityEngine.XR.XRNode.RightHand) hand.Lighting=Object.FindFirstObjectByType<DemoLighting>();
    }

    // Swap generated primitives for a third-party model, keeping one collider on the root.
    private static void UseModel(GameObject tool,string pack,string model)
    {
        GameObject visual=DemoAssetLibrary.Place(pack,model,tool.transform,Vector3.zero,0,1);
        if(visual==null) return;
        for(int i=tool.transform.childCount-1;i>=0;i--)
        {
            Transform child=tool.transform.GetChild(i);
            if(child!=visual.transform) Object.DestroyImmediate(child.gameObject);
        }
        DemoAssetLibrary.AddBoundsCollider(tool);
    }

    private static void Sample(DeployedTool prefab,Transform parent,Vector3 position)
    {
        GameObject sample=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,parent);
        PrefabUtility.UnpackPrefabInstance(sample,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        sample.name="Sample_"+prefab.Kind;
        Object.DestroyImmediate(sample.GetComponent<DeployedTool>());
        Object.DestroyImmediate(sample.GetComponent<Rigidbody>());
        sample.AddComponent<ToolRackSample>().Kind=prefab.Kind;
        sample.transform.localPosition=position;
        sample.transform.localScale=Vector3.one*0.5f;
    }

    private static DeployedTool SaveTool(DemoToolKind kind,bool useImportedAssets)
    {
        DeployedTool temporary=DemoToolGeometry.Create(kind);
        if(useImportedAssets && kind==DemoToolKind.Cone) UseModel(temporary.gameObject,"KhronosSamples","TrafficCone");
        Persist(temporary.gameObject,kind.ToString());
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(temporary.gameObject,Folder+"/"+kind+".prefab");
        Object.DestroyImmediate(temporary.gameObject);
        return prefab.GetComponent<DeployedTool>();
    }

    // Saves the procedural meshes and Paint() materials DemoToolGeometry made in
    // memory as assets (<prefix>_<part>.asset, <material>.mat), so prefabs and scenes
    // keep them. Existing assets are updated in place to keep their references.
    public static void Persist(GameObject root,string prefix)
    {
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh=filter.sharedMesh;
            if(mesh==null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))) continue;
            string path=Folder+"/"+prefix+"_"+filter.name+".asset";
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) AssetDatabase.CreateAsset(mesh,path);
            else { EditorUtility.CopySerialized(mesh,existing); filter.sharedMesh=existing; }
        }
        foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials=renderer.sharedMaterials;
            bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                Material m=materials[i];
                if(m==null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) continue;
                string path=Folder+"/"+m.name+".mat";
                Material existing=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(existing==null) AssetDatabase.CreateAsset(m,path);
                else { EditorUtility.CopySerialized(m,existing); materials[i]=existing; changed=true; }
            }
            if(changed) renderer.sharedMaterials=materials;
        }
        AssetDatabase.SaveAssets();
    }

    // 128 px spot cookie: a bright hotspot inside a soft-edged main beam and a faint
    // outer ring, as a real reflector torch throws. Alpha carries the luminance (the
    // Built-in spot cookie reads alpha); the border stays black so clamping cannot streak.
    public static Texture2D FlashlightCookie()
    {
        const int size=128;
        string path=Folder+"/FlashlightCookie.asset";
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        Texture2D cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool create=cookie==null || !cookie.isReadable || cookie.width!=size || cookie.height!=size;
        if(create) cookie=new Texture2D(size,size,TextureFormat.RGBA32,true,true) {name="FlashlightCookie"};
        Color32[] pixels=new Color32[size*size];
        float half=size*0.5f-2;
        for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                float r=new Vector2(x+0.5f-size*0.5f,y+0.5f-size*0.5f).magnitude/half;
                float hotspot=Mathf.Exp(-(r/0.28f)*(r/0.28f));
                float beam=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.5f,0.9f,r));
                float ring=Mathf.Exp(-((r-0.86f)/0.04f)*((r-0.86f)/0.04f));
                float v=r>=1?0:Mathf.Clamp01(0.6f*beam+0.4f*hotspot+0.12f*ring);
                byte b=(byte)Mathf.RoundToInt(v*255);
                pixels[y*size+x]=new Color32(b,b,b,b);
            }
        cookie.SetPixels32(pixels);
        cookie.wrapMode=TextureWrapMode.Clamp;
        cookie.filterMode=FilterMode.Bilinear;
        cookie.Apply(true,false);
        if(create)
        {
            if(AssetDatabase.LoadAssetAtPath<Texture2D>(path)!=null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(cookie,path);
        }
        else EditorUtility.SetDirty(cookie);
        AssetDatabase.SaveAssets();
        return cookie;
    }

    private static SceneTape SaveTape(SceneTape temporary,string name)
    {
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(temporary.gameObject,Folder+"/"+name+".prefab");
        Object.DestroyImmediate(temporary.gameObject);
        return prefab.GetComponent<SceneTape>();
    }
}
#endif
