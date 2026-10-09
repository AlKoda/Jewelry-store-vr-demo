#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// One Unity unit = one metre. Editor-only generation; no runtime dependency.
public static class JewelryStoreBuilder
{
    private static Transform architecture, furniture, evidence;
    private static Mesh shardMesh;

    [MenuItem("Crime Scene Demo/Create Store Blockout")]
    public static void CreateStore()
    {
        if (GameObject.Find("JewelryStore_Blockout") != null)
        {
            EditorUtility.DisplayDialog("Store already exists",
                "Rename or remove the existing JewelryStore_Blockout before generating another.", "OK");
            return;
        }

        GameObject root = new GameObject("JewelryStore_Blockout");
        Undo.RegisterCreatedObjectUndo(root, "Create jewelry store");
        architecture = Group("Architecture", root.transform);
        furniture = Group("Furniture", root.transform);
        evidence = Group("FixedEvidence", root.transform);
        Transform ceiling = Group("Ceiling_Optional", architecture);
        Group("DeployedTools", root.transform);

        // Interior bounds: x=-5..5, z=0..8. Street lies toward negative z.
        Box("ShowroomFloor", architecture, V(0,-0.1f,4), V(10,0.2f,8));
        Box("Pavement", architecture, V(0,-0.1f,-1.5f), V(12,0.2f,3));
        Box("LeftWall", architecture, V(-5.1f,1.5f,4), V(0.2f,3,8.2f));
        Box("RightWall", architecture, V(5.1f,1.5f,4), V(0.2f,3,8.2f));

        // Rear opening: x=1.8..3.2, 1.4 m wide and 2.3 m high.
        Box("RearWallLeft", architecture, V(-1.6f,1.5f,8.1f), V(6.8f,3,0.2f));
        Box("RearWallRight", architecture, V(4.1f,1.5f,8.1f), V(1.8f,3,0.2f));
        Box("RearDoorLintel", architecture, V(2.5f,2.65f,8.1f), V(1.4f,0.7f,0.2f));

        // Front: window spans -4.5..2.5; entrance spans 3..4.6.
        Box("FrontLeftPier", architecture, V(-4.75f,1.5f,-0.1f), V(0.5f,3,0.2f));
        Box("FrontWindowSill", architecture, V(-1,0.25f,-0.1f), V(7,0.5f,0.2f));
        Box("FrontWindowHeader", architecture, V(-1,2.8f,-0.1f), V(7,0.4f,0.2f));
        Box("EntrancePier", architecture, V(2.75f,1.5f,-0.1f), V(0.5f,3,0.2f));
        Box("FrontRightPier", architecture, V(4.8f,1.5f,-0.1f), V(0.4f,3,0.2f));
        Box("EntranceHeader", architecture, V(3.8f,2.75f,-0.1f), V(1.6f,0.5f,0.2f));
        // Frame remnants and pointed sill fragments; central opening remains clear.
        Box("WindowLeftTrim",architecture,V(-4.45f,1.55f,-0.08f),V(0.06f,2.1f,0.08f));
        Box("WindowRightTrim",architecture,V(2.45f,1.55f,-0.08f),V(0.06f,2.1f,0.08f));
        Box("WindowTopTrim",architecture,V(-1,2.59f,-0.08f),V(6.9f,0.06f,0.08f));
        Box("WindowBottomTrim",architecture,V(-1,0.52f,-0.08f),V(6.9f,0.06f,0.08f));
        for(int i=0;i<6;i++)
        {
            Shard("SillGlassRemnant_"+i,V(-4.1f+i*1.05f,0.58f,-0.08f),0.22f,0);
            evidence.GetChild(evidence.childCount-1).localRotation=Quaternion.Euler(90,0,i*17);
        }

        Box("ShowroomCeiling", ceiling, V(0,3.1f,4), V(10.2f,0.2f,8.2f));
        Box("SafeRoomFloor", architecture, V(2.5f,-0.1f,9.6f), V(3,0.2f,3));
        Box("SafeRoomLeftWall", architecture, V(0.9f,1.5f,9.6f), V(0.2f,3,3));
        Box("SafeRoomRightWall", architecture, V(4.1f,1.5f,9.6f), V(0.2f,3,3));
        Box("SafeRoomBackWall", architecture, V(2.5f,1.5f,11.2f), V(3.4f,3,0.2f));
        Box("SafeRoomCeiling", ceiling, V(2.5f,3.1f,9.6f), V(3.4f,0.2f,3.4f));

        Display("Island_Left", V(-1.7f,0,3.7f), V(1.2f,1,2.6f), true);
        Display("Island_Right", V(1.7f,0,3.7f), V(1.2f,1,2.6f), false);
        Display("WallDisplay_Left", V(-4.45f,0,4), V(0.8f,1,3.2f), false);
        Display("WallDisplay_Right", V(4.45f,0,4), V(0.8f,1,3.2f), true);
        Box("ServiceCounter", furniture, V(-1.8f,0.55f,6.5f), V(3.8f,1.1f,0.75f));
        Box("ToolRack_Placeholder", furniture, V(-3.6f,0.45f,1), V(1.4f,0.9f,0.6f));
        Safe();
        ShoePrint(V(2.6f,0.009f,6.5f));
        Torch();

        // Deterministic debris, no rigidbodies or loose physics.
        System.Random random = new System.Random(27);
        for (int i=0; i<22; i++)
        {
            float x = -3.8f + (float)random.NextDouble()*5.5f;
            float z = -0.65f + (float)random.NextDouble()*1.5f;
            Shard("WindowShard_"+i, V(x,0.012f,z),
                0.1f+(float)random.NextDouble()*0.2f,
                (float)random.NextDouble()*360);
        }

        Transform references = Group("ReferencePoints", root.transform);
        Empty("SuggestedPlayerStart", references, V(3.65f,0,1.3f));
        Empty("SuggestedToolStation", references, V(-3.6f,1,1));
        Empty("SafeRoomEntry", references, V(2.5f,0,8.5f));
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Store blockout created. Save the scene manually. No XR rig, materials or lights added.", root);
    }

