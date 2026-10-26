Shader "FantasyEarth/MapMarker"
{
    Properties { _BaseColor("Color", Color)=(.2,.8,.7,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; half shade:TEXCOORD0; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.shade=.65+.35*saturate(dot(TransformObjectToWorldNormal(a.normalOS),normalize(float3(.4,1,-.4)))); return o; }
            half4 Frag(V v):SV_Target { return half4(_BaseColor.rgb*v.shade,_BaseColor.a); }
            ENDHLSL
        }
    }
}
