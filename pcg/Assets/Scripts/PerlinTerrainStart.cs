using UnityEngine;

// public class PerlinTerrainStart : MonoBehaviour
// {
//     //this is heightmap resolution, must be 2^n + 1, e.g., 129, 257, 513, 1025
//     //this samples the verts which is why it is +1 to the resolution, not pixels, verts! 
//     public int resolution = 513; 
 
//     public float heightScale = 0.2f; //this is a normalized scale (0-1) of the height, .2 = 20% of terrain height

//     //public int octaves = 4;
//     //public float lacunarity = 2f;
//     //public float persistence = 0.5f;

//     void Start()
//     {
//         Terrain terrain = GetComponent<Terrain>();
//         TerrainData data = terrain.terrainData;

//         data.heightmapResolution = resolution;

//         float[,] heights = new float[resolution, resolution];

//         data.SetHeights(0, 0, heights);
//     }
// }


/*
STEP 3 1D NOISE =========
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>(); //grabs the terrain component, this script must be on the terrain object
        TerrainData data = terrain.terrainData; //this is where Unity terrain stores its data

        data.heightmapResolution = resolution; //this tells Unity how many height samples exist

        float[,] heights = new float[resolution, resolution]; //stores height values as 2D array



        float scale = 20f;

        for (int x = 0; x < resolution; x++)
        {
            float sampleX = x / (float)resolution * scale;
            float noiseValue = Mathf.PerlinNoise(sampleX, 0);

            for (int z = 0; z < resolution; z++)
            {
                heights[z, x] = noiseValue * heightScale;
            }
        }
        
        data.SetHeights(0, 0, heights); //X offset, Y offset, array of heights

    }
*/

/*
Step 4 2D Noise
==================================================================
float scale = 20f;

for (int x = 0; x < resolution; x++)
{
    for (int z = 0; z < resolution; z++)
    {
        float sampleX = x / (float)resolution * scale;
        float sampleZ = z / (float)resolution * scale;

        float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ);
        heights[z, x] = noiseValue * heightScale;
    }
}


*/

/*
STEP 5 Octaves
==================================================================
public int octaves = 4;
public float lacunarity = 2f;
public float persistence = 0.5f;

for (int x = 0; x < resolution; x++)
{
    for (int z = 0; z < resolution; z++)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x / (float)resolution * scale * frequency;
            float sampleZ = z / (float)resolution * scale * frequency;

            float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
            noiseHeight += perlin * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        heights[z, x] = noiseHeight * heightScale;
    }
}


*/

/* STEP 6 - Make it real time 
==================================================================  */
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class BasicPerlinTerrain : MonoBehaviour
{
    [Header("Terrain Resolution")]
    [Range(33, 1025)]
    public int resolution = 513;

    [Header("Noise Settings")]
    public float scale = 20f;
    [Range(1, 8)]
    public int octaves = 4;
    [Range(0.01f, 1f)]
    public float persistence = 0.5f;
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Header("Height Settings")]
    [Range(0f, 1f)]
    public float heightScale = 0.2f;

    Terrain terrain;
    TerrainData terrainData;

    void Awake()
    {
        Initialize();
        GenerateTerrain();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            Initialize();
            GenerateTerrain();
        }
    }

    void Initialize()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        // Ensure valid resolution
        resolution = Mathf.ClosestPowerOfTwo(resolution - 1) + 1;
        terrainData.heightmapResolution = resolution;
    }

    void GenerateTerrain()
    {
        float[,] heights = new float[resolution, resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = z / (float)(resolution - 1);

                    float sampleX = nx * scale * frequency;
                    float sampleZ = nz * scale * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
                    noiseHeight += perlin * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[z, x] = Mathf.Clamp01(noiseHeight * heightScale);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
}
/* */
