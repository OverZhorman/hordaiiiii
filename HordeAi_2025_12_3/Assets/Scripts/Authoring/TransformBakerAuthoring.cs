using UnityEngine;
using Unity.Entities;

public class TransformBakerAuthoring : MonoBehaviour
{
    class Baker : Baker<TransformBakerAuthoring>
    {
        public override void Bake(TransformBakerAuthoring authoring)
        {
            GetEntity(TransformUsageFlags.Dynamic);
        }
    }
}