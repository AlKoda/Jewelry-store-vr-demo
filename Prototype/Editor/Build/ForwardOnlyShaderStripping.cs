#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

// Player builds drop every shader's DEFERRED pass: all cameras render Forward (GraphicsSettings'
// default path, NightQuality.Configure), so those Standard variants were compiled and shipped for
// nothing. Stripping switches itself off if a Standalone graphics tier is ever set to Deferred.
// Runs only during player builds; editor rendering and the lightmap bake (Meta pass) are untouched.
// A build-time and player-size cleanup: frame time does not change.
public sealed class ForwardOnlyShaderStripping : IPreprocessShaders
{
    public int callbackOrder => 0;

    public static bool ForwardOnly()
    {
        foreach (GraphicsTier tier in new[] { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3 })
            if (EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, tier).renderingPath == RenderingPath.DeferredShading)
                return false;
        return true;
    }

    // Only PassType.Deferred: the LightPrePass types are obsolete in Unity 6 and match nothing.
    public static bool Strips(PassType pass, bool forwardOnly) => forwardOnly && pass == PassType.Deferred;

    // Evaluated once per build: the instance is created when the build callbacks initialise.
    private readonly bool forwardOnly = ForwardOnly();

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (Strips(snippet.passType, forwardOnly)) data.Clear();
    }
}
#endif
