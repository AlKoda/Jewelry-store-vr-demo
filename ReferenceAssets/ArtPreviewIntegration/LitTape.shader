Shader "ArtPreview/LitTape" {
 Properties { _Color("Yellow",Color)=(.75,.5,.015,1) _Span("Metres",Float)=1 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert addshadow
 #pragma target 3.0
 fixed4 _Color; float _Span;
 struct Input {float2 stripe;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.stripe=float2(v.vertex.z*_Span,v.vertex.y);v.vertex.y-=(1-4*v.vertex.z*v.vertex.z)*min(.035,_Span*.012)/.08;}
 void surf(Input i,inout SurfaceOutputStandard o){float band=abs(i.stripe.y)<.28?1:step(.28,frac(i.stripe.x*4+i.stripe.y*.25));o.Albedo=lerp(float3(.012,.012,.012),_Color.rgb,band);o.Metallic=0;o.Smoothness=.22;o.Alpha=1;}
 ENDCG
 } Fallback "Diffuse"
}
