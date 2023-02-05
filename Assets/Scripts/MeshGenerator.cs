using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

public class MeshGenerator : MonoBehaviour
{
    [Header("Dimentions by the number of points"), Range(2, 100)]
    public int Size = 10;
    [Header("Spacing between points"), Range(0, 2)]
    public float Spacing = 1;
    [Space]
    public float Scale = 2;
    public float IsoLevelThreashhold = 0f;

    // Important stuff
    private List<VertexData> verticesData = new();
    [SerializeField]
    private List<Vector3> vertices = new();
    [SerializeField]
    private List<Vector3> noDuplesVertices = new();
    [SerializeField]
    private List<int> triangles = new();
    private Dictionary<int2, int> noDuplesDict = new();
    private ValueGeneration valueGen = new();
    private SimplexNoiseGenerator noise = new();

    // Runtime data
    private Table table = new();
    private Mesh mesh;
    bool settingsChanged = false;

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateMesh();
    }

    private void CreateMesh()
    {
        ClearPrevData();

        for (int z = 0; z < Size; z++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int cubeIndex = 0;
                    cubeIndex = CalculateCubeIndex(z, y, x, cubeIndex);

                    // Creating local cube with specific vertex position
                    Vector3[] cubepoints = new Vector3[] {
                        new Vector3(x    , y    , z    ),
                        new Vector3(x + 1, y    , z    ),
                        new Vector3(x + 1, y    , z + 1),
                        new Vector3(x    , y    , z + 1),
                        new Vector3(x    , y + 1, z    ),
                        new Vector3(x + 1, y + 1, z    ),
                        new Vector3(x + 1, y + 1, z + 1),
                        new Vector3(x    , y + 1, z + 1)
                    };

                    // Triangulation table tell how to create triangles depending on the vertex positions of local cube
                    int[] triangulation = table.EdgesFromIndex(cubeIndex);

                    for (int i = 0; i < triangulation.Length; i += 3)
                    {
                        if (triangulation[i] == -1)
                        {
                            break;
                        }
                        vertices.Add(CreateVertex(triangulation[i + 0], cubepoints));
                        vertices.Add(CreateVertex(triangulation[i + 1], cubepoints));
                        vertices.Add(CreateVertex(triangulation[i + 2], cubepoints));
                    }
                }
            }
        }

        // Vertex and triangle lists creation without duplicates
        // Not faster than method below
        // for (int i = 0; i < vertices.Count; i++)
        // {
        //     if (noDuplesDict.TryAdd(verticesData[i].id, i))
        //     {
        //         noDuplesVertices.Add(vertices[i]);
        //     }
        // }

        // for (int i = 0; i < vertices.Count; i++)
        // {
        //     if (noDuplesDict.TryGetValue(verticesData[i].id, out int vertIndex))
        //     {
        //         triangles.Add(noDuplesVertices.IndexOf(vertices[vertIndex]));
        //     }
        // }

        // Previous method of deleting the duplicates

        for (int i = 0; i < vertices.Count; i++)
        {
            triangles.Add(i);
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            for (int j = i + 1; j < vertices.Count; j++)
            {
                if (!(verticesData[j].id.x == verticesData[i].id.x && verticesData[j].id.y == verticesData[i].id.y))
                {
                    break;
                }
                if (i < j)
                {
                    triangles[i] = triangles[j];
                    break;
                }
                triangles[j] = triangles[i];
            }
        }

        noDuplesVertices = vertices.Distinct().ToList();

        for (int i = 0; i < triangles.Count; i++)
        {
            int newIndex = noDuplesVertices.IndexOf(vertices[triangles[i]]);
            triangles[i] = newIndex;
        }
        //vertices.Clear();

        ApplyChange(noDuplesVertices, triangles);
    }

    private int CalculateCubeIndex(int z, int y, int x, int cubeIndex)
    {
        // if (noise.coherentNoise(new Vector3(x    , y    , z    )) < IsoLevelThreashhold) cubeIndex |= 1;
        // if (noise.coherentNoise(new Vector3(x + 1, y    , z    )) < IsoLevelThreashhold) cubeIndex |= 2;
        // if (noise.coherentNoise(new Vector3(x + 1, y    , z + 1)) < IsoLevelThreashhold) cubeIndex |= 4;
        // if (noise.coherentNoise(new Vector3(x    , y    , z + 1)) < IsoLevelThreashhold) cubeIndex |= 8;
        // if (noise.coherentNoise(new Vector3(x    , y + 1, z    )) < IsoLevelThreashhold) cubeIndex |= 16;
        // if (noise.coherentNoise(new Vector3(x + 1, y + 1, z    )) < IsoLevelThreashhold) cubeIndex |= 32;
        // if (noise.coherentNoise(new Vector3(x + 1, y + 1, z + 1)) < IsoLevelThreashhold) cubeIndex |= 64;
        // if (noise.coherentNoise(new Vector3(x    , y + 1, z + 1)) < IsoLevelThreashhold) cubeIndex |= 128;

        if (valueGen.SphereFunc(new Vector3(x, y, z), (float)Size) < IsoLevelThreashhold) cubeIndex |= 1;
        if (valueGen.SphereFunc(new Vector3(x + 1, y, z), (float)Size) < IsoLevelThreashhold) cubeIndex |= 2;
        if (valueGen.SphereFunc(new Vector3(x + 1, y, z + 1), (float)Size) < IsoLevelThreashhold) cubeIndex |= 4;
        if (valueGen.SphereFunc(new Vector3(x, y, z + 1), (float)Size) < IsoLevelThreashhold) cubeIndex |= 8;
        if (valueGen.SphereFunc(new Vector3(x, y + 1, z), (float)Size) < IsoLevelThreashhold) cubeIndex |= 16;
        if (valueGen.SphereFunc(new Vector3(x + 1, y + 1, z), (float)Size) < IsoLevelThreashhold) cubeIndex |= 32;
        if (valueGen.SphereFunc(new Vector3(x + 1, y + 1, z + 1), (float)Size) < IsoLevelThreashhold) cubeIndex |= 64;
        if (valueGen.SphereFunc(new Vector3(x, y + 1, z + 1), (float)Size) < IsoLevelThreashhold) cubeIndex |= 128;

        return cubeIndex;
    }

    Vector3 CreateVertex(int index, Vector3[] cubePoints)
    {
        int a = table.cornerAIndexFromEdge[index];
        int b = table.cornerBIndexFromEdge[index];
        Vector3 vertex = CenteredCoord(InterpolatePoint(cubePoints[a], cubePoints[b]));

        VertexData vertexAB;
        int indexA = pointIndexFromCoord(cubePoints[a]);
        int indexB = pointIndexFromCoord(cubePoints[b]);
        vertexAB.id = new int2(Mathf.Min(indexA, indexB), Mathf.Max(indexA, indexB));
        verticesData.Add(vertexAB);

        return vertex;
    }

    Vector3 CenteredCoord(Vector3 pos)
    {
        return (pos - Vector3.one * Size / 2) * Spacing;
    }

    Vector3 InterpolatePoint(Vector3 point1, Vector3 point2)
    {
        // float value1 = noise.coherentNoise(point1);
        // float value2 = noise.coherentNoise(point2);

        float value1 = valueGen.SphereFunc(point1, (float)Size);
        float value2 = valueGen.SphereFunc(point2, (float)Size);
        return point1 + ((IsoLevelThreashhold - value1) * (point2 - point1) / (value2 - value1));
    }

    int pointIndexFromCoord(Vector3 pos)
    {
        return (int)pos.x + (int)pos.y * Size + (int)pos.z * Size * Size;
    }

    private void ApplyChange(List<Vector3> vertexList, List<int> trisList)
    {
        mesh.vertices = vertexList.ToArray();
        mesh.triangles = trisList.ToArray();
        mesh.RecalculateNormals();
        //print(Time.realtimeSinceStartupAsDouble);
    }


    private void Update()
    {
        if (settingsChanged == true)
        {
            CreateMesh();
            settingsChanged = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(Size, Size, Size));
    }

    private void OnValidate()
    {
        //settingsChanged = true;
        // CreateMesh();
    }

    private void ClearPrevData()
    {
        verticesData.Clear();
        vertices.Clear();
        noDuplesVertices.Clear();
        noDuplesDict.Clear();
        triangles.Clear();
        mesh.Clear();
    }
}
