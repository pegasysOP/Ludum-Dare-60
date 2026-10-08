using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody rb;
    public CameraController cameraController;
    public float moveSpeed;
    public float moveAcceleration;
    public float maxVelocity;
    private InputAction jumpAction;
    private InputAction moveAction;

    [Header("Jumping")]
    public GroundDetector groundDetector;
    public float jumpForce;
    public float airAcceleration;
    public float jumpCooldown = 0.30f;

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    public Vector3 inputDir;
    public float speedDebug;
    public bool groundDebug;

    private float footstepTimer;
    private bool wasGrounded;
    private float airTime;
    private float firstGroundedTime = -1f;
    private float lastJumpTime = -999f;

    private bool inCutscene = false;

    public void SetInCutscene(bool value)
    {
        inCutscene = value;
        if (value)
        {
            inputDir = Vector3.zero;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }
    }

    public IEnumerator WalkTo(Vector3 target)
    {
        if (rb == null) yield break;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        //footstepTimer = FootstepInterval;

        while (true)
        {
            Vector3 pos = rb.position;
            Vector3 delta = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
            float dist = delta.magnitude;
            float step = moveSpeed * Time.fixedDeltaTime;

            if (dist <= step || dist < 0.001f)
            {
                rb.MovePosition(new Vector3(target.x, pos.y, target.z));
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                yield break;
            }

            Vector3 dir = delta / dist;
            Vector3 next = pos + dir * step;
            next.y = pos.y;
            rb.MovePosition(next);

            Vector3 lv = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, lv.y, 0f);

            bool grounded = groundDetector != null && groundDetector.IsGrounded;
            if (grounded)
            {
                footstepTimer -= Time.fixedDeltaTime;
                if (footstepTimer <= 0f)
                {
                    //if (movementSounds != null)
                    //    movementSounds.footsteps.PlayAt(transform.position);
                    //footstepTimer = FootstepInterval;
                }
            }

            yield return wait;
        }
    }

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
        moveAction = InputSystem.actions.FindAction("Move");
        wasGrounded = groundDetector != null && groundDetector.IsGrounded;
    }

    private void Update()
    {
        if (GameManager.Instance.LOCKED || inCutscene)
        {
            inputDir = Vector3.zero;
            return;
        }

        HandleJumping();
        HandleMovement();
    }

    private void HandleJumping()
    {
        if (jumpAction.triggered && groundDetector.IsGrounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
            //animator.SetBool("isJumping", true);
        }

        if (groundDetector.IsGrounded)
        {
            //animator.SetBool("isJumping", false);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        inputDir = new Vector3(moveValue.x, 0, moveValue.y);
    }

    private void FixedUpdate()
    {
        if (inCutscene) return;

        rb.MoveRotation(Quaternion.Euler(0, cameraController.yaw, 0));

        Vector3 moveDir = transform.TransformDirection(inputDir.normalized);
        float velocityX = Mathf.Clamp(moveDir.x * moveSpeed, -maxVelocity, maxVelocity);
        float velocityZ = Mathf.Clamp(moveDir.z * moveSpeed, -maxVelocity, maxVelocity);
        Vector3 targetVelocity = new Vector3(velocityX, rb.linearVelocity.y, velocityZ);

        bool wantsMove = inputDir.sqrMagnitude > 0.01f;
        bool grounded = groundDetector != null && groundDetector.IsGrounded;

        if (!grounded)
            airTime += Time.fixedDeltaTime;

        if (grounded && firstGroundedTime < 0f)
            firstGroundedTime = Time.time;

        if (grounded && !wasGrounded)
        {
            airTime = 0f;
        }
        wasGrounded = grounded;

        float currentAcceleration = grounded ? moveAcceleration : airAcceleration;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, currentAcceleration * Time.deltaTime);

        Vector3 groundNormal = groundDetector != null ? groundDetector.GroundNormal : Vector3.up;
        Vector3 gravity = -groundNormal * Physics.gravity.magnitude * rb.mass;
        rb.AddForce(gravity, ForceMode.Acceleration);

        speedDebug = rb.linearVelocity.magnitude;
        groundDebug = groundDetector.IsGrounded;
    }
}