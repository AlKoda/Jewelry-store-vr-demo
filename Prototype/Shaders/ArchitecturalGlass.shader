Shader "CrimeScene/ArchitecturalGlass"
{
 Properties
 {
  _Color ("Tint", Color) = (0.84,0.94,0.93,0.025)
  _Reflection ("Static room reflection", Cube) = "" {}
  _Reflectivity ("Reflection strength", Range(0,1)) = 0.45
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend One OneMinusSrcAlpha
   ZWrite Off
   Cull Back
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO };
   fixed4 _Color; samplerCUBE _Reflection; float _Reflectivity;
   v2f vert(appdata v)
   {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.normal=UnityObjectToWorldNormal(v.normal); UNITY_TRANSFER_FOG(o,o.pos); return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float3 view=normalize(_WorldSpaceCameraPos-i.world), n=normalize(i.normal);
    float fresnel=0.04+0.96*pow(1-saturate(abs(dot(view,n))),5);
    float reflection=fresnel*_Reflectivity;
    float alpha=saturate(_Color.a+reflection);
    fixed3 room=texCUBE(_Reflection,reflect(-view,n)).rgb;
    fixed4 col=fixed4(room*reflection+_Color.rgb*_Color.a,alpha);
    UNITY_APPLY_FOG_COLOR(i.fogCoord,col,fixed4(unity_FogColor.rgb*alpha,alpha));
    return col;
   }
   ENDCG
  }
 }
}
