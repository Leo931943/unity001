using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Camera Distance & Zoom")]
    public float defaultDistance = 5.0f;
    public float minDistance = 2.0f;
    public float maxDistance = 14.0f;
    public float zoomSpeed = 2.5f;

    [Header("Sensitivity & Smoothing")]
    public float mouseSensitivity = 2.0f;
    public float rotationSmoothTime = 0.05f;
    public float minPitch = -25f;
    public float maxPitch = 70f;

    [Header("Collision Avoidance")]
    public LayerMask collisionMask = ~0; // Everything by default
    public float collisionRadius = 0.25f;
    public float collisionOffset = 0.15f;

    private float yaw;
    private float pitch = 15f;
    private float targetDistance;
    private float currentDistance;
    private float currentDistanceVelocity;
    private Vector3 currentRotation;
    private Vector3 rotationSmoothVelocity;
    private bool cursorLocked = true;

    void Start()
    {
        targetDistance = defaultDistance;
        currentDistance = defaultDistance;
        SetCursorLock(true);

        if (target != null)
        {
            yaw = target.eulerAngles.y;
            currentRotation = new Vector3(pitch, yaw, 0);
        }
    }

    void Update()
    {
        HandleCursorLockToggle();
        ReadInput(out float mouseX, out float mouseY, out float scroll);

        if (cursorLocked)
        {
            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Smooth rotation
        Vector3 targetRot = new Vector3(pitch, yaw, 0);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRot, ref rotationSmoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredCameraPos = focusPoint - (rotation * Vector3.forward * targetDistance);

        // Collision detection
        float finalDist = targetDistance;
        RaycastHit hit;
        Vector3 rayDir = (desiredCameraPos - focusPoint).normalized;
        if (Physics.SphereCast(focusPoint, collisionRadius, rayDir, out hit, targetDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            finalDist = Mathf.Clamp(hit.distance - collisionOffset, minDistance, targetDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, finalDist, ref currentDistanceVelocity, 0.05f);
        transform.position = focusPoint - (rotation * Vector3.forward * currentDistance);
        transform.LookAt(focusPoint);
    }

    private void ReadInput(out float mouseX, out float mouseY, out float scroll)
    {
        mouseX = 0;
        mouseY = 0;
        scroll = 0;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            mouseX = delta.x * 0.1f;
            mouseY = delta.y * 0.1f;
            scroll = mouse.scroll.ReadValue().y * 0.01f;
        }
#else
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
    }

    private void HandleCursorLockToggle()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(!cursorLocked);
        }
        else if (!cursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            SetCursorLock(true);
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(!cursorLocked);
        }
        else if (!cursorLocked && Input.GetMouseButtonDown(0))
        {
            SetCursorLock(true);
        }
#endif
    }

    public void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
