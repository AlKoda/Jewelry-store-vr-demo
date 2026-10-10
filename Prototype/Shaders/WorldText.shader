Shader "CrimeScene/WorldText"
{
    Properties { _MainTex ("Font atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 color=i.color;
                color.a*=tex2D(_MainTex,i.uv).a;
                UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
    }
}
