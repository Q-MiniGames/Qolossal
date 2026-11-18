from pathlib import Path

# Retain the installed URP 17.x sprite lighting, normals, alpha and forward fallback.
source = Path('Library/PackageCache/com.unity.render-pipelines.universal@8a9b4021522a/Shaders/2D/Sprite-Lit-Default.shader')
shader = source.read_text().replace('Universal Render Pipeline/2D/Sprite-Lit-Default', 'Qolossal/Upper Terrain Contrast')
shader = '// Derived from Unity URP Sprite-Lit-Default; see the installed package LICENSE.md.\n' + shader
shader = shader.replace('        _MainTex(', '        _SurfaceGrade("Body, edge, depth, protected count", Vector) = (1,1,.9,0)\n        _MainTex(', 1)
shader = shader.replace('                half4 _Color;', '                half4 _Color;\n                float4 _SurfaceGrade;\n                float4 _ProtectedSpans[16];')
# This material is exclusively for MeshRenderer terrain. SpriteRenderer-only flip/color
# globals are not initialized for an arbitrary custom mesh shader.
shader = shader.replace('                SetUpSpriteInstanceProperties();\n', '')
shader = shader.replace('                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);\n', '')
shader = shader.replace(' * unity_SpriteColor', '')
# All passes share one material layout. Normals keep their original path unchanged.
for entry, common in [('Lit', 'Lit'), ('Unlit', 'Unlit')]:
    start = shader.index('            #pragma vertex ' + entry + 'Vertex')
    end = shader.index('            ENDHLSL', start)
    part = shader[start:end]
    part = part.replace('                COMMON_2D_INPUTS', '                COMMON_2D_INPUTS\n                float2 surfaceData : TEXCOORD2;')
    part = part.replace('                COMMON_2D_LIT_OUTPUTS', '                COMMON_2D_LIT_OUTPUTS\n                float2 surfaceData : TEXCOORD4;') if entry == 'Lit' else part.replace('                COMMON_2D_OUTPUTS', '                COMMON_2D_OUTPUTS\n                float2 surfaceData : TEXCOORD4;')
    part = part.replace('                return o;', '                o.surfaceData = input.surfaceData;\n                return o;')
    part = part.replace('                return Common' + common + 'Fragment(input, input.color);', '''                float amount = 1.0;
                [loop] for (int k = 0; k < (int)_SurfaceGrade.w; k++)
                {
                    float4 span = _ProtectedSpans[k];
                    float distance = max(span.x - input.surfaceData.y, input.surfaceData.y - span.y);
                    amount = min(amount, smoothstep(0.0, span.z, distance));
                }
                float grade = lerp(_SurfaceGrade.y, _SurfaceGrade.x,
                    smoothstep(0.04, _SurfaceGrade.z, input.surfaceData.x));
                half4 color = input.color;
                color.rgb *= lerp(1.0, grade, amount);
                return Common''' + common + '''Fragment(input, color);''')
    shader = shader[:start] + part + shader[end:]
Path('Assets/Resources/FX/SurfaceTerrainContrast.shader').write_text(shader)
