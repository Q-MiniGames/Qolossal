from pathlib import Path
s=Path('Assets/Resources/FX/SurfaceTerrainContrast.shader').read_text()
s=s.replace('Qolossal/Upper Terrain Contrast','Qolossal/Causeway Walking Edge')
s=s.replace('float2 surfaceData : TEXCOORD4;', 'float2 surfaceData : TEXCOORD4;\n                float terrainY : TEXCOORD5;')
s=s.replace('o.surfaceData = input.surfaceData;', 'o.surfaceData = input.surfaceData;\n                o.terrainY = input.positionOS.y;')
s=s.replace('amount = min(amount, smoothstep(0.0, span.z, distance));', '''// The actual cave remains protected; the surface route high above it can darken.
                    float aboveCave = smoothstep(span.w, span.w + 1.0, input.terrainY);
                    float outsideMouth = smoothstep(0.0, span.z, distance);
                    amount = min(amount, max(outsideMouth, aboveCave));''')
Path('Assets/Resources/FX/CausewayWalkingEdge.shader').write_text(s)
