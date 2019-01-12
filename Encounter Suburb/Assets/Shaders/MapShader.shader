Shader "Custom/MapShader" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_WaterTex ("Water Texture", 2D) = "white" {}
		_WaterTint("Water Tint", color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _WaterTex;
        fixed4 _WaterTint;  

		struct Input {
			float2 uv_MainTex;
			float2 uv_WaterTex;
		};


		void surf (Input IN, inout SurfaceOutputStandard o) {
		
		    IN.uv_WaterTex += _Time.x + _SinTime.z * 0.2;
		    fixed4 waterColor = tex2D (_WaterTex, IN.uv_WaterTex) * _WaterTint;
		
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			
			c.rgb = lerp(c.rgb, waterColor.rgb, c.a);
			
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = 0f;
			o.Smoothness = 0f;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
