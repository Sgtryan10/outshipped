Shader "Custom/TreeFoliageOverlay"
{
    Properties
    {
        _MainTex            ("Tree Atlas (Base Color)",     2D) = "white" {}
        _LeafTex            ("Leaf Detail Texture",         2D) = "white" {}
        _LeafNormal         ("Leaf Normal Map",             2D) = "bump"  {}
        _LeafTiling         ("Leaf Tiling",                 Float) = 4.0

        [Header(Green Detection)]
        _GreenHueMin        ("Green Hue Min",               Range(0,1)) = 0.22
        _GreenHueMax        ("Green Hue Max",               Range(0,1)) = 0.45
        _GreenSatMin        ("Min Saturation",              Range(0,1)) = 0.2
        _GreenValMin        ("Min Value/Brightness",        Range(0,1)) = 0.1
        _MaskSoftness       ("Mask Edge Softness",          Range(0.001,0.15)) = 0.04

        [Header(Leaf Blend)]
        _LeafBlendStrength  ("Leaf Blend Strength",         Range(0,1)) = 0.55
        _LeafColorTint      ("Leaf Color Tint",             Color) = (1,1,1,1)
        _LeafBrightness     ("Leaf Brightness",             Range(0.5,2)) = 1.0

        [Header(Lighting)]
        _NormalStrength     ("Normal Strength",             Range(0,2)) = 1.0
        _Smoothness         ("Smoothness",                  Range(0,1)) = 0.15
        _Cutoff             ("Alpha Cutoff",                Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="AlphaTest"
        }
        LOD 300
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_LeafTex);    SAMPLER(sampler_LeafTex);
            TEXTURE2D(_LeafNormal); SAMPLER(sampler_LeafNormal);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _LeafColorTint;
                float  _LeafTiling;
                float  _GreenHueMin;
                float  _GreenHueMax;
                float  _GreenSatMin;
                float  _GreenValMin;
                float  _MaskSoftness;
                float  _LeafBlendStrength;
                float  _LeafBrightness;
                float  _NormalStrength;
                float  _Smoothness;
                float  _Cutoff;
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
            // RGB → HSV
            // ------------------------------------------------------------------
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r),  float4(c.r, p.yzx), step(p.x, c.r));
                float  d = q.x - min(q.w, q.y);
                float  e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0*d + e)), d / (q.x + e), q.x);
            }

            // ------------------------------------------------------------------
            // Builds a smooth 0-1 mask for "is this pixel green?"
            // ------------------------------------------------------------------
            float GreenMask(float3 rgb)
            {
                float3 hsv = RGBtoHSV(rgb);

                // Hue band check (soft edges)
                float hueInRange = smoothstep(_GreenHueMin - _MaskSoftness, _GreenHueMin + _MaskSoftness, hsv.x)
                                 * (1.0 - smoothstep(_GreenHueMax - _MaskSoftness, _GreenHueMax + _MaskSoftness, hsv.x));

                // Saturation and value gates to exclude near-grey / near-black
                float satOk = smoothstep(_GreenSatMin - _MaskSoftness, _GreenSatMin + _MaskSoftness, hsv.y);
                float valOk = smoothstep(_GreenValMin - _MaskSoftness, _GreenValMin + _MaskSoftness, hsv.z);

                return hueInRange * satOk * valOk;
            }

            // ------------------------------------------------------------------
            // Simple stochastic hash offset to break up leaf tiling
            // ------------------------------------------------------------------
            float2 HashOffset(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float4 SampleStochastic(TEXTURE2D_PARAM(tex, samp), float2 uv)
            {
                float2 uvSkew = uv + float2(uv.y * 0.5, 0);
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

                float2 uv1 = uv + HashOffset(v1);
                float2 uv2 = uv + HashOffset(v2);
                float2 uv3 = uv + HashOffset(v3);

                return w.x * SAMPLE_TEXTURE2D(tex, samp, uv1)
                     + w.y * SAMPLE_TEXTURE2D(tex, samp, uv2)
                     + w.z * SAMPLE_TEXTURE2D(tex, samp, uv3);
            }

            // ------------------------------------------------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS  = pos.positionCS;
                OUT.positionWS   = pos.positionWS;
                OUT.uv           = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS     = nrm.normalWS;
                OUT.tangentWS    = nrm.tangentWS;
                OUT.bitangentWS  = nrm.bitangentWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Atlas base sample ---
                float4 baseCol  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Alpha cutout (for foliage cards)
                clip(baseCol.a - _Cutoff);

                // --- Green mask ---
                float greenMask = GreenMask(baseCol.rgb);

                // --- Leaf detail (stochastic, tiled separately) ---
                float2 leafUV   = IN.uv * _LeafTiling;
                float4 leafCol  = SampleStochastic(TEXTURE2D_ARGS(_LeafTex, sampler_LeafTex), leafUV);
                leafCol.rgb    *= _LeafColorTint.rgb * _LeafBrightness;

                // Overlay blend mode: preserves contrast better than lerp
                // overlay(a,b) = a<0.5 ? 2ab : 1-2(1-a)(1-b)
                float3 overlaid;
                overlaid.r = baseCol.r < 0.5
                    ? 2.0 * baseCol.r * leafCol.r
                    : 1.0 - 2.0*(1.0-baseCol.r)*(1.0-leafCol.r);
                overlaid.g = baseCol.g < 0.5
                    ? 2.0 * baseCol.g * leafCol.g
                    : 1.0 - 2.0*(1.0-baseCol.g)*(1.0-leafCol.g);
                overlaid.b = baseCol.b < 0.5
                    ? 2.0 * baseCol.b * leafCol.b
                    : 1.0 - 2.0*(1.0-baseCol.b)*(1.0-leafCol.b);

                // Blend overlay into base, gated by green mask and strength
                float3 finalCol = lerp(baseCol.rgb, overlaid, greenMask * _LeafBlendStrength);

                // --- Leaf normal (only on green areas) ---
                float2 leafNormUV  = IN.uv * _LeafTiling;
                float3 leafNormTS  = UnpackNormal(SAMPLE_TEXTURE2D(_LeafNormal, sampler_LeafNormal, leafNormUV));
                leafNormTS.xy     *= _NormalStrength;

                // Blend toward flat normal on non-green areas
                float3 flatNorm    = float3(0, 0, 1);
                float3 blendedNorm = normalize(lerp(flatNorm, leafNormTS, greenMask));

                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );
                float3 normalWS = normalize(mul(blendedNorm, TBN));

                // --- URP PBR ---
                InputData lightData     = (InputData)0;
                lightData.positionWS    = IN.positionWS;
                lightData.normalWS      = normalWS;
                lightData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightData.shadowCoord   = TransformWorldToShadowCoord(IN.positionWS);

                SurfaceData surface     = (SurfaceData)0;
                surface.albedo          = finalCol;
                surface.smoothness      = _Smoothness * (1.0 - greenMask * 0.5);
                surface.occlusion       = 1.0;
                surface.alpha           = baseCol.a;

                return UniversalFragmentPBR(lightData, surface);
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _LeafTiling;
                float  _GreenHueMin; float _GreenHueMax;
                float  _GreenSatMin; float _GreenValMin;
                float  _MaskSoftness; float _LeafBlendStrength;
                float  _LeafBrightness; float _NormalStrength; float _Smoothness;
                float4 _LeafColorTint;
            CBUFFER_END

            struct A { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            V ShadowVert(A IN)
            {
                V OUT;
                float3 ws  = TransformObjectToWorld(IN.pos.xyz);
                float3 nws = TransformObjectToWorldNormal(IN.n);
                OUT.pos    = TransformWorldToHClip(ApplyShadowBias(ws, nws, _MainLightPosition.xyz));
                OUT.uv     = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 ShadowFrag(V IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(col.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
