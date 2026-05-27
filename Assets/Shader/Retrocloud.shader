Shader "Custom/RetroCloud"
{
    Properties
    {
        _CloudColor    ("Cloud Color",        Color)       = (1,1,1,1)
        _NoiseScale    ("Noise Scale",        Float)       = 3.0
        _AlphaThreshold("Alpha Threshold",    Range(0,1))  = 0.4
        _EdgeSoftness  ("Edge Softness",      Range(0,0.5))= 0.15
        _WaveHeight    ("Wave Height",        Float)       = 0.3
        _WaveFreqX     ("Wave Frequency X",   Float)       = 1.5
        _WaveFreqZ     ("Wave Frequency Z",   Float)       = 1.2
        _WaveSpeed     ("Wave Speed",         Float)       = 1.0
        _ScrollSpeed   ("Noise Scroll Speed", Vector)      = (0.05, 0.03, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CloudColor;
                float  _NoiseScale;
                float  _AlphaThreshold;
                float  _EdgeSoftness;
                float  _WaveHeight;
                float  _WaveFreqX;
                float  _WaveFreqZ;
                float  _WaveSpeed;
                float4 _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // ── Noise ─────────────────────────────────────────────────────

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            // FBM — formas orgânicas de nuvem
            float fbm(float2 uv)
            {
                float val  = 0.0;
                float amp  = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 5; i++)
                {
                    val  += valueNoise(uv * freq) * amp;
                    amp  *= 0.5;
                    freq *= 2.1;
                }
                return val;
            }

            // ── Vertex: ondulação da mesh ─────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float t = _Time.y * _WaveSpeed;

                // Ondas compostas em X e Z para parecer orgânico
                float wave =
                    sin(IN.positionOS.x * _WaveFreqX + t) * 0.6 +
                    sin(IN.positionOS.z * _WaveFreqZ + t * 0.8) * 0.4 +
                    sin((IN.positionOS.x + IN.positionOS.z) * _WaveFreqX * 0.5 + t * 1.3) * 0.2;

                float3 pos = IN.positionOS.xyz;
                pos.y += wave * _WaveHeight;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv          = IN.uv;
                return OUT;
            }

            // ── Fragment: alpha via noise ─────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float t  = _Time.y;
                float2 uv = IN.uv;

                // Scroll do noise — nuvem se move
                float2 noiseUV = uv * _NoiseScale + _ScrollSpeed.xy * t;

                // Duas camadas de FBM defasadas dão mais volume
                float n1 = fbm(noiseUV);
                float n2 = fbm(noiseUV * 1.4 + float2(5.2, 1.3));
                float n  = n1 * 0.7 + n2 * 0.3;

                // Alpha: smoothstep com threshold e softness controláveis
                float alpha = smoothstep(
                    _AlphaThreshold,
                    _AlphaThreshold + _EdgeSoftness,
                    n
                );

                half4 col = _CloudColor;
                col.a     = alpha * _CloudColor.a;

                return col;
            }
            ENDHLSL
        }
    }

}
