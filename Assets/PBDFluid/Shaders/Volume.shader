
Shader "PBDFluid/Volume"
{
	Properties
	{
		AbsorptionCoff("Absorption Coff", Vector) = (0.45, 0.029, 0.018)
		AbsorptionScale("Absorption Scale", Range(0.01, 10)) = 1.5
	}
		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100

		GrabPass { "BackGroundTexture" }

		cull front
		ztest always
		blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			#define NUM_SAMPLES 64

			float AbsorptionScale;
			float3 AbsorptionCoff;
			float3 Translate, Scale, Size;
			sampler3D Volume;
			sampler2D BackGroundTexture;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float4 grabPos : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
				OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				OUT.grabPos = ComputeGrabScreenPos(OUT.pos);
				return OUT;
			}

			struct Ray 
			{
				float3 origin;
				float3 dir;
			};

			struct AABB 
			{
				float3 Min;
				float3 Max;
			};

			//find intersection points of a ray with a box
			bool IntersectBox(Ray r, AABB aabb, out float t0, out float t1)
			{
				float3 invR = 1.0 / r.dir;
				float3 tbot = invR * (aabb.Min - r.origin);
				float3 ttop = invR * (aabb.Max - r.origin);
				float3 tmin = min(ttop, tbot);
				float3 tmax = max(ttop, tbot);
				float2 t = max(tmin.xx, tmin.yz);
				t0 = max(t.x, t.y);
				t = min(tmax.xx, tmax.yz);
				t1 = min(t.x, t.y);
				return t0 <= t1;
			}
			
			fixed4 frag (v2f IN) : SV_Target
			{
				float3 pos = _WorldSpaceCameraPos;
				float3 grab = tex2Dproj(BackGroundTexture, IN.grabPos).rgb;

				Ray r;
				r.origin = pos;
				r.dir = normalize(IN.worldPos - pos);

				AABB aabb;
				aabb.Min = float3(-0.5,-0.5,-0.5) * Scale + Translate;
				aabb.Max = float3(0.5,0.5,0.5) * Scale + Translate;

				//figure out where ray from eye hit front of cube
				float tnear, tfar;
				IntersectBox(r, aabb, tnear, tfar);

				//if eye is in cube then start ray at eye
				if (tnear < 0.0) tnear = 0.0;

				float3 rayStart = r.origin + r.dir * tnear;
				float3 rayStop = r.origin + r.dir * tfar;

				//convert to texture space
				rayStart -= Translate;
				rayStop -= Translate;
				rayStart = (rayStart + 0.5 * Scale) / Scale;
				rayStop = (rayStop + 0.5 * Scale) / Scale;

				float3 start = rayStart;
				float dist = distance(rayStop, rayStart);
				float stepSize = dist / float(NUM_SAMPLES);
				float3 ds = normalize(rayStop - rayStart) * stepSize;
				
				//accumulate density though volume along ray
				float density = 0;
				for (int i = 0; i < NUM_SAMPLES; i++, start += ds)
				{
					density += tex3D(Volume, start).x;
				}

				float3 col = grab * exp(-AbsorptionCoff * density * AbsorptionScale);

				return float4(col, 1);
			}
			ENDCG
		}
	}
}

