using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local instructor records; no scores or procedure judgments. review.html is a
// contact sheet of the photographs, a shot list of every entry and one sketched
// plan per entry, all inline: no scripts, fonts or images beyond the copied photos.
public sealed class SessionReviewRecorder : MonoBehaviour
{
    public ToolStation Station;
    public EvidenceCamera EvidenceCamera;
    public DemoSession Session;
    // Shop outline for the sketches: the player's bounds when wired, otherwise the
    // first in the scene, otherwise the default regions added to this object.
    public InteriorBounds Bounds;
    public string Status { get; private set; }="Review ready";
    public string ReviewFolder => Path.Combine(Application.persistentDataPath,"InstructorReviews",sessionId);
    private readonly string sessionId=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
    private readonly ReviewDocument document=new ReviewDocument();
    private int sequence;

    [Serializable] public sealed class ToolRecord
    {
        public string kind;
        public int markerNumber;
        public Vector3 position;
        public Vector3 rotation;
    }
    // A ribbon between two recorded tools (indices into the entry's tools list);
    // kind is the endpoint kind, TapePost or Measure.
    [Serializable] public sealed class ConnectionRecord
    {
        public int from,to;
        public string kind;
    }
    [Serializable] public sealed class Entry
    {
        public string utc,reason,photo;
        public Vector3 cameraPosition,cameraRotation;
        public List<ToolRecord> tools=new List<ToolRecord>();
        // Added later; documents written before it read back with an empty list.
        public List<ConnectionRecord> connections=new List<ConnectionRecord>();
    }
    [Serializable] public sealed class ReviewDocument
    {
        public int schemaVersion=1;
        public string sessionId,scene,startedUtc;
        public List<Entry> entries=new List<Entry>();
    }

    private void OnEnable()
    {
        if(document.startedUtc==null)
        {
            document.sessionId=sessionId;
            document.startedUtc=DateTime.UtcNow.ToString("o");
            document.scene=SceneManager.GetActiveScene().name;
        }
        if(EvidenceCamera!=null) EvidenceCamera.PhotoSaved.AddListener(OnPhoto);
        if(Session!=null) Session.Resetting.AddListener(OnReset);
    }
    private void OnDisable()
    {
        if(EvidenceCamera!=null) EvidenceCamera.PhotoSaved.RemoveListener(OnPhoto);
        if(Session!=null) Session.Resetting.RemoveListener(OnReset);
    }
    public void SaveSnapshot() { Record("Manual snapshot",null); }
    private void OnPhoto(string path) { Record("Photograph",path); }
    private void OnReset() { Record("Before scene reset",null); }

    private void Record(string reason,string originalPhoto)
    {
        Entry entry=new Entry {utc=DateTime.UtcNow.ToString("o"),reason=reason,photo=""};
        if(EvidenceCamera!=null)
        {
            entry.cameraPosition=EvidenceCamera.LastCapturePosition;
            entry.cameraRotation=EvidenceCamera.LastCaptureRotation.eulerAngles;
        }
        if(Station!=null && Station.DeploymentRoot!=null)
        {
            DeployedTool[] deployed=Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>();
            foreach(DeployedTool tool in deployed)
                entry.tools.Add(new ToolRecord {kind=tool.Kind.ToString(),markerNumber=tool.MarkerNumber,
                    position=tool.transform.position,rotation=tool.transform.eulerAngles});
            // Ribbons know only their anchors, which hang under the tools they join.
            foreach(SceneTape tape in Station.DeploymentRoot.GetComponentsInChildren<SceneTape>())
            {
                int from=IndexOf(deployed,tape.StartAnchor), to=IndexOf(deployed,tape.EndAnchor);
                if(from>=0 && to>=0) entry.connections.Add(new ConnectionRecord {from=from,to=to,kind=deployed[from].Kind.ToString()});
            }
        }
        document.entries.Add(entry);
        try
        {
            Directory.CreateDirectory(ReviewFolder);
            if(!string.IsNullOrEmpty(originalPhoto))
            {
                string name="Photo_"+(++sequence).ToString("D4")+".png";
                File.Copy(originalPhoto,Path.Combine(ReviewFolder,name),true);
                entry.photo=name;
            }
            WriteFiles();
            Status="Review saved: "+document.entries.Count+" entries";
        }
        catch(Exception exception)
        {
            // Keep the entry in memory if it was appended; manual save retries writing.
            Status="Review save failed: "+exception.Message;
            Debug.LogError(Status,this);
        }
    }

    private static int IndexOf(DeployedTool[] tools,Transform anchor)
    {
        DeployedTool tool=anchor!=null?anchor.GetComponentInParent<DeployedTool>():null;
        return tool!=null?Array.IndexOf(tools,tool):-1;
    }

    private void WriteFiles()
    {
        string json=JsonUtility.ToJson(document,true);
        File.WriteAllText(Path.Combine(ReviewFolder,"session.json"),json,new UTF8Encoding(false));
        StringBuilder html=new StringBuilder();
        html.Append("<!doctype html><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Instructor review</title>");
        html.Append("<style>body{font:15px system-ui,sans-serif;max-width:1100px;margin:32px auto;padding:0 20px;color:#222;background:#fff}");
        html.Append("h1{font-size:26px}h2{font-size:20px;margin:36px 0 12px}h3{font-size:17px;margin:0 0 6px}");
        html.Append(".sheet{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,320px));gap:14px}");
        html.Append(".sheet figure{margin:0}.sheet img{display:block;width:100%;max-width:320px;height:auto;border-radius:4px;background:#000}");
        html.Append("figcaption{font-size:13px;color:#555;margin-top:4px}");
        html.Append("table{border-collapse:collapse;width:100%}td,th{text-align:left;padding:6px 8px;border-bottom:1px solid #ddd;font-size:14px;vertical-align:top}");
        html.Append("article{border-top:1px solid #ccc;padding:20px 0;display:flex;gap:20px;flex-wrap:wrap;align-items:flex-start}");
        html.Append(".plan{flex:0 0 240px}.details{flex:1 1 320px;min-width:0}.details img{display:block;max-width:320px;height:auto;margin:8px 0}");
        html.Append("svg.sketch{display:block;background:#16181c;border-radius:4px}</style>");
        html.Append("<h1>Jewelry store — instructor review</h1><p>Scene: "+Escape(document.scene)+"</p>");
        html.Append("<p>Started: "+Escape(document.startedUtc)+" (UTC). Times below are local. Positions are Unity world metres; sketches look down on the shop with the street at the bottom. Instructor interpretation is required.</p>");

        // Contact sheet: one lazy-loaded tile per photograph, numbered in capture order.
        html.Append("<section class=\"contact-sheet\"><h2>Contact sheet</h2>");
        StringBuilder tiles=new StringBuilder();
        int shot=0;
        foreach(Entry entry in document.entries)
        {
            if(string.IsNullOrEmpty(entry.photo)) continue;
            shot++;
            tiles.Append("<figure><a href=\""+Escape(entry.photo)+"\"><img loading=\"lazy\" alt=\"Photograph "+shot+"\" src=\""+Escape(entry.photo)+"\"></a>");
            tiles.Append("<figcaption>#"+shot+" · "+LocalTime(entry.utc)+" · "+CameraCaption(entry)+"</figcaption></figure>");
        }
        html.Append(shot==0?"<p>No photographs yet.</p>":"<div class=\"sheet\">"+tiles+"</div>");
        html.Append("</section>");

        // Shot list: every entry on one row, photograph or not.
        html.Append("<section class=\"shots\"><h2>Shot list</h2><table class=\"shot-list\"><tr><th>#</th><th>Time</th><th>Reason</th><th>Photo</th><th>Tools</th><th>Markers</th></tr>");
        for(int i=0;i<document.entries.Count;i++)
        {
            Entry entry=document.entries[i];
            html.Append("<tr><td>"+(i+1)+"</td><td>"+LocalTime(entry.utc)+"</td><td>"+Escape(entry.reason)+"</td><td>"+
                (string.IsNullOrEmpty(entry.photo)?"—":"<a href=\""+Escape(entry.photo)+"\">"+Escape(entry.photo)+"</a>")+
                "</td><td>"+entry.tools.Count+"</td><td>"+Markers(entry)+"</td></tr>");
        }
        html.Append("</table></section>");

        // Entries: sketch beside the photograph and the tool table.
        html.Append("<section class=\"entries\"><h2>Entries</h2>");
        for(int i=0;i<document.entries.Count;i++)
        {
            Entry entry=document.entries[i];
            html.Append("<article><div class=\"plan\">");
            AppendSketch(html,entry);
            html.Append("</div><div class=\"details\"><h3>"+(i+1)+". "+Escape(entry.reason)+"</h3><p>"+LocalTime(entry.utc)+" local · "+Escape(entry.utc)+" · "+CameraCaption(entry)+"</p>");
            if(!string.IsNullOrEmpty(entry.photo))
                html.Append("<a href=\""+Escape(entry.photo)+"\"><img loading=\"lazy\" alt=\"Saved photograph\" src=\""+Escape(entry.photo)+"\"></a>");
            html.Append("<table><tr><th>Tool</th><th>Marker</th><th>Position (x, y, z)</th></tr>");
            foreach(ToolRecord tool in entry.tools)
                html.Append("<tr><td>"+Escape(tool.kind)+"</td><td>"+(tool.markerNumber>0?tool.markerNumber.ToString():"—")+
                    "</td><td>"+Escape(tool.position.ToString("F2"))+"</td></tr>");
            html.Append("</table></div></article>");
        }
        html.Append("</section>");
        File.WriteAllText(Path.Combine(ReviewFolder,"review.html"),html.ToString(),new UTF8Encoding(false));
    }

    private static string Escape(string value) { return WebUtility.HtmlEncode(value??""); }
    // Invariant formatting: SVG coordinates and captions need a decimal point on every locale.
    private static string F(float value) { return value.ToString("0.###",CultureInfo.InvariantCulture); }

    // Entries store UTC round-trip strings; the instructor reads wall-clock time.
    private static string LocalTime(string utc)
    {
        DateTime time;
        return DateTime.TryParseExact(utc,"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out time)
            ? time.ToLocalTime().ToString("HH:mm:ss",CultureInfo.InvariantCulture) : Escape(utc);
    }

    // Camera position to the centimetre and its yaw to the degree.
    private static string CameraCaption(Entry entry)
    {
        Vector3 p=entry.cameraPosition;
        return "camera "+p.x.ToString("F2",CultureInfo.InvariantCulture)+", "+p.y.ToString("F2",CultureInfo.InvariantCulture)+", "+
            p.z.ToString("F2",CultureInfo.InvariantCulture)+" m · yaw "+(Mathf.RoundToInt(entry.cameraRotation.y)%360)+"°";
    }

    private static string Markers(Entry entry)
    {
        List<int> numbers=new List<int>();
        foreach(ToolRecord tool in entry.tools) if(tool.markerNumber>0) numbers.Add(tool.markerNumber);
        if(numbers.Count==0) return "—";
        numbers.Sort();
        return string.Join(", ",numbers);
    }

    // --- Scene sketch: an inline SVG whose user units are world centimetres, so
    // coordinates, strokes and font sizes are ordinary SVG magnitudes (browsers may
    // clamp fractional font sizes before the viewBox scales them) ---

    private const float SketchScale=100;

    private InteriorBounds SketchBounds
    {
        get
        {
            if(Bounds==null) Bounds=FindFirstObjectByType<InteriorBounds>();
            if(Bounds==null) Bounds=InteriorBounds.On(gameObject);
            return Bounds;
        }
    }

    // Drawing extents: the regions plus half a metre of margin, used as the SVG viewBox.
    public static Rect SketchViewBox(InteriorBounds.Region[] regions)
    {
        float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
        if(regions!=null)
            foreach(InteriorBounds.Region region in regions)
            {
                minX=Mathf.Min(minX,region.MinX); maxX=Mathf.Max(maxX,region.MaxX);
                minZ=Mathf.Min(minZ,region.MinZ); maxZ=Mathf.Max(maxZ,region.MaxZ);
            }
        if(minX>maxX) { minX=-4.5f; maxX=4.5f; minZ=0.5f; maxZ=10.5f; } // nothing to outline: a shop-sized page
        return Rect.MinMaxRect((minX-0.5f)*SketchScale,(minZ-0.5f)*SketchScale,(maxX+0.5f)*SketchScale,(maxZ+0.5f)*SketchScale);
    }
    public Rect SketchViewBox() { return SketchViewBox(SketchBounds.Regions); }

    // World x runs right across the page and world z up it, so z is flipped inside
    // the box (SVG y grows downward).
    public static Vector2 SketchPoint(Vector3 world,Rect viewBox)
    {
        return new Vector2(world.x*SketchScale,viewBox.yMin+viewBox.yMax-world.z*SketchScale);
    }

    private void AppendSketch(StringBuilder html,Entry entry)
    {
        Rect box=SketchViewBox();
        int height=Mathf.RoundToInt(240*box.height/box.width);
        html.Append("<svg class=\"sketch\" width=\"240\" height=\""+height+"\" viewBox=\""+F(box.xMin)+" "+F(box.yMin)+" "+F(box.width)+" "+F(box.height)+
            "\" role=\"img\" aria-label=\"Plan of the shop with tools and camera\">");
        foreach(InteriorBounds.Region region in SketchBounds.Regions)
        {
            Vector2 corner=SketchPoint(new Vector3(region.MinX,0,region.MaxZ),box);
            html.Append("<rect x=\""+F(corner.x)+"\" y=\""+F(corner.y)+"\" width=\""+F((region.MaxX-region.MinX)*SketchScale)+"\" height=\""+F((region.MaxZ-region.MinZ)*SketchScale)+
                "\" fill=\"#2a2e35\" stroke=\"#8d96a3\" stroke-width=\"6\"/>");
        }
        foreach(ConnectionRecord link in entry.connections)
        {
            if(link.from<0 || link.from>=entry.tools.Count || link.to<0 || link.to>=entry.tools.Count) continue;
            Vector2 a=SketchPoint(entry.tools[link.from].position,box), b=SketchPoint(entry.tools[link.to].position,box);
            html.Append("<line x1=\""+F(a.x)+"\" y1=\""+F(a.y)+"\" x2=\""+F(b.x)+"\" y2=\""+F(b.y)+"\" stroke=\""+(link.kind=="Measure"?"#ffd54a":"#ff4033")+
                "\" stroke-width=\"5\"/>");
        }
        foreach(ToolRecord tool in entry.tools) AppendGlyph(html,tool,SketchPoint(tool.position,box));
        // No capture yet leaves the camera at the origin; drawing it there would mislead.
        if(entry.cameraPosition!=Vector3.zero)
        {
            Vector2 c=SketchPoint(entry.cameraPosition,box);
            html.Append("<polygon points=\"0,-42 24,18 -24,18\" fill=\"#3ee6ff\" transform=\"translate("+F(c.x)+" "+F(c.y)+") rotate("+F(entry.cameraRotation.y)+")\"/>");
        }
        html.Append("</svg>");
    }

    // One glyph per kind in the presenter map's colours: cone triangle, numbered
    // marker square, tape post circle, white L-scale, measuring reel dot. Unity yaw
    // and SVG rotate both turn clockwise when seen from above.
    private static void AppendGlyph(StringBuilder html,ToolRecord tool,Vector2 p)
    {
        string at="translate("+F(p.x)+" "+F(p.y)+")";
        switch(tool.kind)
        {
            case "Cone": html.Append("<polygon points=\"0,-26 24,16 -24,16\" fill=\"#ff8c1a\" transform=\""+at+"\"/>"); break;
            case "Marker":
                html.Append("<rect x=\"-22\" y=\"-22\" width=\"44\" height=\"44\" fill=\"#ffe633\" transform=\""+at+"\"/>");
                html.Append("<text font-size=\"34\" font-family=\"system-ui,sans-serif\" font-weight=\"bold\" text-anchor=\"middle\" dominant-baseline=\"central\" fill=\"#111\" transform=\""+at+"\">"+tool.markerNumber+"</text>");
                break;
            case "TapePost": html.Append("<circle r=\"17\" fill=\"#ff4033\" transform=\""+at+"\"/>"); break;
            case "Scale": html.Append("<polyline points=\"0,-30 0,0 30,0\" fill=\"none\" stroke=\"#fff\" stroke-width=\"8\" transform=\""+at+" rotate("+F(tool.rotation.y)+")\"/>"); break;
            default: html.Append("<circle r=\"12\" fill=\"#ffd54a\" transform=\""+at+"\"/>"); break;
        }
    }

    public void OpenReview()
    {
        try
        {
            Directory.CreateDirectory(ReviewFolder);
            WriteFiles();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName=Path.Combine(ReviewFolder,"review.html"),UseShellExecute=true});
#else
            Debug.Log("Review folder: "+ReviewFolder,this);
#endif
        }
        catch(Exception exception) {Status="Cannot open review: "+exception.Message;Debug.LogError(Status,this);}
    }
}
