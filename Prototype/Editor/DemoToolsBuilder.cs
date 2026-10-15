#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

public static class DemoToolsBuilder
{
    private const string DefaultFolder="Assets/CrimeSceneDemo/GeneratedTools";
    // Where Create saves prefabs, meshes, materials and the cookie: the layout's own
    // folder, so one store's build never overwrites another store's tool prefabs.
    private static string Folder=DefaultFolder;

    // Where the station's pieces go, in the store root's space (all stores sit at the
    // world origin). Shop reproduces the original small shop exactly; the night store
    // passes NightStoreLayout.Tools.
    public struct ToolLayout
    {
        public string StoreRoot;
        // Hidden from the overview map's camera (the ceilings).
        public string CeilingPath;
        // Half-size rack samples, each standing on the rack or table top.
        public Vector3 Cone,Marker,Scale,TapePost,Measure;
        // The scene lamp's sample (1.6 m stand, so 0.8 m as a sample) where it fits;
        // null leaves it off the rack (the small shop's rack has the photo frame above it).
        public Vector3? Lamp;
        public Vector3 SpawnPoint;
        public Vector3 Camera; public float CameraYaw;
        public Vector3 PhotoFrame; public float PhotoFrameYaw;
        public Vector3 StatusBoard; public float StatusBoardYaw;
        public Vector3 Ambience;
        public Vector2 MapCentre; public float MapHalfSize;
        // Asset folder for this layout's tool prefabs; null keeps GeneratedTools (the shop).
        public string AssetFolder;
        // Style the prefabs here (materials, depth-tested labels, police tape) before the
        // rack samples are made. The shop leaves this off: its later passes
        // (ShopPresentationExpansion, ShopGlassRefinement) style the same prefabs.
        public bool Styled;
        // Barrier-tape wording for a Styled layout (the night store's comes from PoliceLook);
        // null keeps DefaultTapeText.
        public string TapeText;

        // Half-size samples spaced 26 cm apart along the 1.4 m rack top (x -4.3 to -2.9);
        // the flat L-scale takes the middle slot because the handheld camera rests 15 cm
        // above it. Last photograph on the wall above the rack, the status text beside it.
        public static ToolLayout Shop => new ToolLayout
        {
            StoreRoot="JewelryStore_Blockout",CeilingPath="Architecture/Ceiling_Optional",
            Cone=new Vector3(-4.12f,0.9f,1),Marker=new Vector3(-3.86f,0.9f,1),Scale=new Vector3(-3.64f,0.9f,0.96f),
            TapePost=new Vector3(-3.34f,0.9f,1),Measure=new Vector3(-3.08f,0.9f,1),
            SpawnPoint=new Vector3(-3.6f,0.9f,1),Camera=new Vector3(-3.6f,1.1f,1),CameraYaw=90,
            PhotoFrame=new Vector3(-4.95f,2.1f,2.6f),PhotoFrameYaw=-90,
            StatusBoard=new Vector3(-4.9f,1.9f,1),StatusBoardYaw=-90,
            Ambience=new Vector3(-1,1.5f,-0.5f),
            MapCentre=new Vector2(0,5.5f),MapHalfSize=6
        };
    }

    [MenuItem("Crime Scene Demo/Create Tool Station")]
    public static void Create() { Create(true); }

    public static void Create(bool useImportedAssets) { Create(useImportedAssets,ToolLayout.Shop); }