    private static void Display(string name, Vector3 p, Vector3 size, bool damaged)
    {
        Transform group = Group(name, furniture);
        Box("Base", group, p+V(0,0.4f,0), V(size.x,0.8f,size.z));
        Box("DisplaySurface", group, p+V(0,0.82f,0), V(size.x,0.04f,size.z));
        // Frame only: avoid opaque placeholder glass hiding the display.
        foreach (float x in new float[] {-1,1})
        foreach (float z in new float[] {-1,1})
            Box("FramePost", group,
                p+V(x*(size.x/2-0.035f),1.01f,z*(size.z/2-0.035f)),
                V(0.035f,0.36f,0.035f));
        foreach (float x in new float[] {-1,1})
            Box("TopRail", group, p+V(x*(size.x/2-0.035f),1.2f,0),
                V(0.035f,0.035f,size.z));
        foreach (float z in new float[] {-1,1})
            Box("TopRail", group, p+V(0,1.2f,z*(size.z/2-0.035f)),
                V(size.x,0.035f,0.035f));

        for (int i=0;i<4;i++)
        {
            float z = -size.z*0.32f+i*size.z*0.21f;
            Box("JewelryPad_"+i, group, p+V(0,0.87f,z), V(0.32f,0.06f,0.28f));
            // Two empty pads on damaged cases suggest missing stock.
            if (!damaged || i<2)
            {
                if(i%2==0)
                {
                    Primitive("NecklaceBust",PrimitiveType.Sphere,group,
                        p+V(0,0.98f,z),V(0.16f,0.18f,0.1f),false);
                    Ring("Necklace",group,p+V(0,1.0f,z-0.055f),0.06f,0.009f,true);
                }
                else
                {
                    Box("RingStand",group,p+V(0,0.94f,z),V(0.045f,0.08f,0.045f),false);
                    Ring("JewelryRing",group,p+V(0,0.98f,z-0.025f),0.025f,0.007f,true);
                }
            }
        }
        if (damaged)
            for (int i=0;i<5;i++)
                Shard(name+"_DisplayShard_"+i, p+V(0.12f*(i-2),0.913f,0.14f*i),
                    0.12f, i*47);
    }

    private static void Safe()
    {
        Transform g = Group("OpenSafe", furniture);
        Vector3 p = V(2.5f,0,10.55f);
        Box("Bottom",g,p+V(0,0.12f,0),V(1.3f,0.24f,0.8f));
        Box("Top",g,p+V(0,1.62f,0),V(1.3f,0.16f,0.8f));
        Box("Left",g,p+V(-0.57f,0.88f,0),V(0.16f,1.4f,0.8f));
        Box("Right",g,p+V(0.57f,0.88f,0),V(0.16f,1.4f,0.8f));
        Box("Back",g,p+V(0,0.88f,0.34f),V(1.3f,1.4f,0.12f));
        Box("Shelf",g,p+V(0,0.83f,0),V(1.1f,0.06f,0.65f));
        Transform hinge = Group("DoorHinge",g);
        hinge.localPosition=p+V(-0.65f,0,-0.4f);
        Box("Door",hinge,V(0.65f,0.87f,0),V(1.3f,1.7f,0.14f));
        Ring("SafeHandleWheel",hinge,V(0.78f,0.88f,-0.1f),0.12f,0.025f,true);
        for(int i=0;i<3;i++)
        {
            GameObject spoke=Box("WheelSpoke",hinge,V(0.78f,0.88f,-0.1f),V(0.2f,0.018f,0.025f),false);
            spoke.transform.localRotation=Quaternion.Euler(0,0,i*60);
        }
        Primitive("CombinationDial",PrimitiveType.Cylinder,hinge,V(0.35f,1.1f,-0.1f),
            V(0.08f,0.025f,0.08f),false).transform.localRotation=Quaternion.Euler(90,0,0);
        hinge.localRotation=Quaternion.Euler(0,110,0);
    }

