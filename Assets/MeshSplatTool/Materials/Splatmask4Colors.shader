Shader "UnityCoder/Splatmask4Colors" {
	Properties {
		_Color1 ("Flat Angle Color", Color) = (176,124,70,1)
		_Color2 ("Medium Angle Color", Color) = (43,54,16,1)
		_Color3 ("Steep Angle Color", Color) = (80,75,73,1)
		_Color4 ("Road Color", Color) = (82,60,41,1)
		_Mask ("SplatMask (RGBA)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

//		sampler2D _MainTex;
		float3 _Color1;
		float3 _Color2;
		float3 _Color3;
		float4 _Color4;
		sampler2D _Mask;

		struct Input {
//			float2 uv_MainTex;
			float2 uv_Mask;
		};

		void surf (Input i, inout SurfaceOutput o) 
		{
			float4 mask = tex2D( _Mask, i.uv_Mask.xy );
			
			float3 c = _Color1 * mask.r + _Color2 * mask.g + _Color3 * mask.b;
			
			c = lerp(c,_Color4,mask.a);

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
