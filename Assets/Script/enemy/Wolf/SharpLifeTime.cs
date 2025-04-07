using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharpLifeTime : MonoBehaviour
{
    // Start is called before the first frame update
    public float lifetime = 2f;
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
