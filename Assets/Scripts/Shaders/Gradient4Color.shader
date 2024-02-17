 Shader "Custom/Gradient_4Color" {
     Properties {
         [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
         _ColorTop ("Top Color", Color) = (1,1,1,1)
         _ColorMidTop ("MidTop Color", Color) = (1,1,1,1)
         _ColorMidBot ("MidBot Color", Color) = (1,1,1,1)
         _ColorBot ("Bot Color", Color) = (1,1,1,1)
         _MiddleBot ("MiddleBot", Range(0.001, 0.999)) = 0.33
          _MiddleTop ("MiddleTop", Range(0.001, 0.999)) = 0.66
     }

     SubShader {
         Tags {"Queue"="Background"  "IgnoreProjector"="True"}
         LOD 100

         ZWrite On

         Pass {
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
         #include "UnityCG.cginc"

         fixed4 _ColorTop;
         fixed4 _ColorMidTop;
         fixed4 _ColorMidBot;
         fixed4 _ColorBot;

          float  _MiddleBot;
          float  _MiddleTop;

         struct v2f {
             float4 pos : SV_POSITION;
             float4 texcoord : TEXCOORD0;
         };

         v2f vert (appdata_full v) {
             v2f o;
             o.pos = UnityObjectToClipPos (v.vertex);
             o.texcoord = v.texcoord;
             return o;
         }

         fixed4 frag (v2f i) : COLOR {

             fixed4 c = lerp(_ColorBot, _ColorMidBot, i.texcoord.y / _MiddleBot) * step(i.texcoord.y, _MiddleBot);
             c += lerp(_ColorMidBot, _ColorMidTop, (i.texcoord.y - _MiddleBot) /(_MiddleTop - _MiddleBot) ) * step(_MiddleBot,i.texcoord.y) * step(i.texcoord.y,_MiddleTop);
             c += lerp(_ColorMidTop, _ColorTop, (i.texcoord.y - _MiddleTop) / (1 - _MiddleTop)) * step(_MiddleTop, i.texcoord.y);
             c.a = 1;

             return c;
         }
         ENDCG
         }
     }
 }