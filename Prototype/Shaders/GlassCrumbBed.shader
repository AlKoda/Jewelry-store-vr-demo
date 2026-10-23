// Fine debris is a lit, low-opacity contact layer below the real 3D fragments.
// No emission, screen grab, per-particle transforms or additional lights.
Shader "CrimeScene/GlassCrumbBed" {
 Properties { _MainTex("Fine clear glass coverage",2D)="white"{} _Density("Scattered fine-crystal coverage",Range(0,1))=.27 _Cutoff("Reject transparent haze",Range(0,1))=.60 }
 SubShader {
 Tags {"Queue"="Transparent-2" "RenderType"="Transparent"} LOD 180 Cull Back ZWrite Off Offset -1,-1
 CGPROGRAM
 #pragma target 3.0
 #pragma surface surf StandardSpecular alpha:premul fullforwardshadows
 sampler2D _MainTex;half _Density,_Cutoff;
 struct Input {float2 uv_MainTex;};
 void surf(Input i,inout SurfaceOutputStandardSpecular o){fixed4 c=tex2D(_MainTex,i.uv_MainTex);half coverage=saturate((c.a-_Cutoff)/(1-_Cutoff))*saturate((max(c.r,max(c.g,c.b))-.66)*3.0);o.Albedo=c.rgb*.08;o.Specular=half3(.04,.04,.04)*coverage*_Density;o.Smoothness=.76;o.Alpha=coverage*_Density;o.Occlusion=1;}
 ENDCG
 } FallBack Off
}
