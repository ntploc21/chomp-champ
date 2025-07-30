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

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
    
    Vector3 GetRandomWalkablePosition()
    {
        Tilemap tilemap = GameObject.Find("PickupSpawn")?.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogWarning("PickupSpawn tilemap not found!");
            return Vector3.zero;
        }

        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int cell;
        Vector3 worldPos;

        for (int i = 0; i < 5; i++)
        {
            cell = new Vector3Int(
                Random.Range(bounds.xMin, bounds.xMax),
                Random.Range(bounds.yMin, bounds.yMax),
                0
            );

            if (!tilemap.HasTile(cell))
                continue;

            worldPos = tilemap.GetCellCenterWorld(cell);
            return worldPos;

            // Check if worldPos is inside the composite collider
            // if (groundCollider.OverlapPoint(worldPos))
            // {
            //     return worldPos;
            // }
        }

        Debug.LogWarning("Could not find a walkable position after 5 tries.");
        return Vector3.zero;
    }
}