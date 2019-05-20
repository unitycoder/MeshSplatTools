Shader "UnityCoder/Splatmask4Textures" {
	Properties {
		_MainTex1 ("Flat", 2D) = "white" {}
		_MainTex1Normal ("Flat (bump)", 2D) = "bump" {}
		_MainTex2 ("Medium Angle Texture", 2D) = "white" {}
		_MainTex2Normal ("Medium Angle (bump)", 2D) = "bump" {}
		_MainTex3 ("Steep Angle Texture", 2D) = "white" {}
		_MainTex3Normal ("Steep Angle (bump)", 2D) = "bump" {}
		_MainTex4 ("Road Texture", 2D) = "white" {}
		_MainTex4Normal ("Road (bump)", 2D) = "bump" {}
		
		_Mask ("SplatMask (RGBA)", 2D) = "white" {}
		
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0

		sampler2D _MainTex1;
		sampler2D _MainTex2;
		sampler2D _MainTex3;
		sampler2D _MainTex4;
		sampler2D _MainTex1Normal;
		sampler2D _MainTex2Normal;
		sampler2D _MainTex3Normal;
		sampler2D _MainTex4Normal;
		sampler2D _Mask;

		struct Input {
			float2 uv_MainTex1;
			float2 uv_Mask;
		};

		void surf (Input i, inout SurfaceOutput o) 
		{
			// mix colors using mask
			fixed3 color1 = tex2D( _MainTex1, i.uv_MainTex1.xy ).rgb;
			fixed3 color2 = tex2D( _MainTex2, i.uv_MainTex1.xy ).rgb;
			fixed3 color3 = tex2D( _MainTex3, i.uv_MainTex1.xy ).rgb;
			fixed3 color4 = tex2D( _MainTex4, i.uv_MainTex1.xy ).rgb;
			
			fixed4 mask = tex2D( _Mask, i.uv_Mask.xy );
			
			fixed3 c = color1 * mask.r + color2 * mask.g + color3 * mask.b;
			c = lerp(c,color4,mask.a);
			
			// normals
			fixed3 normal1 = UnpackNormal(tex2D (_MainTex1Normal, i.uv_MainTex1.xy));
			fixed3 normal2 = UnpackNormal(tex2D (_MainTex2Normal, i.uv_MainTex1.xy));
			fixed3 normal3 = UnpackNormal(tex2D (_MainTex3Normal, i.uv_MainTex1.xy));
			fixed3 normal4 = UnpackNormal(tex2D (_MainTex4Normal, i.uv_MainTex1.xy));
			
			fixed3 n = normal1 * mask.r + normal2 * mask.g + normal3 * mask.b;
			n = lerp(n,normal4,mask.a);
			

			// output
			o.Albedo = c;
			o.Normal = n;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
