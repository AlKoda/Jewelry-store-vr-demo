using System.Collections.Generic;
using UnityEngine;

// One authored fluorescent trace (a quad with the CrimeScene/UVFluorescence shader)
// and the static manager that decides, while the UV torch is on, which traces may
// glow. The shader alone would light a trace through a wall or a closed case, since
// it only knows the beam's cone; so up to five points of each trace (centre and four
// inner corners) are tested for cone, range, facing and a clear sight line to the torch,
// and its per-renderer _Visibility eases towards the fraction that pass over FadeSeconds.
// A half-covered trace therefore glows at half strength instead of all or nothing.
// Blockers follow the torch's shadow map: deployed tools block, clear glass (renderers
// that cast no shadow) does not.
// The shader multiplies that by its own per-pixel cone and distance falloff.
//
// Traces are observation cues the instructor chooses to show, not evidence detection:
// nothing here counts, highlights or scores them.
[RequireComponent(typeof(Renderer))]
public sealed class UVTrace : MonoBehaviour
{
    // The collider the trace lies on (set by UVTraceBuilder); never counts as a blocker.
    public Collider Surface;

    public const float FadeSeconds = 0.1f;
    // The sight line ends this far short of the trace, towards the torch, so the surface
    // under a trace at a grazing angle does not block it.
    public const float EndOffset = 0.02f;
    // The authored store has 44 traces; cone/range culling precedes occlusion rays.
    public const int MaxTraces = 48;

    private static readonly List<UVTrace> traces = new List<UVTrace>();
    private static readonly RaycastHit[] hits = new RaycastHit[32];
    private static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
    private static MaterialPropertyBlock block;
    private Renderer view;

    public float Visibility { get; private set; }
    // Enabled traces (a hidden example group registers none).
    public static int ActiveCount => traces.Count;
    // The quad faces its local -z (Unity's quad), lying on its surface.
    public Vector3 Normal => -transform.forward;

    private void OnEnable()
    {
        view = GetComponent<Renderer>();
        if (!traces.Contains(this)) traces.Add(this);
        Apply(0);
    }

    private void OnDisable()
    {
        traces.Remove(this);
        Apply(0);
    }

    // Moves every trace's visibility towards its target for this beam; deltaTime of
    // FadeSeconds or more completes the fade at once.
    public static void UpdateAll(Vector3 torch, Vector3 direction, float cosHalfAngle, float range, float deltaTime)
    {
        float step = FadeSeconds > 0 ? deltaTime / FadeSeconds : 1;
        for (int i = 0; i < traces.Count; i++)
        {
            UVTrace trace = traces[i];
            float target = i < MaxTraces ? trace.LitFraction(torch, direction, cosHalfAngle, range) : 0;
            if (target != trace.Visibility) trace.Apply(Mathf.MoveTowards(trace.Visibility, target, step));
        }
    }

    // The torch went off: every trace dark at once.
    public static void HideAll()
    {
        foreach (UVTrace trace in traces) trace.Apply(0);
    }

    // The sight lines start this far along the beam, matching the torch shadow's near plane.
    public const float StartOffset = 0.06f;
    // Corner samples sit this fraction of the quad's half size in from its edges.
    public const float CornerInset = 0.8f;
    private static readonly Vector2[] Samples = { Vector2.zero, new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) };

    // Share of the five sample points that are lit; a quad too small to matter (under 4 cm)
    // tests its centre only.
    public float LitFraction(Vector3 torch, Vector3 direction, float cosHalfAngle, float range)
    {
        Vector3 scale = transform.lossyScale;
        bool small = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) < 0.04f;
        int tested = small ? 1 : Samples.Length, lit = 0;
        for (int i = 0; i < tested; i++)
        {
            Vector3 offset = transform.right * (Samples[i].x * 0.5f * CornerInset * scale.x)
                + transform.up * (Samples[i].y * 0.5f * CornerInset * scale.y);
            if (LitAt(transform.position + offset, torch, direction, cosHalfAngle, range)) lit++;
        }
        return (float)lit / tested;
    }

    // Inside the cone and range, lit on its front side and in clear sight of the torch (centre point).
    public bool Lit(Vector3 torch, Vector3 direction, float cosHalfAngle, float range)
        => LitAt(transform.position, torch, direction, cosHalfAngle, range);

    private bool LitAt(Vector3 point, Vector3 torch, Vector3 direction, float cosHalfAngle, float range)
    {
        Vector3 toTrace = point - torch;
        float distance = toTrace.magnitude;
        if (distance > range || distance < 0.05f) return false;
        Vector3 ray = toTrace / distance;
        if (Vector3.Dot(ray, direction) < cosHalfAngle || Vector3.Dot(Normal, ray) >= 0) return false;
        float start = Mathf.Min(StartOffset, distance * 0.5f);
        return Clear(torch + ray * start, ray, distance - EndOffset - start);
    }

    // True when nothing but the player, the hands, the camera, rack samples, clear glass or the
    // trace's own surface lies on the sight line (triggers are ignored). Deployed tools block,
    // as they block the torch's shadowed light.
    private bool Clear(Vector3 from, Vector3 ray, float length)
    {
        if (length <= 0) return true;
        int count = Physics.RaycastNonAlloc(new Ray(from, ray), hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c == Surface || c is CharacterController || Held(c) || SeeThrough(c)
                || c.GetComponentInParent<ToolRackSample>() != null || c.GetComponentInParent<EvidenceCamera>() != null
                || c.GetComponentInParent<XRControllerInput>() != null) continue;
            return false;
        }
        return true;
    }

    // A tool in a hand never blocks (it rides with the torch); one standing in the scene does.
    private static bool Held(Collider c)
    {
        DeployedTool tool = c.GetComponentInParent<DeployedTool>();
        return tool != null && !tool.Free;
    }

    // Clear glass: its renderer casts no shadow, so the torch light passes it and so does the glow.
    private static bool SeeThrough(Collider c)
    {
        Renderer r = c.GetComponent<Renderer>();
        return r != null && r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Apply(float visibility)
    {
        Visibility = visibility;
        if (view == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        view.GetPropertyBlock(block);
        block.SetFloat(VisibilityId, visibility);
        view.SetPropertyBlock(block);
    }
}
