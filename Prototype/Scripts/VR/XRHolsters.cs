using UnityEngine;
using UnityEngine.XR;

// Body holsters in VR, placed from the head so they follow the trainee standing,
// sitting or bending: the white flashlight on the right hip, the UV torch on the left
// hip and the evidence camera on the chest. Grip with a hand inside a holster's
// TakeRadius takes its item into that hand (a torch comes on in that hand and stays
// there without holding grip); grip again at the same holster with that torch in the
// hand puts it back (off). The camera is held with grip like before: releasing grip
// returns it to its rack. A faint ring marks each holster and brightens when a hand is
// close. Desktop mode never shows them. XRControllerInput calls TryUse on every grip
// press before the normal grab; the validator calls the same methods with plain transforms.
public sealed class XRHolsters : MonoBehaviour
{
    public enum Slot { None = -1, Flashlight, UVTorch, Camera }

    public static XRHolsters Active { get; private set; }
    public Transform Head;
    public DemoLighting Lighting;
    public EvidenceCamera Camera;
    public float TakeRadius = 0.16f;
    // Offsets from the head in its heading frame (x right, y up, z forward).
    public Vector3 HipOffset = new Vector3(0.2f, -0.62f, 0.04f);
    public Vector3 ChestOffset = new Vector3(0, -0.62f, 0.20f);
    // Holsters never go lower than this above the rig's floor (a crouching trainee).
    public float MinHeight = 0.25f;

