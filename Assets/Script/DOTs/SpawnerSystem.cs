using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public partial struct SpawnerSystem : ISystem
{
    // Start is called before the first frame update
    public void OnCreate(ref SystemState state) 
    { 
        state.RequireForUpdate<SpawnerConfig>();
        state.RequireForUpdate<SpawnerTriggerComponent>();
    }

    // Update is called once per frame
    public void OnUpdate(ref SystemState state)
    {
        // state.Enabled = false;
        if (!SystemAPI.HasSingleton<SpawnerConfig>())
        {
            Debug.LogError("SpawnerConfig not found!");
            return;
        }
        SpawnerConfig spawnerConfig = SystemAPI.GetSingleton<SpawnerConfig>();
        if (spawnerConfig.smallSpiderPrefab == Entity.Null)
        {
            Debug.LogError("SpawnerConfig: smallSpiderPrefab is null!");
            return;
        }
        SpawnerTriggerComponent spawnerTrigger = SystemAPI.GetSingleton<SpawnerTriggerComponent>();
        Debug.Log("SpawnerTriggerComponent: shouldSpawn = " + spawnerTrigger.shouldSpawn);

        // Check if spawning is triggered
        if (!spawnerTrigger.shouldSpawn)
        {
            Debug.Log("SpawnerTriggerComponent: shouldSpawn is false. No spawn will occur.");
            return;
        }
        // for (int i = 0; i < spawnerTrigger.spawnCount; i++)
        // {
            Entity spawnedEntity = state.EntityManager.Instantiate(spawnerConfig.smallSpiderPrefab);
            state.EntityManager.AddComponent<smallSpiderCompoent>(spawnedEntity);
            float spawnPosX = UnityEngine.Random.Range(DataCenter._MinX, DataCenter._MaxX);
            float spawnPosY = UnityEngine.Random.Range(DataCenter._MinY, DataCenter._MaxY);
            SystemAPI.SetComponent(spawnedEntity, new smallSpiderCompoent
            {
                health = DataCenter._health,
                moveSpeed = DataCenter._moveSpeed,
                // spiderState = spiderState,
                spiderState = SpiderState.enemy,
                // TargetTransform = LocalTransform.Identity,
                hasHitPlayer = false
            });

            SystemAPI.SetComponent(spawnedEntity, new LocalTransform{
                        Position = new float3(spawnPosX,spawnPosY,0),
                        Scale = 3f
                    });
        // }

        spawnerTrigger.shouldSpawn = false;
        SystemAPI.SetSingleton(spawnerTrigger);
        Debug.Log("SpawnerTriggerComponent: After Spawn Check = " + spawnerTrigger.shouldSpawn);
    }
}
