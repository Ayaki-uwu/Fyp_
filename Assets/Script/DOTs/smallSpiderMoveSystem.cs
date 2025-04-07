using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial struct SmallSpiderMoveSystem : ISystem
{
    private float3 playerPosition;
    public void OnCreate(ref SystemState state) { 
        state.RequireForUpdate<smallSpiderCompoent>();  //make sure system OnUpdate will only run if there is at least 1 Entity with RotateSpeed Component
        // state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        bool isBossDead = SystemAPI.HasSingleton<SpiderBossDeathTag>();
        if (isBossDead)
        {
            // Destroy all spider ECS entities
            foreach (var (spider, entity) in SystemAPI.Query<RefRW<smallSpiderCompoent>>().WithEntityAccess())
            {
                Debug.Log($"Boss is dead: Destroying spider entity {entity.Index}");
                ecb.DestroyEntity(entity);
            }

            // Destroy the signal tag entity
            Entity deathEntity = SystemAPI.GetSingletonEntity<SpiderBossDeathTag>();
            ecb.DestroyEntity(deathEntity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return; // Exit early since spiders are all removed
        }
        playerComponent playerData = SystemAPI.GetSingleton<playerComponent>();
        float3 playerPosition = playerData.position;

        var bulletQuery = SystemAPI.QueryBuilder().WithAll<bulletComponent>().Build();
        var bullets = bulletQuery.ToComponentDataArray<bulletComponent>(Allocator.TempJob);
        var bulletsToDestroy = new NativeList<float3>(Allocator.TempJob);
        // float3 bulletPos = bulletData.position;

        
        // Query to get the player position
        // var moveJobHandle = 
        new SpiderChaseJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            playerPosition = playerPosition
        }.ScheduleParallel();

        // var dealJobHandle = 
        new DealDamageJob
        {
            playerPosition = playerPosition,
            damageRadius=0.3f,
        }.ScheduleParallel();

        // var receiveJobHandle = 
        new ReceiveDamageJob
        {
            bulletPositions = bullets,
            damageRadius = 0.3f,
            bulletsToDestroy = bulletsToDestroy.AsParallelWriter()
        }.ScheduleParallel();

        // state.Dependency = receiveJobHandle;
        state.CompleteDependency();
        foreach (var (spider, entity) in SystemAPI.Query<RefRW<smallSpiderCompoent>>().WithEntityAccess())
        {
            Debug.Log($"Processing spider entity: {entity.Index}");
            if (spider.ValueRW.hasHitPlayer|| spider.ValueRW.health<=0)
            {
                Debug.Log($"Spider {entity.Index} hit player. Destroying entity.");
                // spider.ValueRW.hasHitPlayer = false;
                if (spider.ValueRW.hasHitPlayer && player.instance != null)
                {
                    player.instance.TakeDamage(); // Apply damage to GameObject
                }
                ecb.DestroyEntity(entity);
                
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        bullets.Dispose();
        foreach (var bulletGO in GameObject.FindGameObjectsWithTag("bullet"))
        {
            float3 bulletPos = bulletGO.transform.position;

            for (int i = 0; i < bulletsToDestroy.Length; i++)
            {
                if (math.distance(bulletPos, bulletsToDestroy[i]) < 0.01f) // some tolerance
                {
                    GameObject.Destroy(bulletGO);
                    break;
                }
            }
        }
    }


    //This use to moveToplayer
    [BurstCompile]
    public partial struct SpiderChaseJob: IJobEntity 
    {
        public float deltaTime;
        public float3 playerPosition;
        public void Execute(ref smallSpiderCompoent spider, ref LocalTransform transform)
        {
            switch (spider.spiderState)
            {
                case SpiderState.enemy:
                // Calculate the direction to the player from the spider
                float3 direction = math.normalize(playerPosition - transform.Position);

                // Move the spider towards the player
                transform.Position += direction * spider.moveSpeed * deltaTime;
                break;

                case SpiderState.player:
                // Calculate the direction to the player from the spider
                // float3 direction = math.normalize(playerPosition - transform.Position);

                // // Move the spider towards the player
                // transform.Position += direction * spider.moveSpeed * deltaTime;
                break;

                default:
                break;
            }
        }
    }

    // [BurstCompile]
    // public partial struct checkCollisionJob: IJobEntity {}
    // [BurstCompile]
    public partial struct DealDamageJob : IJobEntity
    { 
        public float3 playerPosition;
        public float damageRadius;
        
        public void Execute(ref smallSpiderCompoent spider, ref LocalTransform transform)
        {
            float distance = math.distance(transform.Position, playerPosition);
            if (distance < damageRadius)
            {
                spider.hasHitPlayer = true;
                Debug.Log($"Spider hit player: Position={transform.Position}, Distance={distance}");
            }
        }
    }

    public partial struct ReceiveDamageJob : IJobEntity
    { 
        [ReadOnly] public NativeArray<bulletComponent> bulletPositions;
        public float damageRadius;
        public NativeList<float3>.ParallelWriter bulletsToDestroy;
        
        public void Execute(ref smallSpiderCompoent spider, ref LocalTransform transform)
        {
            for (int i = 0; i < bulletPositions.Length; i++)
            {
                float distance = math.distance(transform.Position, bulletPositions[i].position);
                if (distance < damageRadius)
                {
                    Debug.Log($"bullet hit a spider: Position={transform.Position}, Distance={distance}");
                    spider.health--;
                    bulletsToDestroy.AddNoResize(bulletPositions[i].position);
                    Debug.Log($"spide health = {spider.health}");
                    break; // Only hit once
                }
            }
        }
    }
}
