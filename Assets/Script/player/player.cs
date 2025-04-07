using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class player : MonoBehaviour
{
    public static player instance;
    //gunState
    public enum PlayerState{
        Gun,
        FireGun,
        SpiderGun,
    }
    public enum PlayerChip{
        None,
        Spider,
        Wolf,
    }

    //player Animation State
    public enum PlayerAnimationState{
        playerIDLE,
        PlayerMove,
        PlayerShoot,
        PlayerReload,
    }
    public PlayerState playerState;
    public PlayerChip chip;
    private PlayerAnimationState animationState;
    public ChipBag chipBag;
    public float speed;
    private float currentSpeed;
    public int currenthealth;
    public bool alive = true;
    public int maxhealth;
    private Rigidbody2D myRigidBody;
    public Vector3 change;
    Vector2 moveDirection;
    private bool Controlable;

    public GameObject bulletPrefab; // Bullet prefab
    public GameObject firePrefab; // Fire prefab
    public GameObject  shootPoint; // Where the bullet will be instantiated
    public float firepointDistance = 0.2f; 
    public float bulletSpeed = 20f; // Speed of the bullet
    [Header("Dash Related")]
    private bool isDashing = false; // To track if the player is currently dashing
    public bool BeingDrag = false;
    private bool canDash = true; // To track if the player can dash
    public float dashSpeed; // Speed multiplier for dashing
    public float dashDuration = 0.5f; // How long the dash lasts
    public float dashCooldown = 2f; // Cooldown time between dashes
    private Vector2 dashDirection; // Direction of the dash
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("Guns")]
    private bool isReloadingGun = false;
    private bool isReloadingFire = false;

    //damage to player
    private HashSet<Collider2D> enemiesInRange = new HashSet<Collider2D>();

    // private Hashtable ChipList = new Hashtable();
    // Dictionary<string, bool> ChipList = new Dictionary<string, bool>();

    [SerializeField]
    public int bulletCount = 30;
    public float reloadTime = 1f;
    public int maxbulletCount = 30;

    //firegun
    public float fireGunMagazineTime = 1f;
    private float fireGunRemainingTime;
    public bool isFiring = false; 
    public float fireGunReloadTime = 2f;

    public GameObject spiderPrefab;
    public float spiderSpawnTime=5;

    //GameBoss && Map Trigger
    public GameObject spiderBoss;
    public GameObject BossFightMap;

    //State playerStatus
    public TextMeshProUGUI bulletText;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI playerHealthText;
    //particle system
    public GameObject dashParticlePrefab;
    private GameObject dashParticles;
    //animation
    private Animator animator; 
    //Chips
    private GameObject chipInRange = null;
    int spidercount;

    //ECS
    private EntityManager entityManager;
    private Entity playerEntity;
    private Entity spawnerEntity;
    //Wolf boss
    [Header("MiniGame")]
    public GameObject crystalInRange = null;
    private bool miniGame;
    [Header("BossFightMap")]
    public GameObject Boss;
    public GameObject BossMap;
    private bool bossDefeated = false;
    [Header("ScriptableObjct")]
    [SerializeField]
    public playerDataAsset playerData;

    [Header("Health Regen Settings")]
    public bool enableRegen = true;          // Toggle regeneration on/off
    public float regenInterval = 5f;         // Time between regen ticks
    public int regenAmount = 1;              // How much HP to regenerate per tick
    public int regenCapThreshold = 10;      // Stop regen if at or above this HP

    public Coroutine regenCoroutine;
    private bool recentlyDamaged = false;
    public float regenCooldownAfterDamage = 2f;

    [Header("Damage Settings")]
    public float damageInterval = 2f;      // How often you can take damage from enemies in range
    public float damageInvulnDuration = 1f; // Time after damage when player becomes immune
    private bool isTakingDamage = false;
    public bool isInvulnerable = false;

    // Start is called before the first frame update
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        chipBag = new ChipBag();
    }
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "SpiderBoss") // or any scene you consider as "New Game Start"
        {
            ResetPlayerData(); // Only reset for a true "New Game"
        }
        if (playerData == null)
        {
            Debug.LogError("PlayerDataAsset is not assigned in the Inspector.");
            return;
        }
        playerState = playerData.playerState;
        currenthealth = playerData.currentHealth;
        maxhealth = playerData.maxHealth;

        Debug.Log(currenthealth +"current" + maxhealth+ "maxhp");
        // playerState = PlayerState.Gun;

        animationState = PlayerAnimationState.playerIDLE;
        animator= GetComponent<Animator>();
        currentSpeed = speed;
        myRigidBody = GetComponent<Rigidbody2D>();
        // currenthealth = maxhealth;
        fireGunRemainingTime = fireGunMagazineTime;
        UpdateBulletCountText();
        UpdateStateText();
        UpdateHealth();
        

        chipBag.RestoreCollectedChips(playerData.collectedChips);
        if (!string.IsNullOrEmpty(playerData.equippedChip))
        {
            chipBag.EquipChip(playerData.equippedChip);
        }
        spidercount=0;

        Boss = GameObject.FindWithTag("Boss");
        BossMap = GameObject.FindWithTag("map");

        // Convert the GameObject to an entity
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        playerEntity = entityManager.CreateEntity();
        // spawnerEntity = entityManager.CreateEntity();

        // Add the PlayerDataComponent to the player entity
        entityManager.AddComponentData(playerEntity, new playerComponent
        {
            position = transform.position, // Set the position
         });
        Controlable = true;
        miniGame = false;
        // spawnerEntity = Entity.Null;
        // entityManager.AddComponentData(spawnerEntity, new SpawnerTriggerComponent
        // {
        //     shouldSpawn = false,
        //     spiderSwapnState = SpiderSwapnState.PlayerSpawn,
        // });

        // if (!entityManager.HasComponent<SpawnerTriggerComponent>(spawnerEntity))
        // {
        //     Debug.LogError("SpawnerTriggerComponent not found on the spawner entity.");
        // }
        if (enableRegen && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(RegenerateHealth());
        }
    }
    public void ResetPlayerData()
    {
        playerData.currentHealth = playerData.maxHealth;
        playerData.playerState = PlayerState.Gun;
        playerData.collectedChips.Clear();
        playerData.equippedChip = null;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(enableRegen+"Regenbable");
        Debug.Log(regenCoroutine+"regenCoroutine");
        Debug.Log(recentlyDamaged+"recentlyDamaged");
        if (!Controlable)
        {
            return;
        }
        if (isDashing)
        {
            return;
        }
        UpdateFirepoint();
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // change = Vector3.zero;
        // change.x = Input.GetAxis("Horizontal");
        // change.y = Input.GetAxis("Vertical");

        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        moveDirection = new Vector2(moveX, moveY).normalized;
        change = new Vector3(moveX, moveY);

        float angle = Mathf.Atan2(mousePosition.y - transform.position.y, mousePosition.x - transform.position.x)*Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0,0,angle);
        

        if (Input.GetButtonDown("Fire1") && alive) // Usually the left mouse button
        {
            if (playerState == PlayerState.Gun && bulletCount > 0 && !isReloadingGun)
                {
                    Shoot();
                }
            else if (playerState == PlayerState.FireGun && fireGunRemainingTime > 0 && !isReloadingFire)
                {
                    if (!isFiring)
                        {
                            StartCoroutine(FireGun());
                        }
                }
            else if (playerState == PlayerState.SpiderGun && spidercount <=2)
            {
                Vector3 spawnPosition = shootPoint.transform.position;
                GameObject smallspider = Instantiate(spiderPrefab, spawnPosition, Quaternion.identity);
                meleeEnemy smallspiderScritp = smallspider.GetComponent<meleeEnemy>();
                // smallspider.spiderState = SpiderState.player;
                smallspiderScritp.SetState(meleeEnemy.SpiderState.player);
                spidercount++;
                
                //WAIT FOR ECS TEST
                // TriggerSpawn();
            }
        }

        else if (Input.GetKeyDown(KeyCode.R) || (playerState == PlayerState.FireGun && fireGunRemainingTime <= 0) || (playerState == PlayerState.Gun && bulletCount <=0))
            {
                ReloadGun();
            }

        if (Input.GetKeyDown(KeyCode.Alpha1)){
            playerState=PlayerState.Gun;
            // Debug.Log("Switched to Gun mode "+playerState);
        }
        // if (Input.GetKeyDown(KeyCode.Alpha2) && chip == PlayerChip.None){
        //     playerState=PlayerState.FireGun;
        //     Debug.Log("Switched to FireGun mode "+playerState);
        // }
        // else if (Input.GetKeyDown(KeyCode.Alpha2) && chip == PlayerChip.Spider){
        //     playerState=PlayerState.SpiderGun;
        //     Debug.Log("Switched to FireGun mode "+playerState);
        // }

        // TESTING
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (chipBag.GetEquippedChip() != "SpiderChip")
            {
                playerState = PlayerState.FireGun;
                // Debug.Log("Switched to FireGun mode " + playerState);
            }
            else if (chipBag.GetEquippedChip() == "SpiderChip")
            {
                playerState = PlayerState.SpiderGun;
                // Debug.Log("Switched to SpiderGun mode " + playerState);
            }
        }

        // // Equip chip if the player presses a key TESTING
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     chipBag.EquipChip("SpiderChip"); // Example: Equip Spider chip
        // }
        // else {
        //     Debug.Log("SpiderChip not found");
        // }
        
        // if (Input.GetKeyDown(KeyCode.Alpha3)) // Example: Press 3 to equip SpiderChip
        // {
        //     if (chipBag.HasChip("SpiderChip"))
        //     {
        //         chipBag.EquipChip("SpiderChip");
        //     }
        //     else
        //     {
        //         Debug.Log("You don't have SpiderChip yet.");
        //     }
        // }

        // Unequip chip if the player presses a key
        // if (Input.GetKeyDown(KeyCode.Q))
        // {
        //     chipBag.UnequipChip();
        // }

        // if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && change != Vector3.zero){
        //     StartCoroutine(Dash(mousePosition));
        // }
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash){
            StartCoroutine(Dash());
        }
        UpdateBulletCountText();
        UpdateStateText();

        if (chipInRange != null && Input.GetKeyDown(KeyCode.E))
        {
            enableChip(chipInRange.name);
            Destroy(chipInRange);
            chipInRange = null; // Reset after collection
        }

        // Update the player's position in the PlayerDataComponent
        if (entityManager.HasComponent<playerComponent>(playerEntity))
        {
            var playerData = entityManager.GetComponentData<playerComponent>(playerEntity);
            playerData.position = transform.position; // Update position
            entityManager.SetComponentData(playerEntity, playerData); // Set the updated position
        }

        //Start MiniGame
        if (Input.GetKeyDown(KeyCode.V) && crystalInRange != null && WolfBoss.Instance.wolfState==WolfBoss.WolfState.Idel)
        {
            // Disable player controls here (optional)
            Controlable = false;

            // Show your mini-game canvas
            crystalInRange.GetComponent<MiniGameActivate>().StartMinigame();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(TemporaryInvulnerability());
        }

        if (!bossDefeated && Boss == null)
        {
            Debug.Log("Boss defeated, moving to next stage...");
            bossDefeated = true;
            NextStage();
        }
        PostProcessingManager.Instance.UpdateEffect(currenthealth,maxhealth);
    }

    void LateUpdate(){
        // if (change != Vector3.zero && !isDashing && alive){
        //     movePlayer();
        // }
        if (!Controlable)
        {
            return;
        }
        if (isDashing)
        {
            return;
        }
        movePlayer();
    }

    void NextStage()
    {
        Debug.Log("NextStage running");
        Transform NextCollision = BossMap.transform.Find("Grid/CollisionLayer");
        Transform NextCollision2 = BossMap.transform.Find("Grid/CollisionLayer2");
        if (NextCollision != null) NextCollision.gameObject.SetActive(false);
        else 
        {
            Debug.Log("NextCollision not found");
        }
        if (NextCollision2 != null) NextCollision2.gameObject.SetActive(false);
                else 
        {
            Debug.Log("NextCollision2 not found");
        }
    }
    public void SetControl()
    {
        Controlable = !Controlable;
    }

    private void TriggerSpawn()
    {
        // Update SpawnerTriggerComponent
        if (entityManager.HasComponent<SpawnerTriggerComponent>(spawnerEntity))
        {
            var spawnerTriggerData = entityManager.GetComponentData<SpawnerTriggerComponent>(spawnerEntity);

            // Update the SpawnerTriggerComponent's state
            spawnerTriggerData.shouldSpawn = true;
            spawnerTriggerData.spiderSwapnState = SpiderSwapnState.PlayerSpawn;

            // Set the updated data back to the entity
            entityManager.SetComponentData(spawnerEntity, spawnerTriggerData);
            Debug.Log("Spider spawn triggered.");
        }
    }
    void die()
    {
        playerHealthText.text = "Player is dead!";
        alive = false;
        Controlable = false;
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        
    }

    void movePlayer(){
        // change.Normalize();

        // myRigidBody.MovePosition (
        //     transform.position + change * currentSpeed * Time.fixedDeltaTime
        // );
        Vector2 targetVelocity = moveDirection * currentSpeed;

        if (BeingDrag)
        {
            Vector2 currentVelocity = myRigidBody.velocity;
            Vector2 neededForce = (targetVelocity - currentVelocity) * 2f;
            myRigidBody.AddForce(neededForce);
        }
        else
        {
            myRigidBody.velocity = targetVelocity;
        }
    }

    void UpdateAnimationAndMove(){
        if (change != Vector3.zero){
            animator.SetBool("moving",true);
        }
        else {
            animator.SetBool("moving",false);
        }
    }

    void enableChip(string Chipname)
    {
        chipBag.CollectChip(Chipname);
        Debug.Log("Chips collected: " + Chipname);
    }
    void EquipChip(string Chipname)
    {
        chipBag.EquipChip(Chipname);
    }

    public void ApplySlowEffect(float slowDownFactor)
    {
        currentSpeed = speed * slowDownFactor; // Reduce speed by the factor
    }

    public void RemoveSlowEffect()
    {
        currentSpeed = speed; // Reset speed back to normal
    }
    public void SetMiniGame()
    {
        miniGame = true;
    }
    
    void Shoot()
    {
        animator.SetBool("shooting",true);
        animationState = PlayerAnimationState.PlayerShoot;
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.transform.position, shootPoint.transform.rotation);
        
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = shootPoint.transform.right * bulletSpeed; // Shoot in the direction of the shootPoint
        bulletCount--;
        animator.SetBool("shooting",false);
        animationState = PlayerAnimationState.playerIDLE;
    }

    IEnumerator FireGun()
    {
        isFiring = true;
        animator.SetBool("shooting",true);
        animationState = PlayerAnimationState.PlayerShoot;
        while (Input.GetButton("Fire1") && fireGunRemainingTime > 0)
        {
            fireGunRemainingTime -= 0.1f; // Decrease magazine time
            fireGunRemainingTime = Mathf.Max(0, fireGunRemainingTime);
            UpdateBulletCountText();

            Vector3 flameOffset = new Vector3(1f, 0f, 0f);
            shootPoint.transform.localScale = new Vector3(3f,3f,1f);
            GameObject fire = Instantiate(firePrefab, shootPoint.transform.position, shootPoint.transform.rotation,shootPoint.transform);
            // fire.transform.localScale = Vector3.one;

            // Calculate the angle to the mouse position
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;

            // Calculate the firepoint's position based on the player's position and the fixed distance
            
            Vector2 firepointPosition = (Vector2)shootPoint.transform.position + direction * firepointDistance;
            fire.transform.position = firepointPosition;

            fire.transform.localRotation = Quaternion.Euler(0,0,shootPoint.transform.rotation.z -90);
            
            Destroy(fire, 0.2f);

            yield return new WaitForSeconds(0.1f); // Adjust fire rate as needed
        }
        if (fireGunRemainingTime <= 0)
            {
                ReloadGun();
            }
        animator.SetBool("shooting",false);
        animationState = PlayerAnimationState.playerIDLE;
        isFiring = false;
    }

    // IEnumerator FireGun()
    // {
    //     isFiring = true;
    //     animator.SetBool("shooting", true);
    //     animationState = PlayerAnimationState.PlayerShoot;

    //     int fireGunRemainingSeconds = Mathf.CeilToInt(fireGunRemainingTime); // Convert to integer seconds

    //     while (Input.GetButton("Fire1") && fireGunRemainingSeconds > 0)
    //     {
    //         fireGunRemainingSeconds--; // Decrease 1 second per shot
    //         UpdateBulletCountText();

    //         shootPoint.transform.localScale = new Vector3(3f,3f,1f);
    //         GameObject fire = Instantiate(firePrefab, shootPoint.transform.position, shootPoint.transform.rotation, shootPoint.transform);

    //         // Calculate the angle to the mouse position
    //         Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //         Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;

    //         // Calculate firepoint position
    //         Vector2 firepointPosition = (Vector2)shootPoint.transform.position + direction * firepointDistance;
    //         fire.transform.position = firepointPosition;

    //         fire.transform.localRotation = Quaternion.Euler(0, 0, shootPoint.transform.rotation.z - 90);
            
    //         Destroy(fire, 0.2f);

    //         yield return new WaitForSeconds(1f); // Wait for 1 second before next shot
    //     }

    //     if (fireGunRemainingSeconds <= 0)
    //     {
    //         ReloadGun();
    //     }

    //     animator.SetBool("shooting", false);
    //     animationState = PlayerAnimationState.playerIDLE;
    //     isFiring = false;
    // }


    void ReloadGun()
    {
        if (!isReloadingGun && playerState == PlayerState.Gun)
        {
            StartCoroutine(ReloadGuns());
        }
        else
        {
            StartCoroutine(ReloadFire());
        }
    }

    void UpdateBulletCountText()
    {
        if (isReloadingGun && playerState == PlayerState.Gun)
        {
            bulletText.text = "Reloading Your Rifle!";
        }
        else if (!isReloadingGun && playerState == PlayerState.Gun)
        {
            bulletText.text = "Bullets: " + bulletCount + "/" + maxbulletCount;
        }
        else if (!isReloadingFire  && playerState == PlayerState.FireGun){
            bulletText.text = "Time Remain: " + fireGunRemainingTime + "/" +fireGunMagazineTime;
        }
        else if (isReloadingFire && playerState == PlayerState.FireGun){
            bulletText.text = "Reloading Your FireGun!";
        }
        else if (playerState == PlayerState.SpiderGun){
            bulletText.text = "SpiderCount: " + spidercount + "/" + 2;
        }
    }

    void UpdateStateText()
    {
        Debug.Log("Current playerState: " + playerState);
        switch(playerState){
            case PlayerState.Gun:
                gunText.text = "Gun State: Rifle";
                break;
            case PlayerState.FireGun:
                gunText.text = "Gun State: FireGun";
                break;
            case PlayerState.SpiderGun:
                gunText.text = "Gun State: SpiderGun";
                break;
            default:
                gunText.text = "Gun State: Rifle";
                break;
            }
            Debug.Log("Updated Gun State Text to: " + gunText.text);
    }

    IEnumerator ReloadGuns()
    {
        animator.SetBool("reloading",true);
        animationState=PlayerAnimationState.PlayerReload;
        isReloadingGun = true;
        Debug.Log("Reloading...");
        if (playerState == PlayerState.Gun)
            {
                yield return new WaitForSeconds(reloadTime);
                bulletCount = maxbulletCount; // Reset bullet count after reload time
            }
        Debug.Log("Reload complete!");
        isReloadingGun = false;
        animator.SetBool("reloading",false);
        animationState=PlayerAnimationState.playerIDLE;
    }

    IEnumerator ReloadFire()
    {
        animator.SetBool("reloading",true);
        animationState=PlayerAnimationState.PlayerReload;
        isReloadingFire = true;
        Debug.Log("Reloading...");
        if (playerState == PlayerState.FireGun)
            {
                yield return new WaitForSeconds(fireGunReloadTime);
                fireGunRemainingTime = fireGunMagazineTime; // Reset magazine time
            }
        Debug.Log("Reload complete!");
        isReloadingFire = false;
        animator.SetBool("reloading",false);
        animationState=PlayerAnimationState.playerIDLE;
    }
    public GameObject ReturnCrystal()
    {   
        return crystalInRange;
    }


    public void UpdateHealth()
    {
        playerHealthText.text = "Player HP: " + currenthealth + " / " + maxhealth;

        if (currenthealth <= 0)
            {
                die(); // Call the die method if health is 0
            }
    }

    IEnumerator Dash()
    {
        // Prevent dashing again until cooldown is complete
        Time.timeScale = 0.7f;
        canDash = false;
        isDashing = true;

        trailRenderer.emitting = true;

        myRigidBody.velocity = moveDirection * dashSpeed;
            yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        trailRenderer.emitting = false;

        // Wait for the cooldown before allowing another dash
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        Time.timeScale = 1f;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Enemy") || other.CompareTag("Boss")){
            Debug.Log("Player hit an enemy: ");
            Debug.Log("Enter!!!! ");
            enemiesInRange.Add(other);
            if (!isTakingDamage)
        {
            StartCoroutine(DamageOverTime());
        }
        }

        if (other.CompareTag("dect"))
        {
            Transform closedDoor = BossFightMap.transform.Find("closed door");
            Transform collisionWithDoor = BossFightMap.transform.Find("collision withdoor");
            Transform openDoor = BossFightMap.transform.Find("open door");
            Transform collisionWithoutDoor = BossFightMap.transform.Find("collision withoutdoor");

            if (closedDoor != null) closedDoor.gameObject.SetActive(false);
            if (collisionWithDoor != null) collisionWithDoor.gameObject.SetActive(false);
            if (openDoor != null) openDoor.gameObject.SetActive(true);
            if (collisionWithoutDoor != null) collisionWithoutDoor.gameObject.SetActive(true);
        }

        if (other.CompareTag("bossDect"))
        {
            if (spiderBoss != null)
            {
                spiderBoss.SetActive(true);
            }
        }

        if (other.CompareTag("Chips"))
        {
            Debug.Log("Enter " + other.name);
            chipInRange = other.gameObject; // Store the chip object
        }

        if (other.CompareTag("NewStageEntry"))
        {
            Transform closedStageDoor = BossFightMap.transform.Find("Nextstagedoor");
            Transform OpenedStageDoor = BossFightMap.transform.Find("NextstagedoorOpen");
            Transform collisionWithoutDoor = BossFightMap.transform.Find("collision withoutdoor");
            Transform collisionWithDoor = BossFightMap.transform.Find("collision withdoor");

            if (closedStageDoor != null) closedStageDoor.gameObject.SetActive(false);
            if (collisionWithDoor != null) collisionWithDoor.gameObject.SetActive(false);

            if (OpenedStageDoor != null) OpenedStageDoor.gameObject.SetActive(true);
            if (collisionWithoutDoor != null) collisionWithoutDoor.gameObject.SetActive(true);
        }

        if (other.CompareTag("laser"))
        {
            Debug.Log("Player hit an laser: ");
            
            if (!isInvulnerable)
            {
                PostProcessingManager.Instance.HurtEffect();
                currenthealth--;
                UpdateHealth();

                StartCoroutine(TemporaryInvulnerability()); // Add invuln
                if (regenCoroutine != null)
                {
                    StopCoroutine(regenCoroutine);
                    regenCoroutine = null;
                }
            }
            Destroy(other.gameObject);
        }

        if (other.CompareTag("IceSharp"))
        {
            Debug.Log("Player hit an laser: ");
            if (!isInvulnerable)
            {
                Debug.Log("Player hit by IceSharp or laser!");
                PostProcessingManager.Instance.HurtEffect();
                currenthealth--;
                UpdateHealth();

                StartCoroutine(TemporaryInvulnerability()); // Add invuln
                StartCoroutine(EffectApply());             // Apply slow if needed
                if (regenCoroutine != null)
                {
                    StopCoroutine(regenCoroutine);
                    regenCoroutine = null;
                }

                // if (!recentlyDamaged)
                //     {
                //         StartCoroutine(DamageRegenCooldown());
                //     }
            }
            Destroy(other.gameObject);
        }

        if (other.CompareTag("tubes"))
        {
            Tubes tubeScript = other.GetComponent<Tubes>();
            if (tubeScript != null && !miniGame)
            {
                crystalInRange = other.gameObject;
                Debug.Log("Player in Crystal Range: ");
            }
        }
    }
    public void SavePlayerData()
    {
        Debug.Log("caling save player");
        playerData.currentHealth = currenthealth;
        playerData.maxHealth = maxhealth;
        playerData.playerState = playerState;
        
        playerData.collectedChips.Clear();
        foreach (var chip in chipBag.GetCollectedChips())
        {
            playerData.collectedChips.Add(chip);
        }

        playerData.equippedChip = chipBag.GetEquippedChip();
    }

    public void loadScence(string sceneToload){
        // SavePlayerData();
        Debug.Log("Scene Changing");
        SceneManager.LoadScene(sceneToload);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            Debug.Log("Exit!!!! ");
            Debug.Log("Enemy exited range: " + other.name);
            enemiesInRange.Remove(other);

            // Stop the damage coroutine if no enemies are in range
            // if (enemiesInRange.Count == 0)
            // {
            //     Debug.Log("No more spider");
            //     isTakingDamage = false;
            //     StopCoroutine(DamageOverTime());
            // }
        }

        if (other.CompareTag("dect"))
        {
            Transform closedDoor = BossFightMap.transform.Find("closed door");
            Transform collisionWithDoor = BossFightMap.transform.Find("collision withdoor");
            Transform openDoor = BossFightMap.transform.Find("open door");
            Transform collisionWithoutDoor = BossFightMap.transform.Find("collision withoutdoor");

            if (closedDoor != null) closedDoor.gameObject.SetActive(true);
            if (collisionWithDoor != null) collisionWithDoor.gameObject.SetActive(true);
            if (openDoor != null) openDoor.gameObject.SetActive(false);
            if (collisionWithoutDoor != null) collisionWithoutDoor.gameObject.SetActive(false);
        }

        if (other.CompareTag("NewStageEntry"))
        {
            Transform closedStageDoor = BossFightMap.transform.Find("Nextstagedoor");
            Transform OpenedStageDoor = BossFightMap.transform.Find("NextstagedoorOpen");
            Transform collisionWithoutDoor = BossFightMap.transform.Find("collision withoutdoor");
            Transform collisionWithDoor = BossFightMap.transform.Find("collision withdoor");

            if (closedStageDoor != null) closedStageDoor.gameObject.SetActive(true);
            if (collisionWithDoor != null) collisionWithDoor.gameObject.SetActive(true);

            if (OpenedStageDoor != null) OpenedStageDoor.gameObject.SetActive(false);
            if (collisionWithoutDoor != null) collisionWithoutDoor.gameObject.SetActive(false);
        }

        if (other.CompareTag("Chips"))
        {
            chipInRange = null;
        }

        if (other.CompareTag("tubes"))
        {
            crystalInRange = null;
        }
    }
    private IEnumerator EffectApply()
    {
        ApplySlowEffect(0.5f);
        yield return new WaitForSeconds(3f);
        RemoveSlowEffect();
    }

    private IEnumerator DamageOverTime()
    {
        isTakingDamage = true;

        while (enemiesInRange.Count > 0) // Only deal damage if enemies are in range
        {
            if (!isDashing && !isInvulnerable)
            {
                Debug.Log("Player taking damage from enemies in range.");
                PostProcessingManager.Instance.HurtEffect();
                currenthealth--; // Reduce player health
                UpdateHealth(); // Update health UI

                StartCoroutine(TemporaryInvulnerability());
            //     if (!recentlyDamaged)
            //     {
            //         StartCoroutine(DamageRegenCooldown());
            //     }
                if (regenCoroutine != null)
                {
                    StopCoroutine(regenCoroutine);
                    regenCoroutine = null;
                }

            StartCoroutine(DamageRegenCooldown());
            }
            else
            {
                Debug.Log("🛡️ Player is dashing — immune to damage!");
            }
            // if (currenthealth <= 0)
            // {
            //     die(); // Call the die method if health is 0
            //     yield break; // Exit the coroutine
            // }

            yield return new WaitForSeconds(damageInterval); // Wait for the interval before dealing damage again
        }

        isTakingDamage = false;
    }
    public void TakeDamage()
    {
        if (!isInvulnerable)
        {
            Debug.Log("Player hit by something!");
            PostProcessingManager.Instance.HurtEffect();
            currenthealth--;
            UpdateHealth();

            StartCoroutine(TemporaryInvulnerability()); // Add invuln
            // if (!recentlyDamaged)
            //     {
            //         StartCoroutine(DamageRegenCooldown());
            //     }
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
                regenCoroutine = null;
            }

            StartCoroutine(DamageRegenCooldown());
        }
    }

    public IEnumerator TemporaryInvulnerability()
    {
        isInvulnerable = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        Color originalColor = sprite.color;
        float flashInterval = 0.1f;

        for (float t = 0; t < damageInvulnDuration; t += flashInterval)
        {
            sprite.color = new Color(1, 1, 1, 0.5f); // Semi-transparent (flash)
            yield return new WaitForSeconds(flashInterval / 2);
            sprite.color = originalColor;
            yield return new WaitForSeconds(flashInterval / 2);
        }

        sprite.color = originalColor;
        isInvulnerable = false;
    }

    void UpdateFirepoint()
    {
        // Calculate the angle to the mouse position
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;

        // Calculate the firepoint's position based on the player's position and the fixed distance
        Vector2 firepointPosition = (Vector2)transform.position + direction * firepointDistance;
        shootPoint.transform.position = firepointPosition;

        // Update the firepoint's rotation to face the mouse
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        shootPoint.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator RegenerateHealth()
    {
        while (enableRegen)
        {
            yield return new WaitForSeconds(regenInterval);
            // Don't regen if player is dead or already full health
            if (alive && !recentlyDamaged && currenthealth < maxhealth)
            {
                currenthealth = Mathf.Min(currenthealth + regenAmount, maxhealth);
                UpdateHealth(); // Update UI
            }
        }
    }

    // private IEnumerator DamageRegenCooldown()
    // {
    //     recentlyDamaged = true;
        
    //     Debug.Log("123");

    //     // Wait before allowing regen again
    //     yield return new WaitForSeconds(regenCooldownAfterDamage);

    //     Debug.Log("321");
    //     recentlyDamaged = false;
    // }
    private IEnumerator DamageRegenCooldown()
    {
        recentlyDamaged = true;
        Debug.Log("recentlyDamaged = true");
        
        yield return new WaitForSeconds(regenCooldownAfterDamage);
        
        recentlyDamaged = false;
        Debug.Log("recentlyDamaged = false");
            // Restart regen coroutine if not running
        if (enableRegen && regenCoroutine == null && alive && currenthealth < maxhealth)
        {
            regenCoroutine = StartCoroutine(RegenerateHealth());
            Debug.Log("Regen coroutine restarted");
        }
    }

}
