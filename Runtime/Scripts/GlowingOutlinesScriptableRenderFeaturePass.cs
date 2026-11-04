using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace GlowingOutlines
{
    public class GlowingOutlinesScriptableRenderFeaturePass : ScriptableRenderPass
    {
        private const float DepthClear = float.MaxValue;
        
        private static readonly int OutlineMask = Shader.PropertyToID("_OutlineMask");
        private static readonly int OutlineDepth = Shader.PropertyToID("_OutlineDepthSrc");
        private static readonly int MaxDepth = Shader.PropertyToID("_MaxDepth");
        private static readonly int BlurParams = Shader.PropertyToID("_BlurParams");
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        private static readonly int SceneDepthTolerance = Shader.PropertyToID("_SceneDepthTolerance");

        
        private Renderer target;
        private GlowingOutlinsRenderFeatureData glowingOutlinsRenderFeatureData;
        
        private TextureHandle maskColor;
        private TextureHandle maskDepth;
        private TextureHandle depth;
        
        private TextureHandle pingCol;
        private TextureHandle pongCol;
        private TextureHandle pingDepth;
        private TextureHandle pongDepth;
        
        private UniversalCameraData universalCameraData;
        private UniversalResourceData universalResourceData;
        
        private int textureWidth;
        private int textureHeight;
        
        private class MaskPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class BlurPassData
        {
            public TextureHandle fromCol;
            public Material blurMaterial;
            public Vector4 blurParams;
        }
        
        private class CompositePassData
        {
            public TextureHandle maskResult;
            public Material compositeMaterial;
        }

        public GlowingOutlinesScriptableRenderFeaturePass(GlowingOutlinsRenderFeatureData renderData)
        {
            glowingOutlinsRenderFeatureData = renderData;
            //  Let Unity know we would like access to the cameras depth texture (pretty please).
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (glowingOutlinsRenderFeatureData.MaskMaterial == null ||
                glowingOutlinsRenderFeatureData.BlurMaterial == null ||
                glowingOutlinsRenderFeatureData.CompositeMaterial == null)
            {
                return;
            }
            
            universalCameraData = frameData.Get<UniversalCameraData>();
            universalResourceData = frameData.Get<UniversalResourceData>();
            
            // If there's no depth texture for the camera this sort of falls apart...
            if (!universalResourceData.cameraDepthTexture.IsValid()) return;

            //  Create the all the required render textures for the masking and blur passes
            CreateTextures(renderGraph);
            
            //  Render objects tagged on the LayerMask into our Mask texture
            BlitObjectsToTexture(renderGraph, frameData);
            //  Take the results of the Mask textures and blur them to create outlines
            BlurObjects(renderGraph);
            //  Composite the blurred results back into the scene
            CompositeObjects(renderGraph);
        }

        private void CreateTextures(RenderGraph renderGraph)
        {
            int down = Mathf.Max(1, glowingOutlinsRenderFeatureData.BlurTextureDownsample);
            //  TODO: Consider using downscaling to reduce texture memory... 
            textureWidth  = Mathf.Max(1, universalCameraData.cameraTargetDescriptor.width / down);
            textureHeight = Mathf.Max(1, universalCameraData.cameraTargetDescriptor.height / down);
            
            //  Store the color output of the Mask blit 
            maskColor = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight,
                GraphicsFormat.R16G16B16A16_SFloat, "_OutlineMaskCol",     true,  Color.clear);
            //  Store the depth output of the Mask blit
            maskDepth = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight, GraphicsFormat.R32_SFloat, 
                "_OutlineMaskDepth",  true,  new Color(DepthClear, 0,0,0)); // write the far depth to the R channel
            //  Depth Texture for test/write
            depth = RenderGraphHelpers.CreateDepthTexture(renderGraph, textureWidth, textureHeight, "_OutlineMaskZ");
            
            //  Blur ping/pong handles to bounce Mask/Depth textures around
            pingCol = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight,
                GraphicsFormat.R16G16B16A16_SFloat, "_OutlineBlurACol", false, Color.clear);
            pongCol = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight, 
                GraphicsFormat.R16G16B16A16_SFloat, "_OutlineBlurBCol", false, Color.clear);
            pingDepth = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight,
                GraphicsFormat.R32_SFloat, "_OutlineBlurADep", false, Color.clear);
            pongDepth = RenderGraphHelpers.CreateColorTexture(renderGraph, textureWidth, textureHeight, 
                GraphicsFormat.R32_SFloat, "_OutlineBlurBDep", false, Color.clear);
        }

        private void BlitObjectsToTexture(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData universalLightData = frameData.Get<UniversalLightData>();
            
            List<ShaderTagId> shaderTags = new()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };
            
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags,
                universalRenderingData, 
                universalCameraData,
                universalLightData,
                SortingCriteria.CommonOpaque);
                
            drawSettings.overrideMaterial = glowingOutlinsRenderFeatureData.MaskMaterial;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, 
                glowingOutlinsRenderFeatureData.LayerMask);
            
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Glowing Outlines - Mask Pass",
                out MaskPassData passData);
            passData.rendererListHandle = renderGraph.CreateRendererList(new RendererListParams(
                universalRenderingData.cullResults, 
                drawSettings,
                filteringSettings));
                
            builder.UseRendererList(passData.rendererListHandle);
            builder.SetRenderAttachment(maskColor, 0);
            builder.SetRenderAttachment(maskDepth, 1);
            builder.SetRenderAttachmentDepth(depth);
            builder.AllowGlobalStateModification(true); 

            builder.SetGlobalTextureAfterPass(maskColor, OutlineMask);
            builder.SetGlobalTextureAfterPass(maskDepth, OutlineDepth);
            
            builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext rasterGraphContext) =>
                rasterGraphContext.cmd.DrawRendererList(data.rendererListHandle));
        }

        private void BlurObjects(RenderGraph renderGraph)
        {
            int iterations = Mathf.Max(1, glowingOutlinsRenderFeatureData.BlurIterations);
            
            TextureHandle srcCol = maskColor;
            TextureHandle srcDepth = maskDepth;
            
            for (int i = 0, counti = iterations; i < counti; i++)
            {
                //  Horizontal blur
                Blur(srcCol, srcDepth, pingCol, pingDepth, 
                    new Vector4(1, 0, textureWidth,  glowingOutlinsRenderFeatureData.BlurStep));
                //  Vertical blur
                Blur( pingCol, pingDepth, pongCol, pongDepth,
                    new Vector4(0, 1, textureHeight,  glowingOutlinsRenderFeatureData.BlurStep));
                
                srcCol = pongCol;
                srcDepth = pongDepth;
            }
            
            return;
            void Blur (TextureHandle fromCol, TextureHandle fromDepth, 
                TextureHandle toCol, TextureHandle toDepth, Vector4 blurParams)
            {
                using IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass("Glowing Outlines - Blur Pass", out BlurPassData passData);
                passData.fromCol = fromCol;
                passData.blurMaterial = glowingOutlinsRenderFeatureData.BlurMaterial;
                passData.blurParams = blurParams;

                builder.UseTexture(fromCol);
                builder.UseTexture(fromDepth);
                builder.SetRenderAttachment(toCol, 0);
                builder.SetRenderAttachment(toDepth, 1);
                builder.AllowGlobalStateModification(true); 

                builder.SetGlobalTextureAfterPass(toDepth, OutlineDepth);
                
                builder.SetRenderFunc(static (BlurPassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalFloat(MaxDepth, DepthClear);
                    ctx.cmd.SetGlobalVector(BlurParams, data.blurParams);
                    Blitter.BlitTexture(ctx.cmd, data.fromCol, new Vector4(1, 1, 0, 0), data.blurMaterial, 0);
                });
            }
        }

        private void CompositeObjects(RenderGraph renderGraph)
        {
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Glowing Outlines - Composite", out CompositePassData data);
            data.maskResult = pongCol;
            data.compositeMaterial = glowingOutlinsRenderFeatureData.CompositeMaterial;

            builder.UseTexture(pongCol);
            builder.UseTexture(pongDepth);
            builder.UseTexture(maskColor);
            builder.UseTexture(universalResourceData.cameraDepthTexture);
            builder.SetRenderAttachment(universalResourceData.activeColorTexture, 0);
            
            data.compositeMaterial.SetFloat(Intensity, glowingOutlinsRenderFeatureData.OutlineIntensity);
            data.compositeMaterial.SetFloat(SceneDepthTolerance, glowingOutlinsRenderFeatureData.SceneDepthTolerance);

            builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, data.maskResult, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0));
        }
    }
}