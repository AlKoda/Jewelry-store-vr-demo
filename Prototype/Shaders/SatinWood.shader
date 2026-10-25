// Keep photographic walnut colour, with restrained grain contrast rather than
// baked dark crevices amplified by a strong normal map under the inspection light.
Shader "CrimeScene/SatinWood" {
 Properties {
  _MainTex("Walnut colour",2D)="white" {}
  _Color("Tint",Color)=(1,1,1,1)
  _GrainContrast("Fine grain contrast",Range(0,1))=.60
  _BumpMap("Grain normal",2D)="bump" {}
  _BumpScale("Grain relief",Range(0,1))=.19
  _MetallicGlossMap("Metallic / smoothness",2D)="white" {}
  _Metallic("Metallic",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.29
  _GlossMapScale("Map smoothness",Range(0,1))=.29
  _OcclusionMap("Occlusion",2D)="white" {}
  _OcclusionStrength("Occlusion strength",Range(0,1))=1
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 250
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma multi_compile_instancing
  #pragma shader_feature_local _NORMALMAP
  #pragma shader_feature_local _METALLICGLOSSMAP
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_MetallicGlossMap,_OcclusionMap;
  fixed4 _Color;half _GrainContrast,_BumpScale,_Metallic,_Glossiness,_GlossMapScale,_OcclusionStrength;
  struct Input {float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed3 detail=tex2D(_MainTex,IN.uv_MainTex).rgb;
   fixed3 broad=tex2Dbias(_MainTex,float4(IN.uv_MainTex,0,3)).rgb;
   o.Albedo=lerp(broad,detail,_GrainContrast)*_Color.rgb;
   o.Metallic=_Metallic;o.Smoothness=_Glossiness;
   #ifdef _METALLICGLOSSMAP
    half4 packed=tex2D(_MetallicGlossMap,IN.uv_MainTex);o.Metallic=packed.r;o.Smoothness=packed.a*_GlossMapScale;
   #endif
   #ifdef _NORMALMAP
    o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_MainTex),_BumpScale);
   #endif
   o.Occlusion=lerp(1,tex2D(_OcclusionMap,IN.uv_MainTex).g,_OcclusionStrength);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}
