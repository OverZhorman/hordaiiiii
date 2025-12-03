using Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct HordeSensorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var builder = DrawingManager.GetBuilder(true);

        SensorJob job = new SensorJob
        {
            builder = builder,
            CollisionWorld = physicsWorld.CollisionWorld
        };

        JobHandle handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;

        builder.DisposeAfter(handle);
    }
}

[BurstCompile]
public partial struct SensorJob : IJobEntity
{
    public CommandBuilder builder;
    [ReadOnly] public CollisionWorld CollisionWorld;

    public void Execute(ref DynamicBuffer<RaycastResult> buffer, in LocalTransform transform, in SensorConfig config)
    {
        int hCount = math.max(1, config.RayCount);
        int vCount = math.max(1, config.VerticalRayCount);
        int totalRays = hCount * vCount;

        if (buffer.Length != totalRays)
        {
            buffer.ResizeUninitialized(totalRays);
        }


        float3 baseForward = new float3(0, 0, 1);
        float3 center = transform.Position;
        float maxDist = config.RayLength;
        float startOffsetRadius = 0.6f; // Don't hit self


        float hTotalAngle = config.AngleDegrees;
        float hStartAngle, hStep;
        if (math.abs(hTotalAngle) >= 360f)
        {
            hStartAngle = 0;
            hStep = hTotalAngle / hCount;
        }
        else
        {
            hStartAngle = -hTotalAngle / 2f;
            hStep = hCount > 1 ? hTotalAngle / (hCount - 1) : 0;
        }


        float vTotalAngle = config.VerticalAngleDegrees;
        float vStartAngle, vStep;


        vStartAngle = -vTotalAngle / 2f;
        vStep = vCount > 1 ? vTotalAngle / (vCount - 1) : 0;

        int rayIndex = 0;

        // Iterate Vertical Rows first, then Horizontal Columns
        for (int v = 0; v < vCount; v++)
        {
            float currentPitch = vStartAngle + (vStep * v);

            quaternion pitchRot = quaternion.RotateX(math.radians(-currentPitch));
            pitchRot = quaternion.RotateX(math.radians(-currentPitch));

            for (int h = 0; h < hCount; h++)
            {
                float currentYaw = hStartAngle + (hStep * h);

                quaternion yawRot = quaternion.RotateY(math.radians(currentYaw));


                quaternion finalRot = math.mul(yawRot, pitchRot);
                float3 rayDir = math.rotate(finalRot, baseForward);


                float3 rayStart = center + (rayDir * startOffsetRadius);
                float3 rayEnd = rayStart + (rayDir * maxDist);

                Unity.Physics.RaycastInput input = new Unity.Physics.RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = config.CollisionMask,
                        GroupIndex = 0
                    }
                };

                float resultValue = 1.0f;

                if (CollisionWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
                {
                    resultValue = hit.Fraction;
                    builder.Line(rayStart, hit.Position, UnityEngine.Color.green); // Hit
                }
                else
                {
                    builder.Line(rayStart, rayEnd, UnityEngine.Color.red); // Miss
                }

                buffer[rayIndex] = new RaycastResult { DistanceNormalized = resultValue };
                rayIndex++;
            }
        }
    }
}