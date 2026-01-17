using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 5;
    public int currentHealth;
    
    [Header("I-Frames")]
    public float invincibilityDuration = 1f; 
    private bool isInvincible = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public bool isBlocking = false;

    public void TakeDamage(int damage)
    {

        // 1. IF BLOCKING, IGNORE DAMAGE
        if (isBlocking) 
        {
            Debug.Log("Blocked!");
            // Optional: Play a "Clang" sound here later
            return; 
        }
        
        if (isInvincible) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " took damage! HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " DIED!");
        gameObject.SetActive(false);
    }

    IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;

            // 1. FIND THE ACTIVE SPRITE
            SpriteRenderer activeSprite = null;
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer sr in allSprites)
            {
                if (sr.gameObject.activeInHierarchy)
                {
                    activeSprite = sr;
                    break;
                }
            }

            if (activeSprite != null)
            {
                // --- SAVE THE ORIGINAL COLOR (Red or Blue) ---
                Color originalColor = activeSprite.color;

                // 2. FLASH RED (Damage)
                activeSprite.color = Color.red;
                yield return new WaitForSeconds(0.1f);

                // 3. FLASH GREY/TRANSPARENT (Invincible)
                // We use the original color but lower the 'alpha' (transparency) to 0.5
                activeSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                
                yield return new WaitForSeconds(invincibilityDuration);

                // --- RESTORE ORIGINAL COLOR ---
                activeSprite.color = originalColor;
            }
            else
            {
                // Safety wait if no sprite found
                yield return new WaitForSeconds(invincibilityDuration + 0.1f);
            }

            isInvincible = false;
        }
}