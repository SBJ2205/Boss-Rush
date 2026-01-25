using UnityEngine;

public class BossMusicManager : MonoBehaviour
{
    [Header("Music Setup")]
    public AudioSource musicSource;
    public AudioClip bossMusicClip; // Your full 5-minute track
    
    [Header("Phase Timings (in seconds)")]
    public float phase1Duration = 120f; // 2 minutes
    public float phase2Duration = 60f;  // 1 minute
    public float phase3Duration = 120f; // 2 minutes
    
    [Header("Transition Settings")]
    public float fadeOutDuration = 2f;
    public float fadeInDuration = 1f;
    
    // Track music state
    private int currentPhase = 1;
    private float phase1StartTime = 0f;
    private float phase2StartTime = 120f;
    private float phase3StartTime = 180f;
    
    private bool isFading = false;
    private float fadeTimer = 0f;
    private float targetVolume = 1f;
    private float originalVolume = 1f;

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
        
        originalVolume = musicSource.volume;
        
        // Start with Phase 1 music
        PlayPhaseMusic(1);
    }

    void Update()
    {
        // Handle music fading
        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float progress = fadeTimer / fadeOutDuration;
            musicSource.volume = Mathf.Lerp(originalVolume, targetVolume, progress);
            
            if (progress >= 1f)
            {
                isFading = false;
                
                // If fading out completely (phase transition)
                if (targetVolume == 0f)
                {
                    musicSource.Pause();
                }
            }
        }
        
        // Check if phase music has ended and needs looping
        if (musicSource.isPlaying)
        {
            float currentTime = musicSource.time;
            
            // Phase 1 loop check
            if (currentPhase == 1 && currentTime >= phase1Duration)
            {
                musicSource.time = phase1StartTime; // Loop back to start
            }
            // Phase 2 loop check
            else if (currentPhase == 2 && currentTime >= phase2StartTime + phase2Duration)
            {
                musicSource.time = phase2StartTime; // Loop Phase 2
            }
            // Phase 3 loop check
            else if (currentPhase == 3 && currentTime >= phase3StartTime + phase3Duration)
            {
                musicSource.time = phase3StartTime; // Loop Phase 3
            }
        }
    }

    // Called by BossController when phase changes
    public void TransitionToPhase(int newPhase)
    {
        if (newPhase == currentPhase) return;
        
        currentPhase = newPhase;
        
        // Fade out current music
        FadeOutMusic();
    }

    public void PlayPhaseMusic(int phase)
    {
        currentPhase = phase;
        
        if (bossMusicClip == null) return;
        
        musicSource.clip = bossMusicClip;
        
        // Set starting point based on phase
        switch (phase)
        {
            case 1:
                musicSource.time = phase1StartTime;
                break;
            case 2:
                musicSource.time = phase2StartTime;
                break;
            case 3:
                musicSource.time = phase3StartTime;
                break;
        }
        
        musicSource.Play();
        FadeInMusic();
    }

    public void FadeOutMusic()
    {
        isFading = true;
        fadeTimer = 0f;
        targetVolume = 0f;
        fadeOutDuration = 2f;
    }

    public void FadeInMusic()
    {
        musicSource.volume = 0f;
        isFading = true;
        fadeTimer = 0f;
        targetVolume = originalVolume;
        fadeOutDuration = fadeInDuration;
    }

    // Called after phase transition/roar is complete
    public void ResumePhaseMusic(int phase)
    {
        PlayPhaseMusic(phase);
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
        FadeInMusic();
    }
}