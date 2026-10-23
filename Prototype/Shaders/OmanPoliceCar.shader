// Flat polygon shading without splitting, moving or replacing any supplied FBX vertices.
Shader "CrimeScene/OmanPoliceCar" {
 Properties {
  _Color("Paint multiplier",Color)=(1,1,1,1)
  _MainTex("Authored paint and printed markings",2D)="white" {}
  [NoScaleOffset] _SurfaceMap("R metal / G polish",2D)="white" {}
  _Glossiness("Surface polish",Range(0,1))=0
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
  sampler2D _MainTex,_SurfaceMap; fixed4 _Color; half _Glossiness,_DebugGeometryNormals;
  struct Input {float2 uv_MainTex;float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  struct SurfaceOutputRawClay {
   fixed3 Albedo;fixed3 Normal;half3 Emission;half Metallic;half Smoothness;half Occlusion;fixed Alpha;
   float3 GeometryNormal;
  };
  SurfaceOutputStandard StandardClay(SurfaceOutputRawClay s){
   SurfaceOutputStandard p;p.Albedo=s.Albedo;p.Normal=s.GeometryNormal;p.Emission=0;p.Metallic=s.Metallic;p.Smoothness=s.Smoothness;p.Occlusion=1;p.Alpha=1;return p;
  }
  half4 LightingRawClay(SurfaceOutputRawClay s,half3 viewDir,UnityGI gi){
   if(_DebugGeometryNormals>.5)return half4(s.GeometryNormal*.5+.5,1);
   return LightingStandard(StandardClay(s),viewDir,gi);
  }
  void LightingRawClay_GI(SurfaceOutputRawClay s,UnityGIInput data,inout UnityGI gi){
   LightingStandard_GI(StandardClay(s),data,gi);
   // Remove interpolated vertex SH from the imported smooth normals. Keep probe
   // lighting and reflections, evaluated per polygon without touching the mesh.
   #ifndef LIGHTMAP_ON
   gi.indirect.diffuse=max(half3(0,0,0),ShadeSH9(half4(s.GeometryNormal,1)));
   #endif
  }
  void surf(Input IN,inout SurfaceOutputRawClay o){
   float3 derivative=cross(ddy(IN.worldPos),ddx(IN.worldPos));
   float3 face=derivative*rsqrt(max(dot(derivative,derivative),1e-20));
   if(dot(face,IN.worldNormal)<0)face=-face;
   half2 finish=tex2D(_SurfaceMap,IN.uv_MainTex).rg;o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Normal=float3(0,0,1);o.Emission=0;o.Metallic=finish.r;o.Smoothness=finish.g;o.Occlusion=1;o.Alpha=1;o.GeometryNormal=face;
  }
  ENDCG
 }
 FallBack Off
}
