// See-through hands for VR: a depth-only pass first, so the hand's own parts never stack
// darker where they overlap, then one alpha-blended pass with a soft rim so the outline
// reads against bright and dark surroundings. _Color.a is the overall opacity.
Shader "CrimeScene/GhostHand"
{
    Properties
    {
        _Color ("Color", Color) = (0.85, 0.9, 1, 0.35)
        _Rim ("Rim strength", Range(0, 2)) = 0.9
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite On
            ColorMask 0
        }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            fixed4 _Color;
            half _Rim;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; half rim : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = normalize(UnityObjectToWorldNormal(v.normal));
                float3 view = normalize(WorldSpaceViewDir(v.vertex));
                o.rim = 1 - saturate(dot(n, view));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _Color;
                half rim = i.rim * i.rim * _Rim;
                c.rgb += rim * 0.35;
                c.a = saturate(c.a * (0.55 + rim));
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
