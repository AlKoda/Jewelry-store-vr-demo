using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

[Serializable] public sealed class PhotoPathEvent : UnityEvent<string> { }

public sealed class EvidenceCamera : MonoBehaviour
{
    [SerializeField] private Camera photoCamera;
    [SerializeField, Min(1)] private int width = 1920;
    [SerializeField, Min(1)] private int height = 1080;
    [SerializeField] private AudioSource shutterAudio;
    public PhotoPathEvent PhotoSaved = new PhotoPathEvent();
    public PhotoPathEvent CaptureFailed = new PhotoPathEvent();
    public UnityEvent Shutter = new UnityEvent();
    public string LastPhotoPath { get; private set; }
    public string Status { get; private set; } = "Ready";
    public bool IsCapturing { get; private set; }
    public string PhotoFolder => Path.Combine(Application.persistentDataPath,"EvidencePhotos");

    private readonly string sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
    private int sequence;
    private RenderTexture target, previousTarget;
    private bool previousEnabled;
    private StereoTargetEyeMask previousStereo;

    public void CapturePhoto()
    {
        if (IsCapturing || !isActiveAndEnabled) return;
        if (photoCamera == null || !photoCamera.gameObject.activeInHierarchy ||
            width < 1 || height < 1)
        {
            Fail("Assign an active dedicated photo camera and positive image dimensions.");
            return;
        }
        StartCoroutine(Capture());
    }

    private AudioClip generatedClick;
    private void Awake()
    {
        if(shutterAudio!=null) return;
        shutterAudio=gameObject.AddComponent<AudioSource>();
        shutterAudio.playOnAwake=false;
        shutterAudio.spatialBlend=0;
        const int sampleRate=22050;
        float[] samples=new float[1102];
        for(int i=0;i<samples.Length;i++)
            samples[i]=Mathf.Sin(i*2*Mathf.PI*1800/sampleRate)*0.15f*(1f-(float)i/samples.Length);
        generatedClick=AudioClip.Create("DemoShutterClick",samples.Length,1,sampleRate,false);
        generatedClick.SetData(samples,0);
        shutterAudio.clip=generatedClick;
    }

    private void OnDestroy()
    {
        if(generatedClick!=null) Destroy(generatedClick);
    }

    private IEnumerator Capture()
    {
        IsCapturing=true;
        Status="Capturing...";
        previousTarget=photoCamera.targetTexture;
        previousEnabled=photoCamera.enabled;
        previousStereo=photoCamera.stereoTargetEye;
        bool prepared=false;
        try
        {
            target=RenderTexture.GetTemporary(width,height,24);
            photoCamera.stereoTargetEye=StereoTargetEyeMask.None;
            photoCamera.targetTexture=target;
            photoCamera.enabled=true;
            prepared=true;
        }
        catch(Exception exception) { Fail(exception.Message); }
        if (!prepared) { Restore(); yield break; }

        // Let the active render pipeline render the dedicated camera normally.
        // Run capture from Play mode or a build, not a paused Editor.
        yield return new WaitForEndOfFrame();
        string path=null;
        try { path=SaveImage(); }
        catch(Exception exception) { Fail(exception.Message); }
        finally { Restore(); }

        if (path != null)
        {
            LastPhotoPath=path;
            Status="Saved photo "+sequence.ToString("D4");
            if (shutterAudio != null && shutterAudio.clip != null) shutterAudio.Play();
            Debug.Log("Photo saved: "+path,this);
            Shutter.Invoke();
            PhotoSaved.Invoke(path);
        }
    }

    private string SaveImage()
    {
        Directory.CreateDirectory(PhotoFolder);
        RenderTexture previousActive=RenderTexture.active;
        Texture2D photo=null;
        try
        {
            RenderTexture.active=target;
            photo=new Texture2D(width,height,TextureFormat.RGB24,false);
            photo.ReadPixels(new Rect(0,0,width,height),0,0);
            photo.Apply();
            int number=sequence+1;
            string path=Path.Combine(PhotoFolder,"Photo_"+sessionId+"_"+number.ToString("D4")+".png");
            File.WriteAllBytes(path,photo.EncodeToPNG());
            sequence=number;
            return path;
        }
        finally
        {
            RenderTexture.active=previousActive;
            if(photo!=null) Destroy(photo);
        }
    }

    private void Restore()
    {
        if (IsCapturing && photoCamera != null)
        {
            photoCamera.targetTexture=previousTarget;
            photoCamera.enabled=previousEnabled;
            photoCamera.stereoTargetEye=previousStereo;
        }
        if(target!=null) RenderTexture.ReleaseTemporary(target);
        target=null;
        IsCapturing=false;
    }

    private void Fail(string message)
    {
        Status="Capture failed: "+message;
        Debug.LogError(Status,this);
        CaptureFailed.Invoke(message);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Restore();
    }

    public void OpenPhotoFolder()
    {
        try
        {
            Directory.CreateDirectory(PhotoFolder);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName=PhotoFolder,
                UseShellExecute=true
            });
#else
            Debug.Log("Photo folder: "+PhotoFolder,this);
#endif
        }
        catch(Exception exception) { Fail("Cannot open photo folder: "+exception.Message); }
    }
}
