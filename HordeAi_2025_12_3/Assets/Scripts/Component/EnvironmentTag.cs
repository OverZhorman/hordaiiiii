using Unity.Entities;
using Unity.Mathematics;

// A tag attached to a pure entity representing a specific Environment
public struct EnvironmentTag : IComponentData
{
    public int EnvID;
    public float3 CenterPosition;
    public bool IsInitialized;
}
