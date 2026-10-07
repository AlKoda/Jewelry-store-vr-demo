#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
        if(!AssetDatabase.IsValidFolder("Assets/CrimeSceneDemo"))
            AssetDatabase.CreateFolder("Assets","CrimeSceneDemo");
        if(!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/CrimeSceneDemo","GeneratedTools");

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
        Selection.activeGameObject=root;
        Debug.Log("Tool station created. Save scene; XR grabbing/activation and lighting still require integration.",root);
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
