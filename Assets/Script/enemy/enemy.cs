using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemy : MonoBehaviour
{
    public int health;
    public int maxHP;
    public string enemyName;
    public int baseAttack;
    public float moveSpeed;
    public bool control;
    // public Transform target;
    // private player playerScript;

    // Start is called before the first frame update
    void Start()
    {   
        control= true;
        maxHP = health;
    }
    
    // Update is called once per frame
    protected virtual void Update()
    {   
        if(!control)
        {
            return;
        }
        CheckHealth(); // Check health in every frame
    }

    protected virtual void Die()
    {
        // Play death animation or effects here if needed
        Debug.Log(enemyName + " has been destroyed!");

        Destroy(transform.gameObject); // Destroy the enemy GameObject
    }

    private void CheckHealth()
    {
        if (health <= 0)
        {
            Die(); // Call the method to handle enemy destruction
        }
    }

    public void setControl()
    {
        control = !control;
    }
    

}
