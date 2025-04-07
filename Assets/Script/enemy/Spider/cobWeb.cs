using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cobWeb : MonoBehaviour
{
    // Start is called before the first frame update
    public float slowDownFactor = 0.5f; // How much the player's movement is slowed
    private Rigidbody2D rb;
    [SerializeField] float speed;
    public Transform target;
    public Transform shootPoint;
    [SerializeField] Transform groundCheckUp;
    [SerializeField] Transform groundCheckDown;
    [SerializeField] Transform groundCheckRight;
    [SerializeField] Transform groundCheckLeft;
    [SerializeField] float wallCheckRadius;
    [SerializeField] LayerMask wallLayer;
    // [SerializeField] float moveSpeed;
    // [SerializeField] Vector2 moveDir;

    public bool isTouchingUp;
    public bool isTouchingDown;
    public bool isTouchingRight;
    public bool isTouchingLeft;
    private Vector2 lastVelocity; 

    bool goingUp;
    bool shouldStop;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        float randomTime = Random.Range(1f, 3f);
        StartCoroutine(StopWebAfterTime(randomTime)); // Stop after 2 seconds
        shouldStop = false;
    }

    // Update is called once per frame
    void Update()
    {
        isTouchingUp = Physics2D.OverlapCircle(groundCheckUp.position,wallCheckRadius,wallLayer);
        isTouchingDown = Physics2D.OverlapCircle(groundCheckDown.position,wallCheckRadius,wallLayer);
        isTouchingRight = Physics2D.OverlapCircle(groundCheckRight.position,wallCheckRadius,wallLayer);
        isTouchingLeft = Physics2D.OverlapCircle(groundCheckLeft.position,wallCheckRadius,wallLayer);
        if (!shouldStop)
        {
            move();
        }
    }

    void move()
    {
        bool bounced = false;

        if (isTouchingUp || isTouchingDown)
        {
            lastVelocity.y *= -1;
            bounced = true;
        }

        if (isTouchingLeft || isTouchingRight)
        {
            lastVelocity.x *= -1;
            bounced = true;
        }

        if (bounced)
        {
            rb.velocity = lastVelocity.normalized * speed;
        }
    }


    public void SetDirection(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        direction.Normalize(); // Ensure it's a unit vector
        lastVelocity = direction;
        rb.velocity = lastVelocity * speed;
    }

    void ChangeDir()
    {
        goingUp = !goingUp;
        lastVelocity.y *= -1;
        lastVelocity.y *= -1;
    }

    void Flip()
    {
        // shouldFlip = !shouldFlip;
        lastVelocity.x *= -1;
        lastVelocity.x *= -1;
        // transform.Rotate(0,180,0);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(groundCheckUp.position,wallCheckRadius);
        Gizmos.DrawWireSphere(groundCheckDown.position,wallCheckRadius);
        Gizmos.DrawWireSphere(groundCheckRight.position,wallCheckRadius); 
        Gizmos.DrawWireSphere(groundCheckLeft.position,wallCheckRadius); 
    }

    // Coroutine to stop the web after a specified time
    IEnumerator StopWebAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified time
        shouldStop = true;
        rb.velocity = Vector2.zero; // Stop the web by setting its velocity to zero
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Slow down the player's movement
            player player = other.GetComponent<player>();
            if (player != null)
            {
                player.ApplySlowEffect(slowDownFactor);
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void OnTriggerExit2D (Collider2D other){
        if (other.CompareTag("Player"))
        {
            // Slow down the player's movement
            player player = other.GetComponent<player>();
            if (player != null)
            {
                player.RemoveSlowEffect();
            }
        }
    }
}
