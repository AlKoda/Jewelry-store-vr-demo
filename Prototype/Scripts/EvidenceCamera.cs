using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
    public Vector3 LastCapturePosition { get; private set; }
    public Quaternion LastCaptureRotation { get; private set; }
    public string PhotoFolder => Path.Combine(Application.persistentDataPath,"EvidencePhotos");
    // Hand or view currently carrying the camera body; null while on its rack.
    public Transform Holder { get; private set; }

    private Transform rack;
    private Vector3 rackPosition;
    private Quaternion rackRotation;

    // Carry the camera body in front of a hand or view. Colliders are disabled so
    // it neither blocks walking nor the pointer. Passing it between holders keeps
    // the original rack pose.
    public void HoldBy(Transform holder,Vector3 localOffset)
    {
        if(holder==null) return;
        if(Holder==null)
        {
            rack=transform.parent;
            rackPosition=transform.localPosition;
            rackRotation=transform.localRotation;
        }
        Holder=holder;
        transform.SetParent(holder,false);
        transform.localPosition=localOffset;
        transform.localRotation=Quaternion.identity;
        SetCollidersEnabled(false);
    }

    public void ReturnToRack()
    {
        if(Holder==null) return;
        transform.SetParent(rack,false);
        transform.localPosition=rackPosition;
        transform.localRotation=rackRotation;
        Holder=null;
        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach(Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled=enabled;
    }

    private readonly string sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
    private int sequence;
    private RenderTexture target, previousTarget;
    private bool previousEnabled;
    private bool rendered;
    private bool restorePhotoPose;
    private Vector3 savedPhotoPosition;
    private Quaternion savedPhotoRotation;
    private StereoTargetEyeMask previousStereo;

    public void CaptureFromView(Transform viewpoint)
    {
        if(IsCapturing || !isActiveAndEnabled || photoCamera==null || viewpoint==null) return;
        savedPhotoPosition=photoCamera.transform.localPosition;
        savedPhotoRotation=photoCamera.transform.localRotation;
        restorePhotoPose=true;
        photoCamera.transform.SetPositionAndRotation(viewpoint.position,viewpoint.rotation);
        CapturePhoto();
        if(!IsCapturing) RestorePhotoPose();
    }

    private void RestorePhotoPose()
    {
        if(restorePhotoPose && photoCamera!=null)
        {
            photoCamera.transform.localPosition=savedPhotoPosition;
            photoCamera.transform.localRotation=savedPhotoRotation;
        }
        restorePhotoPose=false;
    }

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
        if(LastPhoto!=null) Destroy(LastPhoto);
    }

    private IEnumerator Capture()
    {
        LastCapturePosition=photoCamera.transform.position;
        LastCaptureRotation=photoCamera.transform.rotation;
        IsCapturing=true;
        rendered=false;
        Camera.onPostRender+=OnCameraRendered;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=OnPipelineCameraRendered;
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
        float deadline=Time.realtimeSinceStartup+10f;
        while(!rendered && Time.realtimeSinceStartup<deadline) yield return null;
        if(!rendered) { Fail("Photo camera did not render within 10 seconds."); Restore(); yield break; }
        byte[] pixels=null;
        try { pixels=ReadPixels(); }
        catch(Exception exception) { Fail(exception.Message); }
        finally { RestoreCamera(); }
        if(pixels==null) { IsCapturing=false; yield break; }

        // The shutter fires at the moment of capture; encoding and writing the
        // PNG (about 100 ms at 1920x1080) run off the main thread so neither the
        // headset nor the presenter view drops frames.
        if (shutterAudio != null && shutterAudio.clip != null) shutterAudio.Play();
        Shutter.Invoke();
        Status="Saving...";
        int number=sequence+1;
        string path=Path.Combine(PhotoFolder,"Photo_"+sessionId+"_"+number.ToString("D4")+".png");
        int w=width, h=height;
        Task save=Task.Run(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8_UNorm, (uint)w, (uint)h));
        });
        while(!save.IsCompleted) yield return null;
        IsCapturing=false;
        if(save.IsFaulted) { Fail(save.Exception.GetBaseException().Message); yield break; }
        sequence=number;
        LastPhotoPath=path;
        Status="Saved photo "+sequence.ToString("D4");
        Debug.Log("Photo saved: "+path,this);
        PhotoSaved.Invoke(path);
    }

    // The most recent photograph as a texture, kept for the wall frame and the
    // panel preview so neither has to read the PNG back from disk.
    public Texture2D LastPhoto { get; private set; }

    // Copies the rendered frame into LastPhoto and returns its raw RGB bytes.
    private byte[] ReadPixels()
    {
        RenderTexture previousActive=RenderTexture.active;
        try
        {
            RenderTexture.active=target;
            if(LastPhoto==null || LastPhoto.width!=width || LastPhoto.height!=height)
            {
                if(LastPhoto!=null) Destroy(LastPhoto);
                LastPhoto=new Texture2D(width,height,TextureFormat.RGB24,false) { name="LastEvidencePhoto" };
            }
            LastPhoto.ReadPixels(new Rect(0,0,width,height),0,0);
            LastPhoto.Apply(false);
            return LastPhoto.GetRawTextureData();
        }
        finally { RenderTexture.active=previousActive; }
    }

    private void OnCameraRendered(Camera camera)
    {
        if(camera==photoCamera) rendered=true;
    }

    private void OnPipelineCameraRendered(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
    {
        OnCameraRendered(camera);
    }

    private void Restore()
    {
        RestoreCamera();
        IsCapturing=false;
    }

    private void RestoreCamera()
    {
        Camera.onPostRender-=OnCameraRendered;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=OnPipelineCameraRendered;
        if (IsCapturing && photoCamera != null)
        {
            photoCamera.targetTexture=previousTarget;
            photoCamera.enabled=previousEnabled;
            photoCamera.stereoTargetEye=previousStereo;
        }
        if(target!=null) RenderTexture.ReleaseTemporary(target);
        target=null;
        RestorePhotoPose();
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
