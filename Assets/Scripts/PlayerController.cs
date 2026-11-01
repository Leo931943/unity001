using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private static readonly Vector3 SpawnPosition = new Vector3(0f, 0.02f, -8f);
    [Header("Model Alignment")]
    [Tooltip("Visual offset for model to match ground plane")]
    public Vector3 modelOffset = Vector3.zero;
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
    [Tooltip("Maximum one-touch impulse applied to the soccer ball")]
    [Min(0f)] public float kickForce = 1.4f;
    [Tooltip("Minimum delay between touches of the same ball")]
    [Min(0.01f)] public float ballTouchCooldown = 0.12f;

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
    private bool isJumping;
    private Rigidbody lastTouchedBall;
    private float lastBallTouchTime;

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

        // Movement is controlled by CharacterController.  Letting a humanoid
        // clip also apply root motion offsets the visible mesh and made it sink
        // into the pitch while idle.
        if (animator != null)
            animator.applyRootMotion = false;

        // The CharacterController's origin sits at ground level, while this FBX
        // is authored around the hips.  Keep the rendered mesh above that origin
        // so only its feet, rather than its torso, meet the pitch.
        Transform model = transform.Find("Meshy_Model");
        if (model != null)
        {
            model.localPosition = modelOffset;
        }

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
            isJumping = false;
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
            isJumping = true;
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
        animator.SetBool("IsJumping", isJumping || (!isGrounded && verticalVelocity > 1.0f));
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
        transform.position = SpawnPosition;
        transform.rotation = Quaternion.identity;
        verticalVelocity = 0;
        currentSpeed = 0;
        isJumping = false;
        controller.enabled = true;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // CharacterController contacts are reported every frame.  Applying a full
        // impulse on each report made the ball accelerate unrealistically fast.
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic || currentSpeed < 0.2f)
            return;

        if (rb == lastTouchedBall && Time.time < lastBallTouchTime + ballTouchCooldown)
            return;

        Vector3 planarMove = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (planarMove.sqrMagnitude < 0.0001f)
            return;

        float speedFactor = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, runSpeed));
        Vector3 pushDir = (planarMove.normalized + Vector3.up * 0.08f).normalized;
        float impulse = Mathf.Lerp(0.45f, Mathf.Min(kickForce, 1.8f), speedFactor);

        rb.AddForce(pushDir * impulse, ForceMode.Impulse);
        if (rb.linearVelocity.magnitude > 12f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * 12f;
        }

        lastTouchedBall = rb;
        lastBallTouchTime = Time.time;
    }
}
