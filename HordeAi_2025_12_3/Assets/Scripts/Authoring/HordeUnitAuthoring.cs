using UnityEngine;
using Unity.Entities;

public class HordeUnitAuthoring : MonoBehaviour
{

    class Baker : Baker<HordeUnitAuthoring>
    {
        public override void Bake(HordeUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);


            AddComponent(entity, new AgentCommand
            {
                MoveSpeed = 0,
                MoveVector = Unity.Mathematics.float3.zero
            });


            AddComponent(entity, new SquadMember
            {
                SquadID = -1,
                EnvID = -1
            });


            AddComponent(entity, new SensorConfig());


            AddBuffer<RaycastResult>(entity);
        }
    }
}