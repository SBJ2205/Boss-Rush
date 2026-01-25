using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    
    [Header("Phase Thresholds")]
    public float phase2HealthPercent = 0.66f; // 66% HP
    public float phase3HealthPercent = 0.33f; // 33% HP
    
    [Header("Phase Speed Multipliers")]
    public float phase1SpeedMultiplier = 1f;
    public float phase2SpeedMultiplier = 1.3f;
    public float phase3SpeedMultiplier = 1.6f;
    
    [Header("Attack Settings")]
    public float jumpForce = 15f;   
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float chargeSpeed = 15f;
    public float chargeDuration = 0.5f;
    public float projectileSpeed = 8f;
    public int multiProjectileCount = 5;
    public float projectileSpreadAngle = 45f;
    
    [Header("Cooldowns")]
    public float chargeCooldown = 3f;
    public float projectileCooldown = 4f;
    public float dodgeCooldown = 2f;
    
    [Header("Dodge Settings")]
    public float dodgeDistance = 3f;
    public float dodgeSpeed = 10f;
    
    [Header("Phase Transition")]
    public GameObject roarEffect;
    public float roarDuration = 3f;
    public float invulnerabilityDuration = 2f;
    
    [Header("References")]
    public BossMusicManager musicManager;
    public SpriteRenderer spriteRenderer;
    
    [Header("Aggro Settings")]
    public float aggroRange = 10f;
    public float transformationDuration = 3f;
        
    public static bool IsPlayerFrozen { get; private set; } = false;

    // Private variables
    private Rigidbody2D rb;
    private Transform playerTarget;
    private Health bossHealth;
    
    private int currentPhase = 1;
    private bool isInvulnerable = false;
    private bool isTransitioning = false;
    private bool isCharging = false;
    private bool isDodging = false;
    
    private float currentSpeed;
    private float nextChargeTime = 0f;
    private float nextProjectileTime = 0f;
    private float nextDodgeTime = 0f;
    
    private Color originalColor;

    private bool hasAggro = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<Health>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        currentSpeed = moveSpeed * phase1SpeedMultiplier;
        
        // Subscribe to health changes
        if (bossHealth != null)
            {
                // Force the Health script to use OUR max health value
                bossHealth.SetMaxHealth(maxHealth);
                
                // Subscribe to the event
                bossHealth.OnHealthChanged += CheckPhaseTransition;
        }
    }

    void Update()
    {
        // If boss hasn't been triggered yet, check for aggro
        if (!hasAggro)
        {
            CheckAggro();
            return; // Don't do anything else while idle
        }
        
        if (isTransitioning) return;
        
        // Find active player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            
            if (!isCharging && !isDodging)
            {
                MoveAndFacePlayer();
                DecideNextAction();
            }
        }
    }

    void MoveAndFacePlayer()
    {
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

        // Flip sprite
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, 1);
    }

    void DecideNextAction()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        
        // Charge attack (close to medium range)
        if (Time.time >= nextChargeTime && distanceToPlayer > attackRange && distanceToPlayer < 8f)
        {
            StartCoroutine(ChargeAttack());
            nextChargeTime = Time.time + chargeCooldown;
        }
        // Projectile attack (medium to far range)
        else if (Time.time >= nextProjectileTime && distanceToPlayer > 5f)
        {
            StartCoroutine(ProjectileAttack());
            nextProjectileTime = Time.time + projectileCooldown;
        }
    }

    IEnumerator ChargeAttack()
    {
        isCharging = true;
        
        // Brief pause before charge
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
        
        // Charge toward player
        Vector2 chargeDirection = (playerTarget.position - transform.position).normalized;
        rb.linearVelocity = chargeDirection * chargeSpeed;
        
        yield return new WaitForSeconds(chargeDuration);
        
        // Stop charge
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isCharging = false;
    }

    IEnumerator ProjectileAttack()
        {
            isCharging = true; // Lock the boss state

            // 1. JUMP UP
            rb.linearVelocity = new Vector2(0, jumpForce);
            
            // Wait a bit to reach the peak of the jump (0.5 seconds)
            yield return new WaitForSeconds(0.5f); 

            // 2. HOVER (Freeze in air)
            float defaultGravity = rb.gravityScale; // Remember normal gravity
            rb.gravityScale = 0; // Turn off gravity
            rb.linearVelocity = Vector2.zero; // Stop moving completely

            // 3. SHOOT (Your existing logic)
            int projectileCount = currentPhase == 1 ? 1 : multiProjectileCount;
            
            if (projectileCount == 1)
            {
                FireProjectile(0);
            }
            else
            {
                float startAngle = -projectileSpreadAngle / 2;
                float angleStep = projectileSpreadAngle / (projectileCount - 1);
                
                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = startAngle + (angleStep * i);
                    FireProjectile(angle);
                    yield return new WaitForSeconds(0.1f); // Gap between shots
                }
            }
            
            // Recovery time while still in air
            yield return new WaitForSeconds(0.5f);
            
            // 4. LAND (Drop down)
            rb.gravityScale = defaultGravity; // Restore gravity
            
            // Wait for him to hit the ground (approximate) before attacking again
            yield return new WaitForSeconds(0.5f); 

            isCharging = false; // Unlock state
        }


    IEnumerator FreezePlayer(float duration)
    {
        IsPlayerFrozen = true;
        
        // Disable player input
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


    void FireProjectile(float angleOffset)
    {
        if (projectilePrefab == null) return;
        
        Vector2 direction = (playerTarget.position - transform.position).normalized;
        
        // Apply angle offset
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + angleOffset;
        
        Vector2 finalDirection = new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        );
        
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        
        if (projRb != null)
        {
            projRb.linearVelocity = finalDirection * projectileSpeed;
        }
    }

    public void TryDodge()
    {
        if (Time.time >= nextDodgeTime && !isCharging && !isDodging)
        {
            StartCoroutine(DodgeBack());
            nextDodgeTime = Time.time + dodgeCooldown;
        }
    }

    IEnumerator DodgeBack()
    {
        isDodging = true;
        
        // Dodge away from player
        Vector2 dodgeDirection = (transform.position - playerTarget.position).normalized;
        rb.linearVelocity = dodgeDirection * dodgeSpeed;
        
        yield return new WaitForSeconds(0.3f);
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isDodging = false;
    }

    void CheckPhaseTransition()
    {
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
        
        // Freeze player during transition
        StartCoroutine(FreezePlayer(roarDuration + 1f));
        
        // Camera shake effect
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(roarDuration, 0.3f);
        }
        // Fade out music
        if (musicManager != null)
        {
            musicManager.FadeOutMusic();
        }
        
        yield return new WaitForSeconds(1f);
        
        // Roar/Dialogue
        if (roarEffect != null)
        {
            GameObject roar = Instantiate(roarEffect, transform.position, Quaternion.identity);
            Destroy(roar, roarDuration);
        }
        
        // Visual effect - flash color
        StartCoroutine(FlashColor(Color.red, roarDuration));
        
        yield return new WaitForSeconds(roarDuration);
        
        // Update phase
        currentPhase = newPhase;
        UpdatePhaseStats();
        
        // Resume music with new phase
        if (musicManager != null)
        {
            musicManager.ResumePhaseMusic(currentPhase);
        }
        
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

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Try to dodge when player gets too close
            TryDodge();
            
            // Deal contact damage
            Health playerHealth = collision.gameObject.GetComponentInParent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
                // Add a slightly longer freeze for player getting hurt
                if (HitStop.Instance != null) HitStop.Instance.Stop(0.1f);  
            }
        }
    }

    // Called by Health script when taking damage
    public void OnDamageTaken()
    {
        if (isInvulnerable) return;
        
        // Chance to dodge after being hit
        if (Random.value < 0.3f) // 30% chance
        {
            TryDodge();
        }
    }

    public bool CanTakeDamage()
    {
        return !isInvulnerable && !isTransitioning;
    }

    void CheckAggro()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float distance = Vector2.Distance(transform.position, playerObj.transform.position);
            if (distance <= aggroRange)
            {
                StartCoroutine(InitialTransformation());
            }
        }
    }

    IEnumerator InitialTransformation()
    {
        hasAggro = true;
        isTransitioning = true; // <--- LOCK BOSS LOGIC

        // Freeze player during transformation
        StartCoroutine(FreezePlayer(transformationDuration));
        
        // Camera shake
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(transformationDuration, 0.2f);
        
        // Visual effect (flash yellow during transformation)
        StartCoroutine(FlashColor(Color.yellow, transformationDuration));
        
        yield return new WaitForSeconds(transformationDuration);
        
        isTransitioning = false; // <--- UNLOCK BOSS LOGIC
        // Start phase 1 music
        if (musicManager != null)
            musicManager.PlayPhaseMusic(1);
    }
}