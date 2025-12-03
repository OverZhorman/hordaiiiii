using Unity.Entities;
using Unity.Mathematics;

public struct GlobalHordeConfig : IComponentData
{
    // Squad Settings
    public Entity MemberPrefab;
    public Entity TargetPrefab;
    public int MembersPerSquad;
    public float SpawnRadius;

    // Movement
    public float MoveSpeed;

    // H Vision
    public int RayCount;
    public float AngleDegrees;

    // V Vision
    public int VerticalRayCount;
    public float VerticalAngleDegrees;

    public float RayLength;
    public uint VisionMask;
}