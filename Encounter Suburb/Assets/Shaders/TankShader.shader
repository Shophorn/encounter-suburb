Shader "Custom/TankShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_ComboMask ("Combo Mask", 2D) = "black" {} // metal on red, smooth on green, color change on blue
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
        sampler2D _ComboMask;
        
		struct Input {
			float2 uv_MainTex;
		};

        
		UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)	
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			
			fixed3 mask = tex2D(_ComboMask, IN.uv_MainTex).rgb;
			fixed3 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color).rgb;
			
			o.Albedo = c.rgb * lerp(float3(1,1,1), color, mask.b);
			// Metallic and smoothness come from slider variables
			o.Metallic = mask.r;
			o.Smoothness = mask.g;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
