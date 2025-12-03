using Unity.Entities;

public struct SpawnerConfig : IComponentData
{
    public Entity UnitPrefab;
    public int SquadCount;
    public int MembersPerSquad;
    public float SpawnRadius;
    public int EnvID; // New
    public bool IsInitialized;
}