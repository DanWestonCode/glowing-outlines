using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace GlowingOutlines
{
    public static class RenderGraphHelpers
    {
        /// <summary>
        ///     Returns a TextureHandle which can be used for the given frame.
        ///     RenderGraph will release the resource after RecordRenderGraph.
        ///     Unity decided to make the version of UniversalRenderer.CreateRenderGraphTexture
        ///     which takes a clear color to be internal only hence why this exists
        /// </summary>
        public static TextureHandle CreateColorTexture(RenderGraph renderGraph, int width, int height,
            GraphicsFormat format, string name, bool clear, Color clearColor)
        {
            TextureDesc textureDesc = new TextureDesc(width, height)
            {
                name = name,
                colorFormat = format,
                depthBufferBits = DepthBits.None,
                msaaSamples = MSAASamples.None,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = clear,
                clearColor = clearColor,
                dimension = TextureDimension.Tex2D,
                slices = 1,
            };
            
            return renderGraph.CreateTexture(textureDesc);
        }
        
        
        /// <summary>
        ///     Returns a TextureHandle depth texture for the given frame.
        ///     RenderGraph will release the resource after RecordRenderGraph.
        /// </summary>
        public static TextureHandle CreateDepthTexture(RenderGraph renderGraph, int width, int height, string name)
        {
            TextureDesc textureDesc = new TextureDesc(width, height)
            {
                name = name,
                colorFormat = GraphicsFormat.None,
                depthBufferBits = DepthBits.Depth32,
                msaaSamples = MSAASamples.None,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = true,
                dimension = TextureDimension.Tex2D,
                slices = 1
            };
            return renderGraph.CreateTexture(textureDesc);
        }
    }
}