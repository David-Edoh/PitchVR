using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Editor-only keyboard and mouse navigation for the XR Origin.</summary>
[DefaultExecutionOrder(1000)]
public sealed class EditorXRCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float scrollSpeed = 0.5f;
    [SerializeField] private float keyLookSpeed = 90f;

    private Transform viewTransform;

    private void Awake()
    {
        Camera view = GetComponentInChildren<Camera>();
        if (view != null) viewTransform = view.transform;
    }

    private void Update()
    {
        if (!Application.isEditor || Keyboard.current == null || viewTransform == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Vector3 movement = Vector3.zero;
        if (keyboard.wKey.isPressed) movement += Vector3.forward;
        if (keyboard.sKey.isPressed) movement += Vector3.back;
        if (keyboard.dKey.isPressed) movement += Vector3.right;
        if (keyboard.aKey.isPressed) movement += Vector3.left;
        if (keyboard.eKey.isPressed) movement += Vector3.up;
        if (keyboard.qKey.isPressed) movement += Vector3.down;

        transform.Translate(movement.normalized * moveSpeed * Time.unscaledDeltaTime, Space.Self);

        Vector2 keyLook = Vector2.zero;
        if (keyboard.leftArrowKey.isPressed) keyLook.x = -1f;
        if (keyboard.rightArrowKey.isPressed) keyLook.x = 1f;
        if (keyboard.upArrowKey.isPressed) keyLook.y = 1f;
        if (keyboard.downArrowKey.isPressed) keyLook.y = -1f;
        RotateView(keyLook * keyLookSpeed * Time.unscaledDeltaTime);

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 scroll = mouse.scroll.ReadValue();
        if (scroll.y != 0f)
            transform.position += viewTransform.forward * (scroll.y * scrollSpeed * Time.unscaledDeltaTime);

        if (mouse.rightButton.isPressed)
        {
            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            RotateView(new Vector2(look.x, -look.y));
        }
    }

    private void RotateView(Vector2 look)
    {
        Vector3 pivot = viewTransform.position;
        transform.RotateAround(pivot, Vector3.up, look.x);
        transform.RotateAround(pivot, transform.right, look.y);
    }
}
