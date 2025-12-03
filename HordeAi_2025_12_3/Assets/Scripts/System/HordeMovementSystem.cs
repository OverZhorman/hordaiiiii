using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial struct HordeMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        new HordeMovementJob
        {
            DeltaTime = dt
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct HordeMovementJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(ref PhysicsVelocity velocity, in LocalTransform transform, in AgentCommand command)
    {
        float3 targetVelocity = command.MoveVector * command.MoveSpeed;

        velocity.Linear.x = targetVelocity.x;
        velocity.Linear.z = targetVelocity.z;

        float rollSpeed = 2.0f;
        velocity.Angular.x = targetVelocity.z * rollSpeed;
        velocity.Angular.z = -targetVelocity.x * rollSpeed;
    }
}