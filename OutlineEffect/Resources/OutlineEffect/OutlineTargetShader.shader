Shader "Hidden/OutlineEffect" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LineColor ("Line Color", Color) = (1,1,1,.5)
		
	}
	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
			LOD 200
			ZTest Always
			ZWrite Off
			
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _OutlineSource;

			struct v2f {
			   float4 position : SV_POSITION;
			   float2 uv : TEXCOORD0;
			};
			
			v2f vert(appdata_img v)
			{
			   	v2f o;
				o.position = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				
			   	return o;
			}

			float _LineThicknessX;
			float _LineThicknessY;
			float _LineIntensity;
			half4 _LineColor;

			half4 frag (v2f input) : COLOR
			{	
				half4 originalPixel = tex2D(_MainTex, input.uv);
				half4 outlineSource = tex2D(_OutlineSource, input.uv);
								
				float h = .95f;
				half4 outline = 0;

				float3 sample1 = tex2D(_OutlineSource, input.uv + float2(_LineThicknessX,0.0)).rgb;
				float3 sample2 = tex2D(_OutlineSource, input.uv + float2(-_LineThicknessX,0.0)).rgb;
				float3 sample3 = tex2D(_OutlineSource, input.uv + float2(.0,_LineThicknessY)).rgb;
				float3 sample4 = tex2D(_OutlineSource, input.uv + float2(.0,-_LineThicknessY)).rgb;
				
				if(outlineSource.b > h && (
					   sample1.b < h
					|| sample2.b < h
					|| sample3.b < h
					|| sample4.b < h
					))
					outline = _LineIntensity;
								
				return originalPixel + outline * _LineColor;
				//return outlineSource;
			}
			
			ENDCG
		}
	} 
	FallBack "Diffuse"
}