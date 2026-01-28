// Code template by Chris Wren and ChatGPT
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModularTileBSPDungeon : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int mapWidth = 40;
    public int mapHeight = 40;
    public int minPartitionSize = 6;
    public int maxRoomSize = 10;

    [Header("Random Seed")]
    [Tooltip("If false, dungeon generation will be deterministic using the seed below.")]
    public bool useRandomSeed = true;
    public int seed = 0;

    [Header("Prefabs")]
    public GameObject roomTilePrefab; // 1x1 tile with Floor + 4 Walls

    [Header("Gizmo Settings")]
    public float levelHeight = 1.0f;
    public bool showBSPNodes = false;
    public bool showRooms = false;
    public bool showLabels = false;

    private int[,] mapGrid; // 0 = empty, 1 = room, 2 = corridor
    private List<RoomTile> placedTiles = new List<RoomTile>();
    private BSPNode rootNode;

    // My additions
    public GameObject objectToSpawn;
    public GameObject player;

    // -----------------------------
    // Unity Start
    // -----------------------------
    void Start()
    {
        // Initialize RNG
        if (useRandomSeed)
        {
            seed = System.Environment.TickCount;
        }
        Random.InitState(seed);

        mapGrid = new int[mapWidth, mapHeight];

        rootNode = new BSPNode(0, 0, mapWidth, mapHeight);

        Split(rootNode);
        CreateRooms(rootNode);
        ConnectRooms(rootNode);
        InstantiateTiles();
        RemoveInteriorWalls();

        AddRandomObjects();
        
    }

    void AddRandomObjects()
    {
        int itr = 0;
        foreach (Transform child in gameObject.transform)
        {
            if (itr == 0) // spawn player in a random corridor whatever man
            {
                int spawnPoint = Random.Range(0, child.childCount);
                Debug.Log(child);
                Debug.Log(child.GetChild(spawnPoint));
                Debug.Log(child.GetChild(spawnPoint).position);
                Instantiate(player, new Vector3(-100,100,100), Quaternion.identity);
            }
            if (itr != 0) // skip first GameObject since it's all the corridors
            {
                int numSpaces = child.childCount;
                if (numSpaces > 0) // is a room
                {
                    int chance = Random.Range(0,2); // magic number for now
                    if (chance < 1)
                    {
                        int spawnPoint = Random.Range(0, numSpaces);
                        Instantiate(objectToSpawn, child.GetChild(spawnPoint).position + new Vector3(0,0.5f,0), Quaternion.identity);
                    }
                }
            }
            itr++;
        }
        return;
    }

    // -----------------------------
    // Data Structures
    // -----------------------------
    class BSPNode
    {
        public int x, y, width, height;
        public BSPNode left, right;
        public Room room;
        public string label;
        public GameObject hierarchyParent;

        public BSPNode(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            width = w;
            height = h;
        }

        public bool IsLeaf() => left == null && right == null;
    }

    class Room
    {
        public int x, y, width, height;
        public int centerX => x + width / 2;
        public int centerY => y + height / 2;
    }

    enum TileType { Room, Corridor }

    class RoomTile
    {
        public int x, y;
        public GameObject instance;
        public TileType type;
    }

    // -----------------------------
    // BSP Splitting
    // -----------------------------
    void Split(BSPNode node)
    {
        if (node.width <= minPartitionSize * 2 &&
            node.height <= minPartitionSize * 2)
            return;

        bool splitHorizontal = Random.value > 0.5f;

        if ((splitHorizontal && node.height > minPartitionSize * 2) ||
            node.width <= minPartitionSize * 2)
        {
            int split = Random.Range(minPartitionSize, node.height - minPartitionSize);
            node.left = new BSPNode(node.x, node.y, node.width, split);
            node.right = new BSPNode(node.x, node.y + split, node.width, node.height - split);
        }
        else
        {
            int split = Random.Range(minPartitionSize, node.width - minPartitionSize);
            node.left = new BSPNode(node.x, node.y, split, node.height);
            node.right = new BSPNode(node.x + split, node.y, node.width - split, node.height);
        }

        Split(node.left);
        Split(node.right);
    }

    // -----------------------------
    // Room Creation
    // -----------------------------
    void CreateRooms(BSPNode node)
    {
        if (!node.IsLeaf())
        {
            CreateRooms(node.left);
            CreateRooms(node.right);
            return;
        }

        int roomW = Random.Range(3, Mathf.Min(maxRoomSize, node.width));
        int roomH = Random.Range(3, Mathf.Min(maxRoomSize, node.height));
        int roomX = node.x + Random.Range(0, node.width - roomW + 1);
        int roomY = node.y + Random.Range(0, node.height - roomH + 1);

        node.room = new Room
        {
            x = roomX,
            y = roomY,
            width = roomW,
            height = roomH
        };

        for (int x = roomX; x < roomX + roomW; x++)
            for (int y = roomY; y < roomY + roomH; y++)
                mapGrid[x, y] = 1;
    }

    // -----------------------------
    // Corridor Creation
    // -----------------------------
    void ConnectRooms(BSPNode node)
    {
        if (node.left == null || node.right == null) return;

        Room a = GetRoom(node.left);
        Room b = GetRoom(node.right);

        CreateLCorridor(a, b);

        ConnectRooms(node.left);
        ConnectRooms(node.right);
    }

    Room GetRoom(BSPNode node)
    {
        if (node.room != null) return node.room;
        Room left = GetRoom(node.left);
        if (left != null) return left;
        return GetRoom(node.right);
    }

    void CreateLCorridor(Room a, Room b)
    {
        int x1 = a.centerX;
        int y1 = a.centerY;
        int x2 = b.centerX;
        int y2 = b.centerY;

        if (Random.value > 0.5f)
        {
            CreateCorridor(x1, x2, y1, true);
            CreateCorridor(y1, y2, x2, false);
        }
        else
        {
            CreateCorridor(y1, y2, x1, false);
            CreateCorridor(x1, x2, y2, true);
        }
    }

    void CreateCorridor(int start, int end, int fixedCoord, bool horizontal)
    {
        int min = Mathf.Min(start, end);
        int max = Mathf.Max(start, end);

        for (int i = min; i <= max; i++)
        {
            int x = horizontal ? i : fixedCoord;
            int y = horizontal ? fixedCoord : i;

            if (mapGrid[x, y] == 0)
                mapGrid[x, y] = 2;
        }
    }

    // -----------------------------
    // Tile Instantiation + Hierarchy
    // -----------------------------
    void InstantiateTiles()
    {
        int letterIndex = 0;
        BuildHierarchyAndTiles(rootNode, 0, "", ref letterIndex);
    }

    void BuildHierarchyAndTiles(BSPNode node, int depth, string parentLabel, ref int letterIndex)
    {
        if (node == null) return;

        string label = depth == 0
            ? "A"
            : parentLabel + (char)('A' + (++letterIndex % 26));

        node.label = label;

        GameObject parentGO = new GameObject("BSP_" + label);
        parentGO.transform.parent = transform;
        node.hierarchyParent = parentGO;

        // Rooms (leaf nodes)
        if (node.IsLeaf() && node.room != null)
        {
            for (int x = node.room.x; x < node.room.x + node.room.width; x++)
            {
                for (int y = node.room.y; y < node.room.y + node.room.height; y++)
                {
                    if (placedTiles.Exists(t => t.x == x && t.y == y)) continue;

                    GameObject tile = Instantiate(roomTilePrefab, new Vector3(x, 0, y), Quaternion.identity);
                    tile.transform.parent = parentGO.transform;

                    placedTiles.Add(new RoomTile { x = x, y = y, instance = tile, type = TileType.Room });
                }
            }
        }

        // Corridors
        for (int x = node.x; x < node.x + node.width; x++)
        {
            for (int y = node.y; y < node.y + node.height; y++)
            {
                if (mapGrid[x, y] == 2 && !placedTiles.Exists(t => t.x == x && t.y == y))
                {
                    GameObject tile = Instantiate(roomTilePrefab, new Vector3(x, 0, y), Quaternion.identity);
                    tile.transform.parent = parentGO.transform;

                    placedTiles.Add(new RoomTile { x = x, y = y, instance = tile, type = TileType.Corridor });
                }
            }
        }

        BuildHierarchyAndTiles(node.left, depth + 1, label, ref letterIndex);
        BuildHierarchyAndTiles(node.right, depth + 1, label, ref letterIndex);
    }

    // -----------------------------
    // Wall Removal (Rooms + Corridors)
    // -----------------------------
    void RemoveInteriorWalls()
    {
        foreach (RoomTile tile in placedTiles)
        {
            Transform n = tile.instance.transform.Find("WallNorth");
            Transform s = tile.instance.transform.Find("WallSouth");
            Transform e = tile.instance.transform.Find("WallEast");
            Transform w = tile.instance.transform.Find("WallWest");

            if (HasTile(tile.x, tile.y + 1)) Destroy(n?.gameObject);
            if (HasTile(tile.x, tile.y - 1)) Destroy(s?.gameObject);
            if (HasTile(tile.x + 1, tile.y)) Destroy(e?.gameObject);
            if (HasTile(tile.x - 1, tile.y)) Destroy(w?.gameObject);
        }
    }

    bool HasTile(int x, int y)
    {
        return placedTiles.Exists(t => t.x == x && t.y == y);
    }

    // -----------------------------
    // Gizmos
    // -----------------------------
    void OnDrawGizmos()
    {
        if (rootNode == null) return;
        int index = 0;
        DrawBSPNode(rootNode, 0, "", ref index);
    }

    void DrawBSPNode(BSPNode node, int depth, string parentLabel, ref int index)
    {
        if (node == null) return;

#if UNITY_EDITOR
        Vector3 pos = new Vector3(
            node.x + node.width / 2f,
            depth * levelHeight,
            node.y + node.height / 2f
        );

        if (showBSPNodes)
        {
            Gizmos.color = Color.HSVToRGB(depth * 0.15f % 1f, 0.5f, 0.7f);
            Gizmos.DrawWireCube(pos, new Vector3(node.width, 0.1f, node.height));
        }

        if (node.room != null && showRooms)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(
                new Vector3(node.room.x + node.room.width / 2f, pos.y, node.room.y + node.room.height / 2f),
                new Vector3(node.room.width, 0.1f, node.room.height)
            );
        }

        if (showLabels)
        {
            Handles.Label(pos + Vector3.up * 0.2f, node.label);
        }
#endif

        DrawBSPNode(node.left, depth + 1, node.label, ref index);
        DrawBSPNode(node.right, depth + 1, node.label, ref index);
    }
}
