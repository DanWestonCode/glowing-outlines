using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GlowingOutlines
{
    public class GlowingOutlinesScriptableRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private GlowingOutlinsRenderFeatureData renderData;
        private GlowingOutlinesScriptableRenderFeaturePass renderPass;

        public override void Create()
        {
            renderPass = new GlowingOutlinesScriptableRenderFeaturePass(renderData)
            {
                // We need to render at this point so the skybox doesn't nuke our lovely outline!
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing 
            };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(renderPass);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            renderPass = null;
        }
    }
}
