// Derived from Unity URP Sprite-Lit-Default; see the installed package LICENSE.md.
Shader "Qolossal/Causeway Painted Terrain Trial"
{
    Properties
    {
        _SurfaceGrade("Body, edge, depth, protected count", Vector) = (1,1,.9,0)
        _RepaintTex("New painted bedrock", 2D) = "white" {}
        _RepaintTile("Native world tile and rim blend", Vector) = (4,4,.38,0)
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                float2 surfaceData : TEXCOORD2;
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                float2 surfaceData : TEXCOORD4;
                float2 paintUV : TEXCOORD5;
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SurfaceGrade;
                float4 _RepaintTile;
                float4 _ProtectedSpans[16];
            CBUFFER_END
            TEXTURE2D(_RepaintTex); SAMPLER(sampler_RepaintTex);
            half3 PaintedRock(float2 uv)
            {
                // Four native samples overlap the source borders; no mirrored V seams.
                float2 p = frac(uv) * .8;
                float2 w = smoothstep(0.0, .2, p);
                float2 dx = ddx(uv) * .8, dy = ddy(uv) * .8;
                half3 a = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p, dx, dy).rgb;
                half3 b = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + float2(.8,0), dx, dy).rgb;
                half3 c = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + float2(0,.8), dx, dy).rgb;
                half3 d = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + .8, dx, dy).rgb;
                return lerp(lerp(d,c,w.x),lerp(b,a,w.x),w.y);
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color;

                o.surfaceData = input.surfaceData;
                o.paintUV = TransformObjectToWorld(input.positionOS).xy / _RepaintTile.xy;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                float amount = 1.0;
                [loop] for (int k = 0; k < (int)_SurfaceGrade.w; k++)
                {
                    float4 span = _ProtectedSpans[k];
                    float distance = max(span.x - input.surfaceData.y, input.surfaceData.y - span.y);
                    amount = min(amount, smoothstep(0.0, span.z, distance));
                }
                // Preserve alpha, the native walking lip, and every protected cave span.
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 paint = input.color.rgb * PaintedRock(input.paintUV);
                float blend = amount * smoothstep(0.06, _RepaintTile.z, input.surfaceData.x);
                main.rgb = lerp(main.rgb, paint, blend);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float4 _SurfaceGrade;
                float4 _RepaintTile;
                float4 _ProtectedSpans[16];
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                float2 surfaceData : TEXCOORD2;
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                float2 surfaceData : TEXCOORD4;
                float2 paintUV : TEXCOORD5;
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SurfaceGrade;
                float4 _RepaintTile;
                float4 _ProtectedSpans[16];
            CBUFFER_END
            TEXTURE2D(_RepaintTex); SAMPLER(sampler_RepaintTex);
            half3 PaintedRock(float2 uv)
            {
                // Four native samples overlap the source borders; no mirrored V seams.
                float2 p = frac(uv) * .8;
                float2 w = smoothstep(0.0, .2, p);
                float2 dx = ddx(uv) * .8, dy = ddy(uv) * .8;
                half3 a = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p, dx, dy).rgb;
                half3 b = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + float2(.8,0), dx, dy).rgb;
                half3 c = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + float2(0,.8), dx, dy).rgb;
                half3 d = SAMPLE_TEXTURE2D_GRAD(_RepaintTex, sampler_RepaintTex, p + .8, dx, dy).rgb;
                return lerp(lerp(d,c,w.x),lerp(b,a,w.x),w.y);
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color;
                o.surfaceData = input.surfaceData;
                o.paintUV = TransformObjectToWorld(input.positionOS).xy / _RepaintTile.xy;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                float amount = 1.0;
                [loop] for (int k = 0; k < (int)_SurfaceGrade.w; k++)
                {
                    float4 span = _ProtectedSpans[k];
                    float distance = max(span.x - input.surfaceData.y, input.surfaceData.y - span.y);
                    amount = min(amount, smoothstep(0.0, span.z, distance));
                }
                // Preserve alpha, the native walking lip, and every protected cave span.
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 paint = input.color.rgb * PaintedRock(input.paintUV);
                float blend = amount * smoothstep(0.06, _RepaintTile.z, input.surfaceData.x);
                main.rgb = lerp(main.rgb, paint, blend);
                return main;
            }
            ENDHLSL
        }
    }
}
