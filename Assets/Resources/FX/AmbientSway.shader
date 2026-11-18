// A sprite that bends with the wind (AmbientLife). The root stays put; the far end moves along _SwayDir by the
// region's wind and parts around Qori. Only vertices move: the art's pixels and every collider are untouched.
Shader "Qolossal/AmbientSway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Root ("Root: uv y at the root, 1 / uv height with its sign", Vector) = (0, 1, 0, 0)
        _Amp ("Bend at the tip (u)", Float) = 0.1
        _Rate ("Rate", Float) = 1
        _Ripple ("Waves per u along the art", Float) = 0.6
        _Part ("Parts around Qori by (u)", Float) = 0.2
        _SwayDir ("World direction of the bend", Vector) = (1, 0, 0, 0)
        _Shiver ("Shiver (0..1)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SpriteFrag
            #pragma target 3.0
            #include "UnitySprites.cginc"
            float4 _Root, _SwayDir;
            float _Amp, _Rate, _Ripple, _Part, _Shiver;
            float4 _AmbientWind;    // x wind (about 0..1.5), y phase that never jumps when the wind changes
            float4 _AmbientActor;   // xy Qori, z how near he must be, w his height span
            float _AmbientTime;
            v2f vert(appdata_t v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(v.vertex.xyz, _Flip));
                float tip = saturate((v.texcoord.y - _Root.x) * _Root.y);
                float phase = _AmbientWind.y * _Rate + world.x * _Ripple + world.y * _Ripple * .5;
                float sway = (sin(phase) * .7 + sin(phase * 2.3 + 1.1) * .3) * (.45 + _AmbientWind.x) + _AmbientWind.x * .35;
                float shiver = sin(_AmbientTime * 34 + world.x * 9) * _Shiver * .05;
                float gap = world.x - _AmbientActor.x, nearby = saturate(1 - abs(gap) / _AmbientActor.z) * step(abs(world.y - _AmbientActor.y), _AmbientActor.w);
                float part = (gap < 0 ? -1 : 1) * nearby * _Part;
                world.xy += _SwayDir.xy * tip * (sway * _Amp + shiver + part);
                o.vertex = mul(UNITY_MATRIX_VP, world);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }
            ENDCG
        }
    }
}
