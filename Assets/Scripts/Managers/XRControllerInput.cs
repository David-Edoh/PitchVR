using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>Controller input shared by the scene's existing interaction scripts.</summary>
public static class XRControllerInput
{
    public enum Hand { Left, Right, Head }
    public enum Button { Primary, Secondary, Trigger, Grip, Stick, Menu }

    private struct ButtonState
    {
        public int frame;
        public bool current;
        public bool previous;
    }

    private static readonly Dictionary<int, ButtonState> states = new Dictionary<int, ButtonState>();
    private static readonly List<InputDevice> devices = new List<InputDevice>();

    public static InputDevice Device(Hand hand)
    {
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.TrackedDevice;
        characteristics |= hand == Hand.Left ? InputDeviceCharacteristics.Left |
            InputDeviceCharacteristics.Controller : hand == Hand.Right ? InputDeviceCharacteristics.Right |
            InputDeviceCharacteristics.Controller : InputDeviceCharacteristics.HeadMounted;
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
        return devices.Count > 0 ? devices[0] : default;
    }

    public static bool Get(Hand hand, Button button)
    {
        InputDevice device = Device(hand);
        bool pressed;
        switch (button)
        {
            case Button.Primary: return device.TryGetFeatureValue(CommonUsages.primaryButton, out pressed) && pressed;
            case Button.Secondary: return device.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed;
            case Button.Trigger: return device.TryGetFeatureValue(CommonUsages.triggerButton, out pressed) && pressed;
            case Button.Grip: return device.TryGetFeatureValue(CommonUsages.gripButton, out pressed) && pressed;
            case Button.Stick: return device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out pressed) && pressed;
            case Button.Menu: return device.TryGetFeatureValue(CommonUsages.menuButton, out pressed) && pressed;
            default: return false;
        }
    }

    private static ButtonState Read(Hand hand, Button button)
    {
        int key = ((int)hand << 8) | (int)button;
        states.TryGetValue(key, out ButtonState state);
        if (state.frame != Time.frameCount)
        {
            state.previous = state.current;
            state.current = Get(hand, button);
            state.frame = Time.frameCount;
            states[key] = state;
        }
        return state;
    }

    public static bool GetDown(Hand hand, Button button)
    {
        ButtonState state = Read(hand, button);
        return state.current && !state.previous;
    }

    public static bool GetUp(Hand hand, Button button)
    {
        ButtonState state = Read(hand, button);
        return !state.current && state.previous;
    }

    public static Vector2 Stick(Hand hand)
    {
        InputDevice device = Device(hand);
        return device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis) ? axis : Vector2.zero;
    }

    public static void Pulse(Hand hand, float amplitude, float duration)
    {
        InputDevice device = Device(hand);
        if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            device.SendHapticImpulse(0, amplitude, duration);
    }
}
