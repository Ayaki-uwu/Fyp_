using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class bullet : MonoBehaviour
{
    public float lifetime = 2f;
    private EntityManager entityManager;
    private Entity bulletEntity;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifetime);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        bulletEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(bulletEntity, new bulletComponent
        {
            position = transform.position,
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (entityManager.Exists(bulletEntity))
        {
            var bulletData = entityManager.GetComponentData<bulletComponent>(bulletEntity);
            bulletData.position = transform.position;
            entityManager.SetComponentData(bulletEntity, bulletData);
        }
    }

    void OnDestroy()
    {
        if (entityManager.Exists(bulletEntity))
        {
            entityManager.DestroyEntity(bulletEntity);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Enemy") || other.CompareTag("Boss")){
            Debug.Log("Bullet hit an enemy: ");
            enemy enemyScript = other.GetComponent<enemy>();
            if (enemyScript != null)
            {
                enemyScript.health -= 1; // Reduce the enemy's health
                Debug.Log("Enemy Health: " + enemyScript.health);
            }
            Destroy(gameObject);
        }

        if(other.CompareTag("tubes")){
            Debug.Log("Bullet hit a tube: ");
            Tubes tubeScript = other.GetComponent<Tubes>();
            if (tubeScript != null && tubeScript.isAbleToBreak())
            {
                tubeScript.currenthealth -= 1; // Reduce the enemy's health
                Debug.Log("tube Health: " + tubeScript.currenthealth);
            }
            Destroy(gameObject);
        }

        if(other.CompareTag("wall")){
            Debug.Log("Bullet hit a wall: ");
            IceWall icewall = other.GetComponent<IceWall>();
            if (icewall != null)
            {
                icewall.health -= 1; // Reduce the enemy's health
                Debug.Log("tube Health: " + icewall.health);
            }
            Destroy(gameObject);
        }
    }
}
