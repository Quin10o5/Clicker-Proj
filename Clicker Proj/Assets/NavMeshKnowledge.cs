using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class NavChunk
{
    public Vector2Int coord;       // (x, z) indices
    public float      interest;    // for pathfinding targets
    public float      lastVisited; // Time.time when last visited
    public float      lastSeen;    // Time.time when last seen
    public Vector3    worldCenter; // for pathfinding targets
}

public class NavMeshKnowledge : MonoBehaviour
{
    [Header("Grid Settings")]
    public float   cellSize    = 5f;
    public bool useSetGridPos = false;
    public Vector2 setGridOrigin  = Vector2.zero;
    [HideInInspector]
    public Vector2 gridOrigin;
    public int     gridWidth   = 20;
    public int     gridHeight  = 20;

    [Header("Debug Gizmos")]
    public bool  drawGizmos    = true;
    public float debugMaxTime  = 30f; // seconds until fully “stale”

    public Dictionary<Vector2Int, NavChunk> _chunks;

    void Awake()
    {
        // ─────────────── gridOrigin ───────────────
        if (useSetGridPos)
        {
            // manual bottom‐left origin
            gridOrigin = setGridOrigin;
        }
        else
        {
            // center the grid on this Transform
            float halfW = gridWidth  * cellSize * 0.5f;
            float halfH = gridHeight * cellSize * 0.5f;
            Vector3 p  = transform.position;
            // bottom‐left corner so centre is at p
            gridOrigin = new Vector2(p.x - halfW, p.z - halfH);
        }

        // ─────────────── build chunks ───────────────
        _chunks = new Dictionary<Vector2Int, NavChunk>(gridWidth * gridHeight);
        for (int x = 0; x < gridWidth; x++)
        for (int z = 0; z < gridHeight; z++)
        {
            var coord = new Vector2Int(x, z);
            var center = new Vector3(
                gridOrigin.x + (x + 0.5f) * cellSize,
                0,
                gridOrigin.y + (z + 0.5f) * cellSize
            );

            NavMeshHit hit;
            if (NavMesh.SamplePosition(center, out hit, cellSize * 0.7f, NavMesh.AllAreas))
            {
                var chunk = new NavChunk {
                    coord        = coord,
                    worldCenter  = hit.position,
                    lastVisited  = float.NegativeInfinity,
                    lastSeen     = float.NegativeInfinity,
                    interest     = -1
                };
                _chunks.Add(coord, chunk);
            }
        }
    }


    public void MarkVisited(Vector3 worldPos)
    {
        var coord = new Vector2Int(
            Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize),
            Mathf.FloorToInt((worldPos.z - gridOrigin.y) / cellSize)
        );
        if (_chunks.TryGetValue(coord, out var c))
        {
            c.lastVisited = Time.time;
            c.lastSeen    = Time.time;
        }
    }

    public void MarkSeen(Vector3 worldPos)
    {
        var coord = new Vector2Int(
            Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize),
            Mathf.FloorToInt((worldPos.z - gridOrigin.y) / cellSize)
        );
        if (_chunks.TryGetValue(coord, out var c))
            c.lastSeen = Time.time;
    }

    public NavChunk GetLeastRecentlyVisitedChunk()
    {
        NavChunk best = null;
        float bestTime = float.MaxValue;
        foreach (var c in _chunks.Values)
        {
            if (c.lastVisited < bestTime)
            {
                bestTime = c.lastVisited;
                best = c;
            }
        }
        return best;
    }

    // —— DEBUG GIZMOS —— 
    void OnDrawGizmos()
    {
        if (!drawGizmos || _chunks == null) return;

        float now = Application.isPlaying ? Time.time : float.PositiveInfinity;
        foreach (var c in _chunks.Values)
        {
            // VISITED: green → red
            float ageV = now - c.lastVisited;
            float tV   = float.IsInfinity(c.lastVisited)
                       ? 1f
                       : Mathf.Clamp01(ageV / debugMaxTime);
            Color colV = Color.Lerp(Color.green, Color.red, tV);

            // SEEN: blue → yellow
            float ageS = now - c.lastSeen;
            float tS   = float.IsInfinity(c.lastSeen)
                       ? 1f
                       : Mathf.Clamp01(ageS / debugMaxTime);
            Color colS = Color.Lerp(Color.blue, Color.yellow, tS);

            // draw a small solid cube for VISITED
            Gizmos.color = colV;
            Gizmos.DrawCube(
                c.worldCenter + Vector3.up * 0.1f,
                Vector3.one * (cellSize * 0.4f)
            );

            // draw a wireframe cube for SEEN
            Gizmos.color = colS;
            Gizmos.DrawWireCube(
                c.worldCenter + Vector3.up * 0.05f,
                Vector3.one * (cellSize * 0.9f)
            );
        }
    }
}
