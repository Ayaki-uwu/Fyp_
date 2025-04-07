using UnityEngine;

public class LaserSettingsHolder : MonoBehaviour
{
    public LaserCollisionSettings settings;

    public static LaserCollisionSettings Instance;

    private void Awake()
    {
        Instance = settings;
    }
}
