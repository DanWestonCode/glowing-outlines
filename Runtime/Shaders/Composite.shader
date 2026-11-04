Shader "GlowingOutlines/Composite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off // prevent the gpu throwing anything away
        ZWrite Off  //  We're not writing any depth 
        ZTest Always // Skip ZTesting, process every fragment

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_OutlineMask);   
            TEXTURE2D(_OutlineDepthSrc); 

            float _Intensity;
            float _DepthTolerance;
            float _MaxDepth;
            
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;

                //  Sample the result of the blur step 
                float4 objectGlow = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                //  Sample the depth of the blurred objects
                float objectDepth = SAMPLE_TEXTURE2D(_OutlineDepthSrc, sampler_PointClamp, uv).r;
                //  Sample the original object blit to determine visibility
                half mask = SAMPLE_TEXTURE2D(_OutlineMask, sampler_LinearClamp, uv).a;
                
                float linearSceneDepth = EyeDepth(SampleSceneDepth(uv));

                //  Determine if the object is in front of the scene depth
                //  Throw some tolerance on the scene depth to handle when 
                //  numbers get real close....
                float visible = step(objectDepth, linearSceneDepth + _DepthTolerance);

                float objectCoverage = objectGlow.a;
                float3 rgb = objectGlow.rgb / objectCoverage;
                //  Scale the outline "glow" with intensity and cut out the space for the context
                //  object within the glow (1-mask). Account for occluded  parts of the mask with the scene depth
                half a = saturate(objectCoverage * _Intensity) * (1.0 - mask) * visible;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}