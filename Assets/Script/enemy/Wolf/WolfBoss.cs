using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WolfBoss : enemy
{

    public enum WolfState{
        Idel,
        Normal,
        SeekingTube,
        Absorbing,
        AbsorbSuccess,
        AbsorbFail,
    }
    public WolfState wolfState = WolfState.Normal;
    public Transform target;
    private player playerScript;
    private Rigidbody2D myRigidbody;
    public GameObject iceSharpPrefab; // ice sharp prefab
    public float iceSharpSpeed = 10f; // Speed of the web shot
    public float iceSharpDuration = 2f;
    public Transform shootPoint; // Where the bullet will be instantiated
    public float cooldownTime ;
    private float nextFireTime = 0f;
    public List <Transform> tubesPos;
    public GameObject iceFloorPrefab; // the Wall
    public GameObject wolfchip;
    int AbsorbTime = 5;
    private GameObject selectedTubes;
    public bool isDashing;
    private bool canDash;
    [SerializeField] private Image uiFill;
    private RectTransform uiTimer;
    [SerializeField] private Canvas canvas;
    private Vector3 UiTimeScale = new Vector3 (0.01f,0.01f,1f);
    public GameObject BossFightMap;

    private Transform selectedTube = null;
    // float distance;
    // Start is called before the first frame update
    //AbsorbIce Counter and list
    private int AbsorbCounter;
    public List<float> absorbHPlist = new List<float> { 0.75f, 0.5f, 0.25f };
    public static WolfBoss Instance { get; private set; }

    private bool Damaged = false;
    private bool awake = false;
    public bool NextSkill = true;
    [Header("UI")]
    public GameObject playerArrowPrefab;
    public float arrowDistanceFromPlayer = 0.5f;
    private GameObject arrowInstance;  

    void Awake(){
        if (Instance == null)
        Instance = this;
        GameObject[] tubeObjects = GameObject.FindGameObjectsWithTag("tubes");
        tubesPos = new List<Transform>();
        // // foreach (Transform tubePos in transform.Find("tube")){
        // //     tubesPos.Add(tubePos);
        // // }
        foreach (GameObject tubeObject in tubeObjects)
        {
        tubesPos.Add(tubeObject.transform);
        }
        uiTimer = uiFill.GetComponentInChildren<RectTransform>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player.instance = playerObject.GetComponent<player>();
            target = playerObject.transform;
        }
        target = GameObject.FindWithTag("Player").transform;
        maxHP = health;
        canDash = true;
        isDashing = false;
        myRigidbody = GetComponent<Rigidbody2D>();
        AbsorbCounter = 0;
        Debug.Log(absorbHPlist.Count);
        wolfState = WolfState.Idel;
        // IceRain();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        StateChanger();
        

        if (Time.time >= nextFireTime && player.instance.alive) {
            DecideAction();
            nextFireTime = Time.time + cooldownTime; // Update the next fire time 
        }
        FindTube();
        Aggression();
        DamageCheck();
        if (wolfState == WolfState.Absorbing && selectedTube != null)
        {
            if (arrowInstance != null)
            {
                arrowInstance.SetActive(true);

                // Calculate direction from player to tube
                Vector2 dirToTube = (selectedTube.position - player.instance.transform.position).normalized;

                // Place the arrow around the player
                Vector2 arrowPos = (Vector2)player.instance.transform.position + dirToTube * arrowDistanceFromPlayer;
                arrowInstance.transform.position = arrowPos;

                // Rotate the arrow to point at the tube
                float angle = Mathf.Atan2(dirToTube.y, dirToTube.x) * Mathf.Rad2Deg;
                arrowInstance.transform.rotation = Quaternion.Euler(0, 0, angle-90);
            }
        }
        else
        {
            if (arrowInstance != null)
                arrowInstance.SetActive(false); // Hide arrow when not absorbing
        }
    }

    void DamageCheck()
    {
        if(maxHP != health && !awake)
        {
            Damaged = true;
            awake = true;
            wolfState = WolfState.Normal;
        }
    }
    void DecideAction()
    {
        switch (wolfState)
        {
            case WolfState.Normal:
                if (!isDashing)
                {
                    float healthPercentage = (float)health / (float)maxHP;

                    float dash = healthPercentage > 0.5f ? 0.3f : 0.4f; // Higher chance when health is > 50%
                    float shoot = healthPercentage > 0.5f ? 0.4f : 0.3f; // Higher chance when health is <= 50%
                    float wall = healthPercentage > 0.5f ? 0.3f : 0.3f;

                    // Normalize weights (ensure they sum to 1)
                    float totalWeight = dash + shoot + wall;
                    dash /= totalWeight;
                    shoot /= totalWeight;
                    wall /= totalWeight;
                        
                    // Randomly decide action based on weights
                    float randomValue = Random.value; // Random number between 0 and 1


                    if (randomValue < dash && canDash)
                    {
                        Debug.Log("Wolf decided to... dash");
                        DashPlayer();
                        canDash = false;
                        NextSkill = false;
                    }
                    else if (randomValue < dash + shoot)
                    {
                        Debug.Log("Wolf decided to... shoot");
                        ThrowIce();
                        NextSkill = false;
                    }
                    else if (randomValue < dash + shoot + wall)
                    {
                        IceWall(); 
                        NextSkill = false;
                    }
                }
                break;
            // case WolfState.SeekingTube:
                // FindTube();
                // break;
            case WolfState.Absorbing:
                AbsorbIce(selectedTube.gameObject);
                break;
            // case WolfState.AbsorbSuccess:
            //     IceRain();
            //     break;

            case WolfState.AbsorbFail:
                wolfState = WolfState.Normal;
                break;
                
            default:
            // wolfState = WolfState.Normal;
            break;
        }
    }

    void StateChanger()
    {
        float healthPercentage = (float)health / (float)maxHP;
        if (wolfState == WolfState.Normal && AbsorbCounter < absorbHPlist.Count && healthPercentage <= absorbHPlist[AbsorbCounter])
        {
            Debug.Log("AbsorbCounter: "+ AbsorbCounter + "absorbHPlist" + absorbHPlist[AbsorbCounter]);
            wolfState = WolfState.SeekingTube;
            AbsorbCounter++;
        }

        if (wolfState == WolfState.AbsorbSuccess)
        {
            IceRain(); // Activate Ice Rain skill
            wolfState = WolfState.Normal; // Reset state after attack
        }
    }

    void Aggression()
    {
        float healthPercentage = (float)health / (float)maxHP;
        if (healthPercentage < 0.5f) 
        {
            AbsorbTime = 3;
            cooldownTime =1f;
        }
        else if (healthPercentage < 0.3f)
        {
            AbsorbTime = 2;
            cooldownTime = 0.5f;
        }
    }

    void DashPlayer()
    {
        if (target != null && !isDashing) // Ensure we have a player target and not already dashing
    {
        // StartCoroutine(DashCoroutine());
        StartCoroutine(RicochetDash(3f, 6f));
    }
    }

    void FindTube()
    {
        Debug.Log("Searching");
        if (target != null && tubesPos.Count > 0 && wolfState == WolfState.SeekingTube) // Ensure player exists and tubes list is not empty
        {
            Transform farthestTube = null;
            float maxDistance = 0f;
            Debug.Log("counts:" + tubesPos.Count );
            // Loop through all tubes to find the furthest one
            foreach (Transform tube in tubesPos)
                {
                    Tubes tubeScript = tube.GetComponent<Tubes>();
                    Debug.Log(tubeScript + " have script");
                    if (tube != null && !tubeScript.isBroken()) // Ensure the tube exists
                    {
                        Debug.Log(tubeScript.GetTubeState() + " current state");
                        float distance = Vector2.Distance(target.position, tube.transform.position);
                        
                        if (distance > maxDistance )
                        {
                            maxDistance = distance;
                            farthestTube = tube;
                            // wolfState = WolfState.Absorbing;
                        }
                    }
                }
            if (farthestTube != null)
            {
                selectedTube = farthestTube;
                wolfState = WolfState.Absorbing;
                if (arrowInstance == null && playerArrowPrefab != null)
                {
                    arrowInstance = Instantiate(playerArrowPrefab);
                }
            }
            else
            {
                Debug.Log("No available tube found.");
            }
        }  
    }

    void AbsorbIce(GameObject selectedTube)
    {
        
        if (tubesPos.Count == 0)
        {
            Debug.LogError("No tubes available.");
            return;
        }
        
        foreach (Transform tube in tubesPos)
        {
            Tubes tubeScript = tube.GetComponent<Tubes>();
            if (tube != null)
            {
                
                SpriteRenderer tubeRenderer = tube.GetComponent<SpriteRenderer>();

                if (tubeRenderer != null)
                {
                    if (tube == selectedTube.transform)
                    {
                        tubeScript.SetState(Tubes.TubeState.AbleToBreak);
                        StartCoroutine(Absorbing(tube.gameObject));
                    }
                    else if (!tubeScript.isBroken())
                    {
                        tubeScript.SetState(Tubes.TubeState.Selectedable);
                    }
                }
            }
        }
    }

    void ThrowIce()
    {
        // if (iceSharpPrefab == null)
        // {
        //     Debug.LogError("iceSharpPrefab is not assigned in the inspector!");
        //     return;
        // }

        // // Direction toward the player
        // Vector2 direction = (target.position - shootPoint.position).normalized;
        float distance = Vector2.Distance(shootPoint.position, target.position);
        // float requiredSpeed = distance / 1f;
        float speed = 5f;

        // // Instantiate web prefab
        // GameObject iceSharp = Instantiate(iceSharpPrefab, shootPoint.position, Quaternion.identity);

        // // Get Rigidbody2D and apply velocity in the direction of the player
        // Rigidbody2D iceSharprb = iceSharp.GetComponent<Rigidbody2D>();
        // iceSharprb.velocity = direction * requiredSpeed;
        int pattern = Random.Range(1, 3); // Randomly pick 1 or 2
        if (pattern == 1)
        {
            Debug.Log("Pattern 1: Circle Burst Toward Player");
            StartCoroutine(MultiWaveCircleBurstTowardPlayer(
                waveCount: 5,
                iceCountPerWave: 6,
                radius: 1f,
                moveSpeed: speed,
                duration: 2f,
                rotationSpeed: 180f,
                waveInterval : 1f
            ));
        }
        else
        {
            Debug.Log("Pattern 2: Rotating Laser Style Burst");
            StartCoroutine(IceWaveCoroutine(30f, 3, 0.8f, 5f, speed)); // 30-degree step, 3 waves, delay between, speed
        }
    }

    public IEnumerator MultiWaveCircleBurstTowardPlayer(
    int waveCount,
    int iceCountPerWave,
    float radius,
    float moveSpeed,
    float duration,
    float rotationSpeed,
    float waveInterval
    )
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            StartCoroutine(CircleWave(
                iceCountPerWave,
                radius,
                moveSpeed,
                duration,
                rotationSpeed
            ));

            yield return new WaitForSeconds(waveInterval);
        }
        NextSkill = true;
    }

    private IEnumerator CircleWave(int count, float radius, float moveSpeed, float duration, float rotationSpeed)
    {
        Vector2 playerPos = target.position;
        Vector2 origin = transform.position;

        // Create the center point
        GameObject center = new GameObject("IceSharpCenter");
        center.transform.position = origin;

        // Spawn and parent ice projectiles
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            Vector2 spawnPos = (Vector2)center.transform.position + offset;

            GameObject ice = Instantiate(iceSharpPrefab, spawnPos, Quaternion.identity);
            ice.transform.SetParent(center.transform); // parent to center
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Move center toward the player
            Vector2 direction = (playerPos - (Vector2)center.transform.position).normalized;
            center.transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

            // Rotate the whole center object to orbit ice projectiles
            center.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy center and all children (ice projectiles)
        Destroy(center);
    }


    // private IEnumerator CircleBurstTowardPlayer(int count, float radius, float speed)
    // {
    //     Vector2 playerPos = target.position;
    //     Vector2 origin = transform.position;

    //     for (int i = 0; i < count; i++)
    //     {
    //         float angle = (360f / count) * i;
    //         Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
    //         Vector2 spawnPos = origin + offset;
    //         Vector2 shootDir = (playerPos - spawnPos).normalized;

    //         GameObject ice = Instantiate(iceSharpPrefab, spawnPos, Quaternion.identity);
    //         Rigidbody2D rb = ice.GetComponent<Rigidbody2D>();
    //         if (rb != null)
    //             rb.velocity = shootDir * speed;

    //         yield return null;
    //     }
    // }


    private IEnumerator IceWaveCoroutine(float angleStep, int waveCount, float waveInterval, float minSpeed, float maxSpeed)
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            float randomStartAngle = Random.Range(0f, angleStep);
            int iceCount = Mathf.RoundToInt(360f / angleStep);
            float radius = 2f;

            for (int i = 0; i < iceCount; i++)
            {
                float angle = randomStartAngle + i * angleStep;
                Vector2 spawnPos = (Vector2)transform.position + GetPositionFromAngle(angle, radius);
                Vector2 moveDir = GetPositionFromAngle(angle, 1f).normalized;
                float speed = Random.Range(minSpeed, maxSpeed);

                GameObject ice = Instantiate(iceSharpPrefab, spawnPos, Quaternion.identity);
                StartCoroutine(MoveIceSharp(ice, moveDir, speed));
            }

            yield return new WaitForSeconds(waveInterval);
        }
        NextSkill = true;
    }

    private Vector2 GetPositionFromAngle(float angle, float radius)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
    }

    private IEnumerator MoveIceSharp(GameObject ice, Vector2 direction, float speed)
    {
        float duration = 4f;
        float elapsed = 0f;

        while (elapsed < duration && ice != null)
        {
            ice.transform.position += (Vector3)(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ice != null)
            Destroy(ice);
    }

    protected override void Die()
    {
        Debug.Log(enemyName + " (Wolf Boss) has been destroyed!");
        // OnBossDied?.Invoke();
        base.Die();
        GameObject chip = Instantiate(wolfchip, shootPoint.position, Quaternion.identity);
    }

    public void IceRain()
    {
        Debug.Log("Ice Rain skill activated!");

        int iceCount = 20; // Number of ice pieces to drop
        Vector2 mapMinBounds = new Vector2(-8f, -6.5f); // Adjust these based on map size
        Vector2 mapMaxBounds = new Vector2(3.7f, 0f);

        StartCoroutine(SpawnIceRain(iceCount, mapMinBounds, mapMaxBounds));
    }

    // void IceWall()
    // {
    //     Debug.Log("Ice Wall Activated!");

    //     // Get the player's Rigidbody2D to find velocity
    //     Rigidbody2D playerRb = playerScript.GetComponent<Rigidbody2D>();

    //     if (playerRb == null)
    //     {
    //         Debug.LogError("Player Rigidbody2D not found!");
    //         return;
    //     }

    //     Vector2 movementDirection = playerScript.change.normalized;

    //     Debug.Log("playerVelocity: " + movementDirection);
    //     // If player is not moving, don't spawn the wall
    //     if (movementDirection.magnitude < 0.1f)
    //     {
    //         Debug.Log("Player is stationary, no Ice Wall spawned.");
    //         return;
    //     }

    //     // Predict the player's future position (1.5 seconds ahead)
    //     Vector2 predictedPosition = (Vector2)playerScript.transform.position + movementDirection * 3f;

    //     // Offset the Ice Wall behind the predicted position
    //     Vector2 spawnPosition = predictedPosition - movementDirection * 3f;

    //     // Calculate the angle based on the player's movement direction
    //     float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;

    //     // Instantiate the Ice Wall and rotate it accordingly
    //     GameObject iceWall = Instantiate(iceFloorPrefab, spawnPosition, Quaternion.Euler(0, 0, angle + 180));

    //     Debug.Log("Ice Wall Spawned at: " + spawnPosition + " with rotation: " + angle + " degrees");
    // }
    void IceWall()
    {
        Debug.Log("Ice Wall Activated!");

        Rigidbody2D playerRb = player.instance.GetComponent<Rigidbody2D>();

        if (playerRb == null)
        {
            Debug.LogError("Player Rigidbody2D not found!");
            return;
        }

        // Use velocity if it's meaningful, fallback to input
        Vector2 movementDirection = playerRb.velocity.magnitude > 0.1f 
            ? playerRb.velocity.normalized 
            : player.instance.change.normalized;

        if (movementDirection.magnitude < 0.1f)
        {
            Debug.Log("Player is stationary, no Ice Wall spawned.");
            return;
        }

        // Block ahead of player instead of behind
        Vector2 spawnPosition = (Vector2)player.instance.transform.position + movementDirection * 1.5f;

        float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;

        GameObject iceWall = Instantiate(iceFloorPrefab, spawnPosition, Quaternion.Euler(0, 0, angle ));
        iceWall.tag = "wall";

        Debug.Log($"Ice Wall Spawned at: {spawnPosition} facing angle: {angle}");
        NextSkill = true;
    }



    private IEnumerator Absorbing(GameObject tube)
    {   Tubes tubeScript = tube.GetComponent<Tubes>();
        int Timer  = AbsorbTime;
        while (Timer >0)
        {
            Timer--;
            yield return new WaitForSeconds(1f); // Wait for the interval
        }
        if(tubeScript.isBroken()){
            wolfState = WolfState.AbsorbFail;
        }
        else 
        {
            tubeScript.SetState(Tubes.TubeState.Absorbed);
            wolfState = WolfState.AbsorbSuccess;
        }
    }
    private IEnumerator RicochetDash(float duration, float speed)
    {
        isDashing = true;
        float elapsed = 0f;

        Vector2 currentDirection = (target.position - transform.position).normalized;

        while (elapsed < duration)
        {
            // Check for wall collision using Raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, currentDirection, speed * Time.deltaTime, LayerMask.GetMask("Wall"));
            if (hit.collider != null)
            {
                // Reflect the direction using surface normal
                currentDirection = Vector2.Reflect(currentDirection, hit.normal).normalized;

                // Optional: Slightly re-aim towards player to keep pressure
                Vector2 toPlayer = (target.position - transform.position).normalized;
                currentDirection = Vector2.Lerp(currentDirection, toPlayer, 0.3f).normalized;

                Debug.Log("🧱 Bounced! New direction: " + currentDirection);
            }

            // Move in currentDirection
            transform.position += (Vector3)(currentDirection * speed * Time.deltaTime);
            // Vector2 newPos = (Vector2)transform.position + currentDirection * speed * Time.deltaTime;
            // myRigidbody.MovePosition(newPos);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        canDash = true;
    }


    private IEnumerator DashCoroutine()
    {
        isDashing = true;

        Vector2 startPos = transform.position;
        Vector2 directionToPlayer = (target.position - transform.position).normalized;
        float dashSpeed = 10f;
        float dashDuration = 0.5f;

        // First dash towards player
        yield return StartCoroutine(DashInDirection(directionToPlayer, dashSpeed, dashDuration));

        // Bounce back direction (simulate a wall hit)
        Vector2 bounceDirection = -directionToPlayer;

        // Wait briefly
        yield return new WaitForSeconds(0.2f);

        // Dash back toward player again (new target position may change slightly)
        Vector2 updatedDirection = (target.position - transform.position).normalized;
        yield return StartCoroutine(DashInDirection(updatedDirection, dashSpeed, dashDuration));

        // isDashing = false;
    }
    
    private IEnumerator DashInDirection(Vector2 direction, float speed, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Use raycast to detect wall
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, speed * Time.deltaTime, LayerMask.GetMask("Wall"));
            if (hit.collider != null)
            {
                Debug.Log("Wall hit during dash!");
                break; // Stop the dash when wall is hit
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isDashing = false;
    }


    private bool CheckPlayerCollision()
    {
        // Perform a check if the wolf is colliding with the player during dash.
        // This could use a simple distance check or trigger detection logic.
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, 0.2f, LayerMask.GetMask("Player"));
        return playerCollider != null;
    }

    public void OnCrystalBroken()
    {
        // brokenCrystalCount++;
        // Debug.Log("Crystal Broken! Count: " + brokenCrystalCount);

        // if (brokenCrystalCount >= crystalsToBreak)
        // {
            Debug.Log("Crystals broken. Wolf Boss activates!");
            wolfState = WolfState.Normal; // Or another combat state
        // }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing && other.CompareTag("Player")) 
        {
            isDashing = false;
            Debug.Log("Wolf Boss hit the player!");

            // Get player's Rigidbody2D
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                // Calculate knockback direction (from wolf to player)
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                float knockbackForce = 5f; // Adjust knockback force

                // Apply knockback force
                // playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                playerRb.velocity = knockbackDirection * knockbackForce;
                StartCoroutine(ResetPlayerVelocity(playerRb, 1f));
            }
        }
        
        if (other.CompareTag("smallspider"))
        {
            health-=1;
            Destroy(other.gameObject);
        }
    }
    private IEnumerator ResetPlayerVelocity(Rigidbody2D playerRb, float delay)
    {
        yield return new WaitForSeconds(delay);
        playerRb.velocity = Vector2.zero; // Stop the knockback effect
    }
    private IEnumerator SpawnIceRain(int count, Vector2 minBounds, Vector2 maxBounds)
    {
        float delayBetweenDrops = 0.3f; // Time delay between each drop

        for (int i = 0; i < count; i++)
        {
            Vector2 randomPosition = new Vector2(
                Random.Range(minBounds.x, maxBounds.x),
                Random.Range(minBounds.y, maxBounds.y)
            );
            RectTransform uiTimerInstance = Instantiate(uiTimer, canvas.transform);
            uiTimerInstance.transform.SetParent(canvas.transform, false);
            StartCoroutine(ShowHintAndDrop(uiTimerInstance, randomPosition, 0.2f));

            yield return new WaitForSeconds(delayBetweenDrops); // Wait before spawning the next one
        }

        Debug.Log("Ice Rain finished!");
        
    }

    private IEnumerator ShowHintAndDrop(RectTransform uiTimer, Vector2 dropPosition, float delay)
    {
        // Update the position of the UI Timer
        uiTimer.position = dropPosition;
        uiTimer.localScale = UiTimeScale;
        // Debug.Log("TEST");

        // Animate the uiFill (child of uiTimer)
        Image uiFill = uiTimer.GetComponentInChildren<Image>();
        
        if (uiFill != null)
        {
            uiFill.color = new Color32(179,52,89,150); // Set the color to red
            uiFill.fillAmount = 1; // Reset the fill amount to full
        }
        else
        {
            Debug.LogError("uiFill not found as a child of uiTimer.");
        }
        // Debug.Log("Ice dropped at: " + dropPosition);

        yield return new WaitForSeconds(0.5f);
        Destroy(uiTimer.gameObject); // Destroy the UI Timer instance
        GameObject ice = Instantiate(iceSharpPrefab, dropPosition, Quaternion.identity);

    }


}
