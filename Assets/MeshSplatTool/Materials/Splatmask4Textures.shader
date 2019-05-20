Shader "UnityCoder/Splatmask4Textures" {
	Properties {
		_MainTex1 ("Flat", 2D) = "white" {}
		_MainTex2 ("Medium Angle Texture", 2D) = "white" {}
		_MainTex3 ("Steep Angle Texture", 2D) = "white" {}
		_MainTex4 ("Road Texture", 2D) = "white" {}
		_Mask ("SplatMask (RGBA)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex1;
		sampler2D _MainTex2;
		sampler2D _MainTex3;
		sampler2D _MainTex4;
		sampler2D _Mask;

		struct Input {
			float2 uv_MainTex1;
			float2 uv_Mask;
		};

		void surf (Input i, inout SurfaceOutput o) 
		{
			float3 color1 = tex2D( _MainTex1, i.uv_MainTex1.xy ).rgb;
			float3 color2 = tex2D( _MainTex2, i.uv_MainTex1.xy ).rgb;
			float3 color3 = tex2D( _MainTex3, i.uv_MainTex1.xy ).rgb;
			half4 color4 = tex2D( _MainTex4, i.uv_MainTex1.xy );
			float4 mask = tex2D( _Mask, i.uv_Mask.xy );
			
			float3 c = color1 * mask.r + color2 * mask.g + color3 * mask.b;
			
			c = lerp(c,color4,mask.a);

/*
			if (mask.a>0)
			{
				c = color4 * mask.a;
			}*/
	
			o.Albedo = c;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
