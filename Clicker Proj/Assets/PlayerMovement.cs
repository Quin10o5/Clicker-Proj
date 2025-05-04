using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementModEffectSpeed = 10f;
    public bool crouched;
    [Range(0,1)]
    public float crouchSpeedMod = .5f;
    public bool sprinting;
    [Range(1,3)]
    public float sprintSpeedMod = 2f;
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 10f;
    [SerializeField] private float desiredMoveSpeed;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float accelerationDuration = 1f;      // Time (in seconds) to ease from 0 → full speed
    [SerializeField] private AnimationCurve movementEaseCurve;     // Curve should go from 0 → 1

    [Header("References")]
    [SerializeField] private Transform Orientation;

    public Vector3 moveDirection;
    private Rigidbody rb;
    private float accelTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (Orientation == null)
            Debug.LogError("PlayerMovement: Orientation is not assigned!");

        // Fallback ease-in-out curve if none assigned
        if (movementEaseCurve == null || movementEaseCurve.length == 0)
            movementEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.LeftShift)) sprinting = true;
        else sprinting = false;
        if(Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C)) crouched = !crouched;
        UpdateSprinting();
        HandleInput();
        SpeedControl();
        moveSpeed = Mathf.Lerp(moveSpeed, desiredMoveSpeed, Time.deltaTime * movementModEffectSpeed);
        
    }

    void UpdateSprinting()
    {
        if(sprinting) desiredMoveSpeed = baseMoveSpeed * sprintSpeedMod;
        else desiredMoveSpeed = baseMoveSpeed;
        UpdateCrouching();
    }
    void UpdateCrouching()
    {
        if(crouched) desiredMoveSpeed *= crouchSpeedMod;
    }
    
    

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void FixedUpdate()
    {
        if (moveDirection.sqrMagnitude > 0.0001f && IsGrounded())
        {
            // 1) Ramp timer up to accelerationDuration
            accelTimer = Mathf.Min(accelTimer + Time.fixedDeltaTime, accelerationDuration);

            // 2) Compute eased factor (0 → 1)
            float t           = accelTimer / accelerationDuration;
            float easedFactor = movementEaseCurve.Evaluate(t);

            // 3) Apply movement force scaled by easedFactor
            rb.AddForce(moveDirection * moveSpeed * easedFactor * 10f, ForceMode.Force);

            // 4) Clamp horizontal velocity to moveSpeed
            Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizVel.magnitude > moveSpeed)
            {
                horizVel = horizVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(horizVel.x, rb.linearVelocity.y, horizVel.z);
            }
        }
        else
        {
            // Reset so it eases again next time you start moving
            accelTimer = 0f;
        }
    }

    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveDirection = (Orientation.forward * v + Orientation.right * h).normalized;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 2f);
    }
}
