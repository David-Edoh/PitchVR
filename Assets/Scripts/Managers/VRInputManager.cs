// ============================================================
// FILE: VRInputManager.cs
// PURPOSE: Handles Meta Quest 2 controller input for the VR
//          phobia management system. Manages button detection,
//          menu navigation, and session control via controllers.
// 
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 08 February 2026
// LAST MODIFIED: 21 June 2026
// 
// DEPENDENCIES:
//   - UnityEngine
//   - Unity XR controller input
//   - LevelManager.cs
//   - SessionManager.cs
// 
// CONTROLLER MAPPING:
//   - A Button: Confirm / Select
//   - B Button: Back / Exit to Menu
//   - X Button: Previous Level (during session)
//   - Y Button: Next Level (during session)
//   - Right Trigger: UI Click
//   - Right Thumbstick Press: Toggle Pause
// 
// KEYBOARD MAPPING (for testing without VR):
//   - Space: Confirm / Select
//   - Escape: Back / Exit to Menu
//   - Left Arrow: Previous Level (player builds only; reserved for XR simulator in Editor)
//   - Right Arrow: Next Level (player builds only; reserved for XR simulator in Editor)
//   - P: Toggle Pause
//   - 1, 2, 3, 4: Start session at that level
// ============================================================

using UnityEngine;
using UnityEngine.Events;

public class VRInputManager : MonoBehaviour
{
    // ========================================
    // INSPECTOR VARIABLES
    // ========================================

    [Header("Manager References")]
    [Tooltip("Reference to the LevelManager script")]
    public LevelManager levelManager;

    [Tooltip("Reference to the SessionManager script")]
    public SessionManager sessionManager;

    [Header("Input Settings")]
    [Tooltip("Enable vibration feedback on button press")]
    public bool enableHapticFeedback = true;

    [Tooltip("Haptic feedback duration in seconds")]
    [Range(0.01f, 0.5f)]
    public float hapticDuration = 0.1f;

    [Tooltip("Haptic feedback strength")]
    [Range(0f, 1f)]
    public float hapticStrength = 0.5f;

    [Header("Debug")]
    [Tooltip("Show input debug messages in console")]
    public bool debugMode = false;

    [Header("Events")]
    [Tooltip("Called when A button (or Space key) is pressed")]
    public UnityEvent onAButtonPressed;

    [Tooltip("Called when B button (or Escape key) is pressed")]
    public UnityEvent onBButtonPressed;

    [Tooltip("Called when trigger (or mouse click) is pressed")]
    public UnityEvent onTriggerPressed;

    // ========================================
    // UNITY LIFECYCLE METHODS
    // ========================================

    private void Start()
    {
        FindManagers();
        Debug.Log("VRInputManager: Initialized with Unity XR controller input + keyboard fallback");
    }

    private void Update()
    {
        ProcessXRControllerInput();

        // Always process keyboard input (for editor testing)
        ProcessKeyboardInput();
    }

    // ========================================
    // VR CONTROLLER INPUT PROCESSING
    // ========================================

