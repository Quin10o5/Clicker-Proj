using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public bool isCrouching = false;
    [Header("Speeds")]
    public float walkSpeed   = 3f;
    public float runSpeed    = 5.5f;
    public float crouchSpeed = 2f;

    [Header("Acceleration")]
    public float accelRate = 20f;
    public float decelRate = 20f;

    [Header("Rotation Smoothing")]
    [Tooltip("Higher = slower turn")]
    public float turnSmoothTime = 0.3f;
    private float turnSmoothVel;

    [Header("References")]
    [Tooltip("Your camera pivot—used to orient movement")]
    public Transform orientation;
    private Rigidbody rb;
    private playerStealth stealth;

    float currentSpeed;
    Vector3 inputDir;

    [Header("Ground Check")]
    [Tooltip("Half-height + a little extra")]
    public float groundCheckDistance = 1.1f;
    public LayerMask groundMask = ~0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        stealth = GetComponent<playerStealth>();

        if (orientation == null)
        {
            if (Camera.main != null)
            {
                orientation = Camera.main.transform;
                Debug.LogWarning("[PlayerMovement] orientation was null—defaulting to MainCamera.");
            }
            else Debug.LogError("[PlayerMovement] orientation is not assigned!");
        }
    }

    

    void Update()
    {
        // 1) read movement input
        Vector2 raw = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        inputDir    = raw.normalized;

        // 2) toggle crouch on key-down
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }
        stealth.sneaking = isCrouching;

        // 3) decide target speed
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        float target = isCrouching    ? crouchSpeed
            : sprint          ? runSpeed
            :                   walkSpeed;
        target *= inputDir.magnitude;

        // 4) smooth accel/decel
        float rate = (currentSpeed < target) ? accelRate : decelRate;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * Time.deltaTime);
    }

    void FixedUpdate()
    {
        // only move if grounded
        if (!IsGrounded()) return;

        if (inputDir.sqrMagnitude < 0.01f) return;

        // build movement vector relative to camera
        Vector3 moveDir = orientation.forward * inputDir.y
                        + orientation.right   * inputDir.x;
        moveDir.y = 0f;

        // apply instantaneous velocity change
        Vector3 desiredVel = moveDir.normalized * currentSpeed;
        Vector3 flatVel    = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 deltaV     = desiredVel - flatVel;
        rb.AddForce(deltaV, ForceMode.VelocityChange);

        // smooth turn toward movement direction
        float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float yaw       = Mathf.SmoothDampAngle(
                              transform.eulerAngles.y,
                              targetYaw,
                              ref turnSmoothVel,
                              turnSmoothTime
                          );
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position;
        bool grounded = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask
        );
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
        return grounded;
    }
}
