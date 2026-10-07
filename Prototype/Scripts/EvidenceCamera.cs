using System;
using System.IO;
using UnityEngine;

// Portable prototype: validate with the selected Unity render pipeline.
public sealed class EvidenceCamera : MonoBehaviour
{
    [SerializeField] private Camera photoCamera;
    [SerializeField, Min(1)] private int width = 1920;
    [SerializeField, Min(1)] private int height = 1080;

    public string PhotoFolder =>
        Path.Combine(Application.persistentDataPath, "EvidencePhotos");

    public void CapturePhoto()
    {
        if (photoCamera == null)
        {
            Debug.LogError("Assign a dedicated photo camera.", this);
            return;
        }

        if (width < 1 || height < 1)
        {
            Debug.LogError("Photo dimensions must be positive.", this);
            return;
        }

        RenderTexture previousTarget = photoCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = null;
        Texture2D photo = null;

        try
        {
            Directory.CreateDirectory(PhotoFolder);
            target = RenderTexture.GetTemporary(width, height, 24);
            photo = new Texture2D(width, height, TextureFormat.RGB24, false);

            photoCamera.targetTexture = target;
            photoCamera.Render();

            RenderTexture.active = target;
            photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            photo.Apply();

            string filename =
                $"Photo_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png";
            string path = Path.Combine(PhotoFolder, filename);
            File.WriteAllBytes(path, photo.EncodeToPNG());
            Debug.Log($"Photo saved: {path}", this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            photoCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (target != null)
                RenderTexture.ReleaseTemporary(target);
            if (photo != null)
                Destroy(photo);
        }
    }
}
