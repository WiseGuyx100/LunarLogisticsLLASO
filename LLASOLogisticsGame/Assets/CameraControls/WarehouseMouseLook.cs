using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class WarehouseMouseLook : MonoBehaviour
{
    [SerializeField] private Transform outputCamera;
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private float pitchLimit = 70f;
    private float yaw;
    private float pitch;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.eulerAngles.x), -pitchLimit, pitchLimit);
        SetCursorLocked(true);
        CopyCameraPose();
    }

    private void Update()
    {
        if (!Application.isFocused) return;
        bool escapePressed = false;
        bool clicked = false;
        Vector2 delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (Mouse.current != null)
        {
            clicked = Mouse.current.leftButton.wasPressedThisFrame;
            delta = Mouse.current.delta.ReadValue();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
        clicked = Input.GetMouseButtonDown(0);
        delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
#endif
        if (escapePressed) { SetCursorLocked(false); return; }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (clicked) SetCursorLocked(true);
            return;
        }
        ApplyLook(delta);
    }

    private void ApplyLook(Vector2 delta)
    {
        yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360f);
        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -pitchLimit, pitchLimit);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LateUpdate() { CopyCameraPose(); }

    private void CopyCameraPose()
    {
        if (outputCamera != null && outputCamera != transform)
            outputCamera.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnDisable() { SetCursorLocked(false); }
}
