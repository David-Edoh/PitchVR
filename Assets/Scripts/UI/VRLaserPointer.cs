// FILE: VRLaserPointer.cs
// PURPOSE: Shows a visible laser beam from the right VR
//          controller for pointing at UI elements.
// 
// AUTHOR: Person 1
// DATE CREATED: June 2026
// ============================================================

using UnityEngine;

public class VRLaserPointer : MonoBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("Color of the laser beam")]
    public Color laserColor = Color.cyan;

    [Tooltip("Maximum length of the laser beam")]
    public float laserLength = 10f;

    [Tooltip("Width of the laser beam")]
    public float laserWidth = 0.005f;

    // Private
    private LineRenderer lineRenderer;

    void Start()
    {
        // Create the LineRenderer for the laser visual
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.positionCount = 2;

        // Create a simple unlit material for the laser
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = laserColor;

        // Don't use world space — positions are relative to this object
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
        // Start point: controller position
        Vector3 startPos = transform.position;

        // End point: forward from controller
        Vector3 endPos = startPos + transform.forward * laserLength;

        // Check if laser hits something (shorten the beam)
        RaycastHit hit;
        if (Physics.Raycast(startPos, transform.forward, out hit, laserLength))
        {
            endPos = hit.point;
        }

        // Update the laser visual
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}
