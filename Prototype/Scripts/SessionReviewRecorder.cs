using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local instructor records; no scores or procedure judgments.
public sealed class SessionReviewRecorder : MonoBehaviour
{
    public ToolStation Station;
    public EvidenceCamera EvidenceCamera;
    public DemoSession Session;
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
    [Serializable] public sealed class Entry
    {
        public string utc,reason,photo;
        public Vector3 cameraPosition,cameraRotation;
        public List<ToolRecord> tools=new List<ToolRecord>();
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
            foreach(DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
                entry.tools.Add(new ToolRecord {kind=tool.Kind.ToString(),markerNumber=tool.MarkerNumber,
                    position=tool.transform.position,rotation=tool.transform.eulerAngles});
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

    private void WriteFiles()
    {
        string json=JsonUtility.ToJson(document,true);
        File.WriteAllText(Path.Combine(ReviewFolder,"session.json"),json,new UTF8Encoding(false));
        StringBuilder html=new StringBuilder();
        html.Append("<!doctype html><meta charset=\"utf-8\"><title>Instructor review</title>");
        html.Append("<style>body{font:16px system-ui;max-width:1000px;margin:32px auto;padding:0 20px;color:#222}img{max-width:100%;height:auto}article{border-top:1px solid #ccc;padding:20px 0}td,th{text-align:left;padding:6px}table{border-collapse:collapse}h1{font-size:26px}</style>");
        html.Append("<h1>Jewelry store — instructor review</h1><p>Scene: "+Escape(document.scene)+"</p>");
        html.Append("<p>Started: "+Escape(document.startedUtc)+" (UTC). Positions are Unity world metres. Instructor interpretation is required.</p>");
        foreach(Entry entry in document.entries)
        {
            html.Append("<article><h2>"+Escape(entry.reason)+"</h2><p>"+Escape(entry.utc)+"</p>");
            if(!string.IsNullOrEmpty(entry.photo))
                html.Append("<a href=\""+Escape(entry.photo)+"\"><img alt=\"Saved photograph\" src=\""+Escape(entry.photo)+"\"></a>");
            html.Append("<table><tr><th>Tool</th><th>Marker</th><th>Position (x, y, z)</th></tr>");
            foreach(ToolRecord tool in entry.tools)
                html.Append("<tr><td>"+Escape(tool.kind)+"</td><td>"+(tool.markerNumber>0?tool.markerNumber.ToString():"—")+
                    "</td><td>"+Escape(tool.position.ToString("F2"))+"</td></tr>");
            html.Append("</table></article>");
        }
        File.WriteAllText(Path.Combine(ReviewFolder,"review.html"),html.ToString(),new UTF8Encoding(false));
    }
    private static string Escape(string value) { return WebUtility.HtmlEncode(value??""); }
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
