using Unity.Entities;
using Unity.Mathematics;

public struct InputComponent : IComponentData
{
    public float2 movement;
    public float2 mosPos;
    public bool LmouseClick;
}
