### Setup:

- Add a `GlowingOutlinesScriptableRenderFeature` instance to your Universal Renderer.
- On the `GlowingOutlinesScriptableRenderFeature` instance: 
    - Set Layer for outlines to "Default".
    - Hook up the BlurMaterial, CompositeMaterial and MaskMaterial to their respective fields.
- Open the Demo scene in the sample folder.
- In the scene select JuliusCaesarBust, hookup your `UniversalRendererData` on the component. 
- Geometry should be outlined! 