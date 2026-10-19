using System.Collections.Generic;
using UnityEngine;

// One authored fluorescent trace (a quad with the CrimeScene/UVFluorescence shader)
// and the static manager that decides, while the UV torch is on, which traces may
// glow. The shader alone would light a trace through a wall or a closed case, since
// it only knows the beam's cone; so each trace inside the cone and range must also
// face the torch and have a clear line of sight to it (one raycast, only for traces
// in range), and its per-renderer _Visibility eases towards 1 or 0 over FadeSeconds.
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
    // Budget: traces beyond this many are never lit (the examples use three).
    public const int MaxTraces = 30;

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
            float target = i < MaxTraces && trace.Lit(torch, direction, cosHalfAngle, range) ? 1 : 0;
            if (target != trace.Visibility) trace.Apply(Mathf.MoveTowards(trace.Visibility, target, step));
        }
    }

    // The torch went off: every trace dark at once.
    public static void HideAll()
    {
        foreach (UVTrace trace in traces) trace.Apply(0);
    }

    // Inside the cone and range, lit on its front side and in clear sight of the torch.
    public bool Lit(Vector3 torch, Vector3 direction, float cosHalfAngle, float range)
    {
        Vector3 toTrace = transform.position - torch;
        float distance = toTrace.magnitude;
        if (distance > range || distance < 0.05f) return false;
        Vector3 ray = toTrace / distance;
        if (Vector3.Dot(ray, direction) < cosHalfAngle || Vector3.Dot(Normal, ray) >= 0) return false;
        return Clear(torch, ray, distance - EndOffset);
    }

    // True when nothing but the player, tools, the camera, rack samples or the trace's
    // own surface lies on the sight line (triggers are ignored).
    private bool Clear(Vector3 from, Vector3 ray, float length)
    {
        if (length <= 0) return true;
        int count = Physics.RaycastNonAlloc(new Ray(from, ray), hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c == Surface || c is CharacterController || c.GetComponentInParent<DeployedTool>() != null
                || c.GetComponentInParent<ToolRackSample>() != null || c.GetComponentInParent<EvidenceCamera>() != null
                || c.GetComponentInParent<XRControllerInput>() != null) continue;
            return false;
        }
        return true;
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
