Shader "CrimeScene/GlassContact" {
 Properties {_Strength("Soft pile contact",Range(0,.2))=.09 _UniformStrength("Shard contact",Range(0,.2))=0}
 // Depth bias seats the sub-millimetre contact projection without floor z-fighting.
 SubShader { Tags {"Queue"="Transparent-10" "RenderType"="Transparent"} ZWrite Off Offset -1,-1 Blend DstColor Zero
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 float _Strength,_UniformStrength;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=(i.uv-.5)*2;float fade=pow(saturate(1-dot(p,p)),3);float transmission=1-max(_Strength*fade,_UniformStrength);return fixed4(transmission,transmission,transmission,1);}
 ENDCG }
 } Fallback Off
}
