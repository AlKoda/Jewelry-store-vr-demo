using UnityEngine;
// Swept contacts follow tracked poses without driving a dynamic controller body.
// Tracking jumps are ignored; both the palm and fingertips can push a door.
public sealed class VRDoorHandPush : MonoBehaviour
{
    public XRControllerInput Input;
    public float Radius = .065f;
    Vector3 previous;
    bool anchored;
    DemoModeSwitch mode;
    GameObject visual;
    Material material;
    Renderer[] controllerParts;
    bool lastActive, appliedOnce;
    static readonly RaycastHit[] sweepHits = new RaycastHit[16];
    static readonly Collider[] overlapHits = new Collider[16];
    void Start()
    {
        mode = FindFirstObjectByType<DemoModeSwitch>();
        if(GetComponent<XRGhostHand>())return;
        visual = new GameObject("Translucent contact hand");
        visual.transform.SetParent(transform, false);
        material = new Material(Shader.Find("Standard"));
        material.color = new Color(.65f,.82f,.88f,.22f);
        material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",1);material.SetInt("_DstBlend",10);
        material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
        Part(new Vector3(0,0,.035f),new Vector3(.075f,.035f,.09f));
        for(int i=0;i<4;i++)Part(new Vector3((i-1.5f)*.019f,0,.105f),new Vector3(.015f,.025f,.07f-Mathf.Abs(i-1.5f)*.01f));
        Part(new Vector3(Input&&Input.Left?.05f:-.05f,-.005f,.045f),new Vector3(.035f,.028f,.055f));
    }
    void Part(Vector3 position,Vector3 size)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.name="Hand";part.transform.SetParent(visual.transform,false);
        part.transform.localPosition=position;part.transform.localScale=size;Destroy(part.GetComponent<Collider>());
        var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    void LateUpdate()
    {
        bool active=mode&&mode.VRActive&&Input&&Input.PoseTracked;
        if(visual&&visual.activeSelf!=active)visual.SetActive(active);
        // The controller model's renderers are collected once and only touched when the state flips.
        if(controllerParts==null){var controller=transform.Find("ControllerModel");if(controller)controllerParts=controller.GetComponentsInChildren<Renderer>(true);}
        if(controllerParts!=null&&(!appliedOnce||active!=lastActive)){foreach(var renderer in controllerParts)if(renderer)renderer.forceRenderingOff=active;lastActive=active;appliedOnce=true;}
        ProcessPose(transform.TransformPoint(new Vector3(0,0,.07f)),active);
    }
    public void ProcessPose(Vector3 position,bool active)
    {
        if(!active||!anchored){previous=position;anchored=active;return;}
        var delta=position-previous;var from=previous;previous=position;float length=delta.magnitude;
        if(length<.002f||length>.5f)return;
        int sweeps=Physics.SphereCastNonAlloc(from,Radius,delta/length,sweepHits,length+.015f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<sweeps;i++){var door=sweepHits[i].collider.GetComponentInParent<PushDoorLeaf>();if(door)door.Push(sweepHits[i].point,delta/length);}
        int overlaps=Physics.OverlapSphereNonAlloc(position,Radius,overlapHits,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<overlaps;i++){var door=overlapHits[i].GetComponentInParent<PushDoorLeaf>();if(door)door.Push(overlapHits[i].ClosestPoint(position),delta/length);}
    }
    void OnDestroy(){if(material)Destroy(material);}
}
