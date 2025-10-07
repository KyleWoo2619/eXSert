
float3 Unity_NormalFromHeight_Tangent(float In, float Strength, float3 Position, float3x3 TangentMatrix)
{
    float3 worldDerivativeX = ddx(Position);
    float3 worldDerivativeY = ddy(Position);

    float3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);
    float3 crossY = cross(worldDerivativeY, TangentMatrix[2].xyz);
    
    float d = dot(worldDerivativeX, crossY);
    
    float sgn = d < 0.0 ? (-1.0f) : 1.0f;
    float surface = sgn / max(0.000000000000001192093f, abs(d));

    float dHdx = ddx(In);
    float dHdy = ddy(In);
    
    float3 surfGrad = surface * (dHdx * crossY + dHdy * crossX);
    
    float3 normals = normalize(TangentMatrix[2].xyz - (Strength * surfGrad));
    normals = TransformWorldToTangent(normals, TangentMatrix);
    
    return normals;
}