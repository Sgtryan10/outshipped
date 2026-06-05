Shader "Custom/GrassStochastic"
{
    Properties
    {
        _BaseColor      ("Base Color Map",  2D) = "white"  {}
        _NormalMap      ("Normal Map",      2D) = "bump"   {}
        _RoughnessMap   ("Roughness Map",   2D) = "white"  {}

        _Tiling         ("Tiling (X/Y)",    Vector) = (60, 60, 0, 0)

        _MacroTex       ("Macro Variation (greyscale)", 2D) = "white" {}
        _MacroTiling    ("Macro Tiling",    Vector) = (1, 1, 0, 0)
        _MacroStrength  ("Macro Strength",  Range(0,1)) = 0.35

        _NormalStrength ("Normal Strength", Range(0,3)) = 1.0
        _Smoothness     ("Smoothness Bias", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseColor);     SAMPLER(sampler_BaseColor);
            TEXTURE2D(_NormalMap);     SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap);  SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_MacroTex);      SAMPLER(sampler_MacroTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor_ST;
                float4 _Tiling;
                float4 _MacroTiling;
                float  _MacroStrength;
                float  _NormalStrength;
                float  _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            // ------------------------------------------------------------------
            // Stochastic sampling helpers (hash + weighted 3-tap blend)
            // Based on Heitz & Neyret 2018 "High-Performance By-Example Noise"
            // ------------------------------------------------------------------
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // Returns a random rotation matrix offset for a given cell
            float2x2 RandomRot(float2 cell)
            {
                float angle = Hash2(cell).x * 6.28318530718;
                float s, c;
                sincos(angle, s, c);
                return float2x2(c, -s, s, c);
            }

            // Sample texture with stochastic tiling (3-tap triangular blend)
            float4 SampleStochastic(TEXTURE2D_PARAM(tex, samp), float2 uv)
            {
                // Skew UV into triangular grid
                const float2x2 M  = float2x2(1.0,  0.0, 0.5, 0.866025);
                const float2x2 Mi = float2x2(1.0,  0.0, -0.5774, 1.1547);

                float2 uvSkew  = mul(M, uv);
                float2 iuv     = floor(uvSkew);
                float2 fuv     = frac(uvSkew);

                // Barycentric weights for the three nearest triangle vertices
                float3 w;
                float2 v1, v2, v3;
                if (fuv.x + fuv.y < 1.0)
                {
                    v1 = iuv;
                    v2 = iuv + float2(1,0);
                    v3 = iuv + float2(0,1);
                    w  = float3(1.0 - fuv.x - fuv.y, fuv.x, fuv.y);
                }
                else
                {
                    v1 = iuv + float2(1,1);
                    v2 = iuv + float2(0,1);
                    v3 = iuv + float2(1,0);
                    w  = float3(fuv.x + fuv.y - 1.0, 1.0 - fuv.x, 1.0 - fuv.y);
                }

                // Per-vertex random offsets in original UV space
                float2 o1 = mul(Mi, Hash2(v1));
                float2 o2 = mul(Mi, Hash2(v2));
                float2 o3 = mul(Mi, Hash2(v3));

                // Apply a random rotation per cell to further break directionality
                float2 uv1 = mul(RandomRot(v1), uv) + o1;
                float2 uv2 = mul(RandomRot(v2), uv) + o2;
                float2 uv3 = mul(RandomRot(v3), uv) + o3;

                // Blend the three samples
                float4 s1 = SAMPLE_TEXTURE2D(tex, samp, uv1);
                float4 s2 = SAMPLE_TEXTURE2D(tex, samp, uv2);
                float4 s3 = SAMPLE_TEXTURE2D(tex, samp, uv3);

                return w.x * s1 + w.y * s2 + w.z * s3;
            }

            // Same but returns float3 (for normal maps — blend in linear space)
            float3 SampleStochasticNormal(TEXTURE2D_PARAM(tex, samp), float2 uv)
            {
                const float2x2 M  = float2x2(1.0,  0.0, 0.5,    0.866025);
                const float2x2 Mi = float2x2(1.0,  0.0, -0.5774, 1.1547);

                float2 uvSkew = mul(M, uv);
                float2 iuv    = floor(uvSkew);
                float2 fuv    = frac(uvSkew);

                float3 w;
                float2 v1, v2, v3;
                if (fuv.x + fuv.y < 1.0)
                {
                    v1 = iuv;
                    v2 = iuv + float2(1,0);
                    v3 = iuv + float2(0,1);
                    w  = float3(1.0 - fuv.x - fuv.y, fuv.x, fuv.y);
                }
                else
                {
                    v1 = iuv + float2(1,1);
                    v2 = iuv + float2(0,1);
                    v3 = iuv + float2(1,0);
                    w  = float3(fuv.x + fuv.y - 1.0, 1.0 - fuv.x, 1.0 - fuv.y);
                }

                float2 o1 = mul(Mi, Hash2(v1));
                float2 o2 = mul(Mi, Hash2(v2));
                float2 o3 = mul(Mi, Hash2(v3));

                float2 uv1 = mul(RandomRot(v1), uv) + o1;
                float2 uv2 = mul(RandomRot(v2), uv) + o2;
                float2 uv3 = mul(RandomRot(v3), uv) + o3;

                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uv2));
                float3 n3 = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uv3));

                return normalize(w.x * n1 + w.y * n2 + w.z * n3);
            }

            // ------------------------------------------------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS  = posInputs.positionCS;
                OUT.positionWS   = posInputs.positionWS;
                OUT.uv           = IN.uv;
                OUT.normalWS     = nrmInputs.normalWS;
                OUT.tangentWS    = nrmInputs.tangentWS;
                OUT.bitangentWS  = nrmInputs.bitangentWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 tiledUV = IN.uv * _Tiling.xy;

                // --- Base Color (stochastic) ---
                float4 baseCol = SampleStochastic(TEXTURE2D_ARGS(_BaseColor, sampler_BaseColor), tiledUV);

                // --- Macro variation overlay (low-frequency, single sample) ---
                float2 macroUV  = IN.uv * _MacroTiling.xy;
                float  macro    = SAMPLE_TEXTURE2D(_MacroTex, sampler_MacroTex, macroUV).r;
                // Remap macro to a soft brightness shift around 1
                float  macroMod = lerp(1.0, macro * 1.4 + 0.3, _MacroStrength);
                baseCol.rgb    *= macroMod;

                // --- Normal (stochastic) ---
                float3 tsNormal = SampleStochasticNormal(TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), tiledUV);
                tsNormal.xy    *= _NormalStrength;
                tsNormal        = normalize(tsNormal);

                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );
                float3 normalWS = normalize(mul(tsNormal, TBN));

                // --- Roughness (stochastic) ---
                float roughness = SampleStochastic(TEXTURE2D_ARGS(_RoughnessMap, sampler_RoughnessMap), tiledUV).r;
                float smoothness = (1.0 - roughness) * _Smoothness;

                // --- URP PBR lighting ---
                InputData lightData = (InputData)0;
                lightData.positionWS        = IN.positionWS;
                lightData.normalWS          = normalWS;
                lightData.viewDirectionWS   = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightData.shadowCoord       = TransformWorldToShadowCoord(IN.positionWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo      = baseCol.rgb;
                surface.normalTS    = tsNormal;
                surface.smoothness  = smoothness;
                surface.occlusion   = 1.0;
                surface.alpha       = 1.0;

                return UniversalFragmentPBR(lightData, surface);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 t : TANGENT; };
            struct V { float4 pos : SV_POSITION; };

            V ShadowVert(A IN)
            {
                V OUT;
                float3 ws  = TransformObjectToWorld(IN.pos.xyz);
                float3 nws = TransformObjectToWorldNormal(IN.n);
                OUT.pos    = TransformWorldToHClip(ApplyShadowBias(ws, nws, _MainLightPosition.xyz));
                return OUT;
            }

            half4 ShadowFrag(V IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
