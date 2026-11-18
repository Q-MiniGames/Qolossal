// Derived from Unity URP Sprite-Lit-Default; see the installed package LICENSE.md.
Shader "Qolossal/Causeway Walking Edge"
{
    Properties
    {
        _SurfaceGrade("Body, edge, depth, protected count", Vector) = (1,1,.9,0)
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
                float terrainY : TEXCOORD5;
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SurfaceGrade;
                float4 _ProtectedSpans[16];
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color;

                o.surfaceData = input.surfaceData;
                o.terrainY = input.positionOS.y;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                float amount = 1.0;
                [loop] for (int k = 0; k < (int)_SurfaceGrade.w; k++)
                {
                    float4 span = _ProtectedSpans[k];
                    float distance = max(span.x - input.surfaceData.y, input.surfaceData.y - span.y);
                    // The actual cave remains protected; the surface route high above it can darken.
                    float aboveCave = smoothstep(span.w, span.w + 1.0, input.terrainY);
                    float outsideMouth = smoothstep(0.0, span.z, distance);
                    amount = min(amount, max(outsideMouth, aboveCave));
                }
                float grade = lerp(_SurfaceGrade.y, _SurfaceGrade.x,
                    smoothstep(0.04, _SurfaceGrade.z, input.surfaceData.x));
                half4 color = input.color;
                color.rgb *= lerp(1.0, grade, amount);
                return CommonLitFragment(input, color);
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
                float terrainY : TEXCOORD5;
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
                float4 _ProtectedSpans[16];
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color;
                o.surfaceData = input.surfaceData;
                o.terrainY = input.positionOS.y;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                float amount = 1.0;
                [loop] for (int k = 0; k < (int)_SurfaceGrade.w; k++)
                {
                    float4 span = _ProtectedSpans[k];
                    float distance = max(span.x - input.surfaceData.y, input.surfaceData.y - span.y);
                    // The actual cave remains protected; the surface route high above it can darken.
                    float aboveCave = smoothstep(span.w, span.w + 1.0, input.terrainY);
                    float outsideMouth = smoothstep(0.0, span.z, distance);
                    amount = min(amount, max(outsideMouth, aboveCave));
                }
                float grade = lerp(_SurfaceGrade.y, _SurfaceGrade.x,
                    smoothstep(0.04, _SurfaceGrade.z, input.surfaceData.x));
                half4 color = input.color;
                color.rgb *= lerp(1.0, grade, amount);
                return CommonUnlitFragment(input, color);
            }
            ENDHLSL
        }
    }
}
