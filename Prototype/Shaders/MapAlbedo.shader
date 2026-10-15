// Replacement shader for the presenter's overview map (DemoOverviewMap.RenderWithShader, RenderType):
// flat albedo, darker with height, so the night store reads as a floor plan instead of a dark lit render.
// Translucent Standard glass and the beacon (RenderType Opaque, alpha below 0.5) are clipped; shaders
// tagged Transparent (glass, text, UV traces) are skipped by the replacement.
Shader "Hidden/CrimeScene/MapAlbedo"
{
    Properties { _MainTex ("", 2D) = "white" {} _Color ("", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float h:TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);
                o.h=mul(unity_ObjectToWorld,v.vertex).y;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(_Color.a-0.5);
                fixed3 c=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
                c*=lerp(1.0,0.45,saturate(i.h/3.0));
                return fixed4(c,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
