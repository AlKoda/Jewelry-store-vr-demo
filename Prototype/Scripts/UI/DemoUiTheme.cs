using UnityEngine;

// Holds the interface fonts for the scene (assigned by the Editor builders from
// ThirdParty/Fonts) and the controller shader, whose reference here also keeps it
// in the build. DemoUi reads them; without this component, or with a font missing,
// the built-in font is used, so the interface always draws.
public sealed class DemoUiTheme : MonoBehaviour
{
    public Font Regular;
    public Font Medium;
    public Font SemiBold;
    public Shader Shaded;
    // See-through VR hands (XRGhostHand); referenced here so it stays in the build.
    public Shader Ghost;
}
