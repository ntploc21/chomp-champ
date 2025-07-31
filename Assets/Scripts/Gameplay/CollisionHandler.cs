using UnityEngine;
using System;
using UnityEngine.Events;

// Interface for pickup items
public interface IPickup
{
  void OnPickedUp(PlayerCore player);
}

public class CollisionHandler : MonoBehaviour
{
  #region Editor Data
  [Header("Collision Settings")]
  public LayerMask playerLayer = 1 << 6;  // Player layer
  public LayerMask enemyLayer = 1 << 7;   // Enemy layer
  public LayerMask pickupLayer = 1 << 8;  // Pickup layer

  // Events for collision outcomes
  public event UnityAction<GameObject, GameObject> OnPlayerEatsEnemy;
  public event UnityAction<GameObject, GameObject> OnEnemyEatsPlayer;
  public event UnityAction<GameObject, GameObject> OnPickupCollected;
  #endregion

  #region Runtime Data
  private void OnTriggerEnter2D(Collider2D other)
  {
    HandleCollision(other);
  }

  private void OnCollisionEnter2D(Collision2D other)
  {
    HandleCollision(other.collider);
  }
  #endregion

  #region Collision Handling
  private void HandleCollision(Collider2D other)
  {
    GameObject thisObject = gameObject;
    GameObject otherObject = other.gameObject;

    // Check collision types
    bool isThisPlayer = IsInLayerMask(thisObject, playerLayer);
    bool isThisEnemy = IsInLayerMask(thisObject, enemyLayer);
    bool isOtherPlayer = IsInLayerMask(otherObject, playerLayer);
    bool isOtherEnemy = IsInLayerMask(otherObject, enemyLayer);
    bool isOtherPickup = IsInLayerMask(otherObject, pickupLayer);

    // Player vs Enemy collision
    if (isThisPlayer && isOtherEnemy)
    {
      HandlePlayerEnemyCollision(thisObject, otherObject);
    }
    else if (isThisEnemy && isOtherPlayer)
    {
      HandlePlayerEnemyCollision(otherObject, thisObject);
    }
    // Player vs Pickup collision
    else if (isThisPlayer && isOtherPickup)
    {
      HandlePickupCollision(thisObject, otherObject);
    }
    // Enemy vs Enemy collision (optional - for enemy eating each other)
    else if (isThisEnemy && isOtherEnemy)
    {
      HandleEnemyEnemyCollision(thisObject, otherObject);
    }
  }

  private void HandlePlayerEnemyCollision(GameObject player, GameObject enemy)
  {
    // Get size components
    PlayerCore playerCore = player.GetComponent<PlayerCore>();
    EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();

    if (playerCore == null || enemyCore == null)
    {
      Debug.LogWarning("Missing PlayerCore or EnemyCore components in collision!");
      return;
    }

    // Compare Level
    float levelDifference = playerCore.CurrentLevel - enemyCore.CurrentLevel;

    if (levelDifference >= 0) // Player is larger
    {
      // Player eats enemy
      OnPlayerEatsEnemy?.Invoke(player, enemy);

      // Trigger growth on player
      playerCore.EatEnemy(enemyCore);

      // Trigger death on enemy
      enemyCore.OnEaten();
    }
    else if (levelDifference < 0) // Enemy is larger
    {
      // Enemy eats player (player loses life)
      OnEnemyEatsPlayer?.Invoke(enemy, player);

      // Trigger player death/life loss
      bool hasBeenEaten = playerCore.OnEaten();

      // Trigger enemy eating effect
      if (hasBeenEaten && enemyCore.Effects != null)
      {
        enemyCore.Effects.PlayEatEffect();
      }
    }
  }

  private void HandlePickupCollision(GameObject player, GameObject pickup)
  {
    PlayerCore playerCore = player.GetComponent<PlayerCore>();
    if (playerCore == null) return;

    // Handle different pickup types
    IPickup pickupComponent = pickup.GetComponent<IPickup>();
    if (pickupComponent != null)
    {
      pickupComponent.OnPickedUp(playerCore);
    }

    OnPickupCollected?.Invoke(player, pickup);

    // Disable pickup
    pickup.SetActive(false);

    Debug.Log($"Player collected {pickup.name}!");
  }

  private void HandleEnemyEnemyCollision(GameObject enemy1, GameObject enemy2)
  {
    EnemyCore enemyCore1 = enemy1.GetComponent<EnemyCore>();
    EnemyCore enemyCore2 = enemy2.GetComponent<EnemyCore>();

    if (enemyCore1 == null || enemyCore2 == null) return;

    // Only allow predators to eat prey
    if (enemyCore1.Data.behaviorType == EnemyType.Predator &&
        enemyCore2.Data.behaviorType == EnemyType.Prey &&
        enemyCore1.CurrentLevel > enemyCore2.CurrentLevel)
    {
      // Enemy 1 eats Enemy 2
      enemyCore2.OnEaten();
    }
    else if (enemyCore2.Data.behaviorType == EnemyType.Predator &&
             enemyCore1.Data.behaviorType == EnemyType.Prey &&
             enemyCore2.CurrentLevel > enemyCore1.CurrentLevel)
    {
      // Enemy 2 eats Enemy 1
      enemyCore1.OnEaten();
    }
  }

  private void HandleSimilarSizeCollision(GameObject obj1, GameObject obj2)
  {
    // Optional: Add bounce effect or push apart mechanic
    Rigidbody2D rb1 = obj1.GetComponent<Rigidbody2D>();
    Rigidbody2D rb2 = obj2.GetComponent<Rigidbody2D>();

    if (rb1 != null && rb2 != null)
    {
      Vector2 direction = (obj1.transform.position - obj2.transform.position).normalized;
      float bounceForce = 5f;

      rb1.AddForce(direction * bounceForce, ForceMode2D.Impulse);
      rb2.AddForce(-direction * bounceForce, ForceMode2D.Impulse);
    }
  }
  #endregion

  private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
  {
    return (layerMask.value & (1 << obj.layer)) > 0;
  }
}

