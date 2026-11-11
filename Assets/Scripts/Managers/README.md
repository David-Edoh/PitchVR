# Manager Scripts

This folder contains core manager scripts for the VR Phobia Management System.

## Scripts Overview

### LevelManager.cs

Controls the visibility of student groups based on the current exposure level.

**Levels (Updated for 16-chair classroom):**
| Level | Description | Students |
|-------|-------------|----------|
| 1 | Empty Classroom | 0 |
| 2 | Small Group | 5 |
| 3 | Medium Class | 11 |
| 4 | Full Classroom | 16 |

**Student Group Distribution:**
- Level2Group: Student_01 to Student_05 (5 students)
- Level3Additional: Student_06 to Student_11 (6 students)
- Level4Additional: Student_12 to Student_16 (5 students)

**Public Methods:**

- `SetLevel(int level)` - Set exposure level (1-4)
- `GetCurrentLevel()` - Get current level number
- `GetCurrentLevelName()` - Get current level name string
- `GetCurrentStudentCount()` - Get number of visible students
- `NextLevel()` - Advance to next level
- `PreviousLevel()` - Go back to previous level

### SessionManager.cs

Manages the VR practice session state and timing.

**Public Methods:**

- `StartSession(int level)` - Start a new session
- `EndSession()` - End current session
- `PauseSession()` - Pause the session
- `ResumeSession()` - Resume paused session
- `TogglePause()` - Toggle pause state
- `StartSessionLevel1()` to `StartSessionLevel4()` - Convenience methods for UI buttons

### VRInputManager.cs

Handles Quest controller input through Unity XR, with a keyboard fallback.

**Controller Mapping (VR):**
| Button | Action |
|--------|--------|
| A | Confirm/Select |
| B | Back/Exit to Menu |
| X | Previous Level |
| Y | Next Level |
| Right Trigger | UI Click |
| Right Thumbstick Press | Toggle Pause |

**Keyboard Mapping (Editor Testing):**
| Key | Action |
|-----|--------|
| 1, 2, 3, 4 | Start session at that level |
| Escape | End session / Return to menu |
| Left/Right Arrow | Previous/Next level |
| P | Toggle Pause |
| Space | Confirm |
| Ctrl+Q | Quit |

### PerformanceManager.cs

Optimizes runtime settings for Meta Quest 2 standalone performance.

**Features:**
- Sets target frame rate to 72 FPS
- Disables shadows
- Sets antialiasing level

## Setup Instructions

1. Create empty GameObject named "GameManager"
2. Attach LevelManager, SessionManager, VRInputManager, and PerformanceManager
3. Connect references in Inspector
4. Connect UI buttons to call StartSessionLevel1-4 methods

## Dependencies

- UnityEngine
- TMPro (TextMeshPro)
- XR Interaction Toolkit 3.6.1
- Oculus XR Plug-in 4.5.2 for Quest display and tracking
- XR Plug-in Management

## Author

DulakshiniDharmarathne

## Last Updated

June 2026
