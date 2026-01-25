using UnityEngine;
public class ProjectileDamage : MonoBehaviour {
    void OnTriggerEnter2D(Collider2D hitInfo) {
        Health player = hitInfo.GetComponent<Health>(); // Checks parent too?
        if (player == null) player = hitInfo.GetComponentInParent<Health>();

        if (player != null && player.isPlayer) {
            player.TakeDamage(1);
            if (HitStop.Instance != null) HitStop.Instance.Stop(0.1f); 
            Destroy(gameObject); // Destroy bullet
        }
    }
}