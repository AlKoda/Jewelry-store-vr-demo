Shader "CrimeScene/Tape"
{
 Properties { _Color ("Yellow", Color) = (1,0.78,0.03,1) _Span ("Metres", Float) = 1 }
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
  fixed4 _Color; float _Span;
  struct Input { float2 stripe; };
  void vert(inout appdata_full v, out Input o)
  {
   UNITY_INITIALIZE_OUTPUT(Input, o);
   float height = v.vertex.y;
   // Sag: up to 3.5 cm at mid-span (object y is scaled by the 8 cm ribbon height).
   v.vertex.y -= (1 - 4 * v.vertex.z * v.vertex.z) * min(.035, _Span * .012) / .08;
   o.stripe = float2(v.vertex.z * _Span, height);
  }
  void surf(Input i, inout SurfaceOutputStandard o)
  {
   float band = abs(i.stripe.y) < .28 ? 1 : step(.28, frac(i.stripe.x * 4 + i.stripe.y * .25));
   o.Albedo = lerp(fixed3(.025, .025, .018), _Color.rgb, band);
   o.Metallic = 0;
   o.Smoothness = 0.3;
   // Optional, if the instructor wants the tape faintly readable in the dark: o.Emission = o.Albedo * 0.02;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
