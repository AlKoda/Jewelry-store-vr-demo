// Native surface separation for Koda's supplied 15k triangle Tripo sedan.
// Body-space typography avoids floating decals and reads correctly on both sides.
Shader "CrimeScene/PatrolCarFinish" {
 Properties {
  _MainTex("Original Tripo detail",2D)="white" {}
  [NoScaleOffset] _DoorTex("Native police door artwork",2D)="black" {}
  [NoScaleOffset] _PlateTex("Arabic plate, no numbers",2D)="white" {}
  _PaintPolish("Paint surface smoothness",Range(0,1))=.79
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 250
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
  #pragma multi_compile_instancing
  #include "UnityCG.cginc"
  sampler2D _MainTex,_DoorTex,_PlateTex;
  half _PaintPolish;
  struct Input {float2 uv_MainTex;float3 carPosition;};
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.carPosition=v.vertex.xyz;}
  float between(float x,float a,float b){return step(a,x)*step(x,b);}
  float3 OriginalDoorPaint(float along,float up,float side){
   // Four unlettered paint samples on each side, found by rays against the
   // original GLB (see paint-samples.json). Sample LOD 2 to reject JPEG noise.
   float2 a=side>0?float2(.07610283,.00767017):float2(.01064517,.23842353);
   float2 b=side>0?float2(.08115417,.14263871):float2(.01153243,.10333471);
   float2 c=side>0?float2(.53585333,.79955870):float2(.06465255,.23877080);
   float2 d=side>0?float2(.13511002,.14051086):float2(.06559059,.10372558);
   float x=saturate((along+.18)/.255),y=saturate((up-.070)/.102);
   float3 top=lerp(tex2Dlod(_MainTex,float4(a,0,2)).rgb,tex2Dlod(_MainTex,float4(b,0,2)).rgb,x);
   float3 bottom=lerp(tex2Dlod(_MainTex,float4(c,0,2)).rgb,tex2Dlod(_MainTex,float4(d,0,2)).rgb,x);
   return lerp(bottom,top,y);
  }
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 p=IN.carPosition/5.12;
   // Normalized reference axes: X along body (nose negative), Y width, Z height.
   float along=-p.z,side=p.x,up=p.y,ab=abs(side);
   fixed3 source=tex2D(_MainTex,IN.uv_MainTex).rgb;
   float lightness=max(source.r,max(source.g,source.b));
   float navySide=between(along,-.211,.265)*between(up,.061,.197)*step(.154,ab);
   float bonnet=step(along,-.217)*step(.163,up)*step(ab,.163);
   float navy=max(navySide,bonnet);
   // Preserve the supplied colour texture. The native artwork patch is restricted
   // to the generated lettering area; all other source paint/detail remains.
   o.Albedo=source*.92;o.Metallic=0;o.Smoothness=_PaintPolish;o.Occlusion=1;o.Alpha=1;
   float repair=step(.170,ab)*smoothstep(-.212,-.203,along)*(1-smoothstep(.085,.098,along))*smoothstep(.066,.073,up)*(1-smoothstep(.170,.179,up));
   if(repair>.005)o.Albedo=lerp(o.Albedo,OriginalDoorPaint(along,up,side)*.92,repair);
   // Keep only small source shading around panel joins, never the generated text.
   float seam= min(min(abs(along+.211),abs(along-.020)),abs(along-.265));
   float seamMask=(1-smoothstep(.0007,.0025,seam))*navySide;
   o.Albedo*=1-.34*seamMask;
   // Glazing stays opaque and tinted: this exterior-only mesh has no cabin.
   float sideGlass=step(.125,ab)*between(along,-.215,.325)*between(up,.205,.299);
   float frontGlass=between(along,-.239,-.085)*between(up,.206,.296)*step(ab,.168);
   float rearGlass=between(along,.247,.404)*between(up,.203,.294)*step(ab,.169);
   float glass=max(sideGlass,max(frontGlass,rearGlass))*step(lightness,.66);
   if(glass>.5){o.Albedo=float3(.018,.026,.032);o.Smoothness=.94;}
   float pillar=sideGlass*(1-smoothstep(.004,.012,abs(along-.036)));
   if(pillar>.01){o.Albedo=lerp(o.Albedo,float3(.011,.013,.015),pillar);o.Smoothness=lerp(o.Smoothness,.62,pillar);}
   // Imported window rubbers, sill trim, black grille and the underside.
   float trim=step(lightness,.025)*step(glass,.5)*step(navy,.5)*step(up,.06);
   if(trim>.5||up<.025){o.Albedo=float3(.018,.020,.021);o.Metallic=0;o.Smoothness=.25;}
   float radius=min(length(float2(along+.326,up-.068)),length(float2(along-.302,up-.068)));
   float wheel=step(.168,ab)*step(radius,.072)*step(up,.143);
   if(wheel>.5){
    float rim=step(radius,.050)*step(.162,ab);
    o.Albedo=lerp(float3(.024,.026,.027),lerp(float3(.13,.15,.17),float3(.49,.52,.55),smoothstep(.06,.7,lightness)),rim);
    o.Metallic=rim*.92;o.Smoothness=lerp(.20,.69,rim);
   }
   // Real grille, reflector and tail-lens detail from the supplied atlas.
   float grille=step(along,-.454)*between(up,.085,.167)*step(ab,.13);
   if(grille>.5){o.Albedo=source*.92;o.Metallic=smoothstep(.07,.4,lightness)*.9;o.Smoothness=.72;}
   float lamp=step(along,-.428)*between(up,.143,.197)*between(ab,.127,.205);
   if(lamp>.5){o.Albedo=source*.92;o.Metallic=.34;o.Smoothness=.88;o.Emission=float3(.18,.145,.098)*smoothstep(.3,.85,lightness);}
   float rear=step(.439,along)*between(up,.122,.206)*step(.135,ab);
   if(rear>.5){o.Metallic=.06;o.Smoothness=.86;}
   float handle=step(.167,ab)*between(up,.195,.211)*max(between(along,-.077,-.033),between(along,.162,.208));
   if(handle>.5&&lightness>.18){o.Metallic=.93;o.Smoothness=.78;}
   float bar=step(.304,up);
   if(bar>.5){o.Albedo=float3(.012,.050,.14);o.Metallic=.12;o.Smoothness=.87;}
   // Clean front-door artwork, mirrored projection direction, never mirrored letters.
   float2 duv=float2(.5-(along+.109)*sign(side)/.173,(up-.102)/.04325);
   float door=step(.175,ab)*between(along,-.199,-.019)*between(up,.100,.147)*step(glass,.5);
   fixed4 ink=tex2D(_DoorTex,saturate(duv));
   o.Albedo=lerp(o.Albedo,ink.rgb,ink.a*door);o.Smoothness=lerp(o.Smoothness,.56,ink.a*door);
   float front=step(along,-.478)*between(up,.064,.099)*step(ab,.060);
   float back=step(.483,along)*between(up,.094,.130)*step(ab,.062);
   float plate=max(front,back);
   float2 puv=float2(.5+side*sign(along)/.12,(up-lerp(.064,.094,back))/.035);
   fixed3 plateInk=tex2D(_PlateTex,saturate(puv)).rgb;
   if(plate>.5){o.Albedo=plateInk;o.Metallic=.08;o.Smoothness=.49;}
   // Restrained road dust is a roughness change, concentrated at the lower sill.
   float dust=(1-smoothstep(.035,.10,up))*step(wheel,.5);
   o.Smoothness=lerp(o.Smoothness,.35,dust*.25);
  }
  ENDCG
 }
 FallBack "Standard"
}
