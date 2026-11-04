using System;
using UnityEngine;

namespace GlowingOutlines
{
    [Serializable]
    public class GlowingOutlinsRenderFeatureData
    {
        [field: SerializeField] public LayerMask LayerMask { get; set; }
        [field: SerializeField] public Material MaskMaterial { get; private set; }
        [field: SerializeField] public Material BlurMaterial { get; private set; }
        [field: SerializeField] public Material CompositeMaterial { get; private set; }

        [field: SerializeField] public int BlurTextureDownsample { get; private set; } = 1;
        [field: SerializeField] public int BlurIterations { get; private set; } = 1;

        //  The spacing between each blur step in the Gaussian blur, cranking this up will degrade "smoothness"
        [field: SerializeField] public float BlurStep { get; private set; } = 1.5f;
        //  "Intensity" changes the "glowy-ness" of the outline
        [field: SerializeField, Range(0,10)] public float OutlineIntensity { get; private set; } = 1.0f;
        //  Tolerance applied to scene depth check during the final composite step 
        [field: SerializeField, Range(0,1)] public float SceneDepthTolerance { get; private set; } = .3f;

    }
}