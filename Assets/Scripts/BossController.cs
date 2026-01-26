using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    #region Inspector Variables
    
    [Header("Boss Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    
    [Header("Phase Thresholds")]
    public float phase2HealthPercent = 0.66f;
    public float phase3HealthPercent = 0.33f;
    
    [Header("Phase Speed Multipliers")]
    public float phase1SpeedMultiplier = 1f;
    public float phase2SpeedMultiplier = 1.3f;
    public float phase3SpeedMultiplier = 1.6f;
    
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileSpeed = 8f;
    public int multiProjectileCount = 5;
    public float projectileSpreadAngle = 45f;
    public float jumpForce = 15f;
    
    [Header("Lunge Attack")]
    public float lungeCooldown = 4f;
    public float lungeSpeed = 12f;
    public float lungeDuration = 0.4f;
    public float lungeTelegraphTime = 0.3f;
    
    [Header("Charge Attack")]
    public float chargeCooldown = 5f;
    public float chargeSpeed = 15f;
    public float chargeDuration = 0.5f;
    
    [Header("Ground Slam Attack")]
    public float slamCooldown = 6f;
    public float slamJumpHeight = 8f;
    public float shockwaveRange = 5f;
    public GameObject shockwaveEffect;
    
    [Header("Tail Swipe Attack")]
    public float swipeRange = 2.5f;
    public float swipeKnockback = 5f;
    public GameObject swipeEffect;
    
    [Header("Roar Attack")]
    public float roarCooldown = 10f;
    public float roarStunRange = 6f;
    public float roarStunDuration = 1.5f;
    
    [Header("Predictive Shot")]
    public float predictiveShotCooldown = 5f;
    public float predictiveProjectileSpeed = 12f;
    public float predictionTime = 0.5f;
    
    [Header("Vertical Rain Attack")]
    public float projectileCooldown = 7f;
    public int minRainCount = 5;
    public int maxRainCount = 9;
    public float rainSpawnHeight = 5f;
    public float rainSpread = 8f;

    [Header("Circular Projectile Attack")]
    public float circularProjectileCooldown = 8f;
    public int phase2CircularCount = 5;
    public int phase3CircularCount = 8;
    public float circularOrbitRadius = 2f;
    public float circularOrbitDuration = 1.5f;
    public float circularOrbitSpeed = 180f;
    
    [Header("Dodge Settings")]
    public float dodgeCooldown = 2f;
    public float dodgeDistance = 3f;
    public float dodgeSpeed = 10f;
    
    [Header("Phase Transition")]
    public GameObject roarEffect;
    public float roarDuration = 3f;
    public float invulnerabilityDuration = 2f;
    
    [Header("Aggro Settings")]
    public float aggroRange = 10f;
    public float transformationDuration = 3f;

    [Header("Chase Mode Settings")]
    public float chaseSpeedMultiplier = 1.5f;
    public float chaseDistanceThreshold = 15f;
    public float chaseTimeThreshold = 10f;
    public float chaseDuration = 5f;

    [Header("Combo Attack Settings")]
    public float comboAttackCooldown = 12f; 
    
    [Header("References")]
    public BossMusicManager musicManager;
    public SpriteRenderer spriteRenderer;


    
    #endregion
    
    #region Private Variables
    
    // Static Properties
    public static bool IsPlayerFrozen { get; private set; } = false;
    
    // Components
    private Rigidbody2D rb;
    private Transform playerTarget;
    private Health bossHealth;
    private Color originalColor;
    
    // State
    private int currentPhase = 1;
    private bool hasAggro = false;
    private bool isInvulnerable = false;
    private bool isTransitioning = false;
    private bool isAttacking = false;
    private bool isDodging = false;
    private bool isChasingPlayer = false;
    private float currentSpeed;
    
    // Attack Timers
    private float nextLungeTime = 0f;
    private float nextChargeTime = 0f;
    private float nextSlamTime = 0f;
    private float nextRoarTime = 0f;
    private float nextPredictiveShotTime = 0f;
    private float nextProjectileTime = 0f;
    private float nextDodgeTime = 0f;
    private float nextCircularProjectileTime = 0f;
    private float nextComboTime = 0f;
    private float timePlayerFar = 0f;

    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeComponents();
        InitializeHealth();
    }
    
    void Update()
    {
        if (!hasAggro)
        {
            CheckAggro();
            return;
        }
        
        if (isTransitioning) return;
        
        UpdatePlayerTarget();
        
        // Track player distance for chase mode (Phase 2+)
        if (playerTarget != null && currentPhase >= 2)
        {
            float distance = Vector2.Distance(transform.position, playerTarget.position);
            
            if (distance > chaseDistanceThreshold)
            {
                timePlayerFar += Time.deltaTime;
                
                if (timePlayerFar > chaseTimeThreshold && !isChasingPlayer && !isAttacking)
                {
                    StartCoroutine(ChaseMode());
                }
            }
            else
            {
                timePlayerFar = 0f;
            }
        }
        
        if (!isAttacking && !isDodging && !isChasingPlayer)
        {
            MoveAndFacePlayer();
            DecideNextAction();
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryDodge();
            DealContactDamage(collision.gameObject);
        }
    }
    
    #endregion
    
    #region Initialization
    
    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<Health>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        currentSpeed = moveSpeed * phase1SpeedMultiplier;
    }
    
    void InitializeHealth()
    {
        if (bossHealth != null)
        {
            bossHealth.SetMaxHealth(maxHealth);
            bossHealth.OnHealthChanged += CheckPhaseTransition;
        }
    }
    
    #endregion
    
    #region Movement & Targeting
    
    void UpdatePlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }
    
    void MoveAndFacePlayer()
    {
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);

        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        FlipSprite(direction);
    }
    
    void FlipSprite(float direction)
    {
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * direction, 
            transform.localScale.y, 
            1
        );
    }

    IEnumerator ChaseMode()
    {
        isChasingPlayer = true;
        float elapsed = 0f;
        
        // Visual indicator - flash red
        StartCoroutine(FlashColor(Color.red, chaseDuration));
        
        // Optional: Play angry sound effect
        Debug.Log("Boss is chasing the player!");
        
        while (elapsed < chaseDuration)
        {
            if (playerTarget != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                
                // Stop chasing if close enough
                if (distanceToPlayer < 8f)
                {
                    Debug.Log("Boss caught up to player!");
                    break;
                }
                
                // Move toward player at increased speed
                float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
                rb.linearVelocity = new Vector2(direction * currentSpeed * chaseSpeedMultiplier, rb.linearVelocity.y);
                
                // Flip sprite
                FlipSprite(direction);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isChasingPlayer = false;
        timePlayerFar = 0f;
    }
    
    #endregion
    
    #region Attack Decision System
    
    void DecideNextAction()
    {
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        
        // Phase 1 Attack Priority System
        
        // 1. Roar - Occasional, teaches blocking (any range)
        if (currentPhase == 1 && Time.time >= nextRoarTime && Random.value < 0.15f)
        {
            StartCoroutine(RoarAttack());
            nextRoarTime = Time.time + roarCooldown;
            return;
        }
        
        // 2. Ground Slam - Close range, teaches jumping
        if (Time.time >= nextSlamTime && distanceToPlayer >= 3f && distanceToPlayer <= 6f)
        {
            StartCoroutine(GroundSlamAttack());
            nextSlamTime = Time.time + slamCooldown;
            return;
        }
        
        // 3. Lunge + Tail Swipe - Close-medium range, teaches spacing
        if (Time.time >= nextLungeTime && distanceToPlayer >= 3f && distanceToPlayer <= 7f)
        {
            StartCoroutine(LungeAttack());
            nextLungeTime = Time.time + lungeCooldown;
            return;
        }
        
        // 4. Predictive Shot - Medium range (Phase 1 only)
        if (currentPhase == 1 && Time.time >= nextPredictiveShotTime && 
            distanceToPlayer > 7f && distanceToPlayer <= 10f)
        {
            StartCoroutine(PredictiveShot());
            nextPredictiveShotTime = Time.time + predictiveShotCooldown;
            return;
        }
        
        // 5. Vertical Rain - Far range, teaches blocking
        if (Time.time >= nextProjectileTime && distanceToPlayer > 8f)
        {
            StartCoroutine(VerticalRainAttack());
            nextProjectileTime = Time.time + projectileCooldown;
            return;
        }
        
        // 6. Charge - Medium range fallback
        if (Time.time >= nextChargeTime && distanceToPlayer > 5f && distanceToPlayer < 10f)
        {
            StartCoroutine(ChargeAttack());
            nextChargeTime = Time.time + chargeCooldown;
            return;
        }

        // Phase 2+ Additional Attacks
        if (currentPhase >= 2)
        {
            // Combo Attack (occasional, medium range)
            if (Time.time >= nextComboTime && distanceToPlayer > 4f && distanceToPlayer < 9f && Random.value < 0.25f)
            {
                StartCoroutine(ComboAttack_LungeSlamSwipe());
                nextComboTime = Time.time + comboAttackCooldown;
                return;
            }
            
            // Circular Projectile Attack (far range)
            if (Time.time >= nextCircularProjectileTime && distanceToPlayer > 10f)
            {
                StartCoroutine(CircularProjectileAttack());
                nextCircularProjectileTime = Time.time + circularProjectileCooldown;
                return;
            }
        }
    }
    
    #endregion
    
    #region Attack Implementations
    
    IEnumerator LungeAttack()
    {
        isAttacking = true;
        
        // Telegraph - crouch down
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(
            originalScale.x, 
            originalScale.y * 0.8f, 
            originalScale.z
        );
        
        yield return new WaitForSeconds(lungeTelegraphTime);
        
        // Execute lunge
        Vector2 lungeDirection = (playerTarget.position - transform.position).normalized;
        rb.linearVelocity = lungeDirection * lungeSpeed;
        
        // Restore scale
        transform.localScale = originalScale;
        
        yield return new WaitForSeconds(lungeDuration);
        
        // Stop lunge
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Follow-up tail swipe if player is still close
        if (currentPhase == 1)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer < 4f)
            {
                yield return new WaitForSeconds(0.2f);
                yield return StartCoroutine(TailSwipe());
            }
        }
        
        isAttacking = false;
    }
    
    IEnumerator TailSwipe()
    {
        // Brief windup
        yield return new WaitForSeconds(0.15f);
        
        // Spawn visual effect
        if (swipeEffect != null)
        {
            Vector2 swipePos = (Vector2)transform.position + 
                new Vector2(Mathf.Sign(transform.localScale.x) * 1.5f, 0);
            GameObject effect = Instantiate(swipeEffect, swipePos, Quaternion.identity);
            effect.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1, 1);
            Destroy(effect, 0.3f);
        }
        
        // Check for hit
        Vector2 swipeCenter = (Vector2)transform.position + 
            new Vector2(Mathf.Sign(transform.localScale.x) * 1.5f, 0);
        Collider2D[] hits = Physics2D.OverlapCircleAll(swipeCenter, swipeRange);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                DamagePlayer(hit.gameObject, 1);
                ApplyKnockback(hit.gameObject, swipeKnockback);
                
                if (HitStop.Instance != null) 
                    HitStop.Instance.Stop(0.08f);
            }
        }
        
        yield return new WaitForSeconds(0.3f);
    }
    
    IEnumerator GroundSlamAttack()
    {
        isAttacking = true;
        
        // Telegraph - crouch
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(
            originalScale.x, 
            originalScale.y * 0.7f, 
            originalScale.z
        );
        
        yield return new WaitForSeconds(0.4f);
        
        // Jump up
        transform.localScale = originalScale;
        rb.linearVelocity = new Vector2(0, slamJumpHeight);
        
        yield return new WaitForSeconds(0.5f);
        
        // Force slam down
        rb.linearVelocity = new Vector2(0, -slamJumpHeight * 1.5f);
        
        // Wait until boss hits ground
        yield return new WaitUntil(() => Mathf.Abs(rb.linearVelocity.y) < 0.1f);
        
        // Impact effects
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.3f, 0.2f);
        
        // Spawn shockwave visual
        if (shockwaveEffect != null)
        {
            GameObject wave = Instantiate(shockwaveEffect, transform.position, Quaternion.identity);
            Destroy(wave, 1f);
        }
        
        // Damage grounded players in range
        DamageGroundedPlayersInRange(shockwaveRange);
        
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
    }

    IEnumerator ComboAttack_LungeSlamSwipe()
    {
        isAttacking = true;
        
        // Attack 1: Lunge
        yield return StartCoroutine(LungeAttack_Internal());
        
        yield return new WaitForSeconds(0.3f);
        
        // Attack 2: Ground Slam
        yield return StartCoroutine(GroundSlamAttack_Internal());
        
        yield return new WaitForSeconds(0.2f);
        
        // Attack 3: Tail Swipe (if player is close enough)
        if (playerTarget != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer < 5f)
            {
                yield return StartCoroutine(TailSwipe());
            }
        }
        
        isAttacking = false;
    }

    // Internal version of Lunge (doesn't manage isAttacking)
    IEnumerator LungeAttack_Internal()
    {
        if (playerTarget == null) yield break;
        
        // Telegraph - crouch
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.8f, originalScale.z);
        
        yield return new WaitForSeconds(lungeTelegraphTime);
        
        // Execute lunge
        Vector2 lungeDirection = (playerTarget.position - transform.position).normalized;
        rb.linearVelocity = lungeDirection * lungeSpeed;
        
        // Restore scale
        transform.localScale = originalScale;
        
        yield return new WaitForSeconds(lungeDuration);
        
        // Stop lunge
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // Internal version of Ground Slam (doesn't manage isAttacking)
    IEnumerator GroundSlamAttack_Internal()
    {
        // Telegraph - crouch
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.7f, originalScale.z);
        
        yield return new WaitForSeconds(0.4f);
        
        // Jump up
        transform.localScale = originalScale;
        rb.linearVelocity = new Vector2(0, slamJumpHeight);
        
        yield return new WaitForSeconds(0.5f);
        
        // Force slam down
        rb.linearVelocity = new Vector2(0, -slamJumpHeight * 1.5f);
        
        // Wait until boss hits ground
        yield return new WaitUntil(() => Mathf.Abs(rb.linearVelocity.y) < 0.1f);
        
        // Impact effects
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.3f, 0.2f);
        
        // Spawn shockwave visual
        if (shockwaveEffect != null)
        {
            GameObject wave = Instantiate(shockwaveEffect, transform.position, Quaternion.identity);
            Destroy(wave, 1f);
        }
        
        // Damage grounded players
        DamageGroundedPlayersInRange(shockwaveRange);
        
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
    }
    
    IEnumerator RoarAttack()
    {
        isAttacking = true;
        
        // Visual buildup
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColor(Color.yellow, 0.5f));
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Roar effect
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.5f, 0.15f);
        
        // Check if player is in range and not blocking
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer <= roarStunRange)
        {
            Health playerHealth = GetPlayerHealth();
            
            if (playerHealth != null && !playerHealth.isBlocking)
            {
                StartCoroutine(StunPlayer(roarStunDuration));
            }
        }
        
        yield return new WaitForSeconds(1f);
        isAttacking = false;
    }
    
    IEnumerator PredictiveShot()
    {
        isAttacking = true;
        
        // Calculate prediction
        Vector2 targetPos = PredictPlayerPosition();
        Vector2 aimDirection = (targetPos - (Vector2)transform.position).normalized;
        
        // Telegraph - flash cyan
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.cyan;
            yield return new WaitForSeconds(0.3f);
            spriteRenderer.color = originalColor;
        }
        
        // Fire projectile
        FirePredictiveProjectile(aimDirection);
        
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }
    
    IEnumerator VerticalRainAttack()
    {
        isAttacking = true;

        // Jump up
        rb.linearVelocity = new Vector2(0, jumpForce);
        yield return new WaitForSeconds(0.5f);

        // Hover in air
        float defaultGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        // Spawn rain projectiles
        int rainCount = Random.Range(minRainCount, maxRainCount);
        float startX = playerTarget.position.x - (rainSpread / 2f);
        float gap = rainSpread / (rainCount - 1);

        for (int i = 0; i < rainCount; i++)
        {
            float xPos = startX + (i * gap) + Random.Range(-0.5f, 0.5f);
            Vector2 spawnPos = new Vector2(xPos, transform.position.y + rainSpawnHeight);

            SpawnVerticalProjectile(spawnPos);
            
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(1f);

        // Land
        rb.gravityScale = defaultGravity;
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }

    IEnumerator CircularProjectileAttack()
    {
        isAttacking = true;

        // Jump and hover
        rb.linearVelocity = new Vector2(0, jumpForce);
        yield return new WaitForSeconds(0.5f);

        float defaultGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        // Determine projectile count based on phase
        int projCount = currentPhase == 2 ? phase2CircularCount : phase3CircularCount;
        GameObject[] chargedProjs = new GameObject[projCount];
        
        // PHASE 1: Spawn projectiles in circle around boss
        for (int i = 0; i < projCount; i++)
        {
            float angle = (360f / projCount) * i;
            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * circularOrbitRadius;
            
            Vector2 spawnPos = (Vector2)transform.position + offset;
            chargedProjs[i] = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            
            // Stop movement while charging
            Rigidbody2D projRb = chargedProjs[i].GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                projRb.linearVelocity = Vector2.zero;
                projRb.gravityScale = 0; // Don't fall while orbiting
            }
            
            // Visual charge effect (yellow glow)
            SpriteRenderer projSprite = chargedProjs[i].GetComponent<SpriteRenderer>();
            if (projSprite != null)
            {
                projSprite.color = Color.yellow;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // PHASE 2: Orbit around boss for telegraph
        float elapsed = 0f;
        
        while (elapsed < circularOrbitDuration)
        {
            for (int i = 0; i < chargedProjs.Length; i++)
            {
                if (chargedProjs[i] != null)
                {
                    float angle = ((360f / projCount) * i) + (circularOrbitSpeed * elapsed);
                    Vector2 offset = new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad)
                    ) * circularOrbitRadius;
                    
                    chargedProjs[i].transform.position = (Vector2)transform.position + offset;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // PHASE 3: Fire all projectiles at once
        for (int i = 0; i < chargedProjs.Length; i++)
        {
            if (chargedProjs[i] != null)
            {
                // Direction = away from boss
                Vector2 direction = (chargedProjs[i].transform.position - transform.position).normalized;
                
                Rigidbody2D projRb = chargedProjs[i].GetComponent<Rigidbody2D>();
                if (projRb != null)
                {
                    projRb.gravityScale = 1; // Re-enable gravity
                    projRb.linearVelocity = direction * projectileSpeed;
                }
                
                // Change color back to normal
                SpriteRenderer projSprite = chargedProjs[i].GetComponent<SpriteRenderer>();
                if (projSprite != null)
                {
                    projSprite.color = Color.white;
                }
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Land
        rb.gravityScale = defaultGravity;
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }
    
    IEnumerator ChargeAttack()
    {
        isAttacking = true;
        
        // Brief pause
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
        
        // Charge toward player
        Vector2 chargeDirection = (playerTarget.position - transform.position).normalized;
        rb.linearVelocity = chargeDirection * chargeSpeed;
        
        yield return new WaitForSeconds(chargeDuration);
        
        // Stop charge
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isAttacking = false;
    }
    
    #endregion
    
    #region Projectile Spawning
    
    void SpawnVerticalProjectile(Vector2 position)
    {
        if (projectilePrefab == null) return;
        
        GameObject proj = Instantiate(projectilePrefab, position, Quaternion.Euler(0, 0, -90));
        
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = Vector2.down * projectileSpeed;
        }
        
        ProjectileProperties props = proj.GetComponent<ProjectileProperties>();
        if (props == null) props = proj.AddComponent<ProjectileProperties>();
        props.isBlockable = true;
        props.isVertical = true;
        
        // Visual tint
        SpriteRenderer projSprite = proj.GetComponent<SpriteRenderer>();
        if (projSprite != null)
        {
            projSprite.color = new Color(1f, 0.5f, 0.5f);
        }
    }
    
    void FirePredictiveProjectile(Vector2 direction)
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;
        
        GameObject proj = Instantiate(
            projectilePrefab, 
            projectileSpawnPoint.position, 
            Quaternion.identity
        );
        
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = direction * predictiveProjectileSpeed;
        }
        
        SpriteRenderer projSprite = proj.GetComponent<SpriteRenderer>();
        if (projSprite != null)
        {
            projSprite.color = Color.cyan;
        }
    }
    
    #endregion
    
    #region Utility Functions
    
    Vector2 PredictPlayerPosition()
    {
        Vector2 targetPos = playerTarget.position;
        
        Rigidbody2D playerRb = playerTarget.GetComponent<Rigidbody2D>();
        if (playerRb == null) 
            playerRb = playerTarget.GetComponentInParent<Rigidbody2D>();
        
        if (playerRb != null)
        {
            targetPos += playerRb.linearVelocity * predictionTime;
        }
        
        return targetPos;
    }
    
    void DamageGroundedPlayersInRange(float range)
    {
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        
        if (distanceToPlayer <= range)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            
            // Check if player is on ground
            if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.y) < 0.5f)
            {
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(1);
                    if (HitStop.Instance != null) 
                        HitStop.Instance.Stop(0.1f);
                }
            }
        }
    }
    
    void DamagePlayer(GameObject playerObj, int damage)
    {
        Health playerHealth = playerObj.GetComponent<Health>();
        if (playerHealth == null)
            playerHealth = playerObj.GetComponentInParent<Health>();
        
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
    
    void ApplyKnockback(GameObject playerObj, float force)
    {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb == null)
            playerRb = playerObj.GetComponentInParent<Rigidbody2D>();
        
        if (playerRb != null)
        {
            Vector2 knockbackDir = (playerObj.transform.position - transform.position).normalized;
            playerRb.linearVelocity = knockbackDir * force;
        }
    }
    
    Health GetPlayerHealth()
    {
        if (playerTarget == null) return null;
        
        Health health = playerTarget.GetComponent<Health>();
        if (health == null)
            health = playerTarget.GetComponentInParent<Health>();
        
        return health;
    }
    
    void DealContactDamage(GameObject playerObj)
    {
        Health playerHealth = playerObj.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
            if (HitStop.Instance != null) 
                HitStop.Instance.Stop(0.1f);
        }
    }
    
    #endregion
    
    #region Dodge & Defensive
    
    public void TryDodge()
    {
        if (Time.time >= nextDodgeTime && !isAttacking && !isDodging)
        {
            StartCoroutine(DodgeBack());
            nextDodgeTime = Time.time + dodgeCooldown;
        }
    }
    
    IEnumerator DodgeBack()
    {
        isDodging = true;
        
        Vector2 dodgeDirection = (transform.position - playerTarget.position).normalized;
        rb.linearVelocity = dodgeDirection * dodgeSpeed;
        
        yield return new WaitForSeconds(0.3f);
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isDodging = false;
    }
    
    public void OnDamageTaken()
    {
        if (isInvulnerable) return;
        
        if (Random.value < 0.3f)
        {
            TryDodge();
        }
    }
    
    #endregion
    
    #region Player Control
    
    IEnumerator FreezePlayer(float duration)
    {
        IsPlayerFrozen = true;
        
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        yield return new WaitForSeconds(duration);
        
        if (player != null)
        {
            player.enabled = true;
        }
        
        IsPlayerFrozen = false;
    }
    
    IEnumerator StunPlayer(float duration)
    {
        IsPlayerFrozen = true;
        
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) yield break;
        
        player.enabled = false;
        
        // Visual indicator
        SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = Color.yellow;
            }
            yield return new WaitForSeconds(0.1f);
            
            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = Color.white;
            }
            yield return new WaitForSeconds(0.1f);
            
            elapsed += 0.2f;
        }
        
        // Restore
        foreach (SpriteRenderer sr in sprites)
        {
            if (sr != null) sr.color = Color.white;
        }
        
        player.enabled = true;
        IsPlayerFrozen = false;
    }
    
    #endregion
    
    #region Phase Management
    
    void CheckAggro()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;
        
        float distance = Vector2.Distance(transform.position, playerObj.transform.position);
        if (distance <= aggroRange)
        {
            StartCoroutine(InitialTransformation());
        }
    }
    
    IEnumerator InitialTransformation()
    {
        hasAggro = true;
        isTransitioning = true;
        
        StartCoroutine(FreezePlayer(transformationDuration));
        
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(transformationDuration, 0.2f);
        
        StartCoroutine(FlashColor(Color.yellow, transformationDuration));
        
        yield return new WaitForSeconds(transformationDuration);
        
        if (musicManager != null)
            musicManager.PlayPhaseMusic(1);
        
        isTransitioning = false;
    }
    
    void CheckPhaseTransition()
    {
        if (bossHealth == null) return;
        
        float healthPercent = (float)bossHealth.currentHealth / maxHealth;
        
        if (currentPhase == 1 && healthPercent <= phase2HealthPercent)
        {
            StartCoroutine(TransitionToPhase(2));
        }
        else if (currentPhase == 2 && healthPercent <= phase3HealthPercent)
        {
            StartCoroutine(TransitionToPhase(3));
        }
    }
    
    IEnumerator TransitionToPhase(int newPhase)
    {
        isTransitioning = true;
        isInvulnerable = true;
        
        rb.linearVelocity = Vector2.zero;
        
        StartCoroutine(FreezePlayer(roarDuration + 1f));
        
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(roarDuration, 0.3f);
        
        if (musicManager != null)
            musicManager.FadeOutMusic();
        
        yield return new WaitForSeconds(1f);
        
        if (roarEffect != null)
        {
            GameObject roar = Instantiate(roarEffect, transform.position, Quaternion.identity);
            Destroy(roar, roarDuration);
        }
        
        StartCoroutine(FlashColor(Color.red, roarDuration));
        
        yield return new WaitForSeconds(roarDuration);
        
        currentPhase = newPhase;
        UpdatePhaseStats();
        
        if (musicManager != null)
            musicManager.ResumePhaseMusic(currentPhase);
        
        yield return new WaitForSeconds(invulnerabilityDuration);
        
        isInvulnerable = false;
        isTransitioning = false;
    }
    
    void UpdatePhaseStats()
    {
        switch (currentPhase)
        {
            case 1:
                currentSpeed = moveSpeed * phase1SpeedMultiplier;
                break;
            case 2:
                currentSpeed = moveSpeed * phase2SpeedMultiplier;
                chargeCooldown *= 0.8f;
                projectileCooldown *= 0.8f;
                break;
            case 3:
                currentSpeed = moveSpeed * phase3SpeedMultiplier;
                chargeCooldown *= 0.6f;
                projectileCooldown *= 0.6f;
                chargeSpeed *= 1.2f;
                break;
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }
        
        spriteRenderer.color = originalColor;
    }
    
    #endregion
    
    #region Public Interface
    
    public bool CanTakeDamage()
    {
        return !isInvulnerable && !isTransitioning;
    }
    
    #endregion
}