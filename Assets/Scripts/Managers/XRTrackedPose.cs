using UnityEngine;
using UnityEngine.XR;

/// <summary>Updates the XR Origin camera and hand anchors from the active XR provider.</summary>
[DefaultExecutionOrder(-100)]
public sealed class XRTrackedPose : MonoBehaviour
{
    public XRControllerInput.Hand source;

    private void Update()
    {
        InputDevice device = XRControllerInput.Device(source);
        if (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked)
        {
            if (source == XRControllerInput.Hand.Head)
                transform.localPosition = new Vector3(0f, 1.6f, 0f);
            return;
        }
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            if (source == XRControllerInput.Hand.Head && position.y < 0.5f)
                position.y = 1.6f;
            transform.localPosition = position;
        }
        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            transform.localRotation = rotation;
    }
}
