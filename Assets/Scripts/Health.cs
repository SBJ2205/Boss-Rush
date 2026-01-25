using UnityEngine;
using System.Collections;
using System; // Needed for Actions

public class Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100; // Increased default
    public int currentHealth;
    
    // EVENT: Tells other scripts when HP changes
    public Action OnHealthChanged;

    [Header("Settings")]
    public bool isPlayer = false; 
    public bool useIframes = true; 
    
    [Header("I-Frames")]
    public float invincibilityDuration = 1f; 
    private bool isInvincible = false;

    public bool isBlocking = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Allow other scripts (BossController) to set health
    public void SetMaxHealth(int amount)
    {
        maxHealth = amount;
        currentHealth = amount;
        // Update UI or Logic immediately
        OnHealthChanged?.Invoke(); 
    }

    public void TakeDamage(int damage)
    {
        // 1. BLOCK CHECK
        if (isBlocking) return;
        
        // 2. I-FRAME CHECK
        if (isInvincible) return;

        // 3. BOSS CHECK (Don't take damage if transitioning phases)
        BossController boss = GetComponent<BossController>();
        if (boss != null && !boss.CanTakeDamage()) return;

        // 4. APPLY DAMAGE
        currentHealth -= damage;
        Debug.Log(gameObject.name + " took damage! HP: " + currentHealth);

        // --- FIX 2: NOTIFY LISTENERS ---
        OnHealthChanged?.Invoke(); 
        
        // --- FIX 3: TRIGGER DODGE CHANCE ---
        if (boss != null) boss.OnDamageTaken();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(FlashRoutine());
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " DIED!");
        gameObject.SetActive(false);
    }

    IEnumerator FlashRoutine()
    {
        if (useIframes) isInvincible = true;
        // Only do this for the Player! We don't want the Boss falling through the floor.
        if (isPlayer)
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        }

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        
        // Red Flash
        foreach (SpriteRenderer sr in sprites)
        {
            if (sr != null) sr.color = Color.red; 
        }

        yield return new WaitForSeconds(0.1f);

        // Restore Color
        foreach (SpriteRenderer sr in sprites)
        {
            if (sr != null) 
            {
                // Simple check to keep Tank blue
                if (gameObject.name.Contains("Tank")) sr.color = Color.blue;
                else sr.color = Color.white;
            }
        }

        // Invincibility Blinking
        if (useIframes)
        {
            float timer = 0;
            while (timer < invincibilityDuration)
            {
                foreach (SpriteRenderer sr in sprites)
                {
                    if (sr != null) sr.enabled = !sr.enabled;
                }
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            // Ensure visible at end
            foreach (SpriteRenderer sr in sprites) if (sr != null) sr.enabled = true;
            isInvincible = false;

            // If you are still inside the boss, Physics will register it immediately
            if (isPlayer)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
            }
        }
    }
}