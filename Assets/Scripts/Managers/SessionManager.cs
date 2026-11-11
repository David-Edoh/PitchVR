// ============================================================
// FILE: SessionManager.cs
// PURPOSE: Manages the VR practice session state including
//          starting, ending, pausing, and timing sessions.
// 
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 08 February 2026
// LAST MODIFIED: 08 February 2026
// 
// DEPENDENCIES:
//   - UnityEngine
//   - TMPro (TextMeshPro package)
//   - LevelManager.cs
// 
// USAGE:
//   1. Attach this script to the same GameObject as LevelManager
//   2. Assign all public references in Inspector
//   3. Call StartSession() to begin a practice session
//   4. Call EndSession() to end and return to menu
// ============================================================

using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class SessionManager : MonoBehaviour
{
    // ========================================
    // INSPECTOR VARIABLES
    // ========================================

    [Header("Manager References")]
    [Tooltip("Reference to the LevelManager script")]
    public LevelManager levelManager;

    [Header("UI References")]
    [Tooltip("Main menu canvas GameObject")]
    public GameObject mainMenuCanvas;

    [Tooltip("Session UI canvas GameObject (shown during practice)")]
    public GameObject sessionUICanvas;

    [Tooltip("Pause menu canvas GameObject")]
    public GameObject pauseMenuCanvas;

    [Header("Timer Display")]
    [Tooltip("Text element to display session timer")]
    public TMP_Text timerText;

    [Tooltip("Text element to display session status")]
    public TMP_Text statusText;

    [Header("Audio")]
    [Tooltip("Audio source for session start sound")]
    public AudioSource sessionStartSound;

    [Tooltip("Audio source for session end sound")]
    public AudioSource sessionEndSound;

    [Header("Events")]
    [Tooltip("Called when session starts")]
    public UnityEvent onSessionStart;

    [Tooltip("Called when session ends")]
    public UnityEvent onSessionEnd;

    [Tooltip("Called when session is paused")]
    public UnityEvent onSessionPause;

    [Tooltip("Called when session is resumed")]
    public UnityEvent onSessionResume;

    // ========================================
    // PRIVATE VARIABLES
    // ========================================

    private bool isSessionActive = false;
    private bool isSessionPaused = false;
    private bool isSilentMode = false;
    private float sessionStartTime = 0f;
    private float sessionElapsedTime = 0f;
    private float pausedTime = 0f;
    private int currentSessionLevel = 1;

    // Session statistics
    private int totalSessionsCompleted = 0;
    private float totalTimeSpent = 0f;

    // ========================================
    // PROPERTIES
    // ========================================

    /// <summary>
    /// Returns whether a session is currently active.
    /// </summary>
    public bool IsSessionActive => isSessionActive;

    /// <summary>
    /// Returns whether the session is currently paused.
    /// </summary>
    public bool IsSessionPaused => isSessionPaused;

    /// <summary>
    /// Returns the elapsed time of the current session in seconds.
    /// </summary>
    public float ElapsedTime => sessionElapsedTime;

    /// <summary>
    /// Returns the current session level.
    /// </summary>
    public int CurrentSessionLevel => currentSessionLevel;

    /// <summary>
    /// True when the current session was started in silent mode
    /// (background ambient sound intentionally disabled).
    /// </summary>
    public bool IsSilentMode => isSilentMode;

    // ========================================
    // UNITY LIFECYCLE METHODS
    // ========================================

    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    private void Start()
    {
        // Initialize UI state
        ShowMainMenu();

        // Try to find LevelManager if not assigned
        if (levelManager == null)
        {
            levelManager = GetComponent<LevelManager>();
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }

            if (levelManager == null)
            {
                Debug.LogError("SessionManager: LevelManager reference not found!");
            }
        }

        Debug.Log("SessionManager: Initialized");
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    private void Update()
    {
        // Update timer if session is active and not paused
        if (isSessionActive && !isSessionPaused)
        {
            UpdateTimer();
        }
    }

    // ========================================
    // PUBLIC SESSION CONTROL METHODS
    // ========================================

    /// <summary>
    /// Starts a new practice session at the specified level.
    /// </summary>
    /// <param name="level">The level to start at (1-4)</param>
    public void StartSession(int level, bool silent = false)
    {
        // Validate level
        if (level < 1 || level > 4)
        {
            Debug.LogError("SessionManager: Invalid level " + level);
            return;
        }

        // Don't start if already in session
        if (isSessionActive)
        {
            Debug.LogWarning("SessionManager: Session already active. End current session first.");
            return;
        }

        // Set session state
        isSessionActive = true;
        isSessionPaused = false;
        isSilentMode = silent;
        currentSessionLevel = level;
        sessionStartTime = Time.time;
        sessionElapsedTime = 0f;
        pausedTime = 0f;

        // Update UI
        HideMainMenu();
        ShowSessionUI();
        HidePauseMenu();

        // Set level
        if (levelManager != null)
        {
            levelManager.SetLevel(level);
        }

        // Update status
        UpdateStatusText("Pitch in progress");

        // Play sound
        if (sessionStartSound != null)
        {
            sessionStartSound.Play();
        }

        // Invoke event
        onSessionStart?.Invoke();

        Debug.Log("SessionManager: Session started at Level " + level);
    }

    /// <summary>
    /// Ends the current practice session.
    /// </summary>
    public void EndSession()
    {
        if (!isSessionActive)
        {
            Debug.LogWarning("SessionManager: No active session to end.");
            return;
        }

        // Calculate final elapsed time
        float finalTime = sessionElapsedTime;

        // Update statistics
        totalSessionsCompleted++;
        totalTimeSpent += finalTime;

        // Reset session state
        isSessionActive = false;
        isSessionPaused = false;
        isSilentMode = false;

        // Update UI
        ShowMainMenu();
        HideSessionUI();
        HidePauseMenu();

        // Reset level to empty classroom
        if (levelManager != null)
        {
            levelManager.SetLevel(1);
        }

        // Play sound
        if (sessionEndSound != null)
        {
            sessionEndSound.Play();
        }

        // Invoke event
        onSessionEnd?.Invoke();

        Debug.Log("SessionManager: Session ended. Duration: " + FormatTime(finalTime));
    }

    /// <summary>
    /// Pauses the current session.
    /// </summary>
    public void PauseSession()
    {
        if (!isSessionActive)
        {
            Debug.LogWarning("SessionManager: No active session to pause.");
            return;
        }

        if (isSessionPaused)
        {
            Debug.LogWarning("SessionManager: Session already paused.");
            return;
        }

        isSessionPaused = true;
        pausedTime = Time.time;

        // Show pause menu
        ShowPauseMenu();

        // Update status
        UpdateStatusText("Paused");

        // Invoke event
        onSessionPause?.Invoke();

        Debug.Log("SessionManager: Session paused");
    }

    /// <summary>
    /// Resumes the paused session.
    /// </summary>
    public void ResumeSession()
    {
        if (!isSessionActive)
        {
            Debug.LogWarning("SessionManager: No active session to resume.");
            return;
        }

        if (!isSessionPaused)
        {
            Debug.LogWarning("SessionManager: Session is not paused.");
            return;
        }

        // Adjust start time to account for pause duration
        float pauseDuration = Time.time - pausedTime;
        sessionStartTime += pauseDuration;

        isSessionPaused = false;

        // Hide pause menu
        HidePauseMenu();

        // Update status
        UpdateStatusText("Pitch in progress");

        // Invoke event
        onSessionResume?.Invoke();

        Debug.Log("SessionManager: Session resumed");
    }

    /// <summary>
    /// Toggles pause state.
    /// </summary>
    public void TogglePause()
    {
        if (isSessionPaused)
        {
            ResumeSession();
        }
        else
        {
            PauseSession();
        }
    }

    // ========================================
    // CONVENIENCE METHODS FOR UI BUTTONS
    // ========================================

    /// <summary>
    /// Starts session at Level 1 (Empty Classroom).
    /// </summary>
    public void StartSessionLevel1()
    {
        StartSession(1);
    }

    /// <summary>
    /// Starts session at Level 2 (Small Group).
    /// </summary>
    public void StartSessionLevel2()
    {
        StartSession(2);
    }

    /// <summary>
    /// Starts session at Level 3 (Medium Class).
    /// </summary>
    public void StartSessionLevel3()
    {
        StartSession(3);
    }

    /// <summary>
    /// Starts session at Level 4 (Full Classroom).
    /// </summary>
    public void StartSessionLevel4()
    {
        StartSession(4);
    }

    /// <summary>Starts Level 2 with background sound disabled.</summary>
    public void StartSessionLevel2Silent()
    {
        StartSession(2, true);
    }

    /// <summary>Starts Level 3 with background sound disabled.</summary>
    public void StartSessionLevel3Silent()
    {
        StartSession(3, true);
    }

    /// <summary>Starts Level 4 with background sound disabled.</summary>
    public void StartSessionLevel4Silent()
    {
        StartSession(4, true);
    }

    // ========================================
    // STATISTICS METHODS
    // ========================================

    /// <summary>
    /// Returns the total number of completed sessions.
    /// </summary>
    public int GetTotalSessionsCompleted()
    {
        return totalSessionsCompleted;
    }

    /// <summary>
    /// Returns the total time spent in sessions (seconds).
    /// </summary>
    public float GetTotalTimeSpent()
    {
        return totalTimeSpent;
    }

    /// <summary>
    /// Returns the formatted elapsed time of current session.
    /// </summary>
    public string GetFormattedElapsedTime()
    {
        return FormatTime(sessionElapsedTime);
    }

    // ========================================
    // PRIVATE HELPER METHODS
    // ========================================

    /// <summary>
    /// Updates the session timer.
    /// </summary>
    private void UpdateTimer()
    {
        sessionElapsedTime = Time.time - sessionStartTime;

        // Update timer display
        if (timerText != null)
        {
            timerText.text = FormatTime(sessionElapsedTime);
        }
    }

    /// <summary>
    /// Formats time in seconds to MM:SS format.
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds</param>
    /// <returns>Formatted time string</returns>
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Updates the status text display.
    /// </summary>
    /// <param name="status">Status message to display</param>
    private void UpdateStatusText(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    // ========================================
    // UI VISIBILITY METHODS
    // ========================================

    /// <summary>
    /// Shows the main menu canvas.
    /// </summary>
    private void ShowMainMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the main menu canvas.
    /// </summary>
    private void HideMainMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the session UI canvas.
    /// </summary>
    private void ShowSessionUI()
    {
        if (sessionUICanvas != null)
        {
            sessionUICanvas.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the session UI canvas.
    /// </summary>
    private void HideSessionUI()
    {
        if (sessionUICanvas != null)
        {
            sessionUICanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the pause menu canvas.
    /// </summary>
    private void ShowPauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the pause menu canvas.
    /// </summary>
    private void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
    }
}
