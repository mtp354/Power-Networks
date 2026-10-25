Shader "FantasyEarth/Painterly"
{
    Properties
    {
        [IntRange] _Radius ("Brush radius", Range(1, 6)) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Painterly"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
            CBUFFER_END
            float4 _FE_MistColor;
            float _FE_MistStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                int radius = clamp((int)_Radius, 1, 6);
                float n = (radius + 1) * (radius + 1);
                float3 painted = 0;
                float bestVariance = 1e20;
                [unroll] for (int sector = 0; sector < 4; sector++)
                {
                    float2 direction = float2((sector & 1) ? 1 : -1, (sector & 2) ? 1 : -1);
                    float3 sum = 0;
                    float3 squared = 0;
                    [loop] for (int y = 0; y <= radius; y++)
                    {
                        [loop] for (int x = 0; x <= radius; x++)
                        {
                            float2 offset = float2(x, y) * direction * _BlitTexture_TexelSize.xy;
                            float3 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset, 0).rgb;
                            sum += c;
                            squared += c * c;
                        }
                    }
                    float3 mean = sum / n;
                    float variance = dot(abs(squared / n - mean * mean), float3(1, 1, 1));
                    if (variance < bestVariance)
                    {
                        bestVariance = variance;
                        painted = mean;
                    }
                }
                float band = exp(-pow((uv.y - 0.45) / 0.23, 2));
                painted = lerp(painted, _FE_MistColor.rgb, saturate(_FE_MistStrength) * band);
                float grain = frac(sin(dot(floor(uv * _BlitTexture_TexelSize.zw * 0.5), float2(12.9898, 78.233))) * 43758.5453);
                painted *= 0.99 + grain * 0.02;
                return half4(painted, 1);
            }
            ENDHLSL
        }
    }
}
