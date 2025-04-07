using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public partial struct LaserSpawnerSystem : ISystem
{
    private Entity laserPrefab;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LaserSpawnerConfig>();
        state.RequireForUpdate<LaserSpawnerTriggerComponent>();
        state.RequireForUpdate<BossComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {   
        // var ecb = new EntityCommandBuffer(Allocator.Temp);

        if (!SystemAPI.HasSingleton<LaserSpawnerConfig>())
        {
            Debug.LogError("SpawnerConfig not found!");
            return;
        }
        LaserSpawnerConfig spawnerConfig = SystemAPI.GetSingleton<LaserSpawnerConfig>();
        if (spawnerConfig.Barrage == Entity.Null)
        {
            Debug.LogError("SpawnerConfig: smallSpiderPrefab is null!");
            return;
        }
        LaserSpawnerTriggerComponent spawnerTrigger = SystemAPI.GetSingleton<LaserSpawnerTriggerComponent>();
        Debug.Log("LaserSpawnerTriggerComponent: shouldSpawn = " + spawnerTrigger.shouldSpawn);

        // Check if spawning is triggered
        if (!spawnerTrigger.shouldSpawn)
        {
            Debug.Log("LaserSpawnerTriggerComponent: shouldSpawn is false. No spawn will occur.");
            return;
        }

        if(!SystemAPI.HasSingleton<BossComponent>())
        {
            Debug.LogError("BossComponent not found!");
            return;
        }
        BossComponent Bosspos = SystemAPI.GetSingleton<BossComponent>();
        Debug.Log("LaserSpawnerTriggerComponent: shouldSpawn = " + spawnerTrigger.shouldSpawn);
        
        // foreach (var (spawner, entity) in SystemAPI.Query<LaserSpawnerConfig>().WithEntityAccess())
        // {
            

        //     for (int i = 0; i < spawner.Amount; i++)
        //     {
        //         Entity laser = ecb.Instantiate(spawner.Barrage); // prefab stored from LaserAuthoring prefab

        //         float angle = i * (360f / spawner.Amount);
        //         float3 dir = new float3(math.cos(math.radians(angle)), math.sin(math.radians(angle)), 0f);

        //         ecb.SetComponent(laser, new LaserData
        //         {
        //             direction = dir,
        //             speed = 2f,
        //             lifetime = 5f
        //         });

        //         ecb.SetComponent(laser, LocalTransform.FromPosition(Bosspos.position.x, Bosspos.position.y, 0));
        //     }

        //     ecb.Playback(state.EntityManager);
        //     ecb.Dispose();

        //     // Optionally remove the spawner so it doesn’t repeat
        //     state.EntityManager.DestroyEntity(entity);
        // }

        for (int i = 0; i < spawnerTrigger.spawnCount; i++)
        {
            float angle = i * (360f / spawnerTrigger.spawnCount);
            float3 dir = new float3(math.cos(math.radians(angle)), math.sin(math.radians(angle)), 0f);

            // Entity laser = ecb.Instantiate(spawnerConfig.Barrage);
            Entity spawnedEntity = state.EntityManager.Instantiate(spawnerConfig.Barrage);
            state.EntityManager.AddComponent<LaserData>(spawnedEntity);

            SystemAPI.SetComponent(spawnedEntity, new LaserData
            {
                direction = dir,
                speed = 2f,
                lifetime = 5f
            });

            SystemAPI.SetComponent(spawnedEntity, new LocalTransform{
                        Position = new float3(Bosspos.position.x,
                        Bosspos.position.y,0),
                        Scale = 1f
                    });
        }

        // Reset the trigger after spawning
        var triggerEntity = SystemAPI.GetSingletonEntity<LaserSpawnerTriggerComponent>();
        // ecb.SetComponent(triggerEntity, new LaserSpawnerTriggerComponent
        // {
        //     shouldSpawn = false,
        //     spawnCount = spawnerTrigger.spawnCount // keep same count
        // });

        // ecb.Playback(state.EntityManager);
        // ecb.Dispose();
        spawnerTrigger.shouldSpawn = false;
        SystemAPI.SetSingleton(spawnerTrigger);
        Debug.Log("SpawnerTriggerComponent: After Spawn Check = " + spawnerTrigger.shouldSpawn);
    }
}
