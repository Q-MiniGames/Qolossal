from pathlib import Path

src=Path('Assets/Resources/FX/SurfaceTerrainContrast.shader').read_text()
s=src.replace('Qolossal/Upper Terrain Contrast','Qolossal/Causeway Painted Terrain Trial')
s=s.replace('        _MainTex(', '        _RepaintTex("New painted bedrock", 2D) = "white" {}\n        _RepaintTile("Native world tile and rim blend", Vector) = (4,4,.38,0)\n        _MainTex(',1)
s=s.replace('                float4 _SurfaceGrade;', '                float4 _SurfaceGrade;\n                float4 _RepaintTile;')
for entry in ['Lit','Unlit']:
    start=s.index('            #pragma vertex '+entry+'Vertex')
    end=s.index('            ENDHLSL',start)
    part=s[start:end]
    part=part.replace('                float2 surfaceData : TEXCOORD4;', '                float2 surfaceData : TEXCOORD4;\n                float2 paintUV : TEXCOORD5;')
    part=part.replace('            CBUFFER_END','''            CBUFFER_END
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
            }''')
    part=part.replace('                o.surfaceData = input.surfaceData;', '                o.surfaceData = input.surfaceData;\n                o.paintUV = TransformObjectToWorld(input.positionOS).xy / _RepaintTile.xy;')
    a=part.index('                float grade = lerp(')
    b=part.index('\n            }',a)
    replacement='''                // Preserve alpha, the native walking lip, and every protected cave span.
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 paint = input.color.rgb * PaintedRock(input.paintUV);
                float blend = amount * smoothstep(0.06, _RepaintTile.z, input.surfaceData.x);
                main.rgb = lerp(main.rgb, paint, blend);
'''
    if entry=='Lit':
        replacement+='''                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif
                return CombinedShapeLightShared(surfaceData, inputData);'''
    else:
        replacement+='''                return main;'''
    part=part[:a]+replacement+part[b:]
    s=s[:start]+part+s[end:]
Path('Assets/Resources/FX/CausewayTerrainTrial.shader').write_text(s)
