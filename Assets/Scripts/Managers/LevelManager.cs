// ============================================================
// FILE: LevelManager.cs
// PURPOSE: Controls the visibility of student groups based on
//          the current exposure level in the VR classroom.
//          Manages transitions between 4 difficulty levels.
// 
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 08 February 2026
// LAST MODIFIED: 18 June 2026
// 
// DEPENDENCIES:
//   - UnityEngine
//   - TMPro (TextMeshPro package)
// 
// USAGE:
//   1. Attach this script to a GameObject named "GameManager"
//   2. Assign all public references in Inspector
//   3. Call SetLevel(1-4) to change exposure levels
// 
// LEVELS:
//   Level 1: Empty classroom (0 students)
//   Level 2: Small group (5 students)
//   Level 3: Medium class (11 students)
//   Level 4: Full classroom (16 students)
// ============================================================

using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    // ========================================
    // INSPECTOR VARIABLES
    // ========================================

    [Header("Student Groups")]
    [Tooltip("GameObject containing students 1-5 (for Level 2)")]
    public GameObject level2Group;

    [Tooltip("GameObject containing students 6-11 (additional for Level 3)")]
    public GameObject level3Additional;

    [Tooltip("GameObject containing students 12-16 (additional for Level 4)")]
    public GameObject level4Additional;

    [Header("UI References")]
    [Tooltip("Text element to display current level name")]
    public TMP_Text levelText;

    [Tooltip("Text element to display current student count")]
    public TMP_Text studentCountText;

    [Header("Audio Feedback")]
    [Tooltip("Audio source for level change sound effect")]
    public AudioSource levelChangeSound;

    [Header("Settings")]
    [Tooltip("Starting level when scene loads (1-4)")]
    [Range(1, 4)]
    public int startingLevel = 1;

    // ========================================
    // PRIVATE VARIABLES
    // ========================================

    private int currentLevel = 1;

    // Level information arrays
    private readonly string[] levelNames =
    {
        "",
        "Practice: Empty hall",
        "Pitch: Small audience",
        "Pitch: Medium audience",
        "Pitch: Full audience"
    };

    private readonly string[] levelDescriptions =
    {
        "",
        "Practice in an empty room",
        "5 students present",
        "11 students present",
        "16 students present"
    };

    private readonly int[] studentCounts = { 0, 0, 5, 11, 16 };

    // ========================================
    // UNITY LIFECYCLE METHODS
    // ========================================

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the level system.
    /// </summary>
    private void Awake()
    {
        // Validate starting level
        if (startingLevel < 1 || startingLevel > 4)
        {
            Debug.LogWarning("LevelManager: Invalid starting level. Defaulting to Level 1.");
            startingLevel = 1;
        }
    }

    /// <summary>
    /// Called before the first frame update.
    /// Sets up the initial level state.
    /// </summary>
    private void Start()
    {
        // Initialize to starting level
        SetLevel(startingLevel);

        Debug.Log("LevelManager: Initialized at Level " + startingLevel);
    }

    // ========================================
    // PUBLIC METHODS
    // ========================================

    /// <summary>
    /// Sets the current exposure level and updates student visibility.
    /// </summary>
    /// <param name="level">Level number from 1 to 4</param>
    public void SetLevel(int level)
    {
        // Validate level input
        if (level < 1 || level > 4)
        {
            Debug.LogError("LevelManager: Invalid level " + level + ". Must be between 1 and 4.");
            return;
        }

        // Store current level
        currentLevel = level;

        // Hide all student groups first
        HideAllStudents();

        // Show appropriate students based on level
        switch (level)
        {
            case 1:
                // Empty classroom - all students hidden
                Debug.Log("LevelManager: Level 1 - Empty Classroom (0 students)");
                break;

            case 2:
                // Small group - show first 5 students
                ShowStudentGroup(level2Group);
                Debug.Log("LevelManager: Level 2 - Small Group (5 students)");
                break;

            case 3:
                // Medium class - show 11 students
                ShowStudentGroup(level2Group);
                ShowStudentGroup(level3Additional);
                Debug.Log("LevelManager: Level 3 - Medium Class (11 students)");
                break;

            case 4:
                // Full classroom - show all 16 students
                ShowStudentGroup(level2Group);
                ShowStudentGroup(level3Additional);
                ShowStudentGroup(level4Additional);
                Debug.Log("LevelManager: Level 4 - Full Classroom (16 students)");
                break;
        }

        // Update UI elements
        UpdateUI();

        // Play audio feedback
        PlayLevelChangeSound();
    }

    /// <summary>
    /// Returns the current level number.
    /// </summary>
    /// <returns>Current level (1-4)</returns>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// Returns the name of the current level.
    /// </summary>
    /// <returns>Level name string</returns>
    public string GetCurrentLevelName()
    {
        return levelNames[currentLevel];
    }

    /// <summary>
    /// Returns the student count for the current level.
    /// </summary>
    /// <returns>Number of students visible</returns>
    public int GetCurrentStudentCount()
    {
        return studentCounts[currentLevel];
    }

    /// <summary>
    /// Advances to the next level. Wraps from 4 to 1.
    /// </summary>
    public void NextLevel()
    {
        int nextLevel = currentLevel + 1;
        if (nextLevel > 4)
        {
            nextLevel = 1;
        }
        SetLevel(nextLevel);
    }

    /// <summary>
    /// Goes back to the previous level. Wraps from 1 to 4.
    /// </summary>
    public void PreviousLevel()
    {
        int prevLevel = currentLevel - 1;
        if (prevLevel < 1)
        {
            prevLevel = 4;
        }
        SetLevel(prevLevel);
    }

    // Convenience methods for UI buttons
    public void GoToLevel1() { SetLevel(1); }
    public void GoToLevel2() { SetLevel(2); }
    public void GoToLevel3() { SetLevel(3); }
    public void GoToLevel4() { SetLevel(4); }

    // ========================================
    // PRIVATE HELPER METHODS
    // ========================================

    /// <summary>
    /// Hides all student groups.
    /// </summary>
    private void HideAllStudents()
    {
        HideStudentGroup(level2Group);
        HideStudentGroup(level3Additional);
        HideStudentGroup(level4Additional);
    }

    /// <summary>
    /// Shows a specific student group if it exists.
    /// </summary>
    /// <param name="group">The student group GameObject to show</param>
    private void ShowStudentGroup(GameObject group)
    {
        if (group != null)
        {
            group.SetActive(true);
        }
        else
        {
            Debug.LogWarning("LevelManager: Student group reference is null.");
        }
    }

    /// <summary>
    /// Hides a specific student group if it exists.
    /// </summary>
    /// <param name="group">The student group GameObject to hide</param>
    private void HideStudentGroup(GameObject group)
    {
        if (group != null)
        {
            group.SetActive(false);
        }
    }

    /// <summary>
    /// Updates UI text elements with current level information.
    /// </summary>
    private void UpdateUI()
    {
        // Update level name text
        if (levelText != null)
        {
            levelText.text = levelNames[currentLevel];
        }

        // Update student count text
        if (studentCountText != null)
        {
            studentCountText.text = "Audience: " + studentCounts[currentLevel];
        }
    }

    /// <summary>
    /// Plays the level change sound effect if available.
    /// </summary>
    private void PlayLevelChangeSound()
    {
        if (levelChangeSound != null)
        {
            levelChangeSound.Play();
        }
    }

    // ========================================
    // VALIDATION (EDITOR ONLY)
    // ========================================

#if UNITY_EDITOR
    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed.
    /// Used for validation.
    /// </summary>
    private void OnValidate()
    {
        // Clamp starting level
        startingLevel = Mathf.Clamp(startingLevel, 1, 4);
    }
#endif
}
