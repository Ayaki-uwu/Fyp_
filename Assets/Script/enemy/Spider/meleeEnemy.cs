using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meleeEnemy : enemy
{
    public enum SpiderState{
        enemy,
        player,
    }
    public Transform target;
    public float chaseRadius;
    public float attackRadius;
    public Transform homePosition;
    public SpiderState spiderState;
    private Rigidbody2D myRigidbody;
    private float lifetime = 2f;
    private Vector3 movementTarget;
    private bool shouldMove = false;
    // Start is called before the first frame update
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
    }
    public void SetState(SpiderState newState)
    {
        spiderState = newState;
    }

    // Update is called once per frame
    void Update()
    {
        if (spiderState == SpiderState.enemy)
        {
            target = GameObject.FindWithTag("Player").transform;
            gameObject.tag = "Enemy";
        }
        else 
        {
            target = GameObject.FindWithTag("Boss").transform;
            gameObject.tag = "smallspider";
        }
        
        base.Update();
        CheckDistance();
        EnemyDection();
    }
    void FixedUpdate()
    {
        if (!control)
        {
            return;
        }
        if (shouldMove)
        {
            Vector3 temp = Vector3.MoveTowards(transform.position, movementTarget, moveSpeed * Time.fixedDeltaTime);
            myRigidbody.MovePosition(temp);
        }
    }

    void CheckDistance()
    {
        if (Vector3.Distance(target.position, transform.position) <= chaseRadius &&
            Vector3.Distance(target.position, transform.position) > attackRadius)
        {
            movementTarget = target.position;
            shouldMove = true;
        }
        else
        {
            shouldMove = false;
        }
    }


    void EnemyDection()
    {
        if (target == null)
        {
            Destroy(gameObject, lifetime);
        }
    }
}
