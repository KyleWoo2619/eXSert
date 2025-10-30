#ifndef MY_LIGHTING_INCLUDED
#define MY_LIGHTING_INCLUDED

#include "Assets/Shaders/Lighting.hlsl" 
void MyLighting_float(float3 WorldPos, float3 WorldNormal, out float3 OutColor)
{
    float3 color = 0;

    // Additional lights (spot / point)
#ifdef VERTEXLIGHT_ON
    for (int i = 0; i < 4; i++)
    {
        float3 addLightDir = normalize(unity_4LightPosX0[i].xyz - WorldPos);
        float3 addLightColor = unity_LightColor[i].rgb;
        float ndotl = saturate(dot(WorldNormal, addLightDir));
        color += addLightColor * ndotl;
    }
#endif

    OutColor = color;
}

#endif
