using UnityEngine;
using UnityEngine.U2D.Animation;

public enum EnemyType
{
    [Tooltip("Smaller enemies that flee from player")]
    Prey,      // Smaller enemies that flee from player
    [Tooltip("Larger enemies that chase player")]
    Predator,  // Larger enemies that chase player
    [Tooltip("Enemies that wander regardless of player")]
    Neutral    // Enemies that wander regardless of player
}

/// <summary>
/// ScriptableObject to hold enemy data.
/// This class is used to store various properties of an enemy type, such as its name,
/// sprite, animator controller, movement speed, behavior type, collision layers,
/// death effects, spawn effects, and spawning parameters.
/// 
/// This data can be used to configure enemies in the game without hardcoding values.
/// It allows for easy adjustments and balancing of enemy attributes through the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    #region Editor Data    [Header("Basic Info")]
    [Tooltip("Unique identifier for the enemy type.")]
    public string enemyName = "Enemy";     // Default name

    [Header("Sprite System")]
    [Tooltip("Sprite Library Asset containing organized sprites by category and label.")]
    public SpriteLibraryAsset spriteLibrary;
    [Tooltip("Default sprite category (e.g., 'Idle', 'Run', 'Attack').")]
    public string defaultSpriteCategory = "Idle";
    [Tooltip("Default sprite label (e.g., 'Small', 'Medium', 'Large').")]
    public string defaultSpriteLabel = "Small";
    [Tooltip("Fallback sprite for backward compatibility.")]
    public Sprite fallbackSprite;
    [Tooltip("Animator controller for enemy animations.")]
    public RuntimeAnimatorController animatorController;  // Animator controller for enemy animations

    [Header("Animation Parameters")]
    [Tooltip("Animation parameter name for hit trigger.")]
    public string hitAnimationParameter = "hit";
    [Tooltip("Animation parameter name for death trigger.")]
    public string deathAnimationParameter = "death";
    [Tooltip("Animation parameter name for invincible state.")]
    public string invincibleAnimationParameter = "invincible";
    [Tooltip("Animation parameter name for movement state.")]
    public string isMovingAnimationParameter = "isMoving";

    [Header("Size & Level")]
    [Tooltip("Level of the enemy, affects stats and difficulty.")]
    [Range(1, 10)]
    public int level = 1;               // Enemy level
    [Tooltip("Fixed size of the enemy, used for hitbox and visuals.")]
    [Range(0.1f, 5f)]
    public float size = 1f;                 // Fixed size of the enemy

    [Header("Movement")]
    [Tooltip("Base movement speed of the enemy.")]
    [Range(0.5f, 20f)]
    public float baseSpeed = 3f;            // Base movement speed
    [Tooltip("Speed multiplier when the enemy is fleeing or chasing.")]
    [Range(1f, 50f)]
    public float detectionRange = 5f;       // Range at which enemy detects player

    [Header("Behavior Type")]
    [Tooltip("Type of behavior this enemy exhibits.")]
    public EnemyType behaviorType = EnemyType.Neutral; // Type of behavior (Prey, Predator, Neutral)

    [Tooltip("Wandering behavior radius for the enemy.")]
    [Range(0.5f, 10f)]
    public float wanderRadius = 3f;
    [Tooltip("Speed multiplier when the enemy is fleeing from the player.")]
    [Range(0.5f, 5f)]
    public float fleeSpeedMultiplier = 1.5f;
    [Tooltip("Speed multiplier when the enemy is chasing the player.")]
    [Range(0.5f, 3f)]
    public float chaseSpeedMultiplier = 1.2f;

    [Header("Collision & Physics")]
    [Tooltip("Radius of the enemy's hitbox for collision detection.")]
    [Range(0.1f, 5f)]
    public float hitboxRadius = 0.5f;
    [Tooltip("Layers that this enemy can collide with.")]
    public LayerMask collisionLayers = -1;

    [Header("Effects")]
    [Tooltip("Prefab for the visual effect played on enemy death.")]
    public GameObject deathEffectPrefab;      // Visual effect played on enemy death
    [Tooltip("Prefab for the visual effect played on enemy spawn.")]
    public GameObject spawnEffectPrefab;      // Visual effect played on enemy spawn

    [Header("Spawning")]
    [Tooltip("Minimum number of this enemy type that can spawn.")]
    [Range(1, 50)]
    public int maxSpawnCount = 10; // Maximum number of this enemy type that can spawn
    [Tooltip("Relative spawn chance of this enemy type.")]
    [Range(0.1f, 1f)]
    public float spawnWeight = 1f; // Relative spawn chance
    [Tooltip("Whether this enemy can spawn in waves (schools).")]
    public bool canSpawnInWaves = true; // Whether this enemy can spawn in waves (schools)
    #endregion

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        hitboxRadius = Mathf.Max(0.1f, hitboxRadius);
        baseSpeed = Mathf.Max(0.1f, baseSpeed);
    }
}