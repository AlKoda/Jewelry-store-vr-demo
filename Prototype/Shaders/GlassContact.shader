Shader "CrimeScene/GlassContact" {
 SubShader { Tags {"Queue"="Transparent-10" "RenderType"="Transparent"} ZWrite Off Blend DstColor Zero
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=(i.uv-.5)*2;float fade=pow(saturate(1-dot(p,p)),3);return fixed4(1-.09*fade,1-.09*fade,1-.09*fade,1);}
 ENDCG }
 } Fallback Off
}
