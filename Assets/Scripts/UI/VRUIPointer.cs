// ============================================================
// FILE: VRUIPointer.cs
// PURPOSE: Allows the VR controller to click UI buttons by
//          pointing the laser and pressing the trigger.
//          Uses world-space geometry to detect Canvas buttons
//          WITHOUT needing physics colliders.
//
// AUTHOR: DulakshiniDharmarathne
// DATE CREATED: 21 June 2026
// LAST MODIFIED: 22 June 2026
//
// HOW IT WORKS:
//   Each Canvas button has a flat rectangle in 3D world space.
//   We cast a ray from the controller forward direction, then
//   use plane intersection math to check if the ray passes
//   through each button's rectangle area.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VRUIPointer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The right controller transform under XR Origin")]
    public Transform controllerTransform;

    [Header("Settings")]
    [Tooltip("Maximum reach of the laser in meters")]
    public float maxDistance = 15f;

    [Header("Debug")]
    [Tooltip("Show debug messages so you can verify detection on Quest 2")]
    public bool debugMode = true;

    // Private — list of all UI buttons found in the scene
    private List<Button> allButtons = new List<Button>();
    private Button currentlyPointedButton = null;
    private PitchSimulator pitchSimulator;

    // ====================================================
    // START — collect all buttons in the scene
    // ====================================================
    void Start()
    {
        // FindObjectsOfType(true) = includes disabled/inactive buttons too
        Button[] found = FindObjectsOfType<Button>(true);
        allButtons.AddRange(found);
        pitchSimulator = FindObjectOfType<PitchSimulator>();

        Debug.Log("VRUIPointer: Initialized. Found " + allButtons.Count
                  + " UI buttons in scene.");

        if (controllerTransform == null)
        {
            Debug.LogError("VRUIPointer: *** Controller Transform is NULL! ***"
                + " Please drag RightHandAnchor into the Controller Transform field"
                + " on the VRUIPointerManager object.");
        }
    }

    // ====================================================
    // UPDATE — check every frame where the laser is pointing
    // ====================================================
    void Update()
    {
        if (controllerTransform == null) return;

        // Build a ray starting at the controller, going forward
        Ray laserRay = new Ray(
            controllerTransform.position,
            controllerTransform.forward
        );

        // Find which button (if any) the laser is pointing at
        Button pointedAt = GetButtonAtRay(laserRay);

        // Log when pointing changes (helps with debugging on Quest 2)
        if (pointedAt != currentlyPointedButton)
        {
            if (debugMode)
            {
                if (pointedAt != null)
                    Debug.Log("VRUIPointer: >> Laser is on: " + pointedAt.name);
                else
                    Debug.Log("VRUIPointer: (laser not on any button)");
            }
            currentlyPointedButton = pointedAt;
        }

        // ── CHECK TRIGGER PRESS ──────────────────────────────
        // This fires when the user presses the RIGHT TRIGGER
        bool triggerPressed = XRControllerInput.GetDown(
            XRControllerInput.Hand.Right, XRControllerInput.Button.Trigger);

        if (triggerPressed && (pitchSimulator == null || !pitchSimulator.IsRemoteHeld))
        {
            if (currentlyPointedButton != null)
            {
                Debug.Log("VRUIPointer: ✓ CLICK! -> " + currentlyPointedButton.name);
                currentlyPointedButton.onClick.Invoke();  // Fire the button!
            }
            else
            {
                if (debugMode)
                    Debug.Log("VRUIPointer: Trigger pressed — not pointing at a button.");
            }
        }
    }

    // ====================================================
    // FIND BUTTON — plane intersection, no colliders needed
    // ====================================================
    private Button GetButtonAtRay(Ray ray)
    {
        Button closestButton = null;
        float closestDist = maxDistance;

        foreach (Button btn in allButtons)
        {
            // Skip missing, hidden, or disabled buttons
            if (btn == null) continue;
            if (!btn.gameObject.activeInHierarchy) continue;
            if (!btn.interactable) continue;

            RectTransform rect = btn.GetComponent<RectTransform>();
            if (rect == null) continue;

            // Get this button's 4 corners in WORLD SPACE
            //   corners[0] = bottom-left
            //   corners[1] = top-left
            //   corners[2] = top-right
            //   corners[3] = bottom-right
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            // The surface normal of the button face (works for any canvas rotation)
            Vector3 surfaceNormal = rect.forward;

            // Create a math plane at the button surface
            Plane btnPlane = new Plane(surfaceNormal, corners[0]);

            // Test if the laser ray crosses this plane
            float dist;
            if (!btnPlane.Raycast(ray, out dist)) continue; // Parallel — skip
            if (dist <= 0f) continue; // Behind us — skip
            if (dist > closestDist) continue; // Something closer already — skip

            // Find the exact 3D point where the ray crosses the plane
            Vector3 hitPoint = ray.GetPoint(dist);

            // Check if that point is actually INSIDE the button's rectangle
            if (HitPointIsInsideButton(hitPoint, corners))
            {
                closestDist = dist;
                closestButton = btn;
            }
        }

        return closestButton;
    }

    // ====================================================
    // IS INSIDE BUTTON — is the hit point within the 4 corners?
    // ====================================================
    private bool HitPointIsInsideButton(Vector3 point, Vector3[] corners)
    {
        // Use the bottom-left corner as the origin
        // and measure along two edges of the rectangle
        Vector3 origin = corners[0];
        Vector3 edgeUp = corners[1] - corners[0];  // bottom-left  → top-left
        Vector3 edgeRight = corners[3] - corners[0];  // bottom-left  → bottom-right
        Vector3 toPoint = point - origin;

        // Project the hit point onto each edge
        float projUp = Vector3.Dot(toPoint, edgeUp);
        float projRight = Vector3.Dot(toPoint, edgeRight);

        float lenUpSq = Vector3.Dot(edgeUp, edgeUp);
        float lenRightSq = Vector3.Dot(edgeRight, edgeRight);

        // Point is inside if both projections fall between 0 and the edge length
        bool inHeight = (projUp >= 0f && projUp <= lenUpSq);
        bool inWidth = (projRight >= 0f && projRight <= lenRightSq);

        return inHeight && inWidth;
    }

    // ====================================================
    // REFRESH — call this if buttons are added after Start()
    // ====================================================
    public void RefreshButtonList()
    {
        allButtons.Clear();
        Button[] found = FindObjectsOfType<Button>(true);
        allButtons.AddRange(found);
        Debug.Log("VRUIPointer: Refreshed — found " + allButtons.Count + " buttons.");
    }
}