    private void ProcessXRControllerInput()
    {
        // A Button - Confirm / Select (Right Controller)
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Right, XRControllerInput.Button.Primary))
        {
            if (debugMode) Debug.Log("VRInputManager: A Button pressed");
            OnConfirmPress();
            TriggerHapticPulse(true);
        }

        // B Button - Back / Exit (Right Controller)
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Right, XRControllerInput.Button.Secondary))
        {
            if (debugMode) Debug.Log("VRInputManager: B Button pressed");
            OnBackPress();
            TriggerHapticPulse(true);
        }

        // X Button - Previous Level (Left Controller)
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Left, XRControllerInput.Button.Primary))
        {
            if (debugMode) Debug.Log("VRInputManager: X Button pressed");
            OnPreviousLevelPress();
            TriggerHapticPulse(false);
        }

        // Y Button - Next Level (Left Controller)
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Left, XRControllerInput.Button.Secondary))
        {
            if (debugMode) Debug.Log("VRInputManager: Y Button pressed");
            OnNextLevelPress();
            TriggerHapticPulse(false);
        }

        // Right Trigger - UI Click
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Right, XRControllerInput.Button.Trigger))
        {
            if (debugMode) Debug.Log("VRInputManager: Right Trigger pressed");
            OnTriggerPress();
            TriggerHapticPulse(true);
        }

        // Right Thumbstick Press - Toggle Pause
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Right, XRControllerInput.Button.Stick))
        {
            if (debugMode) Debug.Log("VRInputManager: Thumbstick pressed (Pause)");
            OnPausePress();
            TriggerHapticPulse(true);
        }

        // Start/Menu Button - Toggle Pause
        if (XRControllerInput.GetDown(XRControllerInput.Hand.Left, XRControllerInput.Button.Menu))
        {
            if (debugMode) Debug.Log("VRInputManager: Start button pressed (Pause)");
            OnPausePress();
            TriggerHapticPulse(false);
        }
    }

    private void TriggerHapticPulse(bool rightController)
    {
        if (!enableHapticFeedback) return;

        XRControllerInput.Pulse(rightController ? XRControllerInput.Hand.Right :
            XRControllerInput.Hand.Left, hapticStrength, hapticDuration);
    }

    // ========================================
    // KEYBOARD INPUT PROCESSING
    // ========================================

    private void ProcessKeyboardInput()
    {
        // Space = Confirm (like A button)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnConfirmPress();
        }

        // Escape = Back (like B button)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackPress();
        }

        // The XR Interaction Simulator uses the arrow keys to rotate the camera.
        if (!Application.isEditor && Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnPreviousLevelPress();
        }

        if (!Application.isEditor && Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnNextLevelPress();
        }

        // P = Toggle Pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            OnPausePress();
        }

        // Mouse click = Trigger
        if (Input.GetMouseButtonDown(0))
        {
            OnTriggerPress();
        }

        // Number keys for direct level selection
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            StartSessionAtLevel(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            StartSessionAtLevel(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            StartSessionAtLevel(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            StartSessionAtLevel(4);
        }

        // Q = Quit application
        if (Input.GetKeyDown(KeyCode.Q) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            QuitApplication();
        }
    }

    // ========================================
    // INPUT ACTION HANDLERS
    // ========================================

    private void OnConfirmPress()
    {
        if (debugMode) Debug.Log("VRInputManager: Confirm pressed");
        onAButtonPressed?.Invoke();
    }

    private void OnBackPress()
    {
        if (debugMode) Debug.Log("VRInputManager: Back pressed");

        if (sessionManager != null && sessionManager.IsSessionActive)
        {
            sessionManager.EndSession();
        }

        onBButtonPressed?.Invoke();
    }

    private void OnPreviousLevelPress()
    {
        if (debugMode) Debug.Log("VRInputManager: Previous Level pressed");

        if (sessionManager != null && sessionManager.IsSessionActive && !sessionManager.IsSessionPaused)
        {
            if (levelManager != null)
            {
                levelManager.PreviousLevel();
            }
        }
    }

    private void OnNextLevelPress()
    {
        if (debugMode) Debug.Log("VRInputManager: Next Level pressed");

        if (sessionManager != null && sessionManager.IsSessionActive && !sessionManager.IsSessionPaused)
        {
            if (levelManager != null)
            {
                levelManager.NextLevel();
            }
        }
    }

    private void OnTriggerPress()
    {
        if (debugMode) Debug.Log("VRInputManager: Trigger pressed");
        onTriggerPressed?.Invoke();
    }

    private void OnPausePress()
    {
        if (debugMode) Debug.Log("VRInputManager: Pause pressed");

        if (sessionManager != null && sessionManager.IsSessionActive)
        {
            sessionManager.TogglePause();
        }
    }

    // ========================================
    // PUBLIC METHODS
    // ========================================

    public void StartSessionAtLevel(int level)
    {
        if (debugMode) Debug.Log("VRInputManager: Starting session at level " + level);

        if (sessionManager != null)
        {
            sessionManager.StartSession(level);
        }
        else
        {
            Debug.LogError("VRInputManager: SessionManager reference is null!");
        }
    }

    public void ReturnToMenu()
    {
        if (sessionManager != null)
        {
            sessionManager.EndSession();
        }
    }

    public void QuitApplication()
    {
        Debug.Log("VRInputManager: Quitting application");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    private void FindManagers()
    {
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogWarning("VRInputManager: LevelManager not found in scene.");
            }
        }

        if (sessionManager == null)
        {
            sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager == null)
            {
                Debug.LogWarning("VRInputManager: SessionManager not found in scene.");
            }
        }
    }
}
