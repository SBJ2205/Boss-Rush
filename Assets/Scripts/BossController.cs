using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public float moveSpeed = 3f;
    public float attackRange = 2f; // Stop moving if this close

    private Rigidbody2D rb;
    private Transform playerTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. FIND THE ACTIVE PLAYER
        // We do this often because the player object changes when you switch forms!
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            
            // 2. MOVE TOWARDS PLAYER
            MoveAndFacePlayer();
        }
    }

    void MoveAndFacePlayer()
    {
        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Determine direction (1 is Right, -1 is Left)
        // If player.x > boss.x, player is to the right.
        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);

        // Only move if we are NOT close enough to attack
        if (distanceToPlayer > attackRange)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Stop moving (prepare to attack later)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // 3. FLIP GRAPHIC (Visuals)
        // If moving right (direction 1), scale should be positive. 
        // If moving left (direction -1), scale should be negative.
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, 1);
    }

    // This function runs automatically when the Boss touches something
    void OnCollisionStay2D(Collision2D collision)
    {
        // 1. DID WE HIT THE PLAYER?
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. FIND THE HEALTH SCRIPT
            // Note: The Collider is on the "SwordForm" (Child), 
            // but the Health is on the "PlayerManager" (Parent).
            // We use GetComponentInParent to find it.
            Health playerHealth = collision.gameObject.GetComponentInParent<Health>();

            // 3. DEAL DAMAGE
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1); // Deal 1 damage
                
                // Optional: Knock the player back slightly here (Advanced)
                
            }
        }
    }
}