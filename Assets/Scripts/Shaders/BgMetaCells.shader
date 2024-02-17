Shader "Custom/BgMetaCells"
{
    Properties
    {
		color_bg("Background Color", Color) = (1,1,1,1)
		color_inner("Inner Color", Color) = (1.,0.9,0.16,1)
		color_outer("Outer Color", Color) = (.12,0.59,0.21,1)
        minCellCize("Min Cell Size", float) = 30.75
        maxCellCize("Max Cell Size", float) = 0.25
        timeScale("Time Scale", float) = 1.
        mapScale("Map Scale", float) = 1.
        _Resolution("Resolution (Change if AA is bad)", Range(1, 1024)) = 1
    }
    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Built-in properties
            float _Resolution;

            // GLSL Compatability macros
            #define iResolution float3(_Resolution, _Resolution, _Resolution)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv =  v.uv;
                return o;
            }

#define cellCount 12

            float4 color_bg;
            float4 color_inner;
            float4 color_outer;

            float minCellCize;
            float maxCellCize;

            float timeScale;
            float mapScale;


            static float2 cellSize = float2(minCellCize , minCellCize * 1.5);
            float4 powerToColor(float2 power)
            {
                float tMax = pow(1.03, mapScale*2.2);
                float tMin = 1./tMax;
                float4 color = lerp(color_bg, color_outer, smoothstep(tMin, tMax, power.y));
                color = lerp(color, color_inner, smoothstep(tMin, tMax, power.x));
                return color;
            }

            float2 getCellPower(float2 coord, float2 pos, float2 size)
            {
                float2 power;
                power = size*size/dot(coord-pos, coord-pos);
                power *= power*sqrt(power);
                return power;
            }

            float4 frag (v2f vertex_output) : SV_Target
            {
                float4 color = 0;
                float2 coord = vertex_output.uv * _Resolution;

                float T = _Time.y*timeScale;

                float2 hRes = iResolution.xy*0.5;
                float2 pos;
                float2 power = float2(0., 0.);

                [unroll(cellCount)]
                for (float x = 1.0; x != cellCount+1.0; ++x)
                {
                    pos = hRes * float2(
                        sin(T * frac(0.246 * x) + x * 3.6)
                        * cos(T * frac(0.374 * x) - x * frac(0.6827 * x)) + 1.
                        ,
                        cos(T * frac(0.4523 * x) + x * 5.5)
                        * sin(T * frac(0.128 * x) + x * frac(0.3856 * x))+1.
                    );
                    power += getCellPower(coord.xy, pos, cellSize * (0.75 + frac(0.2834*x) * maxCellCize) / mapScale );
                }

                color.rgba = powerToColor(power);

                return color;
            }
            ENDCG
        }
    }
}
