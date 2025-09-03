using UnityEngine;
using Unity.AI.Navigation; // For NavMeshSurface

public class TileArenaGenerator : MonoBehaviour
{
    [Header("Arena Settings")]
    public int arenaSize = 5;
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    void Start()
    {
        GenerateArena();
        BakeNavMesh();
    }

    void GenerateArena()
    {
        for (int x = 0; x < arenaSize; x++)
        {
            for (int z = 0; z < arenaSize; z++)
            {
                Vector3 pos = new Vector3(x, 0, z);
                Instantiate(floorPrefab, pos, Quaternion.Euler(0, 0, 90), transform);

                // Place walls only on the border
                if (x == 0 || x == arenaSize - 1 || z == 0 || z == arenaSize - 1)
                {
                    Vector3 wallPos = new Vector3(x, 0.5f, z);
                    Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                }
            }
        }

    }

    void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("NavMeshSurface not assigned!");
        }
    }
}
