using Unity.Entities;
using UnityEngine;
using Unity.Rendering;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject smallSpiderPrefab;
    public class Baker : Baker<SpawnerAuthoring> {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SpawnerConfig{
                // smallSpiderPrefab = GetEntity(DataCenter._smallSpiderPrefab, TransformUsageFlags.Dynamic),
                smallSpiderPrefab = GetEntity(authoring.smallSpiderPrefab, TransformUsageFlags.Dynamic),
                Amount = DataCenter._SpiderCount,

                MinX = DataCenter._MinX, // bound of camera
                MinY = DataCenter._MinY,
                MaxX = DataCenter._MaxX,
                MaxY = DataCenter._MaxY,
            });
        }
    };
}

public struct SpawnerConfig : IComponentData{
    public Entity smallSpiderPrefab;

    public int Amount;
    public float MinX;
    public float MinY;
    public float MaxX;
    public float MaxY;
}