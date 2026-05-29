Shader "Custom/TitleLiquid"
{
    Properties
    {
        _MainTex        ("Texture",      2D)    = "white" {}
        _Color          ("Tint Color",   Color)  = (1,1,1,1)

        [Header(Wave A)]
        _AmpA   ("Amplitude A",  Float) = 0.015
        _FreqA  ("Frequency A",  Float) = 2.5
        _SpeedA ("Speed A",      Float) = 0.8

        [Header(Wave B)]
        _AmpB   ("Amplitude B",  Float) = 0.007
        _FreqB  ("Frequency B",  Float) = 5.3
        _SpeedB ("Speed B",      Float) = 1.3

        [Header(Vertical Drift)]
        _DriftAmp   ("Drift Amplitude",  Float) = 0.008
        _DriftFreq  ("Drift Frequency",  Float) = 1.8
        _DriftSpeed ("Drift Speed",      Float) = 0.5

        // Campos internos do UI do Unity — necessários para RawImage
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil     ("Stencil ID",         Float) = 0
        _StencilOp   ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref      [_Stencil]
            Comp     [_StencilComp]
            Pass     [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float4    _ClipRect;

            float _AmpA;   float _FreqA;  float _SpeedA;
            float _AmpB;   float _FreqB;  float _SpeedB;
            float _DriftAmp; float _DriftFreq; float _DriftSpeed;

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.worldPos = IN.vertex;
                OUT.uv       = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color    = IN.color * _Color;
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float t  = _Time.y;
                float2 uv = IN.uv;

                // Onda A — distorce U ao longo de V
                float waveA = sin(uv.y * _FreqA * 6.2831 + t * _SpeedA) * _AmpA;

                // Onda B — frequência maior, mais irregular
                float waveB = sin(uv.y * _FreqB * 6.2831 + t * _SpeedB + 1.3) * _AmpB;

                // Drift vertical — distorce V ao longo de U
                float drift = sin(uv.x * _DriftFreq * 6.2831 + t * _DriftSpeed) * _DriftAmp;

                // Aplica distorção nas UVs (fragment shader — distorce a textura)
                uv.x += waveA + waveB;
                uv.y += drift;

                fixed4 col = tex2D(_MainTex, uv) * IN.color;

                // Respeita o clipping do Canvas
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);

                return col;
            }
            ENDCG
        }
    }
}
