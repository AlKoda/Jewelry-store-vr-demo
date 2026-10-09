using System;
using UnityEngine;

public sealed class DesktopToolPlacement : MonoBehaviour
{
    public ToolStation Station;
    public EvidenceCamera EvidenceCamera;
    public Camera ViewCamera;
    public bool ReadDesktopInput=true;
    public float Reach=6f;
    public DeployedTool Selected { get; private set; }
    public bool IsPlacing { get; private set; }
    public string Status { get; private set; }="Select a tool or spawn one from the panel.";
    private float yaw;
    private GameObject cursor;
    private Material cursorMaterial;

    private void Start()
    {
        cursor=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursor.name="PlacementPreview";
        Destroy(cursor.GetComponent<Collider>());
        cursor.transform.localScale=new Vector3(.09f,.008f,.09f);
        cursorMaterial=new Material(Shader.Find("Unlit/Color"));
        cursorMaterial.color=new Color(.2f,.85f,.65f);
        cursor.GetComponent<Renderer>().sharedMaterial=cursorMaterial;
        cursor.SetActive(false);
    }

    public void Select(DeployedTool tool)
    {
        Selected=tool;
        IsPlacing=false;
        if(cursor!=null) cursor.SetActive(false);
        Status=tool==null?"No tool selected.":"Selected "+tool.name;
    }

    public void BeginPlacement(DeployedTool tool)
    {
        Select(tool);
        if(tool==null) return;
        yaw=tool.transform.eulerAngles.y;
        IsPlacing=true;
        Status="Point at a floor or display surface. Left click places; Q/E rotates; Escape cancels.";
    }

    public void CancelPlacement()
    {
        IsPlacing=false;
        if(cursor!=null) cursor.SetActive(false);
        Status="Placement cancelled; tool unchanged.";
    }

    public static bool IsInside(Vector3 point)
    {
        if(point.x < -4.7f || point.x > 4.7f || point.z < .3f || point.z > 10.8f) return false;
        return point.z <= 8.1f || (point.x>=1.25f && point.x<=3.75f);
    }

    public bool TryFindPlacement(Ray ray,out Vector3 point)
    {
        point=Vector3.zero;
        RaycastHit[] hits=Physics.RaycastAll(ray,Reach,~0,QueryTriggerInteraction.Ignore);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(RaycastHit hit in hits)
        {
            if(hit.collider.GetComponentInParent<ShopWalkController>()!=null) continue;
            DeployedTool other=hit.collider.GetComponentInParent<DeployedTool>();
            if(other==Selected && Selected!=null) continue;
            // The first solid surface occludes surfaces behind it.
            if(other!=null || hit.normal.y<.65f || !IsInside(hit.point)) return false;
            point=hit.point+Vector3.up*.005f;
            return true;
        }
        return false;
    }

    public bool PlaceSelected(Vector3 position,float rotation)
    {
        if(Selected==null || !IsInside(position)) return false;
        Selected.PlaceAt(position,rotation);
        IsPlacing=false;
        if(cursor!=null) cursor.SetActive(false);
        Status="Placed "+Selected.name;
        return true;
    }

    public void PhotographView()
    {
        if(EvidenceCamera!=null && ViewCamera!=null)
            EvidenceCamera.CaptureFromView(ViewCamera.transform);
    }

    private void Update()
    {
        if(!ReadDesktopInput || ViewCamera==null) return;
        if(Input.GetKeyDown(KeyCode.F)) PhotographView();
        if(Input.GetKeyDown(KeyCode.Escape)) CancelPlacement();
        bool overPanel=Input.mousePosition.x<312 && Input.mousePosition.y>12;
        if(Selected==null)
        {
            IsPlacing=false;
            if(cursor!=null) cursor.SetActive(false);
        }
        if(IsPlacing)
        {
            if(Input.GetKeyDown(KeyCode.Q)) yaw-=15;
            if(Input.GetKeyDown(KeyCode.E)) yaw+=15;
            Vector3 point;
            if(!overPanel && TryFindPlacement(ViewCamera.ScreenPointToRay(Input.mousePosition),out point))
            {
                if(cursor!=null) {cursor.SetActive(true);cursor.transform.position=point;}
                if(Input.GetMouseButtonDown(0)) PlaceSelected(point,yaw);
            }
            else if(cursor!=null) cursor.SetActive(false);
        }
        else if(!overPanel && Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if(Physics.Raycast(ViewCamera.ScreenPointToRay(Input.mousePosition),out hit,Reach))
                Select(hit.collider.GetComponentInParent<DeployedTool>());
        }
    }

    private void OnDisable() { if(cursor!=null) cursor.SetActive(false); }
    private void OnDestroy()
    {
        if(cursor!=null) Destroy(cursor);
        if(cursorMaterial!=null) Destroy(cursorMaterial);
    }
}
