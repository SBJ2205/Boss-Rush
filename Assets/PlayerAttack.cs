using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Stats")]
    public float attackRange = 1.5f;
    public int attackDamage = 1;
    public float attackRate = 0.5f; // How fast can you spam attack?

    [Header("Settings")]
    public LayerMask enemyLayer; // What counts as an enemy?

    private float nextAttackTime = 0f;

    void Update()
    {
        // 1. CHECK INPUT & COOLDOWN
        // We use Time.time to manage the "Spamming" speed
        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    void Attack()
    {
        // 2. DETECT ENEMIES
        // Create a circle around the player and collect everything inside it
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        // 3. DAMAGE THEM
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("We hit " + enemy.name);
            
            // Find the health script on the enemy
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    // 4. VISUALIZE RANGE (Editor Only)
    // This draws a Red Wire Sphere in the scene view so you can see your range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}