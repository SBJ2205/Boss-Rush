using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        if (target == null) return;

        // DON'T update position if camera is shaking
        if (CameraShake.IsShaking) return;

        // 1. Calculate where we want to be
        Vector3 desiredPosition = target.position + offset;
        
        // 2. Smoothly move there (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // 3. Apply position
        transform.position = smoothedPosition;
    }
}