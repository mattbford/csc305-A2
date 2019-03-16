// written with help from lab session
Shader "Unlit/islandsShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Tex1("Texture 1", 2D) = "white" {}
		_Tex2("Texture 2", 2D) = "white" {}
		_Tex3("Texture 3", 2D) = "white" {}
		_Thresholds("Height Threshold", vector) = (0,0,0,0)
		_SpecularPow("Specular Power", float) = 1
		_Specular_coef("Specular Coefficient", float) = 1
		_AmbientLight("Ambient Lighting", vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque"
		"LightMode" = "ForwardBase"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				float height : TEXCOORD1;
				float3 normal : TEXCOORD2;
				float4 worldpos : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _Tex1;    // added
			sampler2D _Tex2;    // added
			sampler2D _Tex3;    // added
			float4 _Thresholds; // added
			float _SpecularPow;
			float _SpecularCoef;
			float _AmbientLight;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.height = v.vertex.y; // added
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(v.normal); // added
				o.worldpos = mul(unity_ObjectToWorld, v.vertex); // added
				// UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				/*
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
				*/

				fixed4 col0 = tex2D(_MainTex, i.uv);
				fixed4 col1 = tex2D(_Tex1, i.uv);
				fixed4 col2 = tex2D(_Tex2, i.uv);
				fixed4 col3 = tex2D(_Tex3, i.uv);
				float4 albedo = 0;

				if (i.height >= _Thresholds.x) albedo = col0;
				if (i.height < _Thresholds.x) albedo = col1;
				if (i.height < _Thresholds.y) albedo = col2;
				if (i.height == _Thresholds.z) albedo = col3;


				// ambient
				float4 fragment_color = _AmbientLight * albedo;
				fragment_color.w = 1;

				// diffuse
				float3 L = normalize(_WorldSpaceLightPos0.xyz);
				float3 N = normalize(i.normal);
				float d = dot(L, N);

				if (d > 0)
				{
					fragment_color.xyz += _LightColor0 * d * albedo.xyz;
				}

				//  Phong-Blinn Specular
				float3 V = _WorldSpaceCameraPos.xyz - i.worldpos.xyz;
				V = normalize(V);
				float3 H = (V + L) / 2;
				d = dot(H, N);
				if (d > 0)
				{
					float specular = pow(d, _SpecularPow) * _SpecularCoef;
					fragment_color.xyz += _LightColor0.xyz * specular;
				}
				return fragment_color;
			}
			ENDCG
		}
	}
}