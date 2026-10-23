// Matte, two-sided hair/scarf fringe. Alpha testing avoids glass-like hair cards.
Shader "CrimeScene/CrowdCutout" {
 Properties { _MainTex("Colour and coverage",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Coverage",Range(0,1))=.42 _Glossiness("Surface polish",Range(0,1))=.15 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off LOD 180
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard alphatest:_Cutoff fullforwardshadows addshadow
  sampler2D _MainTex;fixed4 _Color;half _Glossiness;
  struct Input {float2 uv_MainTex;float facing:VFACE;};
  void surf(Input i,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Alpha=c.a;o.Normal=float3(0,0,i.facing>0?1:-1);o.Metallic=0;o.Smoothness=_Glossiness;o.Occlusion=1;}
  ENDCG
 } FallBack "Transparent/Cutout/Diffuse"
}
