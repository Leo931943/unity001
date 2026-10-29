using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Walking speed in m/s")]
    public float walkSpeed = 4.0f;
    [Tooltip("Sprint / running speed in m/s")]
    public float runSpeed = 9.0f;
    [Tooltip("Acceleration and deceleration smoothing")]
    public float speedSmoothTime = 0.1f;
    [Tooltip("Rotation turn smoothing")]
    public float turnSmoothSpeed = 15f;

    [Header("Jumping & Gravity")]
    [Tooltip("Jump velocity in m/s")]
    public float jumpVelocity = 6.5f;
    [Tooltip("Gravity acceleration")]
    public float gravity = -20.0f;
    [Tooltip("Airborne control multiplier")]
    [Range(0f, 1f)]
    public float airControl = 0.6f;

    [Header("Soccer Ball Physics Interaction")]
    [Tooltip("Force applied to soccer ball when kicking")]
    public float kickForce = 7.0f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    // Components
    private CharacterController controller;
    private float currentSpeed;
    private float speedSmoothVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    private bool jumpRequested;

    // Status properties for HUD
    public float CurrentSpeed => currentSpeed;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.skinWidth = 0.02f;
            controller.height = 1.7f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.85f, 0);
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        ReadInput(out Vector2 moveInput, out bool sprintInput, out bool jumpInput, out bool resetInput);

        if (resetInput)
        {
            ResetPosition();
            return;
        }

        if (jumpInput && controller.isGrounded)
        {
            jumpRequested = true;
        }

        IsSprinting = sprintInput && moveInput.sqrMagnitude > 0.05f;
        float targetSpeed = (IsSprinting ? runSpeed : walkSpeed) * moveInput.magnitude;

        // Grounded check
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward velocity to keep stick to slopes
        }

        // Smooth speed transition
        float smoothTime = isGrounded ? speedSmoothTime : (speedSmoothTime / Mathf.Max(0.1f, airControl));
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        // Direction relative to camera
        Vector3 moveDir = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        }

        // Rotate character towards moving direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSmoothSpeed * Time.deltaTime);
        }

        // Jump execution
        if (jumpRequested)
        {
            verticalVelocity = jumpVelocity;
            jumpRequested = false;
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Final motion
        Vector3 velocity = moveDir * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Update Animator parameters if present
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Pass Speed and Ground state
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", !isGrounded && verticalVelocity > 0.5f);
        animator.SetFloat("VerticalVelocity", verticalVelocity);

        // Adjust animation playback speed for walk vs run
        float motionSpeed = 1.0f;
        if (currentSpeed > 0.1f)
        {
            motionSpeed = Mathf.Lerp(0.5f, 1.25f, currentSpeed / runSpeed);
        }
        animator.SetFloat("MotionSpeed", motionSpeed);
    }

    private void ReadInput(out Vector2 moveInput, out bool sprint, out bool jump, out bool reset)
    {
        moveInput = Vector2.zero;
        sprint = false;
        jump = false;
        reset = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            float x = 0;
            float y = 0;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1;
            moveInput = new Vector2(x, y);

            sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            jump = kb.spaceKey.wasPressedThisFrame;
            reset = kb.rKey.wasPressedThisFrame;
        }
#else
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        jump = Input.GetKeyDown(KeyCode.Space);
        reset = Input.GetKeyDown(KeyCode.R);
#endif

        if (moveInput.sqrMagnitude > 1.0f)
            moveInput.Normalize();
    }

    public void ResetPosition()
    {
        controller.enabled = false;
        transform.position = new Vector3(0, 1.2f, 0); // Soccer field center
        transform.rotation = Quaternion.identity;
        verticalVelocity = 0;
        currentSpeed = 0;
        controller.enabled = true;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Interactive soccer ball kicking
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.25f, hit.moveDirection.z).normalized;
            float force = kickForce * Mathf.Max(1.0f, currentSpeed / 2.0f);
            rb.AddForce(pushDir * force, ForceMode.Impulse);
        }
    }
}
