using Unity.Entities;

public struct SensorConfig : IComponentData
{
    // Horizontal
    public int RayCount;
    public float AngleDegrees;

    // Vertical
    public int VerticalRayCount;
    public float VerticalAngleDegrees;

    public float RayLength;
    public uint CollisionMask;
}