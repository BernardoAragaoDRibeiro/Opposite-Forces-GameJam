Shader "Custom/SwampWater"
{
    Properties
    {
        _TextureA     ("Texture A",        2D)          = "white" {}
        _TextureB     ("Texture B",        2D)          = "white" {}
        _NoiseScale   ("Noise Scale",      Float)       = 4.0
        _NoiseStrength("Noise Strength",   Range(0,1))  = 0.5
        _SmoothMin    ("Smooth Edge Min",  Range(0,1))  = 0.3
        _SmoothMax    ("Smooth Edge Max",  Range(0,1))  = 0.7
        _Alpha        ("Alpha",            Range(0,1))  = 0.85
        _SpeedA       ("Speed Texture A",  Vector)      = (0.1, 0.05, 0, 0)
        _SpeedB       ("Speed Texture B",  Vector)      = (-0.05, 0.08, 0, 0)
        _SpeedNoise   ("Speed Noise",      Vector)      = (0.03, 0.02, 0, 0)
        _Tiling       ("Tiling",           Vector)      = (1, 1, 0, 0)
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
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_TextureA); SAMPLER(sampler_TextureA);
            TEXTURE2D(_TextureB); SAMPLER(sampler_TextureB);

            CBUFFER_START(UnityPerMaterial)
                float4 _TextureA_ST;
                float4 _TextureB_ST;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _SmoothMin;
                float  _SmoothMax;
                float  _Alpha;
                float4 _SpeedA;
                float4 _SpeedB;
                float4 _SpeedNoise;
                float4 _Tiling;
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
                float  fogFactor   : TEXCOORD1;
            };

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

            // FBM: empilha oitavas de noise pra formas mais orgânicas
            float fbm(float2 uv)
            {
                float val    = 0.0;
                float amp    = 0.5;
                float freq   = 1.0;
                for (int i = 0; i < 4; i++)
                {
                    val  += valueNoise(uv * freq) * amp;
                    amp  *= 0.5;
                    freq *= 2.0;
                }
                return val;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv * _Tiling.xy;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // Noise com scroll próprio e escala controlável
                float2 noiseUV = uv * _NoiseScale + _SpeedNoise.xy * t;
                float  n       = fbm(noiseUV);

                // Distorce os UVs das texturas com o noise (opcional, dá mais organicidade)
                float2 distort = float2(n - 0.5, fbm(noiseUV + 1.7) - 0.5) * _NoiseStrength;

                float2 uvA = uv + _SpeedA.xy * t + distort;
                float2 uvB = uv + _SpeedB.xy * t + distort * 0.5;

                half4 colA = SAMPLE_TEXTURE2D(_TextureA, sampler_TextureA, uvA);
                half4 colB = SAMPLE_TEXTURE2D(_TextureB, sampler_TextureB, uvB);

                // Máscara com borda suave controlável
                float mask = smoothstep(_SmoothMin, _SmoothMax, n);

                half4 col = lerp(colA, colB, mask);
                col.a     = _Alpha;

                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
