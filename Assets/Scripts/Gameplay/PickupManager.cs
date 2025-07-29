using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;  // Add this for scene management
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 worldSize = mainCamera.ScreenToWorldPoint(screenSize);
        Vector2 spawnPosition = new Vector2(Random.Range(-worldSize.x / 2, worldSize.x / 2), Random.Range(-worldSize.y / 2, worldSize.y / 2));

        string pickupType = Random.Range(0, 3) switch
        {
            0 => "DashPickup",
            1 => "DoubleScorePickup",
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