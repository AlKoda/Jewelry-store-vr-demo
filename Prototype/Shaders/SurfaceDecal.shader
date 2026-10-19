Shader "CrimeScene/SurfaceDecal" {
 Properties {
 _Color("Tint",Color)=(1,1,1,1) _MainTex("Albedo and opacity",2D)="white"{}
 _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1
 _MetallicGlossMap("Metallic smoothness",2D)="white"{} _Metallic("Metallic",Range(0,1))=0
 _Glossiness("Smoothness",Range(0,1))=.2 _GlossMapScale("Smoothness map scale",Range(0,1))=1
 [HideInInspector] _Mode("Mode",Float)=2 [HideInInspector] _SrcBlend("Src",Float)=5
 [HideInInspector] _DstBlend("Dst",Float)=10 [HideInInspector] _ZWrite("ZWrite",Float)=0
 [HideInInspector] _EmissionColor("Emission",Color)=(0,0,0,0) [HideInInspector] _OcclusionMap("AO",2D)="white"{}
 }
 SubShader { Tags {"Queue"="AlphaTest+5" "RenderType"="Transparent"} ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade keepalpha
 #pragma target 3.0

 #pragma shader_feature_local _NORMALMAP
 #pragma shader_feature_local _METALLICGLOSSMAP
 sampler2D _MainTex,_BumpMap,_MetallicGlossMap;fixed4 _Color;half _BumpScale,_Metallic,_Glossiness,_GlossMapScale;
 struct Input {float2 uv_MainTex;};
 void surf(Input i,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Alpha=c.a;o.Metallic=_Metallic;o.Smoothness=_Glossiness;
 #ifdef _NORMALMAP
 o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_MainTex),_BumpScale);
 #endif
 #ifdef _METALLICGLOSSMAP
 fixed4 p=tex2D(_MetallicGlossMap,i.uv_MainTex);o.Metallic=p.r;o.Smoothness=p.a*_GlossMapScale;
 #endif
 }
 ENDCG
 } Fallback Off
}
