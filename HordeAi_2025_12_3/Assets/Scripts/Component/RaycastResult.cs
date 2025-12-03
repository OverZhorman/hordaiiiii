using Unity.Entities;

// Stores the distance hit for each ray (0 = close/touching, 1 = max range)
// We use a Buffer because we might want 3 rays, 5 rays, or 20 rays.
public struct RaycastResult : IBufferElementData
{
    public float DistanceNormalized; // 0.0 to 1.0
}
