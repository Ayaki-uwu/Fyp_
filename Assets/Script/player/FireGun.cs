using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGun : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Enemy") || other.CompareTag("Boss")){
            Debug.Log("Bullet hit an enemy: ");
            enemy enemyScript = other.GetComponent<enemy>();
            if (enemyScript != null)
            {
                enemyScript.health -= 2; // Reduce the enemy's health
                Debug.Log("Enemy Health: " + enemyScript.health);
            }
            Destroy(gameObject);
        }

        if (other.CompareTag("FireGunDestroyable"))
        {
            Destroy(other.gameObject);
        }
    }
}
