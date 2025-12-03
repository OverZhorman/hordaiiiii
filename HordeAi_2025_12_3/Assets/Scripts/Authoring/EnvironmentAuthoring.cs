using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EnvironmentAuthoring : MonoBehaviour
{
    public int EnvID;

    class Baker : Baker<EnvironmentAuthoring>
    {
        public override void Bake(EnvironmentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnvironmentTag
            {
                EnvID = authoring.EnvID,
                CenterPosition = authoring.transform.position,
                IsInitialized = false
            });
        }
    }
}