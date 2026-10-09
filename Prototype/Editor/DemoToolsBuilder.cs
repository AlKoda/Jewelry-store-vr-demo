#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

public static class DemoToolsBuilder
{
    private const string Folder="Assets/CrimeSceneDemo/GeneratedTools";

    [MenuItem("Crime Scene Demo/Create Tool Station")]
    public static void Create()
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
        station.ConePrefab=SaveTool(DemoToolKind.Cone);
        station.MarkerPrefab=SaveTool(DemoToolKind.Marker);
        station.TapePostPrefab=SaveTool(DemoToolKind.TapePost);
        SceneTape temporaryTape=DemoToolGeometry.CreateTape();
        GameObject tapePrefab=PrefabUtility.SaveAsPrefabAsset(temporaryTape.gameObject,Folder+"/SceneTape.prefab");
        Object.DestroyImmediate(temporaryTape.gameObject);
        station.TapePrefab=tapePrefab.GetComponent<SceneTape>();

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
        GameObject body=GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name="Body";
        body.transform.SetParent(cameraBody.transform,false);
        body.transform.localScale=new Vector3(0.16f,0.1f,0.06f);
        Rigidbody rigidbody=cameraBody.AddComponent<Rigidbody>();
        rigidbody.isKinematic=true;
        rigidbody.useGravity=false;
        GameObject lens=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lens.name="Lens";
        lens.transform.SetParent(cameraBody.transform,false);
        lens.transform.localPosition=new Vector3(0,0,0.055f);
        lens.transform.localRotation=Quaternion.Euler(90,0,0);
        lens.transform.localScale=new Vector3(0.065f,0.035f,0.065f);
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
        // A player created before the station is wired up here.
        foreach(ToolHolder existing in Object.FindObjectsByType<ToolHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Wire(existing);
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

        XRHeadTracking head=camera.gameObject.AddComponent<XRHeadTracking>();
        XRControllerInput left=CreateHand(player.transform,"LeftHand",UnityEngine.XR.XRNode.LeftHand,-1,locomotion,bounds);
        XRControllerInput right=CreateHand(player.transform,"RightHand",UnityEngine.XR.XRNode.RightHand,1,locomotion,bounds);
        left.Teleports=true; left.SnapTurns=false;
        right.Teleports=false; right.SnapTurns=true;

        DemoModeSwitch mode=player.AddComponent<DemoModeSwitch>();
        mode.DesktopOnly=new Behaviour[] {walker,interactor};
        mode.VROnly=new Behaviour[] {head};
        mode.VRObjects=new [] {left.gameObject,right.gameObject};
        return walker;
    }

    private static XRControllerInput CreateHand(Transform rig,string name,UnityEngine.XR.XRNode node,float side,
        XRLocomotion locomotion,InteriorBounds bounds)
    {
        Transform hand=Group(name,rig);
        hand.localPosition=new Vector3(side*0.2f,1.1f,0.3f);
        Box("Visual",hand,Vector3.zero,new Vector3(.04f,.04f,.1f));
        HandInteractor interactor=hand.gameObject.AddComponent<HandInteractor>();
        interactor.Bounds=bounds;
        Wire(interactor);
        XRControllerInput input=hand.gameObject.AddComponent<XRControllerInput>();
        input.Node=node;
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
    }

    private static DeployedTool SaveTool(DemoToolKind kind)
    {
        DeployedTool temporary=DemoToolGeometry.Create(kind);
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(temporary.gameObject,Folder+"/"+kind+".prefab");
        Object.DestroyImmediate(temporary.gameObject);
        return prefab.GetComponent<DeployedTool>();
    }
}
#endif
