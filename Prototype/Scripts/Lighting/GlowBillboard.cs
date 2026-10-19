using UnityEngine;

// Camera-facing glow cards: the additive halos round the street-lamp and headlamp
// lenses (NightLighting). Each quad turns to whichever camera is about to render
// (Camera.onPreCull, before that camera culls): the head in VR (one callback for both
// eyes, from the centre eye) and on the desktop, the evidence photo and the live
// viewfinder alike, so a second view never draws cards that face the first. Each sits
// PullToEye in front of its lens towards the eye, so the card lies in front of the
// lamp's housing instead of cutting through it.
// A card with a lens axis (the headlamps) fades out as the eye moves beside or behind
// its lens. The tints are property blocks, which a scene does not save, so they are
// applied again whenever the component is enabled, in edit mode too (faded for the
// view the cards were saved facing); the quads only move in Play mode.
[ExecuteAlways]
public sealed class GlowBillboard : MonoBehaviour
{
    public Transform[] Quads = new Transform[0];
    // Lens centres in this transform's space, one per quad.
    public Vector3[] Anchors = new Vector3[0];
    // _TintColor per quad; alpha is the additive strength.
    public Color[] Tints = new Color[0];
    // Lens axes in this transform's space, one per quad: full glow within 60 degrees of
    // the axis, none from 90. Zero, or no entry, glows alike from every side (street lamps).
    public Vector3[] Facing = new Vector3[0];
    public float PullToEye = 0.25f;

    private const string TintProperty = "_TintColor";
    private MaterialPropertyBlock block;

    private void OnEnable()
    {
        ApplyTints();
        Camera.onPreCull += FaceCamera;
    }

    private void OnDisable() { Camera.onPreCull -= FaceCamera; }

    // Per rendering camera, one transform write per card and, with lens axes, one
    // property-block write per card (six of each in the night store).
    private void FaceCamera(Camera camera)
    {
        if (!Application.isPlaying || camera == null) return;
        Face(camera.transform.position);
    }

    // Turns every quad to the eye (its forward pointing away from it) and pulls it
    // towards the eye, never more than half way.
    public void Face(Vector3 eye)
    {
        for (int i = 0; i < Quads.Length; i++)
        {
            Transform quad = Quads[i];
            if (quad == null) continue;
            Vector3 anchor = i < Anchors.Length ? transform.TransformPoint(Anchors[i]) : quad.position;
            Vector3 toEye = eye - anchor;
            float distance = toEye.magnitude;
            if (distance < 0.001f) continue;
            Vector3 towards = toEye / distance;
            quad.SetPositionAndRotation(anchor + towards * Mathf.Min(PullToEye, distance * 0.5f), Quaternion.LookRotation(-towards));
        }
        if (Facing.Length > 0) ApplyTints();
    }

    // Each card's tint, faded by the angle between its lens axis and the eye it last
    // turned to (a card faces away from the eye, so the eye lies along -forward).
    public void ApplyTints()
    {
        if (block == null) block = new MaterialPropertyBlock();
        for (int i = 0; i < Quads.Length && i < Tints.Length; i++)
        {
            Transform quad = Quads[i];
            Renderer r = quad != null ? quad.GetComponent<Renderer>() : null;
            if (r == null) continue;
            Color tint = Tints[i];
            if (i < Facing.Length && Facing[i] != Vector3.zero)
                tint.a *= Mathf.SmoothStep(0, 1, 2 * Vector3.Dot(transform.TransformDirection(Facing[i]).normalized, -quad.forward));
            r.GetPropertyBlock(block);
            block.SetColor(TintProperty, tint);
            r.SetPropertyBlock(block);
        }
    }
}
