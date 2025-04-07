using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public struct playerComponent : IComponentData
{
    public float3 position;    
}

public struct SpawnerTriggerComponent : IComponentData
{
    public bool shouldSpawn;
    public SpiderSwapnState spiderSwapnState;
    public int spawnCount;
}

public struct bulletComponent : IComponentData
{
    public float3 position;
}

public struct BossComponent : IComponentData
{
    public float3 position;
    
}

public enum SpiderSwapnState{
    SpiderSpawn,
    PlayerSpawn,
}

public struct LaserData : IComponentData
{
    public float3 direction;
    public float speed;
    public float lifetime;
    public bool hasHitPlayer; 
    public bool shouldDestroy;
}

public struct LaserSpawnerTriggerComponent : IComponentData
{
    public bool shouldSpawn;
    public int spawnCount;
}