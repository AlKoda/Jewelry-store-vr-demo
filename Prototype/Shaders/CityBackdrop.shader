Shader "CrimeScene/CityBackdrop" {
 Properties { _MainTex("City panorama",2D)="white"{} _Tint("Tint",Color)=(.75,.75,.75,1) }
 SubShader { Tags {"Queue"="Transparent-50" "RenderType"="Transparent"} ZWrite Off Cull Back Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1) UNITY_VERTEX_OUTPUT_STEREO};
 sampler2D _MainTex;fixed4 _Tint;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.position);return o;}
 fixed4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);fixed4 c=tex2D(_MainTex,i.uv);c.rgb*=_Tint.rgb;UNITY_APPLY_FOG(i.fogCoord,c);c.a=(1-smoothstep(.50,.82,i.uv.y))*smoothstep(0,.18,i.uv.y)*smoothstep(0,.09,i.uv.x)*smoothstep(0,.09,1-i.uv.x)*_Tint.a;return c;}
 ENDCG }
 } Fallback Off
}
