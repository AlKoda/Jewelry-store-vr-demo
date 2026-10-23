Shader "CrimeScene/CrowdGroundContact" {
 SubShader {
  Tags {"Queue"="Transparent-5" "RenderType"="Transparent"} Blend DstColor Zero ZWrite Off Cull Off Offset -1,-1
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed alpha:COLOR;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
   v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.alpha=v.color.a;UNITY_TRANSFER_FOG(o,o.pos);return o;}
   fixed4 frag(v2f i):SV_Target{float2 p=i.uv*2-1;half a=saturate(1-dot(p,p));a=a*a*i.alpha;fixed4 c=fixed4(1-a,1-a,1-a,1);UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(1,1,1,1));return c;}
   ENDCG
  }
 } FallBack Off
}
