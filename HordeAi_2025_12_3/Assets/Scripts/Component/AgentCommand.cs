using Unity.Entities;
using Unity.Mathematics;

// Data: What the Brain tells the Body to do
public struct AgentCommand : IComponentData
{
    public float3 MoveVector; // X, Y, Z direction
    public float MoveSpeed;
}
