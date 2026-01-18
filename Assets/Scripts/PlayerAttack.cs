using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Stats")]
    public float attackRange = 1.5f;
    public int attackDamage = 1;
    public float attackRate = 0.5f;

    [Header("Visuals")]
    public GameObject slashPrefab;

    [Header("Settings")]
    public LayerMask enemyLayer;
    
    [Header("Directional Attack")]
    public float attackWidth = 1f;
    public float attackOffset = 0.5f;
    
    [Header("Attack Mechanics")]
    public float horizontalKnockback = 10f; // INCREASED THIS (3 was too small)
    public float knockbackDuration = 0.2f;  // How long controls are locked
    public float pogoJumpForce = 10f; 

    private float nextAttackTime = 0f;
    private Vector2 lastAttackDirection = Vector2.right;
    
    // Reference to the controller script
    private PlayerController playerController;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Find the controller on the parent object
        playerController = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttackTime)
        {
            Vector2 attackDirection = Vector2.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                attackDirection = Vector2.up;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                attackDirection = Vector2.down;
            else
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                attackDirection = new Vector2(facingDir, 0);
            }
            
            Attack(attackDirection);
            lastAttackDirection = attackDirection;
            nextAttackTime = Time.time + attackRate;
        }
    }

    void Attack(Vector2 direction)
    {
        Vector2 attackPosition = (Vector2)transform.position + (direction * attackOffset);
        
        // Spawn Slash
        if (slashPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            GameObject slash = Instantiate(slashPrefab, attackPosition, rotation, transform);
            Destroy(slash, 0.2f);
        }

        // Calculate Hitbox
        Vector2 boxSize;
        if (Mathf.Abs(direction.x) > 0)
            boxSize = new Vector2(attackRange, attackWidth);
        else 
            boxSize = new Vector2(attackWidth, attackRange);
        
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPosition, boxSize, 0f, enemyLayer);
        bool hitSomething = hitEnemies.Length > 0;
        
        foreach (Collider2D enemy in hitEnemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
        
        // --- 4. APPLY MECHANICS (UPDATED) ---
        
        // Horizontal Knockback (Call the Controller!)
        if (Mathf.Abs(direction.x) > 0 && playerController != null && hitSomething)
        {
            Vector2 knockbackDir = -direction; // Push opposite to attack
            // Call the function we just made in PlayerController
            playerController.ApplyKnockback(knockbackDir * horizontalKnockback, knockbackDuration);
        }
        
        // Downward Pogo (Still works fine on RB directly)
        if (direction.y < 0 && hitSomething && rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoJumpForce);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        DrawAttackGizmo(Vector2.up);
        DrawAttackGizmo(Vector2.down);
        DrawAttackGizmo(Vector2.left);
        DrawAttackGizmo(Vector2.right);
        Gizmos.color = Color.yellow;
        DrawAttackGizmo(lastAttackDirection);
    }
    
    void DrawAttackGizmo(Vector2 direction)
    {
        Vector2 attackPosition = (Vector2)transform.position + (direction * attackOffset);
        Vector2 boxSize = (Mathf.Abs(direction.x) > 0) ? 
            new Vector2(attackRange, attackWidth) : new Vector2(attackWidth, attackRange);
        Gizmos.DrawWireCube(attackPosition, boxSize);
    }
}