using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Your Player GameObject’s root Transform")]
    public Transform target;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    [Range(-89, 89)] public float minPitch = -30f;
    [Range(-89, 89)] public float maxPitch = 60f;
    private float yaw;
    private float pitch;

    [Header("Shoulder Offsets")]
    public Vector3 shoulderOffsetLeft  = new Vector3(0.5f, 1.7f, -2.5f);
    public Vector3 shoulderOffsetRight = new Vector3(-0.5f, 1.7f, -2.5f);
    private bool rightShoulder = false;

    [Header("Smoothing & Collision")]
    [Tooltip("Time for camera to catch up")]
    public float followSmoothTime = 0.1f;
    public float collisionRadius   = 0.2f;
    public LayerMask obstacleMask;
    private Vector3 smoothVel;
    private Vector3 currentOffset;

    void Start()
    {
        if (target == null)
        {
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm) target = pm.transform;
            if (target == null) Debug.LogError("[ThirdPersonCam] target not assigned!");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        yaw   = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        currentOffset = shoulderOffsetLeft;
    }

    void Update()
    {
        // shoulder swap
        if (Input.GetKeyDown(KeyCode.V))
            rightShoulder = !rightShoulder;

        currentOffset = rightShoulder ? shoulderOffsetRight : shoulderOffsetLeft;

        // mouse look
        yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        // toggle cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                                ? CursorLockMode.None
                                : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
    }

    void LateUpdate()
    {
        // build camera rotation
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0);

        // desired world position before collision
        Vector3 desiredPos = target.position + camRot * currentOffset;

        // sphere-cast to avoid clipping
        Vector3 origin = target.position + Vector3.up * currentOffset.y;
        Vector3 dir    = (desiredPos - origin).normalized;
        float   dist   = Vector3.Distance(origin, desiredPos);

        if (Physics.SphereCast(origin, collisionRadius, dir, out RaycastHit hit, dist, obstacleMask))
        {
            desiredPos = hit.point + hit.normal * collisionRadius;
        }

        // smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref smoothVel, followSmoothTime);

        // apply rotation
        transform.rotation = camRot;
    }
}
