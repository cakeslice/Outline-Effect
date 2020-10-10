/*
 * Copyright (c) 2015 José Guerreiro. All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

Shader "Hidden/OutlineBufferEffect" {
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}

	SubShader
	{ 
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull [_Culling]
		Lighting Off
			
		CGPROGRAM

		#pragma surface surf Lambert vertex:vert nofog noshadow noambient nolightmap novertexlights noshadowmask nometa //keepalpha
		#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
		fixed4 _Color;
		float _OutlineAlphaCutoff;

		struct Input
		{
			float2 uv_MainTex;
			//fixed4 color;
		};

		void vert(inout appdata_full v, out Input o)
		{
			#if defined(PIXELSNAP_ON)
			v.vertex = UnityPixelSnap(v.vertex);
			#endif

			UNITY_INITIALIZE_OUTPUT(Input, o);
			//o.color = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);// * IN.color;
			if (c.a < _OutlineAlphaCutoff) discard;

			/* float alpha = c.a * 99999999;
			o.Albedo = _Color * alpha;
			o.Alpha = alpha; */
			o.Albedo = _Color;
			o.Emission = o.Albedo;
		}

		ENDCG		
	}

	Fallback "Transparent/VertexLit"
}
