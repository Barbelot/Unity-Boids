#define READ_GRID 1
#include "GridCommon.hlsl"

bool GetNeighborhoodInfluence(float2 position, float3 centerBox, float3 sizeBox, float searchRadius, out float2 cohesion, out float2 separation, out float2 alignment)
{
    float2 accumulatedAlignement = 0.0f;
    float2 accumulatedPosition = 0.0f;
    float2 accumulatedAvoidPosition = 0.0f;

    uint globalAvgCount = 0u;

    uint2 currentGridPosition = GetGridPosition(position, centerBox.xz, sizeBox.xz);
    for (int i = -searchRadius; i <= searchRadius; ++i)
    {
        for (int j = -searchRadius; j <= searchRadius; ++j)
        {
            int2 gridPosition = (int2)currentGridPosition + int2(i, j);
            uint instanceCount = GetCellCount(gridPosition);
            for (uint instance = 0; instance < instanceCount; ++instance)
            {
                CellData data = GetCellData(gridPosition, instance);

                if (position.x != data.pos.x && position.y != data.pos.y) //float comparison is legit here, it skips the current instance
                {
                    float2 positionVector = position - data.pos;
                    float sqrLength = dot(positionVector, positionVector);

                    accumulatedAlignement += data.vel;
                    accumulatedPosition += data.pos;

                    const float dampingScale = 40.0f;
                    float dampingAvoid = exp(-sqrLength * dampingScale);
                    accumulatedAvoidPosition += normalize(position - data.pos) * dampingAvoid;

                    globalAvgCount++;
                }
            }
        }
    }

    cohesion = separation = alignment = (float2)0.0f;

    accumulatedAlignement /= (float)globalAvgCount;
    accumulatedPosition /= (float)globalAvgCount;

    cohesion = accumulatedPosition - position;
    alignment = accumulatedAlignement;
    separation = accumulatedAvoidPosition;

    return globalAvgCount > 0;
}

void Flock_Simulate(inout VFXAttributes attributes, float3 centerBox, float3 sizeBox, float searchRadius, float cohesion, float alignment, float separation, float deltaTime)
{
    if (attributes.alive)
    {
        float2 cohesionVector, separationVector, alignmentVector;
        if (GetNeighborhoodInfluence(attributes.position.xz, centerBox, sizeBox, searchRadius, cohesionVector, separationVector, alignmentVector))
        {
            float2 velocity = attributes.velocity.xz;
            velocity = lerp(velocity, separationVector, saturate(deltaTime * separation));
            velocity = lerp(velocity, alignmentVector, saturate(deltaTime * alignment));
            velocity = lerp(velocity, cohesionVector, saturate(deltaTime * cohesion));

            attributes.velocity = float3(velocity.x, 0.0f, velocity.y);
        }
    }
}

