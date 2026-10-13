// Controller models in VR: an opaque colour with a fixed key light and a rim, so
// the controllers read clearly in the dark night store without any scene light.
Shader "CrimeScene/UiShaded"
{
    Properties { _Color ("Colour", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            fixed4 _Color;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);
                float3 n=normalize(UnityObjectToWorldNormal(v.normal));
                float3 view=normalize(WorldSpaceViewDir(v.vertex));
                float key=saturate(dot(n,normalize(float3(0.35,0.85,-0.4))));
                float rim=pow(1-saturate(dot(n,view)),3);
                o.color=_Color;
                o.color.rgb*=0.42+0.5*key+0.45*rim;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return i.color;
            }
            ENDCG
        }
    }
}
