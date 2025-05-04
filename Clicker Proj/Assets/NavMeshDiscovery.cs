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

    void Start()
    {
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
        Vector3 eyePos     = _t.position;
        Vector3 forward    = _t.forward;
        float   halfClose  = closeViewAngle * 0.5f;
        float   halfFar    = farViewAngle   * 0.5f;
        float   sqrClose   = closeViewDistance * closeViewDistance;
        float   sqrFar     = farViewDistance   * farViewDistance;

        // bounds based on far distance
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

            // 1) Close-cone check
            if (d2 <= sqrClose)
            {
                float ang = Vector3.Angle(forward, toCenter);
                if (ang <= halfClose && !IsOccluded(eyePos, toCenter, closeViewDistance))
                {
                    chunk.lastSeen = Time.time;
                    continue; // skip far check
                }
            }

            // 2) Far-cone check
            if (d2 <= sqrFar)
            {
                float ang = Vector3.Angle(forward, toCenter);
                if (ang <= halfFar && !IsOccluded(eyePos, toCenter, farViewDistance))
                {
                    chunk.lastSeen = Time.time;
                }
            }
        }
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

        Vector3 origin  = transform.position;
        Vector3 forward = transform.forward;

        // draw close-cone in translucent yellow
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        DrawFOV(origin, forward, closeViewDistance, closeViewAngle);

        // draw far-cone in translucent cyan
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        DrawFOV(origin, forward, farViewDistance, farViewAngle);
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
