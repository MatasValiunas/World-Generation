Shader "Custom/LitVertexColorURP"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Occlusion ("Occlusion", Range(0,1)) = 1.0
        _Specular ("Specular", Range(0,1)) = 0.0
        _ClearCoatMask ("Clear Coat Mask", Range(0,1)) = 0.0
        _ClearCoatSmoothness ("Clear Coat Smoothness", Range(0,1)) = 0.0
        _EmissionStrength ("Emission Strength", Range(0,10)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Smoothness;
                float _Metallic;
                float _Occlusion;
                float _Specular;
                float _ClearCoatMask;
                float _ClearCoatSmoothness;
                float _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                SurfaceData surfaceData;
                surfaceData.albedo = IN.color.rgb;
                surfaceData.metallic = 0;
                surfaceData.smoothness = 0;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = IN.color.rgb * 0;
                surfaceData.occlusion = 1;
                surfaceData.specular = 0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;
                surfaceData.alpha = 0;

                InputData inputData;
                inputData.positionWS = 0; // Not used
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = normalize(IN.viewDirWS);
                inputData.shadowCoord = 0;
                inputData.fogCoord = 0;
                inputData.vertexLighting = 0;
                inputData.bakedGI = 0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
