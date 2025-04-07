using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DataCenter
{
    [Header("Enemy")]
    public static GameObject _smallSpiderPrefab = Resources.Load<GameObject>("BossPrefabs/smallSpiderECS");
    // public static GameObject  _EmptyPrefabECS = Resources.Load<GameObject>("BossPrefabs/EmptyPrefabECS");

    [Header("SpawnArea")]
    public static float _MinX = -6f;
    public static float _MaxX = 5f;
    public static float _MinY = 0f;
    public static float _MaxY = 5f;
    
    [Header("spiderStat")]
    public static int _health = 8;
    public static float _moveSpeed = 2f;

    [Header("SpiderCount")]
    public static int _SpiderCount = 1;

    [Header("Bullet")]
    public static GameObject _bullet = Resources.Load<GameObject>("playerPrefab/bullet");
}
