using UnityEngine;
using Unity.Entities;

public class GlobalSettingsAuthoring : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject MemberPrefab;
    public GameObject TargetPrefab;

    [Header("Squad Settings")]
    public int MembersPerSquad;
    public float SpawnRadius;
    public float MoveSpeed;

    [Header("Horizontal Vision")]
    public int RayCount = 5;
    public float AngleDegrees = 120f;

    [Header("Vertical Vision")]
    public int VerticalRayCount = 3;
    public float VerticalAngleDegrees = 45f;

    [Header("General Vision")]
    public float RayLength = 20f;
    public LayerMask VisionMask;

    class Baker : Baker<GlobalSettingsAuthoring>
    {
        public override void Bake(GlobalSettingsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GlobalHordeConfig
            {
                MemberPrefab = GetEntity(authoring.MemberPrefab, TransformUsageFlags.Dynamic),
                TargetPrefab = GetEntity(authoring.TargetPrefab, TransformUsageFlags.Dynamic),
                MembersPerSquad = authoring.MembersPerSquad,
                SpawnRadius = authoring.SpawnRadius,
                MoveSpeed = authoring.MoveSpeed,

                // Horizontal
                RayCount = authoring.RayCount,
                AngleDegrees = authoring.AngleDegrees,

                // Vertical
                VerticalRayCount = authoring.VerticalRayCount,
                VerticalAngleDegrees = authoring.VerticalAngleDegrees,

                RayLength = authoring.RayLength,
                VisionMask = (uint)authoring.VisionMask.value
            });
        }
    }
}