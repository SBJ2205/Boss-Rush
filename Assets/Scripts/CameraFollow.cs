using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Look Ahead")]
    [Tooltip("Camera looks ahead in the direction of movement")]
    public bool enableLookAhead = true;
    [Range(0f, 5f)]
    public float lookAheadDistance = 2f;
    [Range(0.1f, 2f)]
    public float lookAheadSmoothing = 0.5f;
    
    [Header("Camera Bounds")]
    [Tooltip("Enable to constrain camera within specific bounds")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);
    
    [Header("Dead Zone")]
    [Tooltip("Player can move within this zone without camera moving")]
    public bool useDeadZone = false;
    [Range(0f, 5f)]
    public float deadZoneWidth = 1f;
    [Range(0f, 5f)]
    public float deadZoneHeight = 0.5f;
    
    [Header("Zoom Control")]
    public bool enableDynamicZoom = false;
    [Range(1f, 20f)]
    public float baseOrthographicSize = 5f;
    [Range(0f, 5f)]
    public float zoomSpeed = 1f;
    
    [Header("Screen Shake Compatibility")]
    public bool respectCameraShake = true;
    
    [Header("Advanced")]
    [Tooltip("How fast camera catches up when target moves quickly")]
    [Range(0.1f, 3f)]
    public float catchUpMultiplier = 1.5f;
    [Tooltip("Speed threshold to trigger catch-up")]
    public float catchUpThreshold = 10f;
    
    // Private variables
    private Vector3 currentVelocity;
    private Vector2 lookAheadVelocity;
    private Vector2 currentLookAhead;
    private Camera cam;
    private Vector3 lastTargetPosition;
    private Vector3 deadZoneCenter;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (target != null)
        {
            lastTargetPosition = target.position;
            deadZoneCenter = target.position;
        }
        
        if (enableDynamicZoom && cam != null && cam.orthographic)
        {
            cam.orthographicSize = baseOrthographicSize;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Don't update position if camera is shaking
        if (respectCameraShake && CameraShake.IsShaking) return;
        
        // Calculate desired position with all features
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition = ApplyBounds(desiredPosition);
        }
        
        // Smoothly move to desired position
        Vector3 smoothedPosition = SmoothMove(desiredPosition);
        
        // Apply position (preserve Z)
        transform.position = new Vector3(
            smoothedPosition.x,
            smoothedPosition.y,
            offset.z
        );
        
        // Update last position for next frame
        lastTargetPosition = target.position;
    }
    
    Vector3 CalculateDesiredPosition()
    {
        Vector3 targetPos = target.position;
        
        // Apply dead zone
        if (useDeadZone)
        {
            targetPos = ApplyDeadZone(targetPos);
        }
        
        // Calculate look ahead
        Vector2 lookAhead = Vector2.zero;
        if (enableLookAhead)
        {
            lookAhead = CalculateLookAhead();
        }
        
        // Combine target position, offset, and look ahead
        return new Vector3(
            targetPos.x + offset.x + lookAhead.x,
            targetPos.y + offset.y + lookAhead.y,
            offset.z
        );
    }
    
    Vector2 CalculateLookAhead()
    {
        // Calculate target's velocity
        Vector2 targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
        
        // Clamp velocity to prevent extreme values
        targetVelocity = Vector2.ClampMagnitude(targetVelocity, 20f);
        
        // Calculate desired look ahead based on velocity
        Vector2 desiredLookAhead = targetVelocity.normalized * lookAheadDistance;
        
        // Smooth the look ahead
        currentLookAhead = Vector2.SmoothDamp(
            currentLookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothing
        );
        
        return currentLookAhead;
    }
    
    Vector3 ApplyDeadZone(Vector3 targetPos)
    {
        // Calculate offset from dead zone center
        Vector2 offset2D = new Vector2(
            targetPos.x - deadZoneCenter.x,
            targetPos.y - deadZoneCenter.y
        );
        
        // Check if outside dead zone horizontally
        if (Mathf.Abs(offset2D.x) > deadZoneWidth)
        {
            deadZoneCenter.x = targetPos.x - Mathf.Sign(offset2D.x) * deadZoneWidth;
        }
        
        // Check if outside dead zone vertically
        if (Mathf.Abs(offset2D.y) > deadZoneHeight)
        {
            deadZoneCenter.y = targetPos.y - Mathf.Sign(offset2D.y) * deadZoneHeight;
        }
        
        return deadZoneCenter;
    }
    
    Vector3 SmoothMove(Vector3 desiredPosition)
    {
        // Calculate target speed for catch-up mechanic
        float targetSpeed = (target.position - lastTargetPosition).magnitude / Time.deltaTime;
        
        // Use faster smoothing if target is moving quickly
        float adjustedSmoothing = smoothSpeed;
        if (targetSpeed > catchUpThreshold)
        {
            adjustedSmoothing *= catchUpMultiplier;
            adjustedSmoothing = Mathf.Clamp01(adjustedSmoothing);
        }
        
        // Smooth movement
        return Vector3.Lerp(transform.position, desiredPosition, adjustedSmoothing);
    }
    
    Vector3 ApplyBounds(Vector3 position)
    {
        // Get camera dimensions
        float cameraHalfWidth = 0f;
        float cameraHalfHeight = 0f;
        
        if (cam != null && cam.orthographic)
        {
            cameraHalfHeight = cam.orthographicSize;
            cameraHalfWidth = cameraHalfHeight * cam.aspect;
        }
        
        // Clamp position within bounds (accounting for camera size)
        position.x = Mathf.Clamp(
            position.x,
            minBounds.x + cameraHalfWidth,
            maxBounds.x - cameraHalfWidth
        );
        
        position.y = Mathf.Clamp(
            position.y,
            minBounds.y + cameraHalfHeight,
            maxBounds.y - cameraHalfHeight
        );
        
        return position;
    }
    
    // Public methods for external control
    
    /// <summary>
    /// Instantly snap camera to target without smoothing
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        
        Vector3 snapPosition = target.position + offset;
        
        if (useBounds)
        {
            snapPosition = ApplyBounds(snapPosition);
        }
        
        transform.position = new Vector3(
            snapPosition.x,
            snapPosition.y,
            offset.z
        );
        
        deadZoneCenter = target.position;
        lastTargetPosition = target.position;
        currentLookAhead = Vector2.zero;
    }
    
    /// <summary>
    /// Smoothly transition to a new target
    /// </summary>
    public void SetTarget(Transform newTarget, bool snapImmediately = false)
    {
        target = newTarget;
        
        if (snapImmediately && target != null)
        {
            SnapToTarget();
        }
    }
    
    /// <summary>
    /// Add temporary offset to camera (useful for special events)
    /// </summary>
    public void AddTemporaryOffset(Vector3 additionalOffset, float duration)
    {
        StartCoroutine(TemporaryOffsetCoroutine(additionalOffset, duration));
    }
    
    System.Collections.IEnumerator TemporaryOffsetCoroutine(Vector3 additionalOffset, float duration)
    {
        Vector3 originalOffset = offset;
        offset += additionalOffset;
        
        yield return new WaitForSeconds(duration);
        
        offset = originalOffset;
    }
    
    /// <summary>
    /// Set camera bounds at runtime
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }
    
    /// <summary>
    /// Disable camera bounds
    /// </summary>
    public void DisableBounds()
    {
        useBounds = false;
    }
    
    /// <summary>
    /// Smoothly zoom to a new orthographic size
    /// </summary>
    public void ZoomTo(float targetSize, float duration)
    {
        if (cam != null && cam.orthographic)
        {
            StartCoroutine(ZoomCoroutine(targetSize, duration));
        }
    }
    
    System.Collections.IEnumerator ZoomCoroutine(float targetSize, float duration)
    {
        float startSize = cam.orthographicSize;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.orthographicSize = targetSize;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw bounds
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) / 2f,
                (minBounds.y + maxBounds.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                maxBounds.y - minBounds.y,
                0f
            );
            Gizmos.DrawWireCube(center, size);
        }
        
        // Draw dead zone
        if (useDeadZone && Application.isPlaying && target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                deadZoneCenter,
                new Vector3(deadZoneWidth * 2f, deadZoneHeight * 2f, 0f)
            );
        }
        
        // Draw look ahead indicator
        if (enableLookAhead && Application.isPlaying && target != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 lookAheadPos = target.position + new Vector3(currentLookAhead.x, currentLookAhead.y, 0f);
            Gizmos.DrawLine(target.position, lookAheadPos);
            Gizmos.DrawWireSphere(lookAheadPos, 0.3f);
        }
    }
}