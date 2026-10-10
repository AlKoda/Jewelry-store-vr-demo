using System.IO;
using UnityEngine;

// Shows the most recent photograph on a quad (a frame on the wall by the rack),
// so visitors and the instructor see each picture appear, in VR and on the
// monitor alike. Shows the camera's last photo texture; the frame is replaced on every photo.
[RequireComponent(typeof(MeshRenderer))]
public sealed class PhotoFrame : MonoBehaviour
{
    public EvidenceCamera EvidenceCamera;
    public Texture2D Current { get; private set; }

    private MeshRenderer frame;
    private Material material;

    private void Awake()
    {
        frame = GetComponent<MeshRenderer>();
        material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Glossiness", 0f);
        frame.sharedMaterial = material;
        frame.enabled = false;
    }

    private void OnEnable()
    {
        if (EvidenceCamera != null) EvidenceCamera.PhotoSaved.AddListener(Show);
    }

    private void OnDisable()
    {
        if (EvidenceCamera != null) EvidenceCamera.PhotoSaved.RemoveListener(Show);
    }

    // The camera keeps the photo it just saved as a texture; only an older path
    // (a review re-opening a file) is read back from disk.
    public void Show(string path)
    {
        Texture2D texture = EvidenceCamera != null && EvidenceCamera.LastPhoto != null && EvidenceCamera.LastPhotoPath == path
            ? EvidenceCamera.LastPhoto : LoadFile(path);
        if (texture == null) return;
        ReleaseOwned();
        Current = texture;
        owned = texture != EvidenceCamera?.LastPhoto;
        material.mainTexture = texture;
        frame.enabled = true;
    }

    private bool owned;

    private static Texture2D LoadFile(string path)
    {
        if (!File.Exists(path)) return null;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (texture.LoadImage(File.ReadAllBytes(path))) return texture;
        Destroy(texture);
        return null;
    }

    private void ReleaseOwned()
    {
        if (owned && Current != null) Destroy(Current);
        Current = null;
        owned = false;
    }

    private void OnDestroy()
    {
        ReleaseOwned();
        if (material != null) Destroy(material);
    }
}