    public static void Create(bool useImportedAssets,ToolLayout layout)
    {
        GameObject store=GameObject.Find(layout.StoreRoot);
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
        Folder=string.IsNullOrEmpty(layout.AssetFolder)?DefaultFolder:layout.AssetFolder;
        EnsureFolderPath(Folder);

        GameObject root=new GameObject("ToolSystem");
        root.transform.SetParent(store.transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Create tool station");
        ToolStation station=root.AddComponent<ToolStation>();
        AddTheme(root);
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
        station.LampPrefab=SaveTool(DemoToolKind.Lamp,useImportedAssets);
        station.TapePrefab=SaveTape(DemoToolGeometry.CreateTape(),"SceneTape");
        station.MeasureTapePrefab=SaveTape(DemoToolGeometry.CreateMeasureTape(),"MeasureTape");
        // Styled before the samples, so the rack samples are instantiated from styled prefabs.
        Material labelMaterial=layout.Styled?StyleStation(station,Folder,layout.TapeText):null;
        // Clicking or grabbing a rack sample spawns a new tool.
        Transform rack=root.transform;
        Sample(station.ConePrefab,rack,layout.Cone);
        Sample(station.MarkerPrefab,rack,layout.Marker);
        Sample(station.ScalePrefab,rack,layout.Scale);
        Sample(station.TapePostPrefab,rack,layout.TapePost);
        Sample(station.MeasurePrefab,rack,layout.Measure);
        if(layout.Lamp.HasValue) Sample(station.LampPrefab,rack,layout.Lamp.Value);

        GameObject spawn=new GameObject("SpawnPoint");
        spawn.transform.SetParent(root.transform,false);
        spawn.transform.localPosition=layout.SpawnPoint;
        station.SpawnPoint=spawn.transform;

        DemoSession session=root.AddComponent<DemoSession>();
        session.Station=station;
        GameObject cameraBody=new GameObject("HandheldCamera");
        cameraBody.transform.SetParent(root.transform,false);
        cameraBody.transform.localPosition=layout.Camera;
        cameraBody.transform.localRotation=Quaternion.Euler(0,layout.CameraYaw,0);
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

        // Last photograph, 16:9, facing the visitor.
        GameObject frame=GameObject.CreatePrimitive(PrimitiveType.Quad);
        frame.name="PhotoFrame";
        Object.DestroyImmediate(frame.GetComponent<Collider>());
        frame.transform.SetParent(root.transform,false);
        frame.transform.localPosition=layout.PhotoFrame;
        frame.transform.localRotation=Quaternion.Euler(0,layout.PhotoFrameYaw,0);
        frame.transform.localScale=new Vector3(1.6f,0.9f,1);
        PhotoFrame photoFrame=frame.AddComponent<PhotoFrame>();
        photoFrame.EvidenceCamera=evidence;

        // Street ambience heard through the broken window.
        GameObject ambience=new GameObject("StreetAmbience");
        ambience.transform.SetParent(root.transform,false);
        ambience.transform.localPosition=layout.Ambience;
        ambience.AddComponent<AudioSource>().playOnAwake=false;
        ambience.AddComponent<AmbientSound>();

        // Top-down map for the presenter panel, in flat albedo; ceilings are hidden only for its render.
        DemoOverviewMap map=root.AddComponent<DemoOverviewMap>();
        map.Centre=layout.MapCentre;
        map.HalfSize=layout.MapHalfSize;
        map.MapShader=Shader.Find("Hidden/CrimeScene/MapAlbedo");
        if(map.MapShader==null) Debug.LogWarning("Hidden/CrimeScene/MapAlbedo missing; the overview map renders lit.");
        foreach(Transform t in store.transform.Find(layout.CeilingPath).GetComponentsInChildren<Transform>(true))
            t.gameObject.layer=map.HiddenLayer;
        panel.Map=map;

        // Status text facing the visitor (on the wall above the shop's rack).
        GameObject boardObject=new GameObject("StatusBoard");
        boardObject.transform.SetParent(root.transform,false);
        boardObject.transform.localPosition=layout.StatusBoard;
        boardObject.transform.localRotation=Quaternion.Euler(0,layout.StatusBoardYaw,0);
        TextMesh boardText=boardObject.AddComponent<TextMesh>();
        boardText.anchor=TextAnchor.MiddleCenter;
        boardText.alignment=TextAlignment.Center;
        boardText.fontSize=48;
        boardText.characterSize=0.02f;
        boardText.color=new Color(0.92f,0.86f,0.62f);
        DeployedTool.EnsureFont(boardText);
        if(labelMaterial!=null) DepthTested(boardText,labelMaterial);
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
        Transform start=GameObject.Find("JewelryStore_Blockout")?.transform.Find("ReferencePoints/SuggestedPlayerStart");
        return CreatePlayer(camera,start!=null?start.position:new Vector3(3.65f,0,1.3f),-30,null);
    }

    // Player at a spawn pose; regions (null keeps InteriorBounds' small-shop defaults)
    // bound walking, teleport, tool placement, the beacon and the review sketch.
    public static ShopWalkController CreatePlayer(Camera camera,Vector3 position,float yaw,InteriorBounds.Region[] regions)
    {
        GameObject player=new GameObject("Player");
        player.transform.position=position;
        player.transform.rotation=Quaternion.Euler(0,yaw,0);
        CharacterController cc=player.AddComponent<CharacterController>();
        cc.height=1.75f; cc.radius=.23f; cc.center=new Vector3(0,.875f,0);
        cc.stepOffset=.15f; cc.skinWidth=.025f;
        camera.transform.SetParent(player.transform,false);
        camera.transform.localPosition=new Vector3(0,1.65f,0);
        camera.transform.localRotation=Quaternion.identity;
        camera.clearFlags=CameraClearFlags.SolidColor;
        if(RenderSettings.fog) { camera.backgroundColor=RenderSettings.fogColor; camera.farClipPlane=120; }

        InteriorBounds bounds=player.AddComponent<InteriorBounds>();
        if(regions!=null) bounds.Regions=(InteriorBounds.Region[])regions.Clone();
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
        WireHand(left);
        WireHand(right);

        DemoModeSwitch mode=player.AddComponent<DemoModeSwitch>();
        mode.DesktopOnly=new Behaviour[] {walker,interactor};
        mode.DesktopColliders=new Collider[] {cc};
        mode.VROnly=new Behaviour[] {head,fade};
        mode.VRObjects=new [] {left.gameObject,right.gameObject};
        return walker;
    }

    // Interface fonts (ThirdParty/Fonts: Barlow, SIL OFL 1.1) and the controller shader
    // for DemoUi; a missing font leaves DemoUi on the built-in one.
    public const string FontFolder=DemoAssetLibrary.Root+"/Fonts";

    public static DemoUiTheme AddTheme(GameObject host)
    {
        DemoUiTheme theme=host.GetComponent<DemoUiTheme>();
        if(theme==null) theme=host.AddComponent<DemoUiTheme>();
        theme.Regular=AssetDatabase.LoadAssetAtPath<Font>(FontFolder+"/Barlow-Regular.ttf");
        theme.Medium=AssetDatabase.LoadAssetAtPath<Font>(FontFolder+"/Barlow-Medium.ttf");
        theme.SemiBold=AssetDatabase.LoadAssetAtPath<Font>(FontFolder+"/Barlow-SemiBold.ttf");
        theme.Shaded=Shader.Find("CrimeScene/UiShaded");
        if(theme.Regular==null||theme.Medium==null||theme.SemiBold==null)
            Debug.LogWarning("Interface fonts missing under "+FontFolder+"; the built-in font is used.");
        if(theme.Shaded==null) Debug.LogWarning("CrimeScene/UiShaded missing; the controllers fall back to a lit shader.");
        return theme;
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

    // The right hand's stick click toggles the white flashlight, the left hand's the UV torch.
    private static void WireHand(XRControllerInput hand)
    {
        DemoLighting lighting=Object.FindFirstObjectByType<DemoLighting>();
        if(hand.Node==UnityEngine.XR.XRNode.RightHand) hand.Lighting=lighting;
        else hand.UVLighting=lighting;
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
        // A sample is a static display: the lamp's light goes (its face stays unlit).
        foreach(Light light in sample.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light.gameObject);
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
        EnsureFolderPath(Folder);
        foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh=filter.sharedMesh;
            if(mesh==null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))) continue;
            DemoGeometry.SaveMesh(ref mesh,Folder+"/"+prefix+"_"+filter.name+".asset");
            filter.sharedMesh=mesh;
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
        EnsureFolderPath(Folder);
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

    // Creates every missing folder along an Assets/... path.
    private static void EnsureFolderPath(string path)
    {
        string[] parts=path.Split('/');
        string current=parts[0];
        for(int i=1;i<parts.Length;i++) { current+="/"+parts[i]; EnsureFolder(current); }
    }

    // ---------------------------------------------------------------- tool styling
    // The presentation pass for the tool prefabs, shared by the shop's passes
    // (ShopPresentationExpansion.StyleTools, ShopGlassRefinement.StyleTape) and by Create
    // for layouts with Styled set (the night store). Materials live in the given
    // folder under the shop's names; returns the depth-tested label material. tapeText
    // (null: DefaultTapeText) is the barrier tape's wording.
    public static Material StyleStation(ToolStation station,string folder,string tapeText=null)
    {
        EnsureFolderPath(folder);
        Material charcoal=DemoGeometry.Mat(folder,"Graphite",new Color(.10f,.12f,.13f),.18f);
        Material ivory=DemoGeometry.Mat(folder,"Linen",new Color(.82f,.77f,.63f),.18f);
        Material orange=DemoGeometry.Mat(folder,"ConeOrange",new Color(.95f,.29f,.03f),.18f);
        Material yellow=DemoGeometry.Mat(folder,"EvidenceYellow",new Color(.95f,.75f,.08f),.18f);
        Material label=ShaderMaterial(folder,"ToolNumber","CrimeScene/WorldText");
        Material tape=ShaderMaterial(folder,"PoliceTape","CrimeScene/Tape");
        StylePrefabs(station,charcoal,ivory,yellow,orange,label);
        StyleSceneTape(station,tape,FlexibleRibbon(folder+"/FlexibleRibbon.asset"),label,tapeText);
        AssetDatabase.SaveAssets();
        return label;
    }

    private static Material ShaderMaterial(string folder,string name,string shader)
    {
        string path=folder+"/"+name+".mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(material,path); }
        return material;
    }

    // Charcoal/ivory scale bands, yellow markers and reels, orange cone, depth-tested
    // black numbers; the measuring tape gets a yellow ribbon, charcoal caps and a
    // depth-tested distance label. Imported textured models and DemoToolGeometry's own
    // painted parts (cone, stanchion, lamp) keep their materials.
    public static void StylePrefabs(ToolStation station,Material charcoal,Material ivory,Material yellow,Material orange,Material labelMat)
    {
        foreach(DeployedTool prefab in new []{station.ConePrefab,station.MarkerPrefab,station.TapePostPrefab,station.ScalePrefab,station.MeasurePrefab,station.LampPrefab})
        {
            if(prefab==null) continue;
            string path=AssetDatabase.GetAssetPath(prefab);
            GameObject contents=PrefabUtility.LoadPrefabContents(path);
            foreach(MeshRenderer r in contents.GetComponentsInChildren<MeshRenderer>())
            {
                TextMesh text=r.GetComponent<TextMesh>();
                if(text!=null) { DepthTested(text,labelMat); text.color=Color.black; }
                else if(r.sharedMaterial!=null && (r.sharedMaterial.mainTexture!=null || DemoToolGeometry.IsPainted(r.sharedMaterial))) continue;
                else if(prefab.Kind==DemoToolKind.Scale) r.sharedMaterial=r.name=="BandBlack"?charcoal:ivory;
                else if(prefab.Kind==DemoToolKind.Measure) r.sharedMaterial=r.name=="Body"?yellow:charcoal;
                else r.sharedMaterial=prefab.Kind==DemoToolKind.Cone?orange:
                    prefab.Kind==DemoToolKind.Marker?yellow:charcoal;
            }
            PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
        }
        string tapePath=AssetDatabase.GetAssetPath(station.TapePrefab);
        GameObject tape=PrefabUtility.LoadPrefabContents(tapePath);
        tape.GetComponentInChildren<MeshRenderer>().sharedMaterial=yellow;
        PrefabUtility.SaveAsPrefabAsset(tape,tapePath);PrefabUtility.UnloadPrefabContents(tape);
        string measurePath=AssetDatabase.GetAssetPath(station.MeasureTapePrefab);
        GameObject measure=PrefabUtility.LoadPrefabContents(measurePath);
        Transform ribbon=measure.GetComponent<SceneTape>().Ribbon;
        foreach(MeshRenderer r in measure.GetComponentsInChildren<MeshRenderer>())
        {
            TextMesh text=r.GetComponent<TextMesh>();
            if(text!=null) DepthTested(text,labelMat);
            else r.sharedMaterial=r.transform==ribbon?yellow:charcoal;
        }
        PrefabUtility.SaveAsPrefabAsset(measure,measurePath);PrefabUtility.UnloadPrefabContents(measure);
    }

    // The shop's barrier-tape wording and the default of every layout without its own.
    public const string DefaultTapeText="CRIME SCENE - DO NOT CROSS";

    // Barrier tape: flexible ribbon mesh, CrimeScene/Tape material and depth-tested
    // warnings on both faces (added once; a re-run reuses them). A given text is set on
    // both warnings, new or reused; null leaves reused ones as they are and writes
    // DefaultTapeText on new ones (the shop's path, unchanged).
    public static void StyleSceneTape(ToolStation station,Material tape,Mesh ribbonMesh,Material ink,string text=null)
    {
        string path=AssetDatabase.GetAssetPath(station.TapePrefab);
        GameObject contents=PrefabUtility.LoadPrefabContents(path);
        SceneTape component=contents.GetComponent<SceneTape>();
        MeshRenderer ribbon=component.Ribbon!=null?component.Ribbon.GetComponent<MeshRenderer>():null;
        if(ribbon==null) ribbon=contents.GetComponentInChildren<MeshRenderer>();
        ribbon.GetComponent<MeshFilter>().sharedMesh=ribbonMesh;
        ribbon.sharedMaterial=tape;
        string words=text??DefaultTapeText;
        TextMesh front=component.FrontWarning!=null?component.FrontWarning:
            Text("FrontWarning",contents.transform,words,Vector3.zero,90,.012f,Color.black);
        TextMesh back=component.BackWarning!=null?component.BackWarning:
            Text("BackWarning",contents.transform,words,Vector3.zero,-90,.012f,Color.black);
        foreach(TextMesh warning in new[]{front,back})
        {
            if(text!=null) warning.text=text;
            DepthTestedLabel label=warning.GetComponent<DepthTestedLabel>();
            if(label==null) label=warning.gameObject.AddComponent<DepthTestedLabel>();
            label.LabelMaterial=ink;label.Refresh();
        }
        component.FrontWarning=front;component.BackWarning=back;
        PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
    }

    // Unit box ribbon in eight segments along z (SceneTape scales it to the run).
    public static Mesh FlexibleRibbon(string path)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<=8;i++)
        {
            float z=-.5f+i/8f;
            vertices.Add(V(-.5f,-.5f,z));vertices.Add(V(.5f,-.5f,z));
            vertices.Add(V(.5f,.5f,z));vertices.Add(V(-.5f,.5f,z));
        }
        for(int i=0;i<8;i++)for(int j=0;j<4;j++)
        {
            int a=i*4+j,b=i*4+(j+1)%4,c=b+4,d=a+4;
            triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a);triangles.Add(c);triangles.Add(d);
        }
        triangles.AddRange(new[]{0,2,1,0,3,2,32,33,34,32,34,35});
        Mesh ribbonMesh=new Mesh{name="FlexibleRibbon",vertices=vertices.ToArray(),triangles=triangles.ToArray()};
        ribbonMesh.RecalculateNormals();ribbonMesh.RecalculateBounds();
        ribbonMesh.bounds=new Bounds(Vector3.zero,new Vector3(1,2,1));
        SaveMesh(ref ribbonMesh,path);
        return ribbonMesh;
    }

    // World text on the live font atlas with the depth-tested shader (see DepthTestedLabel).
    public static void DepthTested(TextMesh text,Material labelMat)
    {
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        DepthTestedLabel label=text.GetComponent<DepthTestedLabel>();
        if(label==null) label=text.gameObject.AddComponent<DepthTestedLabel>();
        label.LabelMaterial=labelMat;label.Refresh();
    }

    private static SceneTape SaveTape(SceneTape temporary,string name)
    {
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(temporary.gameObject,Folder+"/"+name+".prefab");
        Object.DestroyImmediate(temporary.gameObject);
        return prefab.GetComponent<SceneTape>();
    }
}
#endif
