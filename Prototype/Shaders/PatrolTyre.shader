Shader "CrimeScene/PatrolTyre" {
 Properties { _Color("Rubber",Color)=(.075,.078,.081,1) _Glossiness("Rubber finish",Range(0,1))=.19 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
  #pragma target 3.0
  #pragma multi_compile_instancing
  fixed4 _Color;half _Glossiness;
  struct Input {float3 wheelPosition;};
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.wheelPosition=v.vertex.xyz;}
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 p=IN.wheelPosition;float z=abs(p.z-1.66912)<abs(p.z+1.54624)?1.66912:-1.54624;
   float2 radial=float2(p.y-.34816,p.z-z);float r=length(radial),angle=atan2(radial.y,radial.x),axial=abs(p.x)-.912;
   float tread=smoothstep(.326,.344,r),side=smoothstep(.088,.114,abs(axial));
   float fade=1-smoothstep(.015,.055,length(fwidth(p)));
   float groove=smoothstep(.60,.83,cos(angle*42+abs(axial)*88))*tread*fade;
   float ribs=(1-smoothstep(.001,.003,abs(r-.274)))+(1-smoothstep(.001,.003,abs(r-.319)));
   o.Albedo=_Color.rgb*(1-.30*groove+.055*ribs*side);o.Metallic=0;
   o.Smoothness=saturate(_Glossiness-.07*groove+.025*ribs*side);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}
