Shader "CrimeScene/Tape"
{
 Properties { _Color ("Tint", Color) = (1,1,1,1) _MainTex ("Printed worn vinyl", 2D) = "white" {} _Span ("Metres", Float) = 1 }
 SubShader
 {
  // The sag reads object-space z, which batching would turn into world space.
  Tags { "RenderType"="Opaque" "DisableBatching"="True" }
  CGPROGRAM
  // Lit vinyl: base pass, ForwardAdd (flashlight, lamps), light probes, fog and single-pass
  // instanced stereo come from the surface shader. fullforwardshadows lets the ForwardAdd
  // spot and point lights shadow the tape like the Standard materials around it; addshadow
  // puts the sag into the shadow caster. A runtime-only forward object, so no lightmap,
  // meta or deferred variants.
  #pragma surface surf Standard vertex:vert addshadow fullforwardshadows nolightmap nodynlightmap nodirlightmap nometa exclude_path:deferred
  #pragma target 3.0
  fixed4 _Color; float _Span; sampler2D _MainTex;
  struct Input { float2 stripe; float side; };
  void vert(inout appdata_full v, out Input o)
  {
   UNITY_INITIALIZE_OUTPUT(Input, o);
   float height = v.vertex.y;
   // Slack ribbon, with small uneven folds. Same deformation in its shadow.
   float sag = 1 - 4 * v.vertex.z * v.vertex.z;
   v.vertex.y -= sag * min(.05, _Span * .016) / .08;
   v.vertex.y += sag * sin(v.vertex.z * _Span * 12.7) * .025;
   o.stripe = float2(v.vertex.z * _Span, height);
   o.side = v.normal.x;
  }
  void surf(Input i, inout SurfaceOutputStandard o)
  {
   // One repeat per 1.28 m keeps the physical lettering the same size on any span.
   float u = i.stripe.x / 1.28 + .5;
   if(i.side < 0) u = 1-u;
   fixed3 ink = tex2D(_MainTex,float2(u,i.stripe.y+.5)).rgb;
   float fold = sin(i.stripe.x * 31.7 + sin(i.stripe.x*8.2)*.3);
   o.Albedo = ink * _Color.rgb * (.96 + .04*fold);
   o.Metallic = 0;
   o.Smoothness = .22 + .07*fold;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
