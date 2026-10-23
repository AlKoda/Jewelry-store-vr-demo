// Display-case glass only. StandardSpecular supplies direct-light highlights,
// HDR probe decoding, roughness mip selection, probe blending and box projection.
// Premultiplied transmission preserves reflections even when the pane is clear.
Shader "CrimeScene/DisplayGlass"
{
    Properties
    {
        _Color ("Transmission tint / absorption", Color) = (0.94,0.97,1,0.055)
        _IOR ("Index of refraction", Range(1,2)) = 1.52
        _Smoothness ("Polish", Range(0,1)) = 0.96
        _DetailFade ("Subpixel cut-edge filtering", Range(0,1)) = 0
        _MicroVariation ("Polish variation", Range(0,0.1)) = 0.012
        _WearMap ("Photographic surface wear (alpha)", 2D) = "black" {}
        _WearStrength ("Surface film / polish wear", Range(0,1)) = 0
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
        #pragma shader_feature_local _GLASS_WEAR
        fixed4 _Color;
        sampler2D _WearMap;
        half _IOR, _Smoothness, _MicroVariation, _DetailFade, _WearStrength;
        struct Input { float3 worldPos; float3 worldNormal; float2 uv_WearMap; };
        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            half f0 = (_IOR - 1) / (_IOR + 1);
            f0 *= f0;
            // Low-amplitude, broad polish variation; deliberately no noisy normal
            // distortion or screen-space grab that swims in stereo or photos.
            half variation = sin(IN.worldPos.x * 19 + IN.worldPos.z * 11)
                           * sin(IN.worldPos.y * 23 + IN.worldPos.z * 7);
            // A very faint neutral surface film catches nearby practical light.
            // Absorption grows at grazing angles; Fresnel reflection remains separate.
            half wear=0;
            #ifdef _GLASS_WEAR
            wear=tex2D(_WearMap,IN.uv_WearMap).a*_WearStrength;
            #endif
            o.Albedo = _Color.rgb*(.008+wear*.035);
            half visibility=lerp(1,saturate((15-distance(IN.worldPos,_WorldSpaceCameraPos.xyz))/8),_DetailFade);
            o.Specular = half3(f0,f0,f0)*visibility;
            o.Smoothness = saturate(_Smoothness - _MicroVariation * (0.5 + 0.5 * variation)-wear*.4);
            half grazing=pow(1-saturate(abs(dot(normalize(IN.worldNormal),normalize(_WorldSpaceCameraPos.xyz-IN.worldPos)))),4);
            o.Alpha = saturate((_Color.a+wear*.08)*(1+grazing*.8))*visibility;
            o.Occlusion = 1;
        }
        ENDCG
    }
    FallBack Off
}
