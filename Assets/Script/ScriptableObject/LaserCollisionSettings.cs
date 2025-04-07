using UnityEngine;

[CreateAssetMenu(fileName = "LaserCollisionSettings", menuName = "Laser Collision Settings")]
public class LaserCollisionSettings : ScriptableObject
{
    public Vector2 laserSize = new Vector2(1.5424f, 4.0173f);
    public Vector2 laserOffset = new Vector2(0.0339f, -2.7630f);
    public float debugScale = 1f;
}