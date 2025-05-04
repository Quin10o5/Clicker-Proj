using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Cursor Settings")]
    public bool hideCursor = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    public float accelerationDuration = 1f;      // Time (in seconds) to ease from 0 → full speed
    public AnimationCurve rotationEaseCurve;     // Curve should go from 0 to 1

    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody playerRB;

    private float accelTimer = 0f;

    private void Start()
    {
        // Cursor setup
        if (hideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // If no curve assigned in Inspector, fall back to a default ease-in-out
        if (rotationEaseCurve == null || rotationEaseCurve.length == 0)
        {
            rotationEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void Update()
    {
        // Toggle cursor lock/unlock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            hideCursor = !hideCursor;
            Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !hideCursor;
        }

        // Ensure references are set
        if (player == null || playerObj == null || orientation == null)
        {
            Debug.LogError("ThirdPersonCam: Missing required references.");
            enabled = false;
            return;
        }

        // Gather input
        float horizInput = Input.GetAxis("Horizontal");
        float vertInput  = Input.GetAxis("Vertical");
        Vector3 inputDir = orientation.forward * vertInput + orientation.right * horizInput;

        if (inputDir.sqrMagnitude > 0.0001f)
        {
            // 1) Ramp up our timer until it hits accelerationDuration
            accelTimer = Mathf.Min(accelTimer + Time.deltaTime, accelerationDuration);

            // 2) Normalize (0 → 1), evaluate curve, compute actual speed
            float t                   = accelTimer / accelerationDuration;
            float easedFactor         = rotationEaseCurve.Evaluate(t);
            float adjustedRotationSpeed = rotationSpeed * easedFactor;

            // 3) Smoothly rotate playerObj toward input direction
            Vector3 targetDir = inputDir.normalized;
            playerObj.forward = Vector3.Slerp(
                playerObj.forward,
                targetDir,
                Time.deltaTime * adjustedRotationSpeed
            );
        }
        else
        {
            // Reset so we ease again next time movement starts
            accelTimer = 0f;
        }
    }
}
