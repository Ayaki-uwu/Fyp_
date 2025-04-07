using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public struct smallSpiderCompoent : IComponentData
{
    public int health;
    // public int maxHP;
    public float moveSpeed;
    public SpiderState spiderState;
    public bool hasHitPlayer; 
}

public enum SpiderState{
    None,
    enemy,
    player,
}

public struct SpiderBossDeathTag : IComponentData { }