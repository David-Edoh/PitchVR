// ============================================================
// FILE: PerformanceManager.cs
// PURPOSE: Optimizes Unity runtime settings for Meta Quest 2
//          standalone performance. Sets target frame rate,
//          disables shadows, and configures rendering quality.
// 
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 18 June 2026
// LAST MODIFIED: 18 June 2026
// 
// DEPENDENCIES:
//   - UnityEngine
//   - UnityEngine
// 
// USAGE:
//   1. Attach this script to the GameManager GameObject
//   2. No Inspector configuration needed (uses sensible defaults)
//   3. Automatically applies optimizations when scene loads
// 
// TARGET DEVICE: Meta Quest 2 (72 FPS target)
// ============================================================

using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    // ========================================
    // INSPECTOR VARIABLES
    // ========================================

    [Header("Frame Rate")]
    [Tooltip("Target frame rate for Quest 2 (72 or 90)")]
    [Range(72, 90)]
    public int targetFrameRate = 72;

    [Header("Rendering Quality")]
    [Tooltip("Anti-aliasing level (0=Off, 2=2x, 4=4x)")]
    [Range(0, 4)]
    public int antiAliasingLevel = 2;

    [Tooltip("Disable all dynamic shadows for better performance")]
    public bool disableShadows = true;

    [Header("Debug")]
    [Tooltip("Show performance debug messages in console")]
    public bool debugMode = false;

    // ========================================
    // UNITY LIFECYCLE METHODS
    // ========================================

    /// <summary>
    /// Called before the first frame update.
    /// Applies all performance optimizations.
    /// </summary>
    private void Start()
    {
        ApplyPerformanceSettings();

        if (debugMode)
        {
            Debug.Log("PerformanceManager: All optimizations applied");
        }
    }

    // ========================================
    // PRIVATE METHODS
    // ========================================

    /// <summary>
    /// Applies all performance optimization settings.
    /// </summary>
    private void ApplyPerformanceSettings()
    {
        // Set target frame rate (Quest 2 supports 72 or 90 FPS)
        Application.targetFrameRate = targetFrameRate;
        if (debugMode) Debug.Log("PerformanceManager: Target FPS = " + targetFrameRate);

        // Disable VSync (Quest 2 handles its own display sync)
        QualitySettings.vSyncCount = 0;
        if (debugMode) Debug.Log("PerformanceManager: VSync disabled");

        // Set anti-aliasing level
        QualitySettings.antiAliasing = antiAliasingLevel;
        if (debugMode) Debug.Log("PerformanceManager: Anti-aliasing = " + antiAliasingLevel + "x");

        // Disable shadows for better performance on Quest 2
        if (disableShadows)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            if (debugMode) Debug.Log("PerformanceManager: Shadows disabled");
        }

        // Set texture quality to full resolution
        QualitySettings.globalTextureMipmapLimit = 0;
        if (debugMode) Debug.Log("PerformanceManager: Texture quality set to full");

    }

    // ========================================
    // PUBLIC METHODS
    // ========================================

    /// <summary>
    /// Returns the current target frame rate.
    /// </summary>
    public int GetTargetFrameRate()
    {
        return targetFrameRate;
    }

    /// <summary>
    /// Returns the current actual FPS (for testing/debugging).
    /// </summary>
    public float GetCurrentFPS()
    {
        return 1f / Time.unscaledDeltaTime;
    }
}
