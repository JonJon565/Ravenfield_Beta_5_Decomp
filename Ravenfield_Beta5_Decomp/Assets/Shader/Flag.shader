Shader "Custom/Flag" {
Properties {
 _Color ("Main Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 _TranslucencyColor ("Translucency Color", Color) = (0.730000,0.850000,0.410000,1.000000)
 _Cutoff ("Alpha cutoff", Range(0.000000,1.000000)) = 0.300000
 _TranslucencyViewDependency ("View dependency", Range(0.000000,1.000000)) = 0.700000
 _ShadowStrength ("Shadow Strength", Range(0.000000,1.000000)) = 1.000000
 _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" { }
[HideInInspector]  _TreeInstanceColor ("TreeInstanceColor", Vector) = (1.000000,1.000000,1.000000,1.000000)
[HideInInspector]  _TreeInstanceScale ("TreeInstanceScale", Vector) = (1.000000,1.000000,1.000000,1.000000)
[HideInInspector]  _SquashAmount ("Squash", Float) = 1.000000
}
	//DummyShaderTextExporter
	
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard fullforwardshadows
#pragma target 3.0
		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
		}
		ENDCG
	}
}