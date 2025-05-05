using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class NavMeshDiscovery : MonoBehaviour
{
    [Header("Close-range FOV")]
    public float  closeViewDistance = 4f;
    [Range(0,360)] public float closeViewAngle = 190f;

    [Header("Far-range FOV")]
    public float  farViewDistance = 10f;
    [Range(0,360)] public float farViewAngle = 60f;

    [Header("Common Settings")]
    public LayerMask obstacleMask;
    public float     scanInterval = 0.2f;

    private NavMeshKnowledge        _chunker;
    private Transform               _t;

    private playerStealth player;
    private Vector3 lastPlayerPos;
    void Start()
    {
        player = playerStealth.instance;
        _t       = transform;
        _chunker = GetComponent<NavMeshKnowledge>();
        StartCoroutine(FOVRoutine());
    }

    IEnumerator FOVRoutine()
    {
        var wait = new WaitForSeconds(scanInterval);
        while (true)
        {
            yield return wait;
            ScanChunks();
        }
    }

    void ScanChunks()
{
    Vector3 eyePos  = _t.position;
    Vector3 forward = _t.forward;

    float halfClose = closeViewAngle * 0.5f;
    float halfFar   = farViewAngle   * 0.5f;
    float sqrClose  = closeViewDistance * closeViewDistance;
    float sqrFar    = farViewDistance   * farViewDistance;

    // ─── First pass: chunk centers ──────────────────────────────────────
    int minX = Mathf.FloorToInt((eyePos.x - farViewDistance - _chunker.gridOrigin.x) / _chunker.cellSize);
    int maxX = Mathf.FloorToInt((eyePos.x + farViewDistance - _chunker.gridOrigin.x) / _chunker.cellSize);
    int minZ = Mathf.FloorToInt((eyePos.z - farViewDistance - _chunker.gridOrigin.y) / _chunker.cellSize);
    int maxZ = Mathf.FloorToInt((eyePos.z + farViewDistance - _chunker.gridOrigin.y) / _chunker.cellSize);

    for (int x = minX; x <= maxX; x++)
    for (int z = minZ; z <= maxZ; z++)
    {
        var coord = new Vector2Int(x, z);
        if (!_chunker._chunks.TryGetValue(coord, out var chunk))
            continue;

        Vector3 toCenter = chunk.worldCenter - eyePos;
        float   d2       = toCenter.sqrMagnitude;

        // close cone
        if (d2 <= sqrClose &&
            Vector3.Angle(forward, toCenter) <= halfClose &&
            !IsOccluded(eyePos, toCenter, closeViewDistance))
        {
            chunk.lastSeen = Time.time;
            continue;
        }

        // far cone
        if (d2 <= sqrFar &&
            Vector3.Angle(forward, toCenter) <= halfFar &&
            !IsOccluded(eyePos, toCenter, farViewDistance))
        {
            chunk.lastSeen = Time.time;
        }
    }

    // ─── Second pass: only the player’s chunk ────────────────────────────
    Vector3 playerPos = player.sneaking
        ? player.normalRayPos.position
        : player.stealthRayPos.position;

    Vector3 toPlayer = playerPos - eyePos;
    var     pCoord   = _chunker.WorldToChunkCoord(playerPos);

    if (_chunker._chunks.TryGetValue(pCoord, out var pChunk))
    {
        bool seenClose = IsPlayerVisible(toPlayer, closeViewDistance, closeViewAngle);
        bool seenFar   = IsPlayerVisible(toPlayer, farViewDistance,   farViewAngle);

        if (seenClose || seenFar)
            pChunk.lastPlayerSeen = Time.time;
    }
}



    
    
    

    /// <summary>
    /// Returns true if the target is within viewDistance, within half of viewAngle,
    /// and not occluded by obstacleMask.
    /// </summary>
    private bool IsPlayerVisible(Vector3 toTarget, float viewDistance, float viewAngle)
    {
        // --- 1) distance ---
        float distSqr = toTarget.sqrMagnitude;
        if (distSqr > viewDistance * viewDistance)
            return false;

        // --- 2) angle ---
        float halfAngle = viewAngle * 0.5f;        // ← make sure it’s 0.5, not 0.05
        float angle    = Vector3.Angle(_t.forward, toTarget);
        if (angle > halfAngle)
            return false;

        // --- 3) occlusion ---
        Vector3 eye = _t.position + Vector3.up * 0.5f;
        if (Physics.Raycast(eye,
                toTarget.normalized,
                out RaycastHit hit,
                viewDistance,
                obstacleMask))
        {
            // if the ray hits something closer than the player, it’s blocked
            return hit.distance >= Mathf.Sqrt(distSqr);
        }

        // no hit → clear line of sight
        return true;
    }


    
    bool IsOccluded(Vector3 eyePos, Vector3 toCenter, float maxDist)
    {
        if (Physics.Raycast(eyePos + Vector3.up * 0.5f,
                            toCenter.normalized,
                            out RaycastHit hit,
                            maxDist,
                            obstacleMask))
        {
            return hit.distance < toCenter.magnitude;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 eyePos   = transform.position;
        Vector3 playerPos = player.sneaking
            ? player.stealthRayPos.position
            : player.normalRayPos.position;
        Vector3 toPlayer = playerPos - eyePos;
        
        bool seenClose = IsPlayerVisible(toPlayer, closeViewDistance, closeViewAngle);
        bool seenFar   = IsPlayerVisible(toPlayer, farViewDistance, farViewAngle);

        Gizmos.color = (seenClose || seenFar) ? Color.green : Color.red;
        Gizmos.DrawLine(eyePos, eyePos + toPlayer);

        // draw your cones underneath
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        DrawFOV(eyePos, transform.forward, closeViewDistance, closeViewAngle);
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        DrawFOV(eyePos, transform.forward, farViewDistance, farViewAngle);
    }



    void DrawFOV(Vector3 origin, Vector3 forward, float dist, float angle)
    {
        float half = angle * 0.5f;
        Quaternion leftRot  = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis( half, Vector3.up);
        Vector3 leftDir  = leftRot  * forward;
        Vector3 rightDir = rightRot * forward;

        // edge rays
        Gizmos.DrawRay(origin, leftDir  * dist);
        Gizmos.DrawRay(origin, rightDir * dist);

        // arc
        const int segments = 30;
        Vector3 prev = origin + leftDir * dist;
        for (int i = 1; i <= segments; i++)
        {
            float a = -half + (i/(float)segments) * angle;
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * forward;
            Vector3 next = origin + dir * dist;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
