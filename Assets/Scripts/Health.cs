using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 5;
    public int currentHealth;
    
    [Header("Settings")]
    public bool isPlayer = false; // CHECK THIS TRUE ONLY FOR PLAYER!
    public bool useIframes = true; // Player = YES, Boss = NO (usually)
    
    [Header("I-Frames")]
    public float invincibilityDuration = 1f; 
    private bool isInvincible = false;

    // Blocking (Tank Form)
    public bool isBlocking = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // 1. BLOCK CHECK (Tank Form)
        if (isBlocking) 
        {
            Debug.Log("Blocked!");
            return; 
        }
        
        // 2. I-FRAME CHECK
        if (isInvincible) return;

        // 3. APPLY DAMAGE
        currentHealth -= damage;
        Debug.Log(gameObject.name + " took damage! HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 4. FLASH EFFECT
            StartCoroutine(FlashRoutine());
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " DIED!");
        gameObject.SetActive(false);
        // If it's the Boss, maybe show a "Victory" screen here later?
    }

    IEnumerator FlashRoutine()
    {
        // If we use I-Frames (Player), we become invincible
        if (useIframes) isInvincible = true;

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        
        // --- VISUAL FEEDBACK (RED FLASH) ---
        foreach (SpriteRenderer sr in sprites)
        {
            sr.color = Color.red; // Turn Red instantly
        }

        yield return new WaitForSeconds(0.1f); // Wait 0.1s

        // --- RESTORE COLOR ---
        foreach (SpriteRenderer sr in sprites)
        {
            sr.color = Color.white; // Back to normal
        }

        // --- HANDLE INVINCIBILITY (Player Only) ---
        if (useIframes)
        {
            // Flash transparent while invincible
            float timer = 0;
            while (timer < invincibilityDuration)
            {
                foreach (SpriteRenderer sr in sprites)
                {
                    // Toggle visibility to create a blinking effect
                    sr.enabled = !sr.enabled; 
                }
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            // Make sure sprites are visible at the end
            foreach (SpriteRenderer sr in sprites) sr.enabled = true;
            isInvincible = false;
        }
    }
}