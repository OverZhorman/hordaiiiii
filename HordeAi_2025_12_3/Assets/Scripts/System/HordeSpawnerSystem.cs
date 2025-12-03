using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;

[BurstCompile]
public partial struct HordeSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out GlobalHordeConfig config)) return;

        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorld.CollisionWorld;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (envTag, entity) in SystemAPI.Query<RefRW<EnvironmentTag>>().WithEntityAccess())
        {
            if (envTag.ValueRO.IsInitialized) continue;
            envTag.ValueRW.IsInitialized = true;

            int envID = envTag.ValueRO.EnvID;
            float3 center = envTag.ValueRO.CenterPosition;
            var rand = new Unity.Mathematics.Random((uint)(1 + envID + SystemAPI.Time.ElapsedTime));

            Entity target = ecb.Instantiate(config.TargetPrefab);
            float targetAngle = rand.NextFloat(0, math.PI2);
            float targetDist = config.SpawnRadius + 10f;
            float3 targetXZ = center + new float3(math.cos(targetAngle) * targetDist, 0, math.sin(targetAngle) * targetDist);
            float targetY = GetGroundHeight(collisionWorld, targetXZ, 0.5f);

            ecb.SetComponent(target, LocalTransform.FromPosition(new float3(targetXZ.x, targetY, targetXZ.z)));
            ecb.AddComponent(target, new TargetTag { EnvID = envID });

            float angleStep = math.PI2 / config.MembersPerSquad;

            for (int i = 0; i < config.MembersPerSquad; i++)
            {
                Entity member = ecb.Instantiate(config.MemberPrefab);

                float currentAngle = i * angleStep;
                float3 offset = new float3(math.cos(currentAngle), 0, math.sin(currentAngle)) * 2.0f;
                float3 memberXZ = center + offset;
                float memberY = GetGroundHeight(collisionWorld, memberXZ, 0.5f);

                ecb.SetComponent(member, LocalTransform.FromPosition(new float3(memberXZ.x, memberY, memberXZ.z)));
                ecb.SetComponent(member, new SquadMember { SquadID = 0, EnvID = envID });

                ecb.SetComponent(member, new SensorConfig
                {
                    RayCount = config.RayCount,
                    AngleDegrees = config.AngleDegrees,


                    VerticalRayCount = config.VerticalRayCount,
                    VerticalAngleDegrees = config.VerticalAngleDegrees,

                    RayLength = config.RayLength,
                    CollisionMask = config.VisionMask
                });

                ecb.SetComponent(member, new AgentCommand { MoveSpeed = config.MoveSpeed, MoveVector = float3.zero });
            }
        }
    }

    private float GetGroundHeight(CollisionWorld collisionWorld, float3 xzPos, float verticalOffset)
    {
        float3 start = new float3(xzPos.x, 50f, xzPos.z);
        float3 end = new float3(xzPos.x, -50f, xzPos.z);
        RaycastInput input = new RaycastInput
        {
            Start = start,
            End = end,
            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = ~0u, GroupIndex = 0 }
        };

        if (collisionWorld.CastRay(input, out RaycastHit hit))
        {
            return hit.Position.y + verticalOffset;
        }
        return verticalOffset;
    }
}