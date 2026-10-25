Shader "CrimeScene/PatrolMachinedMetal" {
 Properties {
  _Color("Metal tint",Color)=(.23,.25,.27,1)
  _Metallic("Metallic",Range(0,1))=.72
  _Glossiness("Smoothness",Range(0,1))=.38
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
  #pragma target 3.0
  #pragma multi_compile_instancing
  fixed4 _Color;half _Metallic,_Glossiness;
  struct Input {float3 wheelPosition;};
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.wheelPosition=v.vertex.xyz;}
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 p=IN.wheelPosition;float z=min(abs(p.z-1.66912),abs(p.z+1.54624));
   float radius=length(float2(p.y-.34816,z));
   float visible=1-smoothstep(.35,1.0,fwidth(radius)*2200);
   float machining=sin(radius*2200)*visible;
   o.Albedo=_Color.rgb*(1+machining*.015);o.Metallic=_Metallic;
   o.Smoothness=saturate(_Glossiness+machining*.065);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}