    private static void ShoePrint(Vector3 p)
    {
        Transform g = Group("ShoePrint_GeometryPlaceholder",evidence);
        g.localPosition=p;
        g.localRotation=Quaternion.Euler(0,20,0);
        // Flat raised tread geometry for now; final decal/material deferred.
        for(int row=0;row<5;row++)
        for(int side=0;side<2;side++)
            Box("Tread",g,V((side==0?-1:1)*0.027f,0,0.035f*row),
                V(0.04f,0.003f,0.018f),false);
        for(int i=0;i<3;i++)
            Box("HeelTread",g,V(0,0,-0.09f+i*0.019f),V(0.075f,0.003f,0.011f),false);
        Box("ToeTread",g,V(0,0,0.175f),V(0.065f,0.003f,0.014f),false);
    }

    private static void Torch()
    {
        Transform g = Group("DiscardedTorch_Placeholder",evidence);
        Primitive("Cylinder",PrimitiveType.Cylinder,g,V(3.55f,0.3f,9.5f),
            V(0.22f,0.3f,0.22f),true);
        Box("Valve",g,V(3.55f,0.65f,9.5f),V(0.12f,0.1f,0.08f));
        Primitive("CylinderShoulder",PrimitiveType.Sphere,g,V(3.55f,0.59f,9.5f),
            V(0.22f,0.14f,0.22f),false);
        Ring("ValveWheel",g,V(3.55f,0.71f,9.5f),0.05f,0.012f,false);
        Primitive("PressureGauge",PrimitiveType.Cylinder,g,V(3.47f,0.64f,9.5f),
            V(0.07f,0.02f,0.07f),false).transform.localRotation=Quaternion.Euler(0,0,90);
        Box("TorchHandle",g,V(3.15f,0.05f,9.1f),V(0.08f,0.07f,0.3f));
        Box("TorchNozzle",g,V(3.15f,0.05f,8.86f),V(0.035f,0.035f,0.2f));
        // Segmented hose follows the floor, no dynamic simulation.
        for(int i=0;i<8;i++)
            Box("HoseSegment",g,V(3.45f-i*0.04f,0.025f,9.45f-i*0.04f),
                V(0.045f,0.025f,0.08f),false);
    }

    private static void Ring(string name,Transform parent,Vector3 center,
        float radius,float thickness,bool vertical)
    {
        const int segments=12;
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments;
            float b=(i+1)*Mathf.PI*2/segments;
            Vector3 from=vertical?V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):
                V(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
            Vector3 to=vertical?V(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0):
                V(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
            Vector3 delta=to-from;
            GameObject segment=Box(name+"_Segment",parent,center+(from+to)*0.5f,
                V(thickness,thickness,delta.magnitude),false);
            segment.transform.localRotation=Quaternion.LookRotation(delta,
                vertical?Vector3.forward:Vector3.up);
        }
    }

    private static void Shard(string name, Vector3 p, float size, float angle)
    {
        if(shardMesh==null)
        {
            shardMesh=new Mesh { name="BlockoutShard" };
            // Separate vertices for each face preserve opposing normals.
            shardMesh.vertices=new Vector3[] {
                V(-0.5f,0,-0.4f),V(0.5f,0,-0.2f),V(0.1f,0,0.6f),
                V(-0.5f,0,-0.4f),V(0.5f,0,-0.2f),V(0.1f,0,0.6f)};
            shardMesh.triangles=new int[] {0,2,1,3,4,5};
            shardMesh.RecalculateNormals();
            shardMesh.RecalculateBounds();
            // Save generated mesh so scene references survive reopening Unity.
            const string dir="Assets/CrimeSceneDemo/Generated";
            EnsureFolder("Assets","CrimeSceneDemo");
            EnsureFolder("Assets/CrimeSceneDemo","Generated");
            string path=dir+"/BlockoutShard.asset";
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null) { Object.DestroyImmediate(shardMesh); shardMesh=existing; }
            else AssetDatabase.CreateAsset(shardMesh,path);
        }
        GameObject o=new GameObject(name);
        o.transform.SetParent(evidence,false);
        o.transform.localPosition=p;
        o.transform.localScale=V(size,1,size);
        o.transform.localRotation=Quaternion.Euler(0,angle,0);
        o.AddComponent<MeshFilter>().sharedMesh=shardMesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=
            AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    }

    private static void EnsureFolder(string parent,string name)
    {
        if(!AssetDatabase.IsValidFolder(parent+"/"+name))
            AssetDatabase.CreateFolder(parent,name);
    }

    private static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    private static Transform Group(string name,Transform parent)
    {
        GameObject g=new GameObject(name);
        g.transform.SetParent(parent,false);
        return g.transform;
    }
    private static void Empty(string name,Transform parent,Vector3 position)
    {
        Transform t=Group(name,parent);
        t.localPosition=position;
    }
    private static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,bool collision=true)
        => Primitive(name,PrimitiveType.Cube,parent,p,size,collision);
    private static GameObject Primitive(string name,PrimitiveType type,Transform parent,
        Vector3 p,Vector3 size,bool collision)
    {
        GameObject g=GameObject.CreatePrimitive(type);
        g.name=name;
        g.transform.SetParent(parent,false);
        g.transform.localPosition=p;
        g.transform.localScale=size;
        if(!collision) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
}
#endif
