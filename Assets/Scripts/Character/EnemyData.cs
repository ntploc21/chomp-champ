using UnityEngine;

public enum EnemyType
{
    Prey,      // Smaller enemies that flee from player
    Predator,  // Larger enemies that chase player
    Neutral    // Enemies that wander regardless of player
}

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Enemy";     // Default name
    public Sprite enemySprite;             // Default sprite
    public RuntimeAnimatorController animatorController;  // Animator controller for enemy animations

    [Header("Size & Growth")]
    [Range(1, 10)]
    public int sizeLevel = 1;               // Enemy level (which affects eating conditions)

    [Header("Movement")]
    [Range(0.5f, 20f)]
    public float baseSpeed = 3f;            // Base movement speed
    [Range(1f, 50f)]
    public float detectionRange = 5f;       // Range at which enemy detects player

    [Header("Behavior Type")]
    public EnemyType enemyType = EnemyType.Prey;
    [Range(0.5f, 10f)]
    public float wanderRadius = 3f;
    [Range(0.5f, 5f)]
    public float fleeSpeedMultiplier = 1.5f;
    [Range(0.5f, 3f)]
    public float chaseSpeedMultiplier = 1.2f;

    [Header("Collision & Physics")]
    [Range(0.1f, 5f)]
    public float hitboxRadius = 0.5f;
    public LayerMask collisionLayers = -1;

    [Header("Audio & Effects")]
    public AudioClip spawnSound;               // Sound played when enemy spawns
    public AudioClip deathSound;               // Sound played when enemy dies
    public AudioClip eatSound;                 // Sound played when enemy eats
    public GameObject deathEffectPrefab;      // Visual effect played on enemy death
    public GameObject spawnEffectPrefab;      // Visual effect played on enemy spawn

    [Header("Spawning")]
    [Range(0.1f, 1f)]
    public float spawnWeight = 1f; // Relative spawn chance
    public bool canSpawnInWaves = true; // Whether this enemy can spawn in waves (schools)

    // Validation
    private void OnValidate()
    {
        sizeLevel = Mathf.Max(1, sizeLevel);
        hitboxRadius = Mathf.Max(0.1f, hitboxRadius);
        baseSpeed = Mathf.Max(0.1f, baseSpeed);
    }
}