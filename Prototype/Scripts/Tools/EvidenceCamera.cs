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
    // Photographs saved this session; the sequence number of the last one.
    public int PhotoCount => sequence;

    // Handheld controls (VR right hand, unverified in the headset): optical zoom, a flash
    // that lights the frame for the capture, and reviewing this session's photos on the
    // rear screen, newest first. Evidence photos are never deleted from the camera.
    public float MaxZoom = 4f;
    public float Zoom { get; private set; } = 1f;
    public bool FlashOn { get; private set; }
    public float FlashIntensity = 3f;
    public float FlashRange = 7f;
    public Light FlashLight => flash;
    // -1 while showing the live view; otherwise the index into this session's photos (0 = oldest).
    public int ReviewIndex { get; private set; } = -1;
    public bool Reviewing => ReviewIndex >= 0 && ReviewPhoto != null;
    public Texture2D ReviewPhoto { get; private set; }
    public int SessionPhotoCount => sessionPhotos.Count;
    private readonly System.Collections.Generic.List<string> sessionPhotos = new System.Collections.Generic.List<string>();
    private float baseFov = -1;
    private Light flash;

    public void ZoomBy(float factor)
    {
        if (photoCamera == null) return;
        if (baseFov < 0) baseFov = photoCamera.fieldOfView;
        Zoom = Mathf.Clamp(Zoom * factor, 1f, MaxZoom);
        photoCamera.fieldOfView = baseFov / Zoom;
    }

    public void ResetZoom() { if (baseFov > 0) ZoomBy(1f / Zoom); }

    public void ToggleFlash()
    {
        FlashOn = !FlashOn;
        DemoFeedback.Post(FlashOn ? "Camera flash on" : "Camera flash off");
    }

    // Steps back through this session's photos (newest first); past the oldest it returns to the live view.
    public void ReviewStep()
    {
        if (sessionPhotos.Count == 0) { DemoFeedback.Post("No photos yet"); return; }
        int next = ReviewIndex < 0 ? sessionPhotos.Count - 1 : ReviewIndex - 1;
        if (next < 0) { EndReview(); DemoFeedback.Post("Live view"); return; }
        try
        {
            if (ReviewPhoto == null) ReviewPhoto = new Texture2D(2, 2, TextureFormat.RGB24, true) { name = "ReviewedEvidencePhoto" };
            if (!ReviewPhoto.LoadImage(File.ReadAllBytes(sessionPhotos[next]))) throw new IOException("unreadable PNG");
            ReviewIndex = next;
            DemoFeedback.Post("Photo " + (next + 1) + " of " + sessionPhotos.Count + "  (B: older, trigger: back to live)");
        }
        catch (Exception e) { EndReview(); DemoFeedback.Post("Could not show that photo: " + e.Message, FeedbackTone.Warning); }
    }

    public void EndReview() { ReviewIndex = -1; }

    private void SetFlash(bool lit)
    {
        if (lit && flash == null && photoCamera != null)
        {
            GameObject go = new GameObject("CameraFlash");
            go.transform.SetParent(photoCamera.transform, false);
            flash = go.AddComponent<Light>();
            flash.type = LightType.Spot;
            flash.spotAngle = 75;
            flash.color = new Color(1f, 0.97f, 0.92f);
            flash.shadows = LightShadows.Soft;
            flash.shadowCustomResolution=1024;
            flash.shadowBias=.015f;flash.shadowNormalBias=.15f;
            DemoPlatform.TuneShadows(flash, 1024);
            flash.renderMode = LightRenderMode.ForcePixel;
            // A two-frame flash is not worth a shadow map on the mobile GPU; it still lights per pixel.
            if (DemoPlatform.Standalone) flash.shadows = LightShadows.None;
        }
        if (flash == null) return;
        flash.intensity = FlashIntensity;
        flash.range = FlashRange;
        flash.enabled = lit;
        flash.gameObject.SetActive(lit);
    }
    // Live view through the lens for the presenter panel, non-null only while the
    // body is held. The dedicated camera stays disabled and renders into this
    // texture about 15 times a second from a coroutine, so holding the camera
    // costs a fraction of a second camera instead of one per frame.
    public Texture Viewfinder => viewfinder;
    public float ViewfinderInterval = 1f/15;
    // The rear screen redraws the scene a second time; on the Quest alone it refreshes at 8 per second.
    private float Interval => DemoPlatform.Standalone ? Mathf.Max(ViewfinderInterval, 1f/8) : ViewfinderInterval;

    private Transform rack;
    private Vector3 rackPosition;
    private Quaternion rackRotation;
    // The local pose the body rests in on its rack (recorded at Awake, kept while held).
    public Vector3 RackPosition => rackPosition;
    public Quaternion RackRotation => rackRotation;
    private RenderTexture viewfinder;
    private Coroutine viewfinderLoop;
    private DemoLighting lighting;

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
        if(holder.GetComponent<HandInteractor>()!=null){
            var input=holder.GetComponent<XRControllerInput>();var pose=ControllerGrip(input!=null&&input.Left);
            transform.localPosition=pose.position;transform.localRotation=pose.rotation;
        }else{transform.localPosition=localOffset;transform.localRotation=Quaternion.identity;}
        SetCollidersEnabled(false);
        StartViewfinder();
    }
    // Retain the accepted wrist/lens angle. Offset the body to the opposite side
    // of the controller, seating each hand on its corresponding camera edge.
    public static readonly Quaternion HandHold=Quaternion.Euler(90,0,0);
    public static Vector3 HandSeat(bool left)=>new Vector3(left?.085f:-.085f,.012f,.075f);
    public static Pose ControllerGrip(bool left)=>new Pose(HandSeat(left),HandHold);

    public void ReturnToRack()
    {
        if(Holder==null) return;
        StopViewfinder();
        transform.SetParent(rack,false);
        transform.localPosition=rackPosition;
        transform.localRotation=rackRotation;
        Holder=null;
        ResetZoom();
        EndReview();
        SetCollidersEnabled(true);
    }

    // The texture exists from the moment the camera is taken (the first frame is
    // rendered right away, so the panel never shows a black box) until it is returned.
    private void StartViewfinder()
    {
        if(viewfinder!=null || photoCamera==null || !isActiveAndEnabled) return;
        viewfinder=new RenderTexture(480,270,24) { name="Viewfinder" };
        TickViewfinder();
        viewfinderLoop=StartCoroutine(RenderViewfinder());
    }

    private void StopViewfinder()
    {
        if(viewfinderLoop!=null) StopCoroutine(viewfinderLoop);
        viewfinderLoop=null;
        if(viewfinder==null) return;
        viewfinder.Release();
        Destroy(viewfinder);
        viewfinder=null;
    }

    private IEnumerator RenderViewfinder()
    {
        float next=Time.unscaledTime+Interval;
        while(viewfinder!=null)
        {
            yield return null;
            if(Time.unscaledTime<next) continue;
            next=Time.unscaledTime+Interval;
            TickViewfinder();
        }
    }

    // One manual render into the viewfinder; the camera's target, enabled state and pose are exactly
    // as before afterwards, and a capture in progress owns the camera, so ticks wait until it has
    // finished. Shadows stay on, as in the saved photo: switching them off globally for the preview
    // let the torch and lamp beams pass through walls and counters on the rear screen.
    private void TickViewfinder()
    {
        if(IsCapturing || viewfinder==null || photoCamera==null || !photoCamera.gameObject.activeInHierarchy) return;
        RenderTexture previous=photoCamera.targetTexture;
        try
        {
            photoCamera.targetTexture=viewfinder;
            photoCamera.Render();
        }
        finally
        {
            // In the editor these are the project's quality settings: restored even if Render throws.
            photoCamera.targetTexture=previous;
        }
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
        rack=transform.parent;
        rackPosition=transform.localPosition;
        rackRotation=transform.localRotation;
        lighting=GetComponentInParent<DemoLighting>(); // the body rests on the ToolSystem root, which carries DemoLighting
        if(lighting==null) lighting=FindFirstObjectByType<DemoLighting>();
        if(photoCamera!=null)
        {
            photoCamera.cullingMask&=~(1<<DemoUi.HeadsetUiLayer); // photos and the viewfinder never show the headset interface
            // LDR like the night head camera (NightQuality): with no post-processing the clamped image is
            // the same, without the FP16 intermediate and its resolve. Anti-aliasing comes from the
            // capture target's samples.
            photoCamera.allowHDR=false;
            photoCamera.allowMSAA=true;
        }
        // The camera setup stays above this return: a scene may already carry a shutter AudioSource.
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
        if(ReviewPhoto!=null) Destroy(ReviewPhoto);
        if(flash!=null) Destroy(flash.gameObject);
        if(display!=null) { display.Release(); Destroy(display); }
        if(scratch!=null) Destroy(scratch);
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
            // Anti-aliased like the head view; ReadPixels resolves it before reading.
            target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.Default,RenderTextureReadWrite.Default,Mathf.Max(1,QualitySettings.antiAliasing));
            photoCamera.stereoTargetEye=StereoTargetEyeMask.None;
            photoCamera.targetTexture=target;
            photoCamera.enabled=true;
            SetFlash(FlashOn);
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
        // A recorded shutter (DemoSoundBank) replaces the generated click, never both.
        AudioClip staged=DemoSounds.Staged(DemoSound.Shutter);
        if (shutterAudio != null && staged != null) shutterAudio.PlayOneShot(staged,DemoSounds.Volume(DemoSound.Shutter,1));
        else if (shutterAudio != null && shutterAudio.clip != null) shutterAudio.Play();
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
        // Only a photo that reached disk replaces LastPhoto, so the wall frame
        // and panel thumbnail never show a shot whose save failed.
        if(LastPhoto==null || LastPhoto.width!=w || LastPhoto.height!=h)
        {
            if(LastPhoto!=null) Destroy(LastPhoto);
            LastPhoto=new Texture2D(w,h,TextureFormat.RGB24,false) { name="LastEvidencePhoto" };
        }
        LastPhoto.LoadRawTextureData(pixels);
        LastPhoto.Apply(false);
        int dw=Mathf.Max(1,w/2), dh=Mathf.Max(1,h/2);
        if(display==null || display.width!=dw || display.height!=dh)
        {
            if(display!=null) { display.Release(); Destroy(display); }
            // useMipMap and autoGenerateMips are set before the texture is created; clamped so the small
            // mips never blend the photo's opposite edge into the frame's border.
            display=new RenderTexture(dw,dh,0) { name="LastEvidencePhotoDisplay", useMipMap=true, autoGenerateMips=true, filterMode=FilterMode.Trilinear, wrapMode=TextureWrapMode.Clamp };
        }
        RenderTexture previousActive=RenderTexture.active;
        Graphics.Blit(LastPhoto,display); // an exact 2x2 box at half size; the mips are built on the GPU
        RenderTexture.active=previousActive;
        sequence=number;
        LastPhotoPath=path;
        sessionPhotos.Add(path);
        Status="Saved photo "+sequence.ToString("D4");
        Debug.Log("Photo saved: "+path,this);
        DemoFeedback.Post("Photo "+sequence.ToString("D4")+" saved",FeedbackTone.Success);
        PhotoSaved.Invoke(path);
    }

    // The most recent photograph as a texture, kept for the wall frame and the
    // panel preview so neither has to read the PNG back from disk.
    public Texture2D LastPhoto { get; private set; }

    // Half-size copy of LastPhoto with GPU-built mips, so the wall frame, the rear screen and the panel
    // thumbnails do not shimmer (the 1920-wide photo is minified 2-20x there). LastPhoto stays the full photo.
    public Texture LastPhotoDisplay => display!=null ? (Texture)display : LastPhoto;
    private RenderTexture display;

    // Scratch texture the rendered frame is read into; LastPhoto is only
    // updated after the PNG has been written.
    private Texture2D scratch;

    // Reused between photos; IsCapturing guards it until LastPhoto.LoadRawTextureData has run.
    private byte[] pixelBuffer;

    // Copies the rendered frame into the scratch texture's CPU copy and returns its raw RGB bytes in a
    // reused buffer. An anti-aliased target is resolved into a plain temporary first (Blit resolves it).
    // No Apply: nothing samples the scratch texture on the GPU.
    private byte[] ReadPixels()
    {
        RenderTexture previousActive=RenderTexture.active;
        RenderTexture resolved=null;
        try
        {
            RenderTexture source=target;
            if(target.antiAliasing>1)
            {
                resolved=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.Default,RenderTextureReadWrite.Default,1);
                Graphics.Blit(target,resolved);
                source=resolved;
            }
            RenderTexture.active=source;
            if(scratch==null || scratch.width!=width || scratch.height!=height)
            {
                if(scratch!=null) Destroy(scratch);
                scratch=new Texture2D(width,height,TextureFormat.RGB24,false) { name="EvidencePhotoScratch" };
            }
            scratch.ReadPixels(new Rect(0,0,width,height),0,0);
            var raw=scratch.GetRawTextureData<byte>(); // NativeArray view of the CPU copy, no allocation
            if(pixelBuffer==null || pixelBuffer.Length!=raw.Length) pixelBuffer=new byte[raw.Length];
            raw.CopyTo(pixelBuffer);
            return pixelBuffer;
        }
        finally
        {
            RenderTexture.active=previousActive;
            if(resolved!=null) RenderTexture.ReleaseTemporary(resolved);
        }
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
        SetFlash(false);
        if(target!=null) RenderTexture.ReleaseTemporary(target);
        target=null;
        RestorePhotoPose();
    }

    private void Fail(string message)
    {
        Status="Capture failed: "+message;
        Debug.LogError(Status,this);
        DemoFeedback.Post("Photo failed: "+message,FeedbackTone.Warning);
        CaptureFailed.Invoke(message);
    }

    private void OnEnable()
    {
        if(Holder!=null) StartViewfinder();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        viewfinderLoop=null;
        StopViewfinder();
        Restore();
        pixelBuffer=null; // a save task still running keeps its own array
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
