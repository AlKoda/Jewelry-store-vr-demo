using UnityEngine;

// The police car's roof light bar: red and blue halves alternate through their
// emission colour only (a property block per renderer, no light, no material
// copy), so the flashing costs nothing at night. One full red-blue cycle per
// 1 / Hertz seconds.
public sealed class LightBarBlink : MonoBehaviour
{
    public Renderer Red, Blue;
    public Color RedOn = new Color(1f, 0.06f, 0.04f) * 4f;
    public Color BlueOn = new Color(0.08f, 0.25f, 1f) * 4f;
    // The unlit half keeps a trace of colour, like a lens catching the other half.
    public float OffFactor = 0.03f;
    public float Hertz = 1.5f;

    public bool RedLit { get; private set; }

    private const string EmissionProperty = "_EmissionColor";
    private MaterialPropertyBlock block;

    private void Update() { Apply(Time.time); }

    // Shows the state at the given time; validation drives it directly.
    public void Apply(float time)
    {
        RedLit = Mathf.Repeat(time * Hertz, 1f) < 0.5f;
        if (block == null) block = new MaterialPropertyBlock();
        Set(Red, RedLit ? RedOn : RedOn * OffFactor);
        Set(Blue, RedLit ? BlueOn * OffFactor : BlueOn);
    }

    public Color Emission(Renderer target)
    {
        if (target == null) return Color.black;
        if (block == null) block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        return block.GetColor(EmissionProperty);
    }

    private void Set(Renderer target, Color emission)
    {
        if (target == null) return;
        target.GetPropertyBlock(block);
        block.SetColor(EmissionProperty, emission);
        target.SetPropertyBlock(block);
    }
}
