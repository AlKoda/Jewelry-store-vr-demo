using UnityEngine;

// The police car's roof light bar: red and blue halves alternate through their
// emission colour only (a property block per renderer, no light, no material
// copy), so the flashing costs nothing at night. One full red-blue cycle per
// 1 / Hertz seconds (one red flash each); each switch crossfades over Ramp of a cycle
// with a smoothstep centred on it, the way a diffused lamp module rises and falls, so
// the halves never jump. The Washes (additive glow cards on the street and the shop
// floor, NightLighting) take the colour of the lit half through their _TintColor, so
// the bar seems to light its surroundings without a realtime light. Steady
// (SetFlashing(false), the presenter's switch for visitors who find flashing light
// uncomfortable): both halves glow at SteadyFactor and the washes are off.
public sealed class LightBarBlink : MonoBehaviour
{
    public Renderer Red, Blue;
    public Color RedOn = new Color(1f, 0.06f, 0.04f) * 4f;
    public Color BlueOn = new Color(0.08f, 0.25f, 1f) * 4f;
    // The unlit half keeps a trace of colour, like a lens catching the other half.
    public float OffFactor = 0.03f;
    public float Hertz = 1.5f;
    // Crossfade length as a fraction of the cycle: at 0.12, phases 0.1 and 0.6 are full red and full blue.
    [Range(0, 0.5f)] public float Ramp = 0.12f;
    public float SteadyFactor = 0.35f;
    public bool Flashing = true;
    // Glow cards tinted with the lit half; alpha is the additive strength.
    public Renderer[] Washes = new Renderer[0];
    public Color WashRed = new Color(1f, 0.05f, 0.03f, 0.06f);
    public Color WashBlue = new Color(0.06f, 0.2f, 1f, 0.06f);

    public bool RedLit { get; private set; }
    // 1 while the red half is fully lit, 0 while the blue one is, between during a crossfade.
    public float Redness { get; private set; }

    private const string EmissionProperty = "_EmissionColor", TintProperty = "_TintColor";
    private MaterialPropertyBlock block;

    private void Update() { Apply(Time.time); }

    // Presenter switch; a session reset turns flashing back on (DemoLighting).
    public void SetFlashing(bool flashing)
    {
        Flashing = flashing;
        Apply(Time.time);
    }

    // Shows the state at the given time; validation drives it directly.
    public void Apply(float time)
    {
        if (block == null) block = new MaterialPropertyBlock();
        if (!Flashing)
        {
            RedLit = true;
            Redness = 0.5f;
            Set(Red, EmissionProperty, RedOn * SteadyFactor);
            Set(Blue, EmissionProperty, BlueOn * SteadyFactor);
            foreach (Renderer wash in Washes)
            {
                Set(wash, TintProperty, Color.clear);
                if (wash != null && wash.enabled) wash.enabled = false;
            }
            return;
        }
        float r = RednessAt(time);
        Redness = r;
        RedLit = r >= 0.5f;
        Set(Red, EmissionProperty, RedOn * Mathf.Lerp(OffFactor, 1, r));
        Set(Blue, EmissionProperty, BlueOn * Mathf.Lerp(OffFactor, 1, 1 - r));
        Color tint = Color.Lerp(WashBlue, WashRed, r);
        foreach (Renderer wash in Washes)
        {
            if (wash == null) continue;
            if (!wash.enabled) wash.enabled = true;
            Set(wash, TintProperty, tint);
        }
    }

    // Red's share at a time: the cycle is shifted by half a ramp so red fades in over
    // [0, Ramp), holds until half the cycle, fades out over the next Ramp and stays off.
    private float RednessAt(float time)
    {
        float phase = Mathf.Repeat(time * Hertz, 1f);
        if (Ramp <= 0) return phase < 0.5f ? 1 : 0;
        float x = Mathf.Repeat(phase + Ramp * 0.5f, 1f);
        if (x < Ramp) return Mathf.SmoothStep(0, 1, x / Ramp);
        if (x < 0.5f) return 1;
        if (x < 0.5f + Ramp) return Mathf.SmoothStep(1, 0, (x - 0.5f) / Ramp);
        return 0;
    }

    public Color Emission(Renderer target) => Read(target, EmissionProperty);

    // A wash's current tint (clear while steady).
    public Color Tint(Renderer target) => Read(target, TintProperty);

    private Color Read(Renderer target, string property)
    {
        if (target == null) return Color.black;
        if (block == null) block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        return block.GetColor(property);
    }

    private void Set(Renderer target, string property, Color value)
    {
        if (target == null) return;
        target.GetPropertyBlock(block);
        block.SetColor(property, value);
        target.SetPropertyBlock(block);
    }
}
