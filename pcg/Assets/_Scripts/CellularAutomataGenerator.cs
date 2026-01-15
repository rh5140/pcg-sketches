// Code template by Chris Wren and ChatGPT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModularTileCAVisualizer : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int mapWidth = 40;
    public int mapHeight = 40;
    [Range(0f, 1f)]
    public float initialFloorChance = 0.45f;

    [Header("Simulation Settings")]
    public int simulationSteps = 5;
    public int birthLimit = 4;
    public int deathLimit = 3;
    public float stepDelay = 0.5f;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 0;

    [Header("Prefabs")]
    public GameObject roomTilePrefab;

    [Header("Parent Object")]
    public string dungeonParentName = "Dungeon";

    [Header("Floor/Wall Options")]
    public bool invertFloorsAndWalls = false; // Swap all interior floor/wall tiles

    [Header("Gizmos")]
    public bool showGizmos = false;
    public Color floorGizmoColor = new Color(0f, 1f, 0f, 0.25f);
    public Color wallGizmoColor = new Color(1f, 0f, 0f, 0.25f);

    private int[,] mapGrid; // 0 = wall, 1 = floor
    private GameObject dungeonParent;
    private Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        InitializeSeed();
        mapGrid = new int[mapWidth, mapHeight];

        dungeonParent = new GameObject(dungeonParentName);

        InitializeMap();
        StartCoroutine(RunSimulationAnimated());
    }

    void InitializeSeed()
    {
        if (useRandomSeed)
            seed = System.Environment.TickCount;
        Random.InitState(seed);
    }

    void InitializeMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Outer perimeter is always walls
                if (x == 0 || y == 0 || x == mapWidth - 1 || y == mapHeight - 1)
                    mapGrid[x, y] = 0;
                else
                    mapGrid[x, y] = Random.value < initialFloorChance ? 1 : 0;
            }
        }

        if (invertFloorsAndWalls)
            InvertInteriorTiles();

        SpawnTilesFromMap();
    }

    void InvertInteriorTiles()
    {
        for (int x = 1; x < mapWidth - 1; x++)
        {
            for (int y = 1; y < mapHeight - 1; y++)
            {
                mapGrid[x, y] = mapGrid[x, y] == 1 ? 0 : 1;
            }
        }
    }

    IEnumerator RunSimulationAnimated()
    {
        for (int step = 0; step < simulationSteps; step++)
        {
            int[,] newMap = new int[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // Keep outer perimeter solid
                    if (x == 0 || y == 0 || x == mapWidth - 1 || y == mapHeight - 1)
                    {
                        newMap[x, y] = 0;
                        continue;
                    }

                    int floorNeighbors = CountFloorNeighbors(x, y);

                    if (mapGrid[x, y] == 1)
                        newMap[x, y] = (floorNeighbors < deathLimit) ? 0 : 1;
                    else
                        newMap[x, y] = (floorNeighbors > birthLimit) ? 1 : 0;
                }
            }

            mapGrid = newMap;

            if (invertFloorsAndWalls)
                InvertInteriorTiles();

            UpdateTileVisualization();

            yield return new WaitForSeconds(stepDelay);
        }

        RemoveInteriorWalls();
    }

    int CountFloorNeighbors(int x, int y)
    {
        int count = 0;
        for (int nx = x - 1; nx <= x + 1; nx++)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (nx == x && ny == y) continue;

                if (nx < 0 || ny < 0 || nx >= mapWidth || ny >= mapHeight)
                    count++; // treat out-of-bounds as wall
                else if (mapGrid[nx, ny] == 1)
                    count++;
            }
        }
        return count;
    }

    void SpawnTilesFromMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!spawnedTiles.ContainsKey(pos))
                {
                    GameObject tile = Instantiate(roomTilePrefab,
                        new Vector3(x, 0, y),
                        Quaternion.identity,
                        dungeonParent.transform);
                    spawnedTiles[pos] = tile;
                }
            }
        }

        UpdateTileVisualization();
    }

    void UpdateTileVisualization()
    {
        foreach (var kvp in spawnedTiles)
        {
            Vector2Int pos = kvp.Key;
            GameObject tile = kvp.Value;
            bool isFloor = mapGrid[pos.x, pos.y] == 1;
            tile.SetActive(isFloor);
        }
    }

    void RemoveInteriorWalls()
    {
        foreach (var kvp in spawnedTiles)
        {
            Vector2Int pos = kvp.Key;
            GameObject tile = kvp.Value;

            RemoveWallIfFloor(tile, "WallNorth", pos + Vector2Int.up);
            RemoveWallIfFloor(tile, "WallSouth", pos + Vector2Int.down);
            RemoveWallIfFloor(tile, "WallEast", pos + Vector2Int.right);
            RemoveWallIfFloor(tile, "WallWest", pos + Vector2Int.left);
        }
    }

    void RemoveWallIfFloor(GameObject tile, string wallName, Vector2Int neighbor)
    {
        if (neighbor.x >= 0 && neighbor.y >= 0 && neighbor.x < mapWidth && neighbor.y < mapHeight)
        {
            if (mapGrid[neighbor.x, neighbor.y] == 1)
            {
                Transform wall = tile.transform.Find(wallName);
                if (wall != null) Destroy(wall.gameObject);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || mapGrid == null) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Gizmos.color = mapGrid[x, y] == 1 ? floorGizmoColor : wallGizmoColor;
                Gizmos.DrawCube(new Vector3(x, 0.1f, y), Vector3.one);
            }
        }
    }
#endif
}
