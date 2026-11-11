// ============================================================
// FILE: AudioManager.cs
// PURPOSE: Manages all audio in the VR phobia system.
//          Plays ambient classroom sounds, UI sounds, and
//          session-related audio feedback.
//
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 21 June 2026
//
// USAGE:
//   1. Attach to GameManager GameObject
//   2. Assign audio clips in Inspector
//   3. Ambient sound plays automatically when a session starts
// ============================================================

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ========================================
    // INSPECTOR VARIABLES
    // ========================================

    [Header("Ambient Audio")]
    [Tooltip("Background classroom ambience sound (loops during session)")]
    public AudioClip classroomAmbience;

    [Tooltip("Volume of ambient sound (0-1)")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.3f;

    [Header("UI Sound Effects")]
    [Tooltip("Sound when clicking a button")]
    public AudioClip buttonClickSound;

    [Tooltip("Sound when a session starts")]
    public AudioClip sessionStartSound;

    [Tooltip("Sound when changing levels")]
    public AudioClip levelChangeSound;

    [Tooltip("Volume of UI sounds (0-1)")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;

    [Header("Manager References")]
    [Tooltip("Reference to SessionManager to detect session events")]
    public SessionManager sessionManager;

    [Tooltip("Reference to LevelManager to detect level changes")]
    public LevelManager levelManager;

    [Header("Debug")]
    [Tooltip("Show ambient sound start/stop messages in console")]
    public bool debugMode = false;

    // ========================================
    // PRIVATE VARIABLES
    // ========================================

    private AudioSource ambientSource;
    private AudioSource sfxSource;
    private bool wasSessionActive = false;
    private int lastLevel = -1;

    // ========================================
    // UNITY LIFECYCLE
    // ========================================

    private void Start()
    {
        // Create AudioSource for ambient sounds (loops)
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = classroomAmbience;
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f; // 2D sound (no spatial positioning)

        // Create AudioSource for SFX (one-shot sounds)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        // Try to find managers if not assigned
        if (sessionManager == null)
        {
            sessionManager = FindObjectOfType<SessionManager>();
        }
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        Debug.Log("AudioManager: Initialized");
    }

    private void Update()
    {
        if (sessionManager == null) return;

        // Detect session start
        if (sessionManager.IsSessionActive && !wasSessionActive)
        {
            OnSessionStarted();
        }

        // Detect session end
        if (!sessionManager.IsSessionActive && wasSessionActive)
        {
            OnSessionEnded();
        }

        // Detect level change
        if (levelManager != null && sessionManager.IsSessionActive)
        {
            int currentLevel = levelManager.GetCurrentLevel();
            if (currentLevel != lastLevel && lastLevel != -1)
            {
                OnLevelChanged();
            }
            lastLevel = currentLevel;
        }

        // Keep the ambient sound in sync with the current situation,
        // checked every frame. This is what makes Level 1 silent even
        // when reached mid-session via Next/Previous Level, not just
        // when chosen from the main menu.
        UpdateAmbientSoundState();

        wasSessionActive = sessionManager.IsSessionActive;
    }

    /// <summary>
    /// Starts or stops the looping ambient sound so it always matches
    /// whether it SHOULD be playing right now.
    /// </summary>
    private void UpdateAmbientSoundState()
    {
        if (ambientSource == null) return;

        bool shouldPlay = ShouldPlayAmbientSound();

        if (shouldPlay && !ambientSource.isPlaying)
        {
            ambientSource.Play();
            if (debugMode) Debug.Log("AudioManager: Ambient sound started");
        }
        else if (!shouldPlay && ambientSource.isPlaying)
        {
            ambientSource.Stop();
            if (debugMode) Debug.Log("AudioManager: Ambient sound stopped");
        }
    }

    /// <summary>
    /// Decides whether the background classroom sound should be playing.
    /// False during silent-mode sessions, and false at Level 1 (no
    /// students present, so no classroom ambience makes sense).
    /// </summary>
    private bool ShouldPlayAmbientSound()
    {
        if (classroomAmbience == null) return false;
        if (sessionManager == null || !sessionManager.IsSessionActive) return false;
        if (sessionManager.IsSilentMode) return false;
        if (levelManager != null && levelManager.GetCurrentLevel() == 1) return false;

        return true;
    }

    // ========================================
    // EVENT HANDLERS
    // ========================================

    private void OnSessionStarted()
    {
        Debug.Log("AudioManager: Session started");

        // The start chime always plays, even in silent mode — silent
        // mode only affects the looping background classroom sound.
        PlaySFX(sessionStartSound);

        if (levelManager != null)
        {
            lastLevel = levelManager.GetCurrentLevel();
        }
    }

    private void OnSessionEnded()
    {
        Debug.Log("AudioManager: Session ended");

        // Ambient sound is stopped automatically by UpdateAmbientSoundState()
        // on the next frame, since IsSessionActive is now false.
        lastLevel = -1;
    }

    private void OnLevelChanged()
    {
        Debug.Log("AudioManager: Level changed — playing level change sound");
        PlaySFX(levelChangeSound);
    }

    // ========================================
    // PUBLIC METHODS
    // ========================================

    /// <summary>
    /// Plays the button click sound. Call from button On Click events.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Sets the ambient volume (0-1).
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
        }
    }
}