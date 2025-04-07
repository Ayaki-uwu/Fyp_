using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FinalBoss : enemy
{
    public enum BossState{
        Idle,
        Attack,
        Dash,
        Aggressive
    }
    public BossState bossState = BossState.Idle;
    public Transform target;
    private player playerScript;
    private Rigidbody2D myRigidbody;

    public float cooldownTime;
    private float nextFireTime = 0f;
    private bool Draging;
    [SerializeField] private GameObject laserEffectPrefab;

    //Web
    public GameObject webPrefab; // The web prefab to be instantiated
    public float webSpeed = 10f; // Speed of the web shot
    public float webDuration = 2f;
    private List <GameObject> webist;
    private int webCount;
    public Transform shootPoint;

    [Header("Poison Zone Settings")]
    public float poisonDamageInterval = 1f; // How often the player takes poison damage
    public int poisonDamage = 1;            // How much damage per tick
    private bool playerInPoisonZone = false;
    private Coroutine poisonCoroutine;
    [SerializeField] float dashSpeed;
    [SerializeField] Vector2 moveDir;

    [SerializeField] float attackUpNDownSpeed;
    [SerializeField] Vector2 UpNDownmoveDir;

    [SerializeField] Transform GroundCheckUp;
    [SerializeField] Transform GroundCheckDown;
    [SerializeField] Transform GroundCheckWall;
    [SerializeField] float CheckRadius;
    [SerializeField] LayerMask wallLayer;
    bool goingUp;
    bool shouldFlip;
    bool isTouchingUp;
    bool isTouchingDown;
    bool isTouchingWall;

    [Header("ECS spawner")]
    private EntityManager entityManager;
    private Entity FinalBossEntity;
    private Entity spawnerEntity;
    private Entity SpawnerTriggerEntity;
    private int laserCount;

    // Start is called before the first frame update
    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerScript = playerObject.GetComponent<player>();
            target = playerObject.transform;
        }
        target = GameObject.FindWithTag("Player").transform;
        Draging = false;
        webist = new List<GameObject>();
        webCount = 3;
        moveDir.Normalize();
        UpNDownmoveDir.Normalize();
        goingUp = true;
        shouldFlip = false;
        myRigidbody = GetComponent<Rigidbody2D>();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        FinalBossEntity = entityManager.CreateEntity();
        SpawnerTriggerEntity = entityManager.CreateEntity();
        
        entityManager.AddComponentData(FinalBossEntity, new BossComponent
        {
            position = transform.position, // Set the position
        });

        entityManager.AddComponentData(SpawnerTriggerEntity, new LaserSpawnerTriggerComponent
        {
            shouldSpawn = false,
        });
        Debug.Log("SpawnerTriggerEntity created and initialized.");

        if (!entityManager.HasComponent<LaserSpawnerTriggerComponent>(SpawnerTriggerEntity))
        {
            Debug.LogError("SpawnerTriggerComponent not found on the spawner entity.");
        }
        LaserAOE();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        isTouchingUp = Physics2D.OverlapCircle(GroundCheckUp.position,CheckRadius,wallLayer);
        isTouchingDown = Physics2D.OverlapCircle(GroundCheckDown.position,CheckRadius,wallLayer);
        isTouchingWall = Physics2D.OverlapCircle(GroundCheckWall.position,CheckRadius,wallLayer);
        if(bossState == BossState.Aggressive)
        {
            Dashing();
        }
        else
        {
            MoveIdel();
        }
        if (Time.time >= nextFireTime && playerScript.alive) {
            DecideAction();
            nextFireTime = Time.time + cooldownTime; // Update the next fire time 
        }

        // ControlPlayerSpider();
        if (Input.GetKeyDown(KeyCode.C)) {
            // DragPlayer();
            LaserAOE();
        }

        Aggression();

        if (entityManager.HasComponent<BossComponent>(FinalBossEntity))
        {
            var bossData = entityManager.GetComponentData<BossComponent>(FinalBossEntity);
            bossData.position = transform.position; // Update position
            entityManager.SetComponentData(FinalBossEntity, bossData); // Set the updated position
        }
    }

    void Aggression()
    {
        float healthPercentage = (float)health / (float)maxHP;
        if (healthPercentage < 0.5f) 
        {
            cooldownTime =1f;
            webCount=4;
            bossState = BossState.Aggressive;
        }
        else if (healthPercentage < 0.3f)
        {
            cooldownTime = 1f;
            webCount=5;
        }
    }



    void MoveIdel()
    {
        if (isTouchingUp && goingUp)
        {
            ChangeDir();
        }
        else if (isTouchingDown && !goingUp)
        {
            ChangeDir();
        }

        if (isTouchingWall)
        {
            if (shouldFlip)
            {
                Flip();
            }
            else if (!shouldFlip)
            {
                Flip();
            }
        }
        myRigidbody.velocity = moveSpeed * moveDir;
    }

    void ChangeDir()
    {
        goingUp = !goingUp;
        moveDir.y *= -1;
        UpNDownmoveDir.y *= -1;
    }

    void Flip()
    {
        shouldFlip = !shouldFlip;
        moveDir.x *= -1;
        UpNDownmoveDir.x *= -1;
        transform.Rotate(0,180,0);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(GroundCheckUp.position,CheckRadius);
        Gizmos.DrawWireSphere(GroundCheckDown.position,CheckRadius);
        Gizmos.DrawWireSphere(GroundCheckWall.position,CheckRadius); 
    }

    void DecideAction()
    {
        float Drag =  0.34f;
        float Laser = 0.34f;
        float Web = 0.33f;

        float totalWeight = Drag + Laser + Web;
        float randomValue = Random.value;

        Drag /= totalWeight;
        Laser /= totalWeight;
        Web /= totalWeight;

        if (randomValue < Drag && !Draging)
        {
            Debug.Log("Boss drag player");
            DragPlayer();
        }
        else if (randomValue < Drag + Laser)
        {
            Debug.Log("Boss Lasering");
            LaserAOE();
        }
        else if (randomValue < Drag + Laser + Web)
        {
            Debug.Log("Boss Web Attack!");
            ShootWeb();
        }
    }

    void DragPlayer()
    {
        if (target != null) // Ensure the player exists
        {
            StartCoroutine(DragCoroutine());
        }
    }

    void Dashing()
    {
        if (isTouchingUp && goingUp)
        {
            ChangeDir();
        }
        else if (isTouchingDown && !goingUp)
        {
            ChangeDir();
        }

        if (isTouchingWall)
        {
            if (shouldFlip)
            {
                Flip();
            }
            else if (!shouldFlip)
            {
                Flip();
            }
        }
        myRigidbody.velocity = attackUpNDownSpeed * UpNDownmoveDir;
    }

    // void LaserAOE()
    // {
    //     float[] angleSteps = { 15f, 30f, 45f }; // Possible angle step patterns
    //     float chosenAngleStep = angleSteps[Random.Range(0, angleSteps.Length)]; // Randomly pick one pattern
    //     int laserCount = Mathf.RoundToInt(360f / chosenAngleStep); // Compute number of lasers

    //     float radius = 2f; // Distance from boss where lasers appear

    //     float minSpeed = 1f, maxSpeed = 2f; // Random speed range

    //     // float angleStep = 360f / laserCount; // Divide circle into equal parts

    //     for (int i = 0; i < laserCount; i++)
    //     {
    //         float angle = i * chosenAngleStep; // Set laser angle
    //         Vector2 spawnPosition = (Vector2)transform.position + GetPositionFromAngle(angle, radius);
    //         Vector2 moveDirection = GetPositionFromAngle(angle, 1f).normalized;
    //         float laserSpeed = Random.Range(minSpeed, maxSpeed); // Assign random speed

    //         // Instantiate laser at calculated position
    //         GameObject laser = Instantiate(laserEffectPrefab, spawnPosition, Quaternion.identity);
            
    //         // Rotate laser to face outward
    //         float rotationAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
    //         laser.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

    //         // Move laser outward
    //         StartCoroutine(MoveLaser(laser, moveDirection, laserSpeed));
    //     }
    // }

    void LaserAOE()
    {
        float[] angleSteps = { 15f, 30f, 45f }; // Possible angle step patterns
        float chosenAngleStep = angleSteps[Random.Range(0, angleSteps.Length)]; // Random angle step
        int waveCount = 5; // Number of laser waves
        float waveInterval = 1f; // Time between waves
        float minSpeed = 0.5f, maxSpeed = 2f; // Laser speed range

        StartCoroutine(LaserWaveCoroutine(chosenAngleStep, waveCount, waveInterval, minSpeed, maxSpeed));
    }

    Vector2 GetPositionFromAngle(float angle, float radius)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radian) * radius, Mathf.Sin(radian) * radius);
    }



    void ControlPlayerSpider()
    {
        // Find all small spiders in the scene
        GameObject[] smallSpiders = GameObject.FindGameObjectsWithTag("smallspider");

        foreach (GameObject spider in smallSpiders)
        {
            meleeEnemy spiderScript = spider.GetComponent<meleeEnemy>();

            if (spiderScript != null)
            {
                // Check if this spider belongs to the player
                if (spiderScript.spiderState == meleeEnemy.SpiderState.player)
                {
                    // Change the state (e.g., make it attack the boss)
                    spiderScript.SetState(meleeEnemy.SpiderState.enemy);
                }
            }
        }
    }

    private IEnumerator MoveLaser(GameObject laser, Vector2 direction, float speed)
    {
        float duration = 4f; // How long the laser moves before disappearing
        float elapsedTime = 0f;

        Rigidbody2D rb = laser.GetComponent<Rigidbody2D>();
        
        while (elapsedTime < duration)
        {
            if (rb != null)
            {
                rb.velocity = direction * speed;
            }
            else
            {
                laser.transform.position += (Vector3)(direction * speed * Time.deltaTime);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(laser); // Remove the laser after it moves for a while
    }


    private IEnumerator LaserWaveCoroutine(float angleStep, int waveCount, float waveInterval, float minSpeed, float maxSpeed)
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            // float randomStartAngle = Random.Range(0f, angleStep); // Randomize initial angle

            // int laserCount = Mathf.RoundToInt(360f / angleStep); // Compute number of lasers per wave
            // float radius = 2f; // Distance from boss

            // for (int i = 0; i < laserCount; i++)
            // {
            //     float angle = randomStartAngle + (i * angleStep); // Offset by step
            //     Vector2 spawnPosition = (Vector2)transform.position + GetPositionFromAngle(angle, radius);
            //     Vector2 moveDirection = GetPositionFromAngle(angle, 1f).normalized;
            //     float laserSpeed = Random.Range(minSpeed, maxSpeed); // Assign random speed

            //     // Instantiate laser at calculated position
            //     GameObject laser = Instantiate(laserEffectPrefab, spawnPosition, Quaternion.identity);
                
            //     // Rotate laser to face outward
            //     // float rotationAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            //     // laser.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

            //     // Move laser outward
            //     StartCoroutine(MoveLaser(laser, moveDirection, laserSpeed));
            // }
            SpawnLaserBarrage();

            yield return new WaitForSeconds(waveInterval); // Delay before next wave
        }
    }

    void ShootWeb()
    {
        if (webPrefab == null)
        {
            Debug.LogError("webPrefab is not assigned in the inspector!");
            return;
        }

        // webCount = 1; // Number of webs
        float spreadAngle = 15f; // Angle between each web shot

        // Base direction towards the player
        Vector2 baseDirection = (target.position - shootPoint.position).normalized;

        for (int i = 0; i < webCount; i++)
        {
            float angleOffset = (i - 1) * spreadAngle; // Spread angles: -15, 0, +15
            Vector2 rotatedDirection = RotateVector(baseDirection, angleOffset); // Rotate direction

            // Instantiate web
            GameObject web = Instantiate(webPrefab, shootPoint.position, Quaternion.identity);

            // Apply velocity
            Rigidbody2D webRb = web.GetComponent<Rigidbody2D>();
            cobWeb cobWebScript = web.GetComponent<cobWeb>();
            if (webRb != null)
            {
                webRb.velocity = rotatedDirection * 7f; // Adjust speed if needed
                cobWebScript.SetDirection(rotatedDirection);
            }

            webist.Add(web); // Add to the list if needed
        }
    }

    void SpawnLaserBarrage()
    {
        // var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Entity spawnerEntity = entityManager.CreateEntity();

        // entityManager.AddComponentData(spawnerEntity, new LaserSpawnerConfig
        // {
        //     Barrage = laserEntityPrefab,
        //     Amount = 12,
        //     posX = transform.position.x,
        //     posY = transform.position.y,
        // });
        if (entityManager.HasComponent<LaserSpawnerTriggerComponent>(SpawnerTriggerEntity))
        {
            var spawnerTriggerData = entityManager.GetComponentData<LaserSpawnerTriggerComponent>(SpawnerTriggerEntity);
            Debug.Log("Check in spider Boss b4 Spawn: " +spawnerTriggerData.shouldSpawn);
            // Update the SpawnerTriggerComponent's state
            spawnerTriggerData.shouldSpawn = true;
            spawnerTriggerData.spawnCount = 15;

            entityManager.SetComponentData(SpawnerTriggerEntity, spawnerTriggerData);
            Debug.Log("Spider spawn triggered.");
            Debug.Log("Check in spider Boss After Spawn: "+ spawnerTriggerData.shouldSpawn);
        }
        else
        {
            Debug.LogError("SpawnerTriggerComponent not found on the spawner trigger entity.");
        }
    }

    Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            cos * vector.x - sin * vector.y,
            sin * vector.x + cos * vector.y
        );
    }


    private IEnumerator DragCoroutine()
    {
        Draging = true;
        float dragDuration = 2f;
        float elapsed = 0f;

        playerScript.BeingDrag = true;

        while (elapsed < dragDuration)
        {
            if (target == null) break;

            Vector2 dragDirection = ((Vector2)transform.position - (Vector2)target.position).normalized;
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.AddForce(dragDirection * 5f); // Adjust force magnitude as needed
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerScript.BeingDrag = false;
        Draging = false;
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {       
        if (other.CompareTag("smallspider"))
        {
            health-=1;
            Destroy(other.gameObject);
        }
        if (other.CompareTag("Player"))
        {
            playerInPoisonZone = true;
            if (poisonCoroutine == null)
            {
                poisonCoroutine = StartCoroutine(ApplyPoisonDamage(other.GetComponent<player>()));
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInPoisonZone = false;
            if (poisonCoroutine != null)
            {
                StopCoroutine(poisonCoroutine);
                poisonCoroutine = null;
            }
        }
    }
    private IEnumerator ApplyPoisonDamage(player playerScript)
    {
        while (playerInPoisonZone)
        {
            if (playerScript != null && playerScript.alive && !playerScript.isInvulnerable)
            {
                playerScript.currenthealth -= poisonDamage;
                playerScript.UpdateHealth();
                playerScript.StartCoroutine(playerScript.TemporaryInvulnerability()); // brief invuln to prevent instant re-hit

                Debug.Log("☠️ Poison ticks for " + poisonDamage);
            }

            yield return new WaitForSeconds(poisonDamageInterval);
        }

        poisonCoroutine = null;
    }

    // private void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.cyan;
    //     Gizmos.DrawWireSphere(transform.position, 2f);
    // }

}
