/*
* Copyright (c) 2015 José Guerreiro. All rights reserved.
*
* This source code is licensed under the MIT license found in the
* LICENSE file in the root directory of this source tree.
*/

Shader "Hidden/OutlineEffect" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_FillTexture ("Base (RGB)", 2D) = "white" {}		
	}
	SubShader 
	{
		Pass
		{
			Tags{ "RenderType" = "Opaque" }
			LOD 200
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _OutlineSource;

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;

				return o;
			}

			float _LineThicknessX;
			float _LineThicknessY;
			int _FlipY;
			uniform float4 _MainTex_TexelSize;

			half4 frag(v2f input) : COLOR
			{
				float2 uv = input.uv;
				if (_FlipY == 1)
				uv.y = uv.y;
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0)
					uv.y = 1 - uv.y;
				#endif

				//half4 originalPixel = tex2D(_MainTex,input.uv, UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST));
				half4 outlineSource = tex2D(_OutlineSource, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));

				const float h = .95f;

				half4 sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX,0.0));
				half4 sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX,0.0));
				half4 sample3 = tex2D(_OutlineSource, uv + float2(.0,_LineThicknessY));
				half4 sample4 = tex2D(_OutlineSource, uv + float2(.0,-_LineThicknessY));

				bool red = sample1.r > h || sample2.r > h || sample3.r > h || sample4.r > h;
				bool green = sample1.g > h || sample2.g > h || sample3.g > h || sample4.g > h;
				bool blue = sample1.b > h || sample2.b > h || sample3.b > h || sample4.b > h;
				
				if ((red && blue) || (green && blue) || (red && green))
				return float4(0,0,0,0);
				else
				return outlineSource;
			}

			ENDCG
		}

		Pass
		{
			Tags { "RenderType"="Opaque" }
			LOD 200
			ZTest Always
			ZWrite Off
			Cull Off
			
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _OutlineSource;
			sampler2D _FillTexture;

			struct v2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPosition: TEXCOORD1;
			};
			
			v2f vert(appdata_img v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);				
				o.screenPosition = ComputeScreenPos(o.position);
				o.uv = v.texcoord;
				
				return o;
			}

			int _UseFillColor;
			int _UseFillTexture;
			float _FillTextureScaleX;
			float _FillTextureScaleY;
			fixed4 _FillColor;
			float _LineThicknessX;
			float _LineThicknessY;
			float _LineIntensity;
			half4 _LineColor1;
			half4 _LineColor2;
			half4 _LineColor3;
			int _FlipY;
			int _Dark;
			float _FillAmount;
			int _CornerOutlines;
			uniform float4 _MainTex_TexelSize;

			half4 frag (v2f input) : COLOR
			{	
				float2 uv = input.uv;
				if (_FlipY == 1)
				uv.y = 1 - uv.y;
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0)
					uv.y = 1 - uv.y;
				#endif

				half4 originalPixel = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST));
				half4 outlineSource = tex2D(_OutlineSource, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
				
				const float h = .95f;
				half4 outline = 0;
				bool hasOutline = false;

				half4 sample1 = tex2D(_OutlineSource, uv + float2(_LineThicknessX,0.0));
				half4 sample2 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX,0.0));
				half4 sample3 = tex2D(_OutlineSource, uv + float2(.0,_LineThicknessY));
				half4 sample4 = tex2D(_OutlineSource, uv + float2(.0,-_LineThicknessY));
				
				bool outside = outlineSource.a < h;
				bool outsideDark = outside && _Dark;

				if (_CornerOutlines)
				{
					// TODO: Conditional compile
					half4 sample5 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, _LineThicknessY));
					half4 sample6 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, -_LineThicknessY));
					half4 sample7 = tex2D(_OutlineSource, uv + float2(_LineThicknessX, -_LineThicknessY));
					half4 sample8 = tex2D(_OutlineSource, uv + float2(-_LineThicknessX, _LineThicknessY));

					if (sample1.r > h || sample2.r > h || sample3.r > h || sample4.r > h ||
					sample5.r > h || sample6.r > h || sample7.r > h || sample8.r > h)
					{
						outline = _LineColor1 * _LineIntensity * _LineColor1.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor1.a;
						hasOutline = true;
					}
					else if (sample1.g > h || sample2.g > h || sample3.g > h || sample4.g > h ||
					sample5.g > h || sample6.g > h || sample7.g > h || sample8.g > h)
					{
						outline = _LineColor2 * _LineIntensity * _LineColor2.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor2.a;
						hasOutline = true;
					}
					else if (sample1.b > h || sample2.b > h || sample3.b > h || sample4.b > h ||
					sample5.b > h || sample6.b > h || sample7.b > h || sample8.b > h)
					{
						outline = _LineColor3 * _LineIntensity * _LineColor3.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor3.a;
						hasOutline = true;
					}
				}
				else
				{
					if (sample1.r > h || sample2.r > h || sample3.r > h || sample4.r > h)
					{
						outline = _LineColor1 * _LineIntensity * _LineColor1.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor1.a;
						hasOutline = true;
					}
					else if (sample1.g > h || sample2.g > h || sample3.g > h || sample4.g > h)
					{
						outline = _LineColor2 * _LineIntensity * _LineColor2.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor2.a;
						hasOutline = true;
					}
					else if (sample1.b > h || sample2.b > h || sample3.b > h || sample4.b > h)
					{
						outline = _LineColor3 * _LineIntensity * _LineColor3.a;
						if (outsideDark)
						originalPixel *= 1 - _LineColor3.a;
						hasOutline = true;
					}
				}					
				
				//return outlineSource;		
				if (hasOutline)
				{			
					half4 fillTexture = float4(1,1,1,0);		

					if (!outside)
					{		
						if(_UseFillTexture)
						{
							float2 textureCoordinate = input.screenPosition.xy / input.screenPosition.w;
							float aspect = _ScreenParams.x / _ScreenParams.y;
							textureCoordinate.x = textureCoordinate.x * aspect;
							fillTexture = tex2D(_FillTexture, UnityStereoScreenSpaceUVAdjust(textureCoordinate * float2(_FillTextureScaleX, _FillTextureScaleY), _MainTex_ST));
						}
						else{
							fillTexture = float4(1,1,1,1);
						}

						if(_UseFillColor)
						{							
							outline = _FillColor * _FillAmount * fillTexture.a;
						}
						else
						{							
							outline *= _FillAmount * fillTexture.a;
						}
					}

					return lerp(originalPixel + outline, outline, outline.a * _FillAmount * fillTexture.a);
				}
				else
				return originalPixel;
			}
			
			ENDCG
		}
	} 

	FallBack "Diffuse"
}