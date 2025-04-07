using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
// using Unity.Mathematics;
using Unity.Transforms;

public class spiderBoss : enemy
{
    public Transform target;
    private player playerScript;
    public float chaseRadius;
    public float attackRadius;
    public Transform homePosition;
    private Rigidbody2D myRigidbody;

    //spawn spider
    public GameObject spiderPrefab; // spider mobs prefab
    public Transform shootPoint; // Where the bullet will be instantiated
    public float cooldownTime ;
    private float nextFireTime = 0f;

    //list of spider and spawnpos
    private List <GameObject> smallSpiderlist;

    private List <Vector3> SpawnPosList;
    private List <GameObject> webist;

    //Web
    public GameObject webPrefab; // The web prefab to be instantiated
    public float webSpeed = 10f; // Speed of the web shot
    public float webDuration = 2f;

    //jump
    public float jumpHeight = 10f; // Height of the jump
    public float jumpCooldown = 5f; // Cooldown between jumps
    private bool isJumping = false; // Check if jumping

    //chip
    public GameObject spiderchip;

    private Vector3 UiTimeScale = new Vector3 (0.01f,0.01f,1f);

    [SerializeField] private Image uiFill;
    [SerializeField] private RectTransform uiTimer;
    public int Duration;
    private int remainingDuration;
    public GameObject BossFightMap;
    public GameObject StageDoor;
    public GameObject NextStage;
    private int webCount;

    [Header("ECS spawner")]
    private EntityManager entityManager;
    private Entity spiderBossEntity;
    private Entity spawnerEntity;
    private Entity SpawnerTriggerEntity;
    private int spiderCount;

    void Awake(){
        SpawnPosList = new List<Vector3>();
        foreach (Transform spawnPos in transform.Find("SpawnPos")){
            SpawnPosList.Add(spawnPos.position);
        }
    }
    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerScript = playerObject.GetComponent<player>();
            target = playerObject.transform;
        }
        target = GameObject.FindWithTag("Player").transform;
        maxHP = health;
        myRigidbody = GetComponent<Rigidbody2D>();
        smallSpiderlist = new List<GameObject>();
        webist = new List<GameObject>();
        webCount=1;
        spiderCount=0;

        // Convert the GameObject to an entity
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        spiderBossEntity = entityManager.CreateEntity();
        // spawnerEntity = entityManager.CreateEntity();
        // SpawnerTriggerEntity = Entity.Null;
        SpawnerTriggerEntity = entityManager.CreateEntity();

        // Add the PlayerDataComponent to the player entity
        entityManager.AddComponentData(spiderBossEntity, new BossComponent
        {
            position = transform.position, // Set the position
        });

        // entityManager.AddComponentData(spawnerEntity, new SpawnerTriggerComponent
        // {
        //     shouldSpawn = false,
        //     spiderSwapnState = SpiderSwapnState.SpiderSpawn,
        // });
        entityManager.AddComponentData(SpawnerTriggerEntity, new SpawnerTriggerComponent
        {
            shouldSpawn = false,
            spiderSwapnState = SpiderSwapnState.SpiderSpawn,
        });
        Debug.Log("SpawnerTriggerEntity created and initialized.");
        
        
        // spawnerEntity = Entity.Null;

        if (!entityManager.HasComponent<SpawnerTriggerComponent>(SpawnerTriggerEntity))
        {
            Debug.LogError("SpawnerTriggerComponent not found on the spawner entity.");
        }
        TriggerSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (Time.time >= nextFireTime && player.instance.alive && !isJumping) { 
            DecideAction();
            nextFireTime = Time.time + cooldownTime; // Update the next fire time 
        }
        if (uiTimer != null)
        { 
            // Vector2 pos = Camera.main.WorldToViewportPoint(transform.position);
            // uiTimer.position  = Camera.main.ViewportToWorldPoint(pos);
            
            uiTimer.localScale = UiTimeScale;
            uiTimer.position = transform.position;
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            TriggerSpawn();
        }
        Aggression();
    }

    void Aggression()
    {
        float healthPercentage = (float)health / (float)maxHP;
        if (healthPercentage < 0.5f) 
        {
            cooldownTime =1f;
            webCount=2;
        }
        else if (healthPercentage < 0.3f)
        {
            cooldownTime = 0.5f;
            webCount=3;
        }
    }

    void DecideAction()
    {
        float healthPercentage = (float)health / (float)maxHP;

        float spawnSpiderWeight = healthPercentage > 0.5f ? 0.3f : 0.6f; // Higher chance when health is > 50%
        float shootWebWeight = healthPercentage > 0.5f ? 0.6f : 0.3f; // Higher chance when health is <= 50%
        float jumpToPlayerWeight  = 0.1f;

        // Normalize weights (ensure they sum to 1)
        float totalWeight = spawnSpiderWeight + shootWebWeight+jumpToPlayerWeight ;
        spawnSpiderWeight /= totalWeight;
        shootWebWeight /= totalWeight;
        jumpToPlayerWeight /= totalWeight;

        // Randomly decide action based on weights
        float randomValue = Random.value; // Random number between 0 and 1

        if (randomValue < spawnSpiderWeight)
        {
            Debug.Log("Spider Boss decided to spawn a spider.");
            float spiderActionChance = Random.value;

            if (spiderActionChance < 0.45f)
            {
                SpawnSpider();  // 45%
            }
            else if (spiderActionChance < 0.9f)
            {
                TriggerSpawn(); // 45%
            }
            else
            {
                SpawnSpider();
                TriggerSpawn(); // 10%
            }
        }
        else if (randomValue < spawnSpiderWeight + shootWebWeight)
        {
            Debug.Log("Spider Boss decided to shoot a web.");
            ShootWeb();
        }
        else {
            Debug.Log("Spider Boss decided to jump to wards player.");
            JumpTowardPlayer();
        }
    }

    void SpawnSpider() { 
        if (spiderPrefab == null)
        {
            Debug.LogError("spiderPrefab is not assigned in the inspector!");
            return; // Exit if bulletPrefab is missing
        }
        Vector3 spawnPosition = SpawnPosList[Random.Range(0, SpawnPosList.Count)];
        GameObject smallspider = Instantiate(spiderPrefab, spawnPosition, Quaternion.identity); 

        Rigidbody2D smallspiderrb = smallspider.GetComponent<Rigidbody2D>(); // Set the bullet's velocity in the direction of the player 
        smallSpiderlist.Add(smallspider);
        // smallspider.spiderState = SpiderState.enemy;
        meleeEnemy smallspiderScritp = smallspider.GetComponent<meleeEnemy>();
        smallspiderScritp.SetState(meleeEnemy.SpiderState.enemy);
    }


    void JumpTowardPlayer()
    {
        remainingDuration = Duration;
        if (target != null && !isJumping)
        {
            StartCoroutine(TeleportToPlayer());
        }
    }

    private void TriggerSpawn()
    {
        spiderCount++;
        // Update SpawnerTriggerComponent
        if (entityManager.HasComponent<SpawnerTriggerComponent>(SpawnerTriggerEntity))
        {
            var spawnerTriggerData = entityManager.GetComponentData<SpawnerTriggerComponent>(SpawnerTriggerEntity);
            Debug.Log("Check in spider Boss b4 Spawn: " +spawnerTriggerData.shouldSpawn);
            // Update the SpawnerTriggerComponent's state
            spawnerTriggerData.shouldSpawn = true;
            spawnerTriggerData.spiderSwapnState = SpiderSwapnState.SpiderSpawn;
            spawnerTriggerData.spawnCount = spiderCount;

            // Set the updated data back to the entity
            entityManager.SetComponentData(SpawnerTriggerEntity, spawnerTriggerData);
            Debug.Log("Spider spawn triggered.");
            Debug.Log("Check in spider Boss After Spawn: "+ spawnerTriggerData.shouldSpawn);

        }
        else
        {
            Debug.LogError("SpawnerTriggerComponent not found on the spawner trigger entity.");
        }
    }
    IEnumerator TeleportToPlayer()
    {
        isJumping = true;
        
        Vector2 randomOffset = Random.insideUnitCircle * 2f; // Random offset within a 2-unit radius
        transform.position = (Vector2)target.position + randomOffset;


        // Temporarily disable the boss
        gameObject.GetComponent<SpriteRenderer>().enabled = false; // Hide the boss visually
        gameObject.GetComponent<Collider2D>().enabled = false; // Disable collisions

        // Move the boss to the player's position
        transform.position = target.position;

        while (remainingDuration >=0){

        uiFill.fillAmount = Mathf.InverseLerp(0,Duration,remainingDuration);
        remainingDuration--;
        // Wait for a delay for x second
        yield return new WaitForSeconds(1f);

        }
        // Reactivate the boss
        gameObject.GetComponent<SpriteRenderer>().enabled = true; // Hide the boss visually
        gameObject.GetComponent<Collider2D>().enabled = true; // Disable collisions
        // Reset jump state
        isJumping = false;
    }

    //整做扇型
    // void ShootWeb()
    // {
    //     if (webPrefab == null)
    //     {
    //         Debug.LogError("webPrefab is not assigned in the inspector!");
    //         return;
    //     }

    //     // Direction toward the player
    //     Vector2 direction = (target.position - shootPoint.position).normalized;

    //     float distance = Vector2.Distance(shootPoint.position, target.position);
    //     // float requiredSpeed = distance / 1f;

    //     // Instantiate web prefab
    //     GameObject web = Instantiate(webPrefab, shootPoint.position, Quaternion.identity);

    //     // Get Rigidbody2D and apply velocity in the direction of the player
    //     Rigidbody2D webRb = web.GetComponent<Rigidbody2D>();
    //     if (webRb != null)
    //     {
    //         webRb.velocity = direction * 10f;
    //     }
    //     webist.Add(web);

    //     // Destroy the web after a certain duration
    // }
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

    // Helper function to rotate a vector by a given angle
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

    protected override void Die()
    {
        Debug.Log(enemyName + " (Spider Boss) has been destroyed!");

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity deathSignal = entityManager.CreateEntity();
        entityManager.AddComponent<SpiderBossDeathTag>(deathSignal);
        
        DestroyAllEnemies();
        OpenNextStage();
        
        base.Die(); 
        GameObject chip = Instantiate(spiderchip, shootPoint.position, Quaternion.identity);
        chip.name = "SpiderChip";
    }

    private void DestroyAllEnemies()
    {
        foreach(GameObject spider in smallSpiderlist) 
        {
            Destroy(spider);
        }
        smallSpiderlist.Clear();

        foreach (GameObject web in webist)
        {
            Destroy(web);
        }
        webist.Clear();
    }

    private void OpenNextStage()
    {
        StageDoor.SetActive(true);
        NextStage.SetActive(true);
        // Transform closedStageDoor = BossFightMap.transform.Find("Nextstagedoor");
        // Transform OpenedStageDoor = BossFightMap.transform.Find("NextstagedoorOpen");
        // Transform collisionWithoutDoor = BossFightMap.transform.Find("collision withoutdoor");
        // Transform collisionWithDoor = BossFightMap.transform.Find("collision withdoor");

        // if (closedStageDoor != null) closedStageDoor.gameObject.SetActive(false);
        // if (collisionWithDoor != null) collisionWithDoor.gameObject.SetActive(false);

        // if (OpenedStageDoor != null) OpenedStageDoor.gameObject.SetActive(true);
        // if (collisionWithoutDoor != null) collisionWithoutDoor.gameObject.SetActive(true);
    }
}
