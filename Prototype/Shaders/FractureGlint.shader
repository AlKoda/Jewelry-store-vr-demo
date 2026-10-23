// Rough fracture surfaces scatter a little direct light; no baked white glow or refraction grab.
Shader "CrimeScene/FractureGlint" {
 Properties { _Color("Fracture scattering",Color)=(.65,.7,.71,.14) _Smoothness("Fracture polish",Range(0,1))=.60 _DiffuseScatter("Diffuse fracture scattering",Range(0,1))=.8 }
 SubShader {
 Tags {"Queue"="Transparent+1" "RenderType"="Transparent"} LOD 200 Cull Back ZWrite Off
 CGPROGRAM
 #pragma target 3.0
 #pragma surface surf StandardSpecular alpha:premul fullforwardshadows
 #pragma multi_compile_instancing
 fixed4 _Color;half _Smoothness,_DiffuseScatter;
 struct Input {float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandardSpecular o){o.Albedo=_Color.rgb*_DiffuseScatter;half visibility=saturate((15-distance(i.worldPos,_WorldSpaceCameraPos.xyz))/8);o.Specular=half3(.04,.04,.04)*visibility;o.Smoothness=_Smoothness;o.Alpha=_Color.a*visibility;o.Occlusion=1;}
 ENDCG
 } FallBack Off
}
