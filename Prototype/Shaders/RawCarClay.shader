// Flat polygon shading without splitting, moving or replacing any supplied FBX vertices.
Shader "CrimeScene/RawCarClay" {
 Properties {
  _Color("Clay colour",Color)=(.42,.43,.44,1)
  _Glossiness("Surface polish",Range(0,1))=.13
  _Metallic("Metallic",Range(0,1))=0
  [HideInInspector] _EmissionColor("Emission",Color)=(0,0,0,1)
  [HideInInspector] _DebugGeometryNormals("Geometry-normal verification",Float)=0
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf RawClay fullforwardshadows addshadow
  #pragma multi_compile_instancing
  #include "UnityPBSLighting.cginc"
  fixed4 _Color; half _Glossiness,_DebugGeometryNormals;
  struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  struct SurfaceOutputRawClay {
   fixed3 Albedo;fixed3 Normal;half3 Emission;half Metallic;half Smoothness;half Occlusion;fixed Alpha;
   float3 GeometryNormal;
  };
  SurfaceOutputStandard StandardClay(SurfaceOutputRawClay s){
   SurfaceOutputStandard p;p.Albedo=s.Albedo;p.Normal=s.GeometryNormal;p.Emission=0;p.Metallic=0;p.Smoothness=s.Smoothness;p.Occlusion=1;p.Alpha=1;return p;
  }
  half4 LightingRawClay(SurfaceOutputRawClay s,half3 viewDir,UnityGI gi){
   if(_DebugGeometryNormals>.5)return half4(s.GeometryNormal*.5+.5,1);
   return LightingStandard(StandardClay(s),viewDir,gi);
  }
  void LightingRawClay_GI(SurfaceOutputRawClay s,UnityGIInput data,inout UnityGI gi){
   LightingStandard_GI(StandardClay(s),data,gi);
  }
  void surf(Input IN,inout SurfaceOutputRawClay o){
   float3 face=normalize(cross(ddy(IN.worldPos),ddx(IN.worldPos)));
   if(dot(face,IN.worldNormal)<0)face=-face;
   o.Albedo=_Color.rgb;o.Normal=float3(0,0,1);o.Emission=0;o.Metallic=0;o.Smoothness=_Glossiness;o.Occlusion=1;o.Alpha=1;o.GeometryNormal=face;
  }
  ENDCG
 }
 FallBack Off
}
