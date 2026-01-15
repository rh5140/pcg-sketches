// Code template by Chris Wren and ChatGPT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModularTileDFSDungeonAnimated : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int mapWidth = 40;
    public int mapHeight = 40;

    [Header("Generation Timing")]
    [Tooltip("Seconds between carve steps")]
    public float carveDelay = 0.02f;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 0;

    [Header("Prefabs")]
    [Tooltip("1x1 modular tile with Floor + WallNorth/South/East/West")]
    public GameObject roomTilePrefab;

    [Header("Room Options")]
    public bool generateRooms = false;
    [Tooltip("Minimum room size in tiles")]
    public int roomMinSize = 3;
    [Tooltip("Maximum room size in tiles")]
    public int roomMaxSize = 6;

    [Header("DFS Gizmo Visualization")]
    public bool showDFSGizmos = false;
    public float gizmoHeight = 2.0f;
    public Color gizmoForwardColor = new Color(0f, 0.8f, 1f, 0.35f);
    public Color gizmoBacktrackColor = new Color(1f, 0.2f, 0.2f, 0.35f);

    private int[,] mapGrid; // 0 = wall, 1 = floor

    private Dictionary<Vector2Int, GameObject> spawnedTiles =
        new Dictionary<Vector2Int, GameObject>();

    private HashSet<Vector2Int> forwardSteps = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> backtrackSteps = new HashSet<Vector2Int>();

    private readonly Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private GameObject dungeonParent;

    // ------------------------------
    // Unity lifecycle
    // ------------------------------
    void Start()
    {
        InitializeSeed();
        InitializeGrid();

        dungeonParent = new GameObject("Dungeon");

        StartCoroutine(GenerateDungeon());
    }

    void InitializeSeed()
    {
        if (useRandomSeed)
            seed = System.Environment.TickCount;

        Random.InitState(seed);
    }

    void InitializeGrid()
    {
        mapGrid = new int[mapWidth, mapHeight];
    }

    // ------------------------------
    // Main generation
    // ------------------------------
    IEnumerator GenerateDungeon()
    {
        Vector2Int start = new Vector2Int(1, 1);

        mapGrid[start.x, start.y] = 1;
        SpawnTile(start);

        yield return StartCoroutine(DepthFirstCarve(start));

        if (generateRooms)
        {
            GenerateRooms();
        }

        RemoveInteriorWalls();
    }

    // ------------------------------
    // DFS Carving
    // ------------------------------
    IEnumerator DepthFirstCarve(Vector2Int current)
    {
        forwardSteps.Add(current);

        List<Vector2Int> shuffledDirs = new List<Vector2Int>(directions);
        Shuffle(shuffledDirs);

        foreach (Vector2Int dir in shuffledDirs)
        {
            Vector2Int next = current + dir * 2;

            if (IsInside(next) && mapGrid[next.x, next.y] == 0)
            {
                Vector2Int between = current + dir;

                mapGrid[between.x, between.y] = 1;
                mapGrid[next.x, next.y] = 1;

                SpawnTile(between);
                SpawnTile(next);

                forwardSteps.Add(between);
                forwardSteps.Add(next);

                yield return new WaitForSeconds(carveDelay);

                yield return StartCoroutine(DepthFirstCarve(next));
            }
        }

        backtrackSteps.Add(current);
    }

    // ------------------------------
    // Spawn tile
    // ------------------------------
    void SpawnTile(Vector2Int pos)
    {
        if (spawnedTiles.ContainsKey(pos))
            return;

        GameObject tile = Instantiate(
            roomTilePrefab,
            new Vector3(pos.x, 0, pos.y),
            Quaternion.identity,
            dungeonParent.transform
        );

        spawnedTiles[pos] = tile;
    }

    // ------------------------------
    // Generate optional rooms
    // ------------------------------
    void GenerateRooms()
    {
        List<Vector2Int> floorTiles = new List<Vector2Int>();

        // Collect all floor tiles
        foreach (var kvp in spawnedTiles)
        {
            floorTiles.Add(kvp.Key);
        }

        int roomAttempts = Mathf.Max(1, floorTiles.Count / 20);

        for (int i = 0; i < roomAttempts; i++)
        {
            Vector2Int center = floorTiles[Random.Range(0, floorTiles.Count)];

            int roomW = Random.Range(roomMinSize, roomMaxSize + 1);
            int roomH = Random.Range(roomMinSize, roomMaxSize + 1);

            int startX = Mathf.Clamp(center.x - roomW / 2, 1, mapWidth - roomW - 1);
            int startY = Mathf.Clamp(center.y - roomH / 2, 1, mapHeight - roomH - 1);

            // Carve room
            for (int x = startX; x < startX + roomW; x++)
            {
                for (int y = startY; y < startY + roomH; y++)
                {
                    if (mapGrid[x, y] == 0)
                    {
                        mapGrid[x, y] = 1;
                        SpawnTile(new Vector2Int(x, y));
                    }
                }
            }
        }
    }

    // ------------------------------
    // Remove interior walls
    // ------------------------------
    void RemoveInteriorWalls()
    {
        foreach (var pair in spawnedTiles)
        {
            Vector2Int pos = pair.Key;
            GameObject tile = pair.Value;

            TryRemoveWall(tile, "WallNorth", pos + Vector2Int.up);
            TryRemoveWall(tile, "WallSouth", pos + Vector2Int.down);
            TryRemoveWall(tile, "WallEast", pos + Vector2Int.right);
            TryRemoveWall(tile, "WallWest", pos + Vector2Int.left);
        }
    }

    void TryRemoveWall(GameObject tile, string wallName, Vector2Int neighbor)
    {
        if (HasFloor(neighbor))
        {
            Transform wall = tile.transform.Find(wallName);
            if (wall != null)
                Destroy(wall.gameObject);
        }
    }

    // ------------------------------
    // DFS Gizmo visualization
    // ------------------------------
    void OnDrawGizmos()
    {
        if (!showDFSGizmos) return;

        foreach (Vector2Int pos in forwardSteps)
        {
            Gizmos.color = gizmoForwardColor;
            Gizmos.DrawCube(
                new Vector3(pos.x, gizmoHeight, pos.y),
                new Vector3(1f, 0.05f, 1f)
            );
        }

        foreach (Vector2Int pos in backtrackSteps)
        {
            Gizmos.color = gizmoBacktrackColor;
            Gizmos.DrawCube(
                new Vector3(pos.x, gizmoHeight + 0.05f, pos.y),
                new Vector3(1f, 0.05f, 1f)
            );
        }
    }

    // ------------------------------
    // Utilities
    // ------------------------------
    bool HasFloor(Vector2Int p)
    {
        if (p.x < 0 || p.y < 0 || p.x >= mapWidth || p.y >= mapHeight)
            return false;
        return mapGrid[p.x, p.y] == 1;
    }

    bool IsInside(Vector2Int p)
    {
        return p.x > 0 && p.y > 0 && p.x < mapWidth - 1 && p.y < mapHeight - 1;
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
