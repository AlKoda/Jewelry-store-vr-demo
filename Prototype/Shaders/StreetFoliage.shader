Shader "CrimeScene/StreetFoliage" {
 Properties { _MainTex("Leaf colour and alpha",2D)="white"{} _Color("Tint",Color)=(.8,.9,.7,1) _Cutoff("Cutout",Range(0,1))=.45 _Wind("Wind displacement (m)",Range(0,.1))=.028 }
 SubShader {
 Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} LOD 150 Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert alphatest:_Cutoff addshadow fullforwardshadows
 #pragma target 3.0
 #pragma multi_compile_instancing
 sampler2D _MainTex;fixed4 _Color;float _Wind;
 struct Input {float2 uv_MainTex;float facing:VFACE;};
 void vert(inout appdata_full v){
   float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;
   float phase=w.x*.71+w.z*.49+v.color.r*6.28;
   float sway=(sin(_Time.y*1.1+phase)*.7+sin(_Time.y*1.87+phase*1.7)*.3)*_Wind*v.texcoord.y;
   v.vertex.xyz+=mul((float3x3)unity_WorldToObject,float3(sway,.12*sway,sway*.45));
 }
 void surf(Input i,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Alpha=c.a;o.Metallic=0;o.Smoothness=.16;o.Normal=float3(0,0,i.facing>=0?1:-1);}
 ENDCG
 } Fallback "Transparent/Cutout/Diffuse"
}
