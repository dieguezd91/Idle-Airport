Shader "TextMeshPro/Text Blink"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0

        _FlashColor ("Flash Color", Color) = (1,1,0,1)
        _FlashSpeed ("Flash Speed", Range(0.1,20)) = 2
        _BlinkSharpness ("Blink Sharpness (0 = smooth pulse, 1 = hard on/off)", Range(0,1)) = 1
        _MinAlpha ("Minimum Alpha", Range(0,1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // TMP feeds the SDF alpha through uv0 and per-vertex tint through COLOR
            struct VertexInput
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _FaceColor;
            float _FaceDilate;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            fixed4 _FlashColor;
            float _FlashSpeed;
            float _BlinkSharpness;
            float _MinAlpha;

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).a;
                d = d + _FaceDilate * 0.5 - 0.5;

                float outline = _OutlineWidth * 0.5;
                float softness = max(_OutlineSoftness * 0.5, 0.001);

                float faceAlpha = smoothstep(-softness, softness, d);
                float outlineAlpha = smoothstep(-outline - softness, -outline + softness, d);

                fixed4 col = lerp(_OutlineColor, _FaceColor, faceAlpha);
                col.a *= outlineAlpha;

                float wave = sin(_Time.y * _FlashSpeed * 6.28318);
                float smoothBlink = wave * 0.5 + 0.5;
                float hardBlink = step(0, wave);
                float blink = lerp(smoothBlink, hardBlink, _BlinkSharpness);

                col.rgb = lerp(col.rgb, _FlashColor.rgb, blink);
                col.a *= max(blink, _MinAlpha);
                col *= i.color;

                return col;
            }
            ENDCG
        }
    }
}