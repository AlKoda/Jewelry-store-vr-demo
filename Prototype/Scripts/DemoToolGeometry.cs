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
        else if (kind == DemoToolKind.Scale)
        {
            // Forensic L-scale: two 15 cm arms with 1 cm black/white bands, laid flat
            // beside evidence so photographs carry a size reference. Arms collide; bands do not.
            Cube("ArmX",root.transform,new Vector3(0.075f,0.0015f,0.01f),new Vector3(0.15f,0.003f,0.02f));
            Cube("ArmZ",root.transform,new Vector3(0.01f,0.0015f,0.085f),new Vector3(0.02f,0.003f,0.15f));
            for (int i=0;i<15;i++)
            {
                string band = i%2==0 ? "BandBlack" : "BandWhite";
                Visual(band,root.transform,new Vector3(0.005f+i*0.01f,0.0033f,0.01f),new Vector3(0.0095f,0.0006f,0.016f));
                if (i>=2) Visual(band,root.transform,new Vector3(0.01f,0.0033f,0.005f+i*0.01f),new Vector3(0.016f,0.0006f,0.0095f));
            }
        }
        else if (kind == DemoToolKind.Measure)
        {
            // Measuring-tape reel, 8 cm across and lying flat: the body collides, the
            // label plate and hook are decoration. Measuring tape stretches between the
            // anchors on top of two reels, exactly as barrier tape does between posts.
            Primitive("Body",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.0125f,0),new Vector3(0.08f,0.0125f,0.08f));
            Visual("Plate",root.transform,new Vector3(0,0.027f,0),new Vector3(0.045f,0.004f,0.03f));
            Visual("Hook",root.transform,new Vector3(0.045f,0.006f,0),new Vector3(0.012f,0.012f,0.02f));
            Anchor(tool,0.03f);
        }
        else
        {
            Primitive("Base",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.025f,0),new Vector3(0.3f,0.025f,0.3f));
            Primitive("Post",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.47f,0),new Vector3(0.04f,0.44f,0.04f));
            Anchor(tool,0.86f);
        }
        return tool;
    }

    public static SceneTape CreateTape()
    {
        GameObject root=new GameObject("SceneTape");
        SceneTape tape=root.AddComponent<SceneTape>();
        tape.Ribbon=Visual("Ribbon",root.transform,Vector3.zero,Vector3.one).transform;
        return tape;
    }

    // Measuring tape: a flat 2.5 cm ribbon with end caps and a distance label, read
    // by SceneTape.Refresh; materials come from the presentation pass like the tools'.
    public static SceneTape CreateMeasureTape()
    {
        SceneTape tape=CreateTape();
        tape.name="MeasureTape";
        tape.Flat=true;
        tape.RibbonHeight=0.025f;
        tape.StartCap=Visual("StartCap",tape.transform,Vector3.zero,new Vector3(0.03f,0.008f,0.02f)).transform;
        tape.EndCap=Visual("EndCap",tape.transform,Vector3.zero,new Vector3(0.03f,0.008f,0.02f)).transform;
        GameObject label=new GameObject("Distance");
        label.transform.SetParent(tape.transform,false);
        TextMesh text=label.AddComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        text.anchor=TextAnchor.LowerCenter;
        text.alignment=TextAlignment.Center;
        text.fontSize=64;
        text.characterSize=0.006f;
        text.color=Color.white;
        tape.DistanceLabel=text;
        return tape;
    }

    private static void Anchor(DeployedTool tool,float height)
    {
        GameObject anchor=new GameObject("TapeAnchor");
        anchor.transform.SetParent(tool.transform,false);
        anchor.transform.localPosition=new Vector3(0,height,0);
        tool.TapeAnchor=anchor.transform;
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
    // Decoration without a collider, so grabbing and pointing go by the arms.
    private static GameObject Visual(string name,Transform parent,Vector3 p,Vector3 size)
    {
        GameObject g=Cube(name,parent,p,size);
        Collider collider=g.GetComponent<Collider>();
        if (Application.isPlaying) Object.Destroy(collider);
        else Object.DestroyImmediate(collider);
        return g;
    }
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
