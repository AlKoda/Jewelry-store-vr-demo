using System.IO;
using UnityEngine;

// Shows the most recent photograph on a quad (a frame on the wall by the rack),
// so visitors and the instructor see each picture appear, in VR and on the
// monitor alike. Loads the saved PNG; the texture is replaced on every photo.
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
        material = new Material(Shader.Find("Unlit/Texture"));
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

    public void Show(string path)
    {
        if (!File.Exists(path)) return;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!texture.LoadImage(File.ReadAllBytes(path))) { Destroy(texture); return; }
        if (Current != null) Destroy(Current);
        Current = texture;
        material.mainTexture = texture;
        frame.enabled = true;
    }

    private void OnDestroy()
    {
        if (Current != null) Destroy(Current);
        if (material != null) Destroy(material);
    }
}
