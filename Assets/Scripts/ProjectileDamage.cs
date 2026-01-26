using UnityEngine;

public class ProjectileDamage : MonoBehaviour 
{
    void OnTriggerEnter2D(Collider2D hitInfo) 
    {
        // 1. Check if we hit the GROUND
        // Make sure your Floor object has the tag "Ground"
        if (hitInfo.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return; // Stop running code so we don't try to damage the floor
        }

        // 2. Check if we hit the PLAYER
        Health player = hitInfo.GetComponent<Health>();
        if (player == null) player = hitInfo.GetComponentInParent<Health>();

        if (player != null && player.isPlayer) 
        {
            // Check if the bullet is blockable (requires the ProjectileProperties script)
            ProjectileProperties props = GetComponent<ProjectileProperties>();
            
            // If the player is blocking and the projectile is blockable, don't deal damage!
            if (player.isBlocking && props != null && props.isBlockable)
            {
                Destroy(gameObject); // Bullet hits shield and breaks
                return;
            }

            player.TakeDamage(1);
            if (HitStop.Instance != null) HitStop.Instance.Stop(0.1f); 
            Destroy(gameObject); // Destroy bullet
        }
    }
}