Shader "CrimeScene/Tape"
{
 Properties { _Color ("Yellow", Color) = (1,0.78,0.03,1) _Span ("Metres", Float) = 1 }
 SubShader
 {
  Tags { "RenderType"="Opaque" }
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float2 stripe:TEXCOORD0; UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO };
   fixed4 _Color; float _Span;
   v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float height=v.vertex.y; v.vertex.y-=(1-4*v.vertex.z*v.vertex.z)*min(.035,_Span*.012)/.08; o.pos=UnityObjectToClipPos(v.vertex); o.stripe=float2(v.vertex.z*_Span,height); UNITY_TRANSFER_FOG(o,o.pos);return o; }
   fixed4 frag(v2f i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); float band=abs(i.stripe.y)<.28?1:step(.28,frac(i.stripe.x*4+i.stripe.y*.25)); fixed4 c=lerp(fixed4(.025,.025,.018,1),_Color,band); UNITY_APPLY_FOG(i.fogCoord,c);return c; }
   ENDCG
  }
 }
}
