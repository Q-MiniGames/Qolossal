Shader "Qolossal/Leaf Chroma Key"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PropFragment
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            fixed4 PropFragment(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord);
                // Botanical art has no magenta; remove the flat key before applying tint.
                float key = min(color.r, color.b)-color.g;
                color.a *= 1.0-smoothstep(0.12,0.35,key);
                color *= input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
