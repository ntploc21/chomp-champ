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
        }

        BoundsInt bounds = tilemap.cellBounds;

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

    private Vector3 GetRandomWalkablePosition()
    {
        return GetRandomPositionInCell(spawnCells[Random.Range(0, spawnCells.Count)]);
    }

    private Vector3 GetRandomPositionInCell(Vector3Int cell)
    {
        Vector3 cellWorldPosition = tilemap.CellToWorld(cell);
        Vector3 cellSize = tilemap.cellSize;

        float randomX = Random.Range(0f, cellSize.x);
        float randomY = Random.Range(0f, cellSize.y);

        return cellWorldPosition + new Vector3(randomX, randomY, 0);
    }

    private void SpawnPickup()
    {
        // Debug.Log("Spawning pickup...");
        Vector3 spawnPosition = GetRandomWalkablePosition();
        if (spawnPosition == Vector3.zero)
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
            Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"Pickup prefab {pickupType} not found!");
        }
    }
}