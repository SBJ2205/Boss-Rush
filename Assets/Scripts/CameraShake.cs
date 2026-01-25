using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    public static bool IsShaking { get; private set; } = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void Shake(float duration, float magnitude)
    {
        if (IsShaking)
        {
            StopAllCoroutines();
        }
        
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }
    
    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        IsShaking = true;
        
        CameraFollow cameraFollow = GetComponent<CameraFollow>();
        Vector3 originalPos = transform.position; // Use world position, not local
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            // Apply shake offset to world position
            transform.position = new Vector3(
                originalPos.x + x, 
                originalPos.y + y, 
                originalPos.z
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        IsShaking = false;
        
        // Let CameraFollow take over smoothly
        // Don't restore position - let CameraFollow handle it
    }
}