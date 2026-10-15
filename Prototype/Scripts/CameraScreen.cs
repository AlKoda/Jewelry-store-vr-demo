using UnityEngine;

// Rear screen of the handheld camera: the live viewfinder while the body is held,
// the photograph for a moment after it is saved, and black otherwise. The texture
// goes through a property block on an unlit material, so the screen reads as lit at
// night without a light, and no material asset is modified at runtime. It runs in
// Edit mode too so the screen is black (not the unlit material's white) in scene
// captures.
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class CameraScreen : MonoBehaviour
{
    public EvidenceCamera Evidence;
    public float PhotoSeconds = 1.5f;

    // The texture currently on the screen (Texture2D.blackTexture when dark).
    public Texture Shown { get; private set; }

    private const string TextureProperty = "_MainTex";
    private MeshRenderer screen;
    private MaterialPropertyBlock block;
    private float photoUntil = -1;

    private void OnEnable()
    {
        if (Evidence == null) Evidence = GetComponentInParent<EvidenceCamera>();
        if (Evidence != null) Evidence.PhotoSaved.AddListener(OnPhotoSaved);
        Shown = null;
        Refresh();
    }

    private void OnDisable()
    {
        if (Evidence != null) Evidence.PhotoSaved.RemoveListener(OnPhotoSaved);
    }

    private void OnPhotoSaved(string path)
    {
        photoUntil = Time.unscaledTime + PhotoSeconds;
        Refresh();
    }

    // The viewfinder texture is recreated each time the camera is taken, so the
    // choice is re-made every frame; the block is only rewritten when it changes.
    private void LateUpdate() { Refresh(); }

    public void Refresh()
    {
        Texture wanted = null;
        if (Evidence != null)
        {
            // The mipmapped display copy: the few-centimetre screen minifies the photo 15-20x.
            if (Evidence.LastPhoto != null && Time.unscaledTime < photoUntil) wanted = Evidence.LastPhotoDisplay;
            else if (Evidence.Holder != null) wanted = Evidence.Viewfinder;
        }
        if (wanted == null) wanted = Texture2D.blackTexture;
        if (wanted == Shown) return;
        Shown = wanted;
        if (screen == null) screen = GetComponent<MeshRenderer>();
        if (block == null) block = new MaterialPropertyBlock();
        block.Clear();
        block.SetTexture(TextureProperty, wanted);
        screen.SetPropertyBlock(block);
    }
}
