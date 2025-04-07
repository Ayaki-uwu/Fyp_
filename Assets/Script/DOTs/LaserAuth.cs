using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


public class LaserAuthoring : MonoBehaviour
{
    public GameObject laserPrefab;

    public class Baker : Baker<LaserAuthoring>
    {
        public override void Bake(LaserAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LaserSpawnerConfig
            {
                Barrage = GetEntity(authoring.laserPrefab, TransformUsageFlags.Dynamic),
                Amount = 15,
            });
            AddComponentObject(entity, authoring.laserPrefab.GetComponent<Animator>());
        }
    }
}

public struct LaserSpawnerConfig : IComponentData{
    public Entity Barrage;

    public int Amount;
}
