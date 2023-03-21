Shader "Unlit/PostTestShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Strength("Strength",float) = 0
        _Split("Split",float) = 0.1
        _Center("Center",Vector) = (0.5,0.5,1,1)
        _BlendColor("BlendColor",Color) = (1,1,1,1)
        _BlendStrength("BlendStrength",float) = 1
        _StripStrength("StripStrength",float) = 0.15
        _StripeRoll("StripeRoll",float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float _Strength;
            float _Split;
            float _BlendStrength;
            float _StripStrength;
            float4 _Center;
            fixed4 _BlendColor;
            float _StripeRoll;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                _BlendColor.a = 1;

                float dist = length(_Center.xy - i.uv);
                dist = pow(dist,1.5) / 25;
                float2 dir = normalize(i.uv - _Center.xy);
                dir.y = 0.5 * dir.y * _MainTex_TexelSize.w / _MainTex_TexelSize.z;

                float2 samPos = i.uv + dir * dist * _Strength;

                float strip = floor((samPos - 5 * dir * dist * _Strength).y * _MainTex_TexelSize.z / 10 + _StripeRoll);
                float stripStength = 0;
                if (strip % 3 == 0)
                {
                    stripStength = 1 *  _Strength * _StripStrength;
                }

                fixed4 col = tex2D(_MainTex,samPos);
                //col.g = tex2D(_MainTex,i.uv - (dir * dist * (1 + _Split)) * _Strength).g;
                //col.b = tex2D(_MainTex,i.uv - (dir * dist * (1 - _Split)) * _Strength).b;
                fixed f = clamp(pow(col.r + 0.2,1.2) - 0.2,0,1);
                fixed4 resultCol = lerp(fixed4(0,0,0,1),_BlendColor,f);

                resultCol = lerp(resultCol,fixed4(1,1,1,1),f) + resultCol * 0.2;


                resultCol = lerp(col,resultCol,_Strength * 1.5);

                resultCol =  resultCol * (1 - dist * _Strength * _BlendStrength) + _BlendColor * dist * _Strength * _BlendStrength;
                return resultCol * (1 - stripStength) + _BlendColor * stripStength;
            }
            ENDCG
        }
    }
}
