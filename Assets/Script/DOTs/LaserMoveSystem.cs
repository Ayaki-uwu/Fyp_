using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial struct LaserMoveSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LaserData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 playerPosition = SystemAPI.GetSingleton<playerComponent>().position;

        // Create a NativeList to store lasers to destroy
        var lasersToDestroy = new NativeList<Entity>(Allocator.TempJob);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Schedule move job
        // var moveJobHandle = 
        new LaserMoveJob
        {
            deltaTime = deltaTime
        }.ScheduleParallel(); // <-- schedule and pass dependency

        // Schedule damage job
        // var damageJobHandle = 
        new LaserDamageJob
        {
            playerPosition = playerPosition,
            lasersToDestroy = lasersToDestroy.AsParallelWriter()
        }.ScheduleParallel(); // <-- make sure this runs after move

        // Ensure jobs complete before processing destroy logic
        // damageJobHandle.Complete();

        // Handle laser destruction after all jobs finish
        

        state.CompleteDependency();
        foreach (var (laser, entity) in SystemAPI.Query<RefRW<LaserData>>().WithEntityAccess())
        {
            Debug.Log($"Processing laser entity: {entity.Index}");
            if (laser.ValueRW.hasHitPlayer || laser.ValueRW.shouldDestroy)
            {
                Debug.Log($"laser {entity.Index} hit player. Destroying entity.");
                // spider.ValueRW.hasHitPlayer = false;
                if (laser.ValueRW.hasHitPlayer && player.instance != null)
                {
                    player.instance.TakeDamage(); // Apply damage to GameObject
                }
                ecb.DestroyEntity(entity);
                
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        var setting = LaserSettingsHolder.Instance;
        if (setting == null) return;

        foreach (var (laser, transform) in SystemAPI.Query<RefRO<LaserData>, RefRO<LocalTransform>>())
        {
            float2 laserSize = setting.laserSize;
            float2 laserOffset = setting.laserOffset;
            float2 laserPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
            float2 halfSize = laserSize * 0.5f * setting.debugScale;
            float2 center = laserPos + laserOffset;
            float2 min = center - halfSize;
            float2 max = center + halfSize;

            float3 bottomLeft = new float3(min.x, min.y, 0);
            float3 bottomRight = new float3(max.x, min.y, 0);
            float3 topRight = new float3(max.x, max.y, 0);
            float3 topLeft = new float3(min.x, max.y, 0);

            Debug.DrawLine(bottomLeft, bottomRight, Color.red);
            Debug.DrawLine(bottomRight, topRight, Color.red);
            Debug.DrawLine(topRight, topLeft, Color.red);
            Debug.DrawLine(topLeft, bottomLeft, Color.red);
        }
    }

    [BurstCompile]
    public partial struct LaserMoveJob : IJobEntity
    {
        public float deltaTime;

        public void Execute(ref LaserData laser, ref LocalTransform transform)
        {
            transform.Position += laser.direction * laser.speed * deltaTime;
            laser.lifetime -= deltaTime;
        }
    }

    [BurstCompile]
    public partial struct LaserDamageJob : IJobEntity
    {
        public float3 playerPosition;
        public NativeList<Entity>.ParallelWriter lasersToDestroy;

        public void Execute(Entity entity, ref LaserData laser, ref LocalTransform transform)
        {
            float2 laserSize = new float2(0.3f, 0.6f);
            float2 laserOffset = new float2(0, -0.2f);
            float2 laserPos = new float2(transform.Position.x, transform.Position.y);

            if (CheckAABBCollision(laserPos, laserSize, laserOffset, playerPosition))
            {
                laser.hasHitPlayer = true;
                // lasersToDestroy.AddNoResize(entity);
            }

            // Optionally destroy expired lasers too
            if (laser.lifetime <= 0)
            {
                laser.shouldDestroy=true;
                // lasersToDestroy.AddNoResize(entity);
            }
        }

        private bool CheckAABBCollision(float2 laserPos, float2 laserSize, float2 laserOffset, float3 playerPos)
        {
            float2 halfSize = laserSize * 0.5f;
            float2 center = laserPos + laserOffset;
            float2 min = center - halfSize;
            float2 max = center + halfSize;

            return playerPos.x >= min.x && playerPos.x <= max.x &&
                   playerPos.y >= min.y && playerPos.y <= max.y;
        }
    }
}
