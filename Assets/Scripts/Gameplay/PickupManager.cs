using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;  // Add this for scene management
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    public float lastSpawnTime = 0f;
    public float pickupSpawnInterval = 2f;
    public float pickupSpawnRatio = 1f;
    private List<Vector3Int> spawnCells = new List<Vector3Int>();

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Tilemap tilemap;
    private void Initialize()
    {
        tilemap = GameObject.Find("PickupSpawn")?.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogWarning("PickupSpawn tilemap not found!");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        spawnCells.Clear();
        for (int i = bounds.xMin; i < bounds.xMax; i++)
        {
            for (int j = bounds.yMin; j < bounds.yMax; j++)
            {
                Vector3Int cell = new Vector3Int(i, j, 0);
                if (tilemap.HasTile(cell))
                {
                    spawnCells.Add(cell);
                }
            }
        }

        Debug.Log($"Found {spawnCells.Count} valid spawn cells for pickups.");
    } 

    public void Update()
    {
        if (Time.time - lastSpawnTime >= pickupSpawnInterval)
        {
            TrySpawnPickup();
            lastSpawnTime = Time.time;
        }
    }

    private void TrySpawnPickup()
    {
        if (Random.value < pickupSpawnRatio)
        {
            SpawnPickup();
        }
    }

    public void FreeCell(Vector3Int cell)
    {
        spawnCells.Add(cell);
        Debug.Log($"Cell {cell} freed for pickup spawning.");
    }

    private Vector3Int GetRandomSpawnCell()
    {
        if (spawnCells.Count == 0)
        {
            Debug.LogWarning("No valid spawn cells available for pickups.");
            return Vector3Int.zero;
        }
        int random_index = Random.Range(0, spawnCells.Count);
        Vector3Int cell = spawnCells[random_index];
        spawnCells[random_index] = spawnCells.Last();
        spawnCells.RemoveAt(spawnCells.Count - 1);
        Debug.Log($"Selected spawn cell: {cell}");
        return cell;
    }

    private Vector3 GetRandomPositionInCell(Vector3Int cell)
    {
        Vector3 cellWorldPosition = tilemap.CellToWorld(cell);
        Vector3 cellSize = tilemap.cellSize;

        float randomX = cellSize.x / 2f;
        float randomY = cellSize.y / 2f;

        return cellWorldPosition + new Vector3(randomX, randomY, 0);
    }

    private void SpawnPickup()
    {
        // Debug.Log("Spawning pickup...");
        Vector3Int spawnCell = GetRandomSpawnCell();
        if (spawnCell == Vector3.zero)
        {
            Debug.LogWarning("Failed to find a valid spawn position for the pickup.");
            return;
        }

        string pickupType = Random.Range(0, 2) switch
        {
            0 => "DoubleScorePickup",
            _ => "SpeedPickup"
        };
        GameObject pickupPrefab = Resources.Load<GameObject>($"Prefabs/Pickups/{pickupType}");
        if (pickupPrefab != null)
        {
            pickupPrefab.GetComponent<PickupCore>().spawnCell = spawnCell;
            pickupPrefab.GetComponent<PickupCore>().pickupManager = this;
            Instantiate(pickupPrefab, GetRandomPositionInCell(spawnCell), Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"Pickup prefab {pickupType} not found!");
        }
    }
}