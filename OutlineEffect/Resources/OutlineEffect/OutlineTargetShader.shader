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
				float2 fixedUV = float2(input.uv.x,  input.uv.y);
				half4 originalPixel = tex2D(_MainTex, input.uv);
				half4 outlineSource = tex2D(_OutlineSource, fixedUV);
				
			    float _OutLineSpreadX  = _LineThicknessX;
				float _OutLineSpreadY = _LineThicknessY;

				half4 outline = 0;
				float h = .95f;
				if(outlineSource.b > h 
					&& (tex2D(_OutlineSource, fixedUV + float2(_OutLineSpreadX,0.0)).b < h
					|| tex2D(_OutlineSource, fixedUV + float2(-_OutLineSpreadX,0.0)).b < h
					|| tex2D(_OutlineSource, fixedUV + float2(.0,_OutLineSpreadY)).b < h
					|| tex2D(_OutlineSource, fixedUV + float2(.0,-_OutLineSpreadY)).b < h))
					outline = _LineIntensity;
								
				return originalPixel + outline * _LineColor;
			}
			
			ENDCG
		}
	} 
	FallBack "Diffuse"
}