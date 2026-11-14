Shader "PixelPackers/EngineFlame"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 position:POSITION; float4 color:COLOR; };
            struct V { float4 position:SV_POSITION; float4 color:COLOR; };
            V vert(A a) { V o; o.position=TransformObjectToHClip(a.position.xyz);o.color=a.color;return o; }
            half4 frag(V i):SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
