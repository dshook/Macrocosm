Shader "Custom/ScrollingTexture" {
    Properties {
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        _TextureScroll ("Texture Scroll", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Lighting Off
        Cull Off

        pass
        {
            Tags { "RenderType" = "Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 2.0

            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _RendererColor;
            sampler2D _MainTex;
            float2 _TextureScroll;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD1;
                fixed4 color    : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 scrollSpeed = _TextureScroll.xy * _Time;
                o.uv = v.texcoord.xy + scrollSpeed;

                o.color = v.color * _Color * _RendererColor;

                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}