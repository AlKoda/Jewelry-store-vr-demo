using UnityEngine;

// Shared by the Editor prefab builder. No materials or textures are assigned.
public static class DemoToolGeometry
{
    public static DeployedTool Create(DemoToolKind kind)
    {
        GameObject root = new GameObject(kind.ToString());
        DeployedTool tool = root.AddComponent<DeployedTool>();
        tool.Kind = kind;
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (kind == DemoToolKind.Cone)
        {
            Cube("Base", root.transform, new Vector3(0,0.025f,0), new Vector3(0.3f,0.05f,0.3f));
            // Stepped taper avoids a custom mesh asset in this first batch.
            for (int i=0;i<8;i++)
            {
                float diameter = 0.24f - i*0.025f;
                Primitive("Taper", PrimitiveType.Cylinder, root.transform,
                    new Vector3(0,0.075f+i*0.05f,0), new Vector3(diameter,0.025f,diameter));
            }
        }
        else if (kind == DemoToolKind.Marker)
        {
            Cube("Foot",root.transform,new Vector3(0,0.012f,0),new Vector3(0.18f,0.024f,0.16f));
            GameObject front = Cube("Front",root.transform,new Vector3(0,0.105f,-0.034f),
                new Vector3(0.16f,0.2f,0.008f));
            front.transform.localRotation=Quaternion.Euler(20,0,0);
            GameObject back = Cube("Back",root.transform,new Vector3(0,0.105f,0.034f),
                new Vector3(0.16f,0.2f,0.008f));
            back.transform.localRotation=Quaternion.Euler(-20,0,0);
            tool.NumberLabel = Label(front.transform, false);
            tool.BackNumberLabel = Label(back.transform, true);
            tool.SetMarkerNumber(1);
        }
        else
        {
            Primitive("Base",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.025f,0),new Vector3(0.3f,0.025f,0.3f));
            Primitive("Post",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.47f,0),new Vector3(0.04f,0.44f,0.04f));
            GameObject anchor=new GameObject("TapeAnchor");
            anchor.transform.SetParent(root.transform,false);
            anchor.transform.localPosition=new Vector3(0,0.86f,0);
            tool.TapeAnchor=anchor.transform;
        }
        return tool;
    }

    public static SceneTape CreateTape()
    {
        GameObject root=new GameObject("SceneTape");
        SceneTape tape=root.AddComponent<SceneTape>();
        GameObject ribbon=Cube("Ribbon",root.transform,Vector3.zero,Vector3.one);
        Collider collider=ribbon.GetComponent<Collider>();
        if (Application.isPlaying) Object.Destroy(collider);
        else Object.DestroyImmediate(collider);
        tape.Ribbon=ribbon.transform;
        return tape;
    }

    private static TextMesh Label(Transform panel, bool reverse)
    {
        GameObject label=new GameObject("Number");
        label.transform.SetParent(panel,false);
        // Panel's unit cube face is at +/-0.5 along local z.
        label.transform.localPosition=new Vector3(0,0,reverse?0.6f:-0.6f);
        label.transform.localRotation=Quaternion.Euler(0,reverse?180:0,0);
        // Counteract the thin, nonuniform panel scale.
        label.transform.localScale=new Vector3(1/0.16f,1/0.2f,1/0.008f);
        TextMesh text=label.AddComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        text.anchor=TextAnchor.MiddleCenter;
        text.alignment=TextAlignment.Center;
        text.fontSize=64;
        text.characterSize=0.019f;
        text.color=Color.black;
        return text;
    }

    private static GameObject Cube(string name,Transform parent,Vector3 p,Vector3 size)
        => Primitive(name,PrimitiveType.Cube,parent,p,size);
    private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 size)
    {
        GameObject g=GameObject.CreatePrimitive(type);
        g.name=name;
        g.transform.SetParent(parent,false);
        g.transform.localPosition=p;
        g.transform.localScale=size;
        return g;
    }
}
