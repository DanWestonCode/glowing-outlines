Shader "GlowingOutlines/Mask"
{
    //  Need to declare properties here for cbuffer to work...
    Properties
    {
        [HDR] _OutlineColor ("Outline Colour", Color) = (1, 0, 0, 1)
    }
     
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Common.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            CBUFFER_END
            
            struct Attributes
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
            };
            
            struct FragOut
            {
                half4 color : SV_Target0; // write to the colour texture
                float depth : SV_Target1; // write to the depth texture
            };
            
            Varyings vert(Attributes input)
            {         
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.position = TransformObjectToHClip(input.position.xyz);
                return output;
            }
        
            FragOut frag(Varyings input)
            {
                float targetEye = EyeDepth(input.position.z);
                FragOut o;
                //  Render the object with the outline colour 
                o.color = half4(_OutlineColor.rgb, 1);
                //  Write the linear eye depth to the depth texture
                o.depth = targetEye;
                return o;
            }
            ENDHLSL
        }
    }
}
