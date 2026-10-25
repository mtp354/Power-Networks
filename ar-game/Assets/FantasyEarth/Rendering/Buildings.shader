Shader "FantasyEarth/Buildings"
{
    Properties
    {
        _Visibility("Visibility", Range(0,1)) = 1
        _LiveFacade("Observed facade", Float) = 0
        _FacadeStyle("Facade style", Float) = 0
        _FacadeBaseY("Facade base", Float) = 0
        _FacadeTrim("Facade trim", Color) = (.26,.17,.12,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _FacadeTrim;
        float _Visibility, _LiveFacade, _FacadeStyle, _FacadeBaseY;
        CBUFFER_END
        void VisibilityClip(float2 pixel)
        {
            float2 p=fmod(floor(pixel),4);
            float2 low=fmod(p,2), high=floor(p/2);
            float rank=4*(2*low.x+3*low.y-4*low.x*low.y)+2*high.x+3*high.y-4*high.x*high.y;
            clip(_Visibility-(rank+.5)/16);
        }
        ENDHLSL
        Pass
        {
            Name "Storybook Buildings"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1;
                float3 positionOS:TEXCOORD2; float3 normalOS:TEXCOORD3; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=position.positionCS; output.positionWS=position.positionWS;
                output.normalWS=TransformObjectToWorldNormal(input.normalOS); output.color=input.color;
                output.positionOS=input.positionOS.xyz; output.normalOS=input.normalOS;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                VisibilityClip(input.positionCS.xy);
                half3 color=input.color.rgb;
                half emission=input.color.a*1.3h;
                if (_LiveFacade>.5 && abs(normalize(input.normalOS).y)<.65)
                {
                    float horizontal=abs(input.normalOS.x)>abs(input.normalOS.z) ? input.positionOS.z : input.positionOS.x;
                    float2 uv=float2(horizontal/2.6,(input.positionOS.y-_FacadeBaseY)/3);
                    float2 cell=frac(uv);
                    half frame=step(abs(cell.x-.5),.21)*step(.25,cell.y)*step(cell.y,.77);
                    half window=step(abs(cell.x-.5),.155)*step(.31,cell.y)*step(cell.y,.71);
                    half beams=0;
                    if (_FacadeStyle<.5)
                        beams=max(step(cell.x,.045),step(cell.y,.045));
                    else if (_FacadeStyle<1.5)
                    {
                        float2 brick=frac(float2(horizontal/1.4+floor(uv.y*6)*.5,uv.y*6));
                        color*=1-.12*max(step(brick.x,.035),step(brick.y,.055));
                    }
                    color=lerp(color,_FacadeTrim.rgb,max(frame,beams));
                    color=lerp(color,half3(1,.65,.18),window);
                    emission=window*.55;
                }
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse=saturate(dot(normalize(input.normalWS),sun.direction));
                half band=smoothstep(0.08h,0.65h,diffuse);
                half3 light=0.45h+sun.color*(0.55h*band*sun.shadowAttenuation);
                return half4(color*light+color*emission,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS=TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS=TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,_LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z=min(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z=max(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 ShadowFrag(float4 pixel:SV_POSITION):SV_Target { VisibilityClip(pixel.xy); return 0; }
            ENDHLSL
        }
    }
}
