using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PointDistribution2D : MonoBehaviour
{
    [Header("Area Settings")]
    public Vector2 areaSize = new Vector2(10f, 10f);

    [Header("Point Settings")]
    [Min(1)] public int pointCount = 100;

    [Header("Prefab Settings")]
    public GameObject prefab; // Prefab to spawn at each point
    public Transform parent;  // Optional parent for spawned objects

    [Header("Distribution Type")]
    public bool usePoissonDisk = false;

    [Header("Density / Clustering")]
    [Range(0f, 1f)]
    [Tooltip("0 = uniform, 1 = highly clustered")]
    public float clustering = 0.25f;

    [Header("Poisson Disk Settings")]
    [Tooltip("Base minimum distance between points")]
    public float basePoissonRadius = 1f;

    [Header("Debug")]
    public bool regenerate = false;
    public bool drawGizmos = true;

    public List<Vector2> points = new List<Vector2>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void OnValidate()
    {
        if (regenerate)
        {
            regenerate = false;
            GeneratePoints();
            SpawnPrefabs();
        }
    }

    void Start()
    {
        GeneratePoints();
        SpawnPrefabs();
    }

    public void GeneratePoints()
    {
        points.Clear();

        if (usePoissonDisk)
        {
            points = PoissonDiskSampling.GeneratePoints(
                Mathf.Lerp(basePoissonRadius * 1.5f, basePoissonRadius * 0.3f, clustering),
                areaSize,
                30,
                pointCount
            );
        }
        else
        {
            points = GenerateHaltonPoints();
        }
    }

    public void SpawnPrefabs()
    {
        // Clear existing objects
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(obj);
                else
#endif
                    Destroy(obj);
            }
        }
        spawnedObjects.Clear();

        if (prefab == null) return;

        // Spawn new prefabs
        foreach (var p in points)
        {
            Vector3 spawnPos = new Vector3(p.x, 0, p.y); // XZ plane
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity, parent);
            spawnedObjects.Add(obj);
        }
    }

    #region Halton

    List<Vector2> GenerateHaltonPoints()
    {
        List<Vector2> result = new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            float x = Halton(i + 1, 2);
            float y = Halton(i + 1, 3);

            Vector2 p = new Vector2(x, y);

            // Add clustering via jitter
            float jitterStrength = clustering * 0.5f;
            p += Random.insideUnitCircle * jitterStrength;

            p.x = Mathf.Clamp01(p.x);
            p.y = Mathf.Clamp01(p.y);

            // Scale to area
            p = new Vector2(
                (p.x - 0.5f) * areaSize.x,
                (p.y - 0.5f) * areaSize.y
            );

            result.Add(p);
        }

        return result;
    }

    float Halton(int index, int baseValue)
    {
        float result = 0f;
        float f = 1f / baseValue;

        while (index > 0)
        {
            result += f * (index % baseValue);
            index /= baseValue;
            f /= baseValue;
        }

        return result;
    }

    #endregion

    void OnDrawGizmos()
    {
        if (!drawGizmos || points == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, 0, areaSize.y));

        Gizmos.color = Color.cyan;
        foreach (var p in points)
        {
            Gizmos.DrawSphere(new Vector3(p.x, 0, p.y), 0.08f);
        }
    }
}

/// <summary>
/// Simple Poisson Disk Sampling (Bridson-style)
/// </summary>
public static class PoissonDiskSampling
{
    public static List<Vector2> GeneratePoints(
        float radius,
        Vector2 regionSize,
        int rejectionSamples,
        int maxPoints
    )
    {
        float cellSize = radius / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt(regionSize.x / cellSize);
        int gridHeight = Mathf.CeilToInt(regionSize.y / cellSize);

        int[,] grid = new int[gridWidth, gridHeight];
        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        spawnPoints.Add(Vector2.zero);

        while (spawnPoints.Count > 0 && points.Count < maxPoints)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool accepted = false;

            for (int i = 0; i < rejectionSamples; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);

                if (IsValid(candidate, regionSize, cellSize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);

                    int x = (int)((candidate.x + regionSize.x / 2) / cellSize);
                    int y = (int)((candidate.y + regionSize.y / 2) / cellSize);
                    grid[x, y] = points.Count;

                    accepted = true;
                    break;
                }
            }

            if (!accepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        return points;
    }

    static bool IsValid(
        Vector2 candidate,
        Vector2 regionSize,
        float cellSize,
        float radius,
        List<Vector2> points,
        int[,] grid
    )
    {
        if (candidate.x < -regionSize.x / 2 || candidate.x > regionSize.x / 2 ||
            candidate.y < -regionSize.y / 2 || candidate.y > regionSize.y / 2)
            return false;

        int cellX = (int)((candidate.x + regionSize.x / 2) / cellSize);
        int cellY = (int)((candidate.y + regionSize.y / 2) / cellSize);

        int searchStartX = Mathf.Max(0, cellX - 2);
        int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
        int searchStartY = Mathf.Max(0, cellY - 2);
        int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                int index = grid[x, y] - 1;
                if (index != -1)
                {
                    float sqrDist = (candidate - points[index]).sqrMagnitude;
                    if (sqrDist < radius * radius)
                        return false;
                }
            }
        }

        return true;
    }
}
