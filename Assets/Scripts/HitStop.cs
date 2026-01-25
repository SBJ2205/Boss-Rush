using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    // Singleton: Allows us to call HitStop.Stop() from anywhere
    public static HitStop Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Stop(float duration)
    {
        // If we are already frozen, don't start a new routine (or you could extend it)
        if (Time.timeScale < 1f) return;
        
        StartCoroutine(DoHitStop(duration));
    }

    IEnumerator DoHitStop(float duration)
    {
        // 1. Freeze Time
        Time.timeScale = 0f;

        // 2. Wait (Must use Realtime because normal time is stopped!)
        yield return new WaitForSecondsRealtime(duration);

        // 3. Unfreeze Time
        Time.timeScale = 1f;
    }
}