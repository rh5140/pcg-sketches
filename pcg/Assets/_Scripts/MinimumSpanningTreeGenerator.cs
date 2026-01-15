// Code template by Chris Wren and ChatGPT
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MSTDungeonGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;

    [Header("Rooms")]
    public int roomCount = 10;
    public int minRoomSize = 3;
    public int maxRoomSize = 8;

    [Header("Prefabs")]
    public GameObject roomTilePrefab; // 1x1 tile: floor + 4 walls

    [Header("Random")]
    public bool useSeed = false;
    public int seed = 0;

    [Header("Gizmos")]
    public bool showMST = true;
    public Color vertexColor = Color.yellow;
    public Color edgeColor = Color.cyan;
    public float vertexRadius = 0.35f;

    private int[,] map;
    private List<Room> rooms = new();
    private List<Edge> mstEdges = new();
    private List<Tile> placedTiles = new();

    // --------------------------------------------------
    // DATA STRUCTURES
    // --------------------------------------------------

    class Room
    {
        public int x, y, w, h;
        public int index;

        public Vector2Int Center =>
            new Vector2Int(x + w / 2, y + h / 2);
    }

    class Edge
    {
        public Room a;
        public Room b;
        public float weight;

        public Edge(Room a, Room b)
        {
            this.a = a;
            this.b = b;
            weight = Vector2.Distance(a.Center, b.Center);
        }
    }

    class Tile
    {
        public int x, y;
        public GameObject go;
    }

    // --------------------------------------------------
    // UNITY
    // --------------------------------------------------

    void Start()
    {
        if (useSeed)
            Random.InitState(seed);

        map = new int[mapWidth, mapHeight];

        InitializeMap();
        GenerateRooms();
        GenerateMST();
        InstantiateTiles();
        RemoveInteriorWalls();
    }

    // --------------------------------------------------
    // MAP INIT
    // --------------------------------------------------

    void InitializeMap()
    {
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                map[x, y] = 0;
    }

    // --------------------------------------------------
    // ROOM GENERATION
    // --------------------------------------------------

    void GenerateRooms()
    {
        for (int i = 0; i < roomCount; i++)
        {
            int w = Random.Range(minRoomSize, maxRoomSize + 1);
            int h = Random.Range(minRoomSize, maxRoomSize + 1);

            int x = Random.Range(1, mapWidth - w - 1);
            int y = Random.Range(1, mapHeight - h - 1);

            Room room = new Room
            {
                x = x,
                y = y,
                w = w,
                h = h,
                index = i
            };

            rooms.Add(room);

            for (int rx = x; rx < x + w; rx++)
                for (int ry = y; ry < y + h; ry++)
                    map[rx, ry] = 1;
        }
    }

    // --------------------------------------------------
    // MINIMUM SPANNING TREE (Prim)
    // --------------------------------------------------

    void GenerateMST()
    {
        List<Room> connected = new();
        List<Room> remaining = new(rooms);

        connected.Add(remaining[0]);
        remaining.RemoveAt(0);

        while (remaining.Count > 0)
        {
            float best = float.MaxValue;
            Room bestA = null;
            Room bestB = null;

            foreach (Room a in connected)
            {
                foreach (Room b in remaining)
                {
                    float d = Vector2.Distance(a.Center, b.Center);
                    if (d < best)
                    {
                        best = d;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            mstEdges.Add(new Edge(bestA, bestB));
            CarveCorridor(bestA.Center, bestB.Center);

            connected.Add(bestB);
            remaining.Remove(bestB);
        }
    }

    // --------------------------------------------------
    // CORRIDORS
    // --------------------------------------------------

    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        Vector2Int cur = a;

        while (cur.x != b.x)
        {
            map[cur.x, cur.y] = 1;
            cur.x += (b.x > cur.x) ? 1 : -1;
        }

        while (cur.y != b.y)
        {
            map[cur.x, cur.y] = 1;
            cur.y += (b.y > cur.y) ? 1 : -1;
        }
    }

    // --------------------------------------------------
    // TILE INSTANTIATION
    // --------------------------------------------------

    void InstantiateTiles()
    {
        GameObject parent = new GameObject("Dungeon");

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] == 1)
                {
                    GameObject tile = Instantiate(
                        roomTilePrefab,
                        new Vector3(x, 0, y),
                        Quaternion.identity,
                        parent.transform
                    );

                    placedTiles.Add(new Tile { x = x, y = y, go = tile });
                }
            }
        }
    }

    // --------------------------------------------------
    // REMOVE INTERIOR WALLS (ROOMS + CORRIDORS)
    // --------------------------------------------------

    void RemoveInteriorWalls()
    {
        foreach (Tile t in placedTiles)
        {
            Transform n = t.go.transform.Find("WallNorth");
            Transform s = t.go.transform.Find("WallSouth");
            Transform e = t.go.transform.Find("WallEast");
            Transform w = t.go.transform.Find("WallWest");

            if (IsFloor(t.x, t.y + 1)) Destroy(n?.gameObject);
            if (IsFloor(t.x, t.y - 1)) Destroy(s?.gameObject);
            if (IsFloor(t.x + 1, t.y)) Destroy(e?.gameObject);
            if (IsFloor(t.x - 1, t.y)) Destroy(w?.gameObject);
        }
    }

    bool IsFloor(int x, int y)
    {
        if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
            return false;

        return map[x, y] == 1;
    }

    // --------------------------------------------------
    // GIZMOS (MST VISUALIZATION)
    // --------------------------------------------------

    void OnDrawGizmos()
    {
        if (!showMST || rooms == null) return;

        // Draw vertices
        Gizmos.color = vertexColor;
        foreach (Room r in rooms)
        {
            Vector3 p = new Vector3(r.Center.x, 1f, r.Center.y);
            Gizmos.DrawSphere(p, vertexRadius);

#if UNITY_EDITOR
            Handles.Label(p + Vector3.up * 0.2f, ((char)('A' + r.index)).ToString());
#endif
        }

        // Draw edges + weights
        Gizmos.color = edgeColor;
        foreach (Edge e in mstEdges)
        {
            Vector3 a = new Vector3(e.a.Center.x, 1f, e.a.Center.y);
            Vector3 b = new Vector3(e.b.Center.x, 1f, e.b.Center.y);

            Gizmos.DrawLine(a, b);

#if UNITY_EDITOR
            Vector3 mid = (a + b) / 2f;
            Handles.Label(mid + Vector3.up * 0.2f, e.weight.ToString("F1"));
#endif
        }
    }
}
