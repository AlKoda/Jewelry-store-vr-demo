// Simulated fluorescent trace for the UV torch: invisible until the torch's beam
// falls on it. The torch (DemoLighting) publishes _UVTorchPosition, _UVTorchDirection
// and _UVTorchParams (x = cos of the half outer angle, y = range, z = 1 when on);
// UVTrace sets the per-renderer _Visibility (0..1) from its line-of-sight test, so a
// trace never glows through a wall. Per pixel: torch on * soft cone edge * distance
// falloff * _Visibility * mask alpha. Premultiplied output: the glow adds light and a
// faint alpha keeps the deposit reading as a film on the surface. Single-pass
// instanced stereo like WorldText.
Shader "CrimeScene/UVFluorescence"
{
    Properties
    {
        _MainTex ("Trace mask (alpha)", 2D) = "white" {}
        _GlowColor ("Glow colour", Color) = (0.55, 0.95, 0.85, 1)
        _Intensity ("Glow intensity", Range(0, 8)) = 2.5
        _Visibility ("Visibility (set by UVTrace)", Range(0, 1)) = 0
        _EdgeSoftness ("Beam edge softness (cosine)", Range(0.001, 0.2)) = 0.025
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One OneMinusSrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _GlowColor;
            half _Intensity, _Visibility, _EdgeSoftness;
            float4 _UVTorchPosition, _UVTorchDirection, _UVTorchParams;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 ray=i.world-_UVTorchPosition.xyz;
                float dist=length(ray);
                float cosAngle=dot(ray/max(dist,1e-4),_UVTorchDirection.xyz);
                float cone=smoothstep(_UVTorchParams.x,_UVTorchParams.x+_EdgeSoftness,cosAngle);
                float reach=dist/max(_UVTorchParams.y,1e-3);
                float falloff=saturate(1-reach*reach);
                float glow=_UVTorchParams.z*cone*falloff*_Visibility*tex2D(_MainTex,i.uv).a;
                fixed4 color=fixed4(_GlowColor.rgb*(_Intensity*glow),glow*0.2);
                UNITY_APPLY_FOG_COLOR(i.fogCoord,color,fixed4(0,0,0,0));
                return color;
            }
            ENDCG
        }
    }
}