    private readonly Transform[] markers = new Transform[3];
    private readonly Material[] materials = new Material[3];
    private Transform root;
    Transform belt;float beltYaw,yawVelocity;bool headingReady;
    float standingHeadHeight, retreat=.13f;
    // Height loss, rather than looking down, moves the belt clear of floor pickups.
    public static float BendRetreat(float heightLoss)=>.13f+Mathf.Clamp01((heightLoss-.10f)/.45f)*.28f;
    public void UpdateBody(float dt){
        var head=Head?Head:transform;float height=head.position.y-transform.position.y;
        if(standingHeadHeight<=0)standingHeadHeight=Mathf.Clamp(height,1.2f,2.1f);
        standingHeadHeight=Mathf.Max(standingHeadHeight,Mathf.Min(height,2.1f));
        retreat=Mathf.Lerp(retreat,BendRetreat(standingHeadHeight-height),1-Mathf.Exp(-10*dt));
    }
    readonly GameObject[] displays=new GameObject[3];
    public void UpdateHeading(float dt){
        var head=Head?Head:transform;var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up);
        if(forward.sqrMagnitude<.04f)return;
        float wanted=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
        if(!headingReady){beltYaw=wanted;headingReady=true;}else beltYaw=Mathf.SmoothDampAngle(beltYaw,wanted,ref yawVelocity,.2f,180,dt);
    }
    void BuildBelt(){
        belt=new GameObject("Utility belt").transform;belt.SetParent(root,false);
        var v=new Vector3[50];var uv=new Vector2[50];var triangles=new int[24*6];
        for(int i=0;i<=24;i++){float a=i*Mathf.PI*2/24;for(int j=0;j<2;j++){int n=i*2+j;v[n]=new Vector3(Mathf.Cos(a)*.24f,(j-.5f)*.045f,Mathf.Sin(a)*.16f);uv[n]=new Vector2(i/24f,j);}}
        for(int i=0;i<24;i++){int a=i*2,t=i*6;triangles[t]=a;triangles[t+1]=a+2;triangles[t+2]=a+1;triangles[t+3]=a+1;triangles[t+4]=a+2;triangles[t+5]=a+3;}
        var mesh=new Mesh{name="Utility belt band",vertices=v,uv=uv,triangles=triangles};mesh.RecalculateNormals();belt.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        belt.gameObject.AddComponent<MeshRenderer>().sharedMaterial=DemoUi.UnlitMaterial(new Color(.055f,.045f,.03f,.95f));
        for(int i=0;i<3;i++){
            var go=i<2?DemoToolGeometry.CreateFlashlight(i==1):new GameObject("Belt camera");if(i==2)DemoToolGeometry.CreateCameraBody(go.transform);
            go.transform.SetParent(root,false);displays[i]=go;
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach(var c in go.GetComponentsInChildren<Collider>())Destroy(c);
        }
    }
    private DemoModeSwitch mode;
    private bool modeSearched;

    private void OnEnable() { Active = this; }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    public Vector3 Position(Slot slot)
    {
        Transform head = Head != null ? Head : transform;
        Quaternion heading=Quaternion.Euler(0,headingReady?beltYaw:head.eulerAngles.y,0);
        Vector3 offset = slot == Slot.Camera ? ChestOffset
            : new Vector3(slot == Slot.Flashlight ? HipOffset.x : -HipOffset.x, HipOffset.y, HipOffset.z);
        offset.z-=retreat>0?retreat:.13f;
        Vector3 p = head.position + heading * offset;
        p.y = Mathf.Max(p.y, transform.position.y + MinHeight);
        return p;
    }

    public Slot SlotNear(Vector3 hand)
    {
        Slot best = Slot.None;
        float bestDistance = TakeRadius;
        for (int i = 0; i < 3; i++)
        {
            float d = Vector3.Distance(hand, Position((Slot)i));
            if (d <= bestDistance) { bestDistance = d; best = (Slot)i; }
        }
        return best;
    }

    public bool TryUse(XRControllerInput input) => input != null && TryUse(input.Hand, input.Node);

    // Takes from or returns to the holster the hand is in; false when the hand is in none or nothing changed.
    public bool TryUse(HandInteractor hand, XRNode node)
    {
        if (hand == null || !VR) return false;
        Slot slot = SlotNear(hand.transform.position);
        DemoLighting lights = Lighting != null ? Lighting : FindFirstObjectByType<DemoLighting>();
        switch (slot)
        {
            case Slot.Flashlight:
            case Slot.UVTorch:
                if (lights == null || hand.Held != null || hand.HoldingCamera) return false;
                bool uv = slot == Slot.UVTorch;
                bool on = uv ? lights.UVTorchOn : lights.FlashlightOn;
                XRNode at = uv ? lights.UVTorchHand : lights.FlashlightHand;
                if (lights.TorchEquipped(uv) && at == node)
                {
                    lights.HolsterTorch(uv);
                    DemoFeedback.Post(uv ? "UV torch holstered" : "Flashlight holstered");
                    return true;
                }
                if(lights.TorchEquipped(!uv) && (uv?lights.FlashlightHand:lights.UVTorchHand)==node)return false;
                lights.MoveTorch(uv, node);
                if (!on) { if (uv) lights.ToggleUVTorch(); else lights.ToggleFlashlight(); }
                return true;
            case Slot.Camera:
                if (hand.Held != null || hand.HoldingCamera) return false;
                EvidenceCamera camera = Camera != null ? Camera : hand.EvidenceCamera;
                if (camera == null || (camera.Holder != null && camera.Holder != hand.transform)) return false;
                hand.ToggleCamera();
                return hand.HoldingCamera;
        }
        return false;
    }

    private bool VR
    {
        get
        {
            if (mode == null && !modeSearched) { mode = FindFirstObjectByType<DemoModeSwitch>(); modeSearched = true; }
            return mode == null || mode.VRActive;
        }
    }

    // Found once: these were two scene searches and one array allocation per frame.
    private HandInteractor[] hands = new HandInteractor[0];
    private XRControllerInput[] handInputs = new XRControllerInput[0];
    private float handsRefreshed = -10;
    private void RefreshHands()
    {
        hands = FindObjectsByType<HandInteractor>(FindObjectsSortMode.None);
        handInputs = new XRControllerInput[hands.Length];
        for (int i = 0; i < hands.Length; i++) handInputs[i] = hands[i].GetComponent<XRControllerInput>();
        handsRefreshed = Time.unscaledTime;
    }
    private void Start()
    {
        HipOffset.y-=.08f;ChestOffset.y-=.08f;
        if (Lighting == null) Lighting = FindFirstObjectByType<DemoLighting>();
        if (Camera == null) Camera = FindFirstObjectByType<EvidenceCamera>();
        RefreshHands();
    }
    private void LateUpdate()
    {
        UpdateHeading(Time.deltaTime);
        UpdateBody(Time.deltaTime);
        bool show = VR;
        if (!show) { if (root != null) root.gameObject.SetActive(false); return; }
        if (root == null) Build();
        root.gameObject.SetActive(true);
        var head=Head?Head:transform;
        belt.rotation=Quaternion.Euler(0,beltYaw,0);belt.position=head.position+Vector3.up*HipOffset.y-belt.forward*retreat;
        var beltPosition=belt.position;beltPosition.y=Mathf.Max(beltPosition.y,transform.position.y+MinHeight);belt.position=beltPosition;
        var lights=Lighting;var camera=Camera;
        // Hands are re-collected only every 2 s (a rig rebuild or a late-enabled hand), not per frame.
        if (hands.Length < 2 && Time.unscaledTime - handsRefreshed > 2f) RefreshHands();
        for(int i=0;i<3;i++){
            displays[i].SetActive(i<2?lights==null||!lights.TorchEquipped(i==1):camera==null||camera.Holder==null);
            displays[i].transform.SetPositionAndRotation(Position((Slot)i)+(i<2?Vector3.up*.12f:Vector3.zero),Quaternion.Euler(i<2?-90:0,beltYaw,0));
        }
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = Position((Slot)i);
            markers[i].position = p;
            if (Head != null) DemoUi.FaceViewer(markers[i], Head.position);
            bool near = false;
            float proximity=float.PositiveInfinity;
            for (int h = 0; h < hands.Length; h++)
            {
                if (hands[h] == null) continue;
                float d = Vector3.Distance(hands[h].transform.position, p);
                near |= d <= TakeRadius;
                if (handInputs[h] != null && handInputs[h].PoseTracked) proximity = Mathf.Min(proximity, d);
            }
            markers[i].gameObject.SetActive(proximity<.45f || XRControllerInput.HintsVisible);
            materials[i].color = DemoUi.WithAlpha(near ? DemoUi.Accent : DemoUi.Info, near ? 0.85f : 0.22f);
        }
    }

    private void Build()
    {
        root = new GameObject("Holsters").transform;
        root.SetParent(transform, false);
        UiIcon[] icons = { UiIcon.Flashlight, UiIcon.UVTorch, UiIcon.Camera };
        for (int i = 0; i < 3; i++)
        {
            GameObject marker = new GameObject("Holster_" + (Slot)i);
            marker.transform.SetParent(root, false);
            GameObject ring = new GameObject("Ring");
            ring.transform.SetParent(marker.transform, false);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0); // FlatRing lies flat; stand it up facing the eye
            ring.AddComponent<MeshFilter>().sharedMesh = DemoUi.FlatRing(0.05f, 0.006f);
            MeshRenderer r = ring.AddComponent<MeshRenderer>();
            materials[i] = DemoUi.UnlitMaterial(DemoUi.WithAlpha(DemoUi.Info, 0.22f));
            r.sharedMaterial = materials[i];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(marker.transform, false);
            icon.AddComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(0.045f, 0.045f, 0, 1);
            MeshRenderer ir = icon.AddComponent<MeshRenderer>();
            ir.sharedMaterial = DemoUi.IconMaterial(icons[i], DemoUi.WithAlpha(DemoUi.Ink, 0.6f));
            ir.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markers[i] = marker.transform;
        }
        BuildBelt();
        DemoUi.ToHeadsetUiLayer(root);
    }

    private void OnDestroy()
    {
        foreach (Material m in materials) if (m != null) Destroy(m);
        if (root != null)
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.sharedMaterial != null && System.Array.IndexOf(materials, r.sharedMaterial) < 0) Destroy(r.sharedMaterial);
    }
}
