Shader "Custom/MapShader" {
	Properties {
//		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		
		_WaterTex ("Water Texture", 2D) = "white" {}
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

		struct Input {
			float2 uv_MainTex;
			float2 uv_WaterTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
//
//        void vertex(inout appdata_full v, out Input o)
//        {
//            UNITY_INITIALIZE_OUTPUT(Input, o);
//            
//        }


		void surf (Input IN, inout SurfaceOutputStandard o) {
		
		    IN.uv_WaterTex += _Time.x + _SinTime.z * 0.2;
		
		    fixed4 waterColor = tex2D (_WaterTex, IN.uv_WaterTex);// * _Color;
		
		
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);// * _Color;
			
			c.rgb = lerp(c.rgb, waterColor.rgb, c.a);
			
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = c.a * Luminance(waterColor);
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
