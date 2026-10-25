Shader "CrimeScene/OmanGround" {
 Properties {
  _MainTex("Fine aggregate",2D)="gray" {} _Color("Tint",Color)=(1,1,1,1)
  _AgeMap("Broad weather and paint abrasion",2D)="gray" {}
  _CrackMasks("Network / stress / fatigue / concrete",2D)="black" {}
  _PaintMask("Parking layout",2D)="black" {}
  _BumpMap("Aggregate normal",2D)="bump" {} _BumpScale("Relief",Range(0,1))=.23
  _SurfaceType("Concrete paving",Float)=0
  _Glossiness("Smoothness",Range(0,1))=.12
  _AggregateContrast("Aggregate contrast",Range(0,1))=.68
  _DustStrength("Broad settled dust",Range(0,1))=.18
 }
 SubShader {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"} LOD 250
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex,_AgeMap,_CrackMasks,_PaintMask,_BumpMap;
  fixed4 _Color; half _BumpScale,_SurfaceType,_Glossiness,_AggregateContrast,_DustStrength;
  struct Input {float3 worldPos;};
  float2 turn(float2 p){return float2(.7986*p.x-.6018*p.y,.6018*p.x+.7986*p.y);}
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   float2 p=IN.worldPos.xz;
   float age=tex2D(_AgeMap,p/21.7).r;
   float2 grain=p/lerp(.85,.60,_SurfaceType), other=turn(grain)*1.371+float2(.37,.61);
   fixed3 base=lerp(tex2D(_MainTex,grain).rgb,tex2D(_MainTex,other).rgb,.42)*_Color.rgb;
   // Keep real aggregate up close without a high-contrast glitter carpet in VR.
   fixed3 average=fixed3(.16,.158,.15)*_Color.rgb;
   base=lerp(average,base,_AggregateContrast);
   base*=lerp(.90,1.08,age);
   float dust=smoothstep(.43,.78,age)*_DustStrength;
   base=lerp(base,base*1.13+fixed3(.012,.010,.007),dust);
   float2 warp=(float2(age,tex2D(_AgeMap,p/33.1+2.7).r)-.5)*.23;
   // Different masks and incommensurate scales prevent a single repeated fracture stamp.
   float network=tex2D(_CrackMasks,p/7.37+warp).r;
   float stress=tex2D(_CrackMasks,turn(p)/9.13+float2(.31,.69)+warp).g;
   float fatigue=tex2D(_CrackMasks,p/3.17+float2(.57,.23)).b*smoothstep(.44,.60,age);
   float crack=max(network*.78,max(stress*.85,fatigue*.9));
   float2 paintUV=(p-float2(-26,-20))/float2(52,17);
   float inside=step(0,paintUV.x)*step(paintUV.x,1)*step(0,paintUV.y)*step(paintUV.y,1);
   float paint=tex2D(_PaintMask,saturate(paintUV)).r*inside;
   float wear=tex2D(_AgeMap,turn(p)*1.79).r;
   paint*=smoothstep(.29,.51,wear)*.70;
   base=lerp(base,fixed3(.57,.55,.46),paint);
   if(_SurfaceType>.5){
    // Staggered physical-size slab joints, filtered at distance to avoid crawling lines.
    float row=floor(p.y/.60); float2 slab=float2(p.x/.45+fmod(abs(row),2)*.5,p.y/.60);
    float2 cell=floor(slab), f=frac(slab); float2 edge=min(f,1-float2(f.x,f.y));
    float2 aa=max(fwidth(slab),.001);
    float joint=1-min(smoothstep(.004,.014+aa.x,edge.x),smoothstep(.004,.014+aa.y,edge.y));
    base*=lerp(.93,1.05,hash(cell));
    base=lerp(base,base*.50,joint*.65);
    float2 local=frac(slab)*float2(.45,.60);
    float2 q=(local+float2(hash(cell),hash(cell+7))*8)/1.19;
    float concrete=tex2D(_CrackMasks,q).a;
    float damaged=step(.56,hash(cell+19));
    crack=concrete*damaged*.82;
    base*=1-joint*.08*wear;
   }
   // Cracks are applied after paint: fissures cut through the old markings.
   o.Albedo=lerp(base,fixed3(.026,.023,.019),saturate(crack*.93));
   half3 n=UnpackNormal(tex2D(_BumpMap,grain));
   float footprint=max(length(ddx(grain)),length(ddy(grain)));
   half reliefFade=1-smoothstep(.018,.11,footprint);
   n.xy*=_BumpScale*reliefFade;
   o.Normal=normalize(n);o.Metallic=0;o.Smoothness=_Glossiness*(1-crack*.8);
   o.Occlusion=1-crack*.34;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
