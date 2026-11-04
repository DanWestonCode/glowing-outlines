Shader "GlowingOutlines/Blur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_OutlineDepthSrc); // depth texture matching _BlitTexture
            
            float _MaxDepth;

            // xy = direction, z = target size along direction, w = radius (texels)
            //  Encoded blur parameters:
            //  xy = Blur Direction
            //  z = blur size
            //  w = blur radius
            float4 _BlurParams;            

            struct FragOut
            {
                float4 color : SV_Target0;
                float depth : SV_Target1;
            };

            FragOut frag(Varyings i)
            {
                float2 tc = i.texcoord;
                    
                //  Determine the amount to blur, i.e. how far off center to sample from 
                //  1.0 -> blur by one pixel
                //  2.0 -> blur by two pixels, etc.
                float blur = _BlurParams.w / _BlurParams.z;
                
                //  Determine the blur direction with the given offset
                float2 stepUV = _BlurParams.xy * blur;

                //  Pre-calculated distribution 
                //  https://gist.github.com/baba-s/880fdf600a65655f8a2252c275e9a747
                const float weights[9] =
                {
                    0.0162162162, 0.0540540541, 0.1216216216, 0.1945945946, 0.2270270270,
                    0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162
                };

                float4 finalCol = 0;
                float finalDepth = _MaxDepth;
                
                //  9 tap blur:
                const int tap = 4;
                [unroll]
                for (int i = -tap; i <= tap; i++)
                {
                    float2 uv = tc + stepUV * i;
                    finalCol += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * weights[i + tap];
                    finalDepth = min(finalDepth, SAMPLE_TEXTURE2D(_OutlineDepthSrc, sampler_PointClamp, uv).r);
                }

                FragOut o;
                o.color = finalCol;
                o.depth = finalDepth;
                return o;
            }
            ENDHLSL
        }
    }
}