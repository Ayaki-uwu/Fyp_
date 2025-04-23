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
    public enum BossPhrase{
        inti,
        mid,
        final
    }
    public GameObject iceSharpPrefab;
    public GameObject iceFloorPrefab;
    public BossState bossState = BossState.Idle;
    public BossPhrase bossPhrase = BossPhrase.inti;
    public Transform target;
    private player playerScript;
    private Rigidbody2D myRigidbody;

    public float cooldownTime;
    private float nextFireTime = 0f;
    private bool Draging;
    private float dragforce;
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

    private bool isBusy = false;
    int waveCount;
    public GameObject GameWinUi;
    SpriteRenderer sprite;

    // Start is called before the first frame update
    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerScript = playerObject.GetComponent<player>();
            target = playerObject.transform;
        }
        sprite = GetComponent<SpriteRenderer>();
        target = GameObject.FindWithTag("Player").transform;
        Draging = false;
        webist = new List<GameObject>();
        webCount = 3;
        dragforce = 5f;
        moveDir.Normalize();
        UpNDownmoveDir.Normalize();
        goingUp = true;
        shouldFlip = false;
        myRigidbody = GetComponent<Rigidbody2D>();
        waveCount = 5;

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

        ControlPlayerSpider();
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
        if (healthPercentage < 0.3f) 
        {
            cooldownTime = 1f;
            webCount=5;
            waveCount=7;
            dragforce = 9f;
            bossPhrase = BossPhrase.final;
            bossState = BossState.Aggressive;
            sprite.color = Color.red;
        }
        else if (healthPercentage < 0.5f)
        {
            cooldownTime =1f;
            webCount=4;
            waveCount=6;
            dragforce=7f;
            bossPhrase = BossPhrase.mid;
            sprite.color = Color.yellow;
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
        if (isBusy) return;
        switch (bossPhrase)
        {
            case BossPhrase.inti:
                int skills = Random.Range(1, 3);

                if (skills==1 && !Draging)
                {
                    Debug.Log("Boss drag player");
                    DragPlayer();
                }
                else if (skills==2)
                {
                    Debug.Log("Boss Lasering");
                    LaserAOE();
                }
                else if (skills==3)
                {
                    Debug.Log("Boss Web Attack!");
                    StartCoroutine(ShootWebCoroutine());
                }
            break;

            case BossPhrase.mid:
                int moreskills = Random.Range(1, 5);

                if (moreskills==1 && !Draging)
                {
                    Debug.Log("Boss drag player");
                    DragPlayer();
                }
                else if (moreskills==2 ||moreskills== 3)
                {
                    Debug.Log("Boss Lasering");
                    LaserAOE();
                }
                else if (moreskills==4)
                {
                    Debug.Log("Boss Web Attack!");
                    StartCoroutine(ShootWebCoroutine());
                }
                else if (moreskills == 5)
                {
                    Debug.Log("Boss Ice Wall!");
                    IceWall();
                }
            break;

            case BossPhrase.final:
                int finalskillSet = Random.Range(1, 7);

                if (finalskillSet==1 && !Draging)
                {
                    Debug.Log("Boss drag player");
                    DragPlayer();
                }
                else if (finalskillSet==2 ||finalskillSet== 3)
                {
                    Debug.Log("Boss Lasering");
                    LaserAOE();
                }
                else if (finalskillSet==4)
                {
                    Debug.Log("Boss Web Attack!");
                    StartCoroutine(ShootWebCoroutine());
                }
                else if (finalskillSet == 5)
                {
                    Debug.Log("Boss Ice Wall!");
                    IceWall();
                }
                else if (finalskillSet== 6 ||finalskillSet== 7)
                {
                    Debug.Log("Boss Ice Wall!");
                    ThrowIce();
                }
            break;
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
        // int waveCount = 5; // Number of laser waves
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
        isBusy = true;
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
        isBusy = false;
    }

    private IEnumerator ShootWebCoroutine()
    {
        isBusy = true;

        if (webPrefab == null)
        {
            Debug.LogError("webPrefab is not assigned!");
            yield break;
        }

        float spreadAngle = 15f;
        Vector2 baseDirection = (target.position - shootPoint.position).normalized;

        for (int i = 0; i < webCount; i++)
        {
            float angleOffset = (i - 1) * spreadAngle;
            Vector2 rotatedDirection = RotateVector(baseDirection, angleOffset);

            GameObject web = Instantiate(webPrefab, shootPoint.position, Quaternion.identity);

            Rigidbody2D webRb = web.GetComponent<Rigidbody2D>();
            cobWeb cobWebScript = web.GetComponent<cobWeb>();

            if (webRb != null)
            {
                webRb.velocity = rotatedDirection * webSpeed;
                cobWebScript.SetDirection(rotatedDirection);
            }

            webist.Add(web);
        }

        yield return new WaitForSeconds(0.5f); // Delay before allowing next skill
        isBusy = false;
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
        isBusy = true;
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
                rb.AddForce(dragDirection * dragforce); // Adjust force magnitude as needed
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerScript.BeingDrag = false;
        Draging = false;
        isBusy = false;
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

    protected override void Die()
    {
        Debug.Log(enemyName + " (Wolf Boss) has been destroyed!");
        // OnBossDied?.Invoke();
        base.Die();
        DestroyAllEnemies();
        GameWinUi.SetActive(true);
        if (!entityManager.CreateEntityQuery(typeof(BossComponent)).CalculateEntityCount().Equals(0))
        {
            EntityQuery query = entityManager.CreateEntityQuery(typeof(BossComponent));
            entityManager.DestroyEntity(query);
            Debug.LogWarning("playerComponent already exists. Skipping creation.");
        }
        if (!entityManager.CreateEntityQuery(typeof(LaserSpawnerTriggerComponent)).CalculateEntityCount().Equals(0))
        {
            EntityQuery query = entityManager.CreateEntityQuery(typeof(LaserSpawnerTriggerComponent));
            entityManager.DestroyEntity(query);
            Debug.LogWarning("playerComponent already exists. Skipping creation.");
        }
    }

    private void DestroyAllEnemies()
    {
        foreach (GameObject web in webist)
        {
            Destroy(web);
        }
        webist.Clear();
    }

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
    }

    void ThrowIce()
    {
        float distance = Vector2.Distance(shootPoint.position, target.position);
        float speed = 5f;

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
        isBusy = true;
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
        isBusy = false;
    }

    private IEnumerator CircleWave(int count, float radius, float moveSpeed, float duration, float rotationSpeed)
    {
        isBusy = true;
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
        isBusy = false;
    }
    private IEnumerator IceWaveCoroutine(float angleStep, int waveCount, float waveInterval, float minSpeed, float maxSpeed)
    {
        isBusy = true;
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
            isBusy = false;
        }
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
}
