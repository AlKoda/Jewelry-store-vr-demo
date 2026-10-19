// Display-case glass only. StandardSpecular supplies direct-light highlights,
// HDR probe decoding, roughness mip selection, probe blending and box projection.
// Premultiplied transmission preserves reflections even when the pane is clear.
Shader "CrimeScene/DisplayGlass"
{
    Properties
    {
        _Color ("Transmission tint / absorption", Color) = (0.90,0.98,0.96,0.012)
        _IOR ("Index of refraction", Range(1,2)) = 1.52
        _Smoothness ("Polish", Range(0,1)) = 0.96
        _MicroVariation ("Polish variation", Range(0,0.1)) = 0.012
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 300
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular alpha:premul fullforwardshadows
        #pragma multi_compile_instancing
        fixed4 _Color;
        half _IOR, _Smoothness, _MicroVariation;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            half f0 = (_IOR - 1) / (_IOR + 1);
            f0 *= f0;
            // Low-amplitude, broad polish variation; deliberately no noisy normal
            // distortion or screen-space grab that swims in stereo or photos.
            half variation = sin(IN.worldPos.x * 19 + IN.worldPos.z * 11)
                           * sin(IN.worldPos.y * 23 + IN.worldPos.z * 7);
            o.Albedo = _Color.rgb;
            o.Specular = half3(f0,f0,f0);
            o.Smoothness = saturate(_Smoothness - _MicroVariation * (0.5 + 0.5 * variation));
            o.Alpha = _Color.a;
            o.Occlusion = 1;
        }
        ENDCG
    }
    FallBack Off
}
