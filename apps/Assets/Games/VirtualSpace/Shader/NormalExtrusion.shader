Shader "Custom/Normal Extrusion" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	    _TexCutout("Cutout", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		_Amount("Extrusion Amount", Range(-1,1)) = 0.5
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Lambert vertex:vert
		struct Input {
		float2 uv_MainTex;
	};
		fixed4 _Color;
	float _Amount;

	void vert(inout appdata_full v) {
		v.vertex.xyz += v.normal * _Amount;
	}

	sampler2D _MainTex;
	sampler2D _TexCutout;

	void surf(Input IN, inout SurfaceOutput o) {
		//o.Albedo = _Color.rgb;
		//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Emission = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Alpha = tex2D(_TexCutout, IN.uv_MainTex).rgb;
	}
	ENDCG
	}
		Fallback "Diffuse"
}