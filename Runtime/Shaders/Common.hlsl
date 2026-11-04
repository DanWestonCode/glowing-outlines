#ifndef GLOWING_OUTLINES_COMMON
#define GLOWING_OUTLINES_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float EyeDepth(float rawDepth)
{
    //  Handle orthographic depth https://catlikecoding.com/unity/tutorials/custom-srp/particles/
    if (unity_OrthoParams.w == 1.0)
    {
        #if UNITY_REVERSED_Z
        rawDepth = 1.0 - rawDepth;
        #endif
        return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;

    }
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

#endif