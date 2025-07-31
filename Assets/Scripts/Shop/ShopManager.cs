using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
  public string itemId;        // Unique identifier for persistence
  public string itemName;
  public string description;
  public int cost;
  public ShopItemType itemType;
  public int value; // Amount of boost (e.g., +1 life, x2 multiplier)
  public Sprite itemIcon;
  public bool isPurchased;     // Runtime purchase status
  public bool isPersistent;    // Whether this item persists between sessions
}

public enum ShopItemType
{
  LifeBoost,
  ScoreMultiplier,
  // Add more item types as needed
}

public class ShopManager : MonoBehaviour
{
  public static ShopManager Instance { get; private set; }

  [Header("Shop Configuration")]
  [SerializeField] private List<ShopItem> availableItems = new List<ShopItem>();

  [Header("Events")]
  public UnityEvent<ShopItem> OnItemPurchased;
  public UnityEvent<int> OnCurrencyChanged;
  public UnityEvent<string> OnPurchaseError;

  // Active effects for current game session
  private Dictionary<ShopItemType, int> activeEffects = new Dictionary<ShopItemType, int>();

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
      InitializeShop();
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void Start()
  {
    // Subscribe to player data events
    PlayerDataManager.OnPlayerDataChanged += OnPlayerDataChanged;

    // Initialize currency display
    OnCurrencyChanged?.Invoke(GetPlayerCurrency());
  }

  private void OnDestroy()
  {
    // Unsubscribe from events
    PlayerDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
  }
  private void InitializeShop()
  {
    // Initialize default shop items if none are set
    if (availableItems.Count == 0)
    {
      availableItems.Add(new ShopItem
      {
        itemId = "life_boost_1",
        itemName = "Extra Life",
        description = "Gain +1 life for next game",
        cost = 50,
        itemType = ShopItemType.LifeBoost,
        value = 1,
        isPurchased = false,
        isPersistent = false // Consumed after each game
      });

      availableItems.Add(new ShopItem
      {
        itemId = "score_2x",
        itemName = "2x Score Boost",
        description = "Double your score for next game",
        cost = 75,
        itemType = ShopItemType.ScoreMultiplier,
        value = 2,
        isPurchased = false,
        isPersistent = false // Consumed after each game
      });

      availableItems.Add(new ShopItem
      {
        itemId = "score_3x",
        itemName = "3x Score Boost",
        description = "Triple your score for next game",
        cost = 150,
        itemType = ShopItemType.ScoreMultiplier,
        value = 3,
        isPurchased = false,
        isPersistent = false // Consumed after each game
      });
    }

    // Initialize active effects
    InitializeActiveEffects();

    // Load persistent purchases from player data
    LoadPersistentPurchases();
  }

  private void InitializeActiveEffects()
  {
    activeEffects.Clear();
    foreach (ShopItemType itemType in System.Enum.GetValues(typeof(ShopItemType)))
    {
      activeEffects[itemType] = 0;
    }
  }

  private void LoadPersistentPurchases()
  {
    if (PlayerDataManager.CurrentPlayerData.purchasedItems == null)
      return;

    foreach (var item in availableItems)
    {
      if (string.IsNullOrEmpty(item.itemId)) continue;

      if (PlayerDataManager.CurrentPlayerData.purchasedItems.TryGetValue(item.itemId, out PurchasedItemData purchaseData))
      {
        // Load persistent items or active session items
        if (item.isPersistent || purchaseData.isActive)
        {
          item.isPurchased = true;
          ApplyItemEffect(item);
        }
      }
    }
  }

  private void SavePersistentPurchase(ShopItem item)
  {
    if (string.IsNullOrEmpty(item.itemId)) return;

    var purchaseData = new PurchasedItemData(item.itemId, item.itemType, item.value);

    PlayerDataManager.UpdatePlayerData(data =>
    {
      data.purchasedItems[item.itemId] = purchaseData;
    });
  }

  private void ConsumePurchasedItem(string itemId)
  {
    PlayerDataManager.UpdatePlayerData(data =>
    {
      if (data.purchasedItems.TryGetValue(itemId, out PurchasedItemData purchaseData))
      {
        purchaseData.isActive = false;
        purchaseData.timesUsed++;
      }
    });
  }

  #region Public Methods

  /// <summary>
  /// Attempts to purchase an item by index
  /// </summary>
  public bool PurchaseItem(int itemIndex)
  {
    if (itemIndex < 0 || itemIndex >= availableItems.Count)
    {
      OnPurchaseError?.Invoke("Invalid item index");
      return false;
    }

    return PurchaseItem(availableItems[itemIndex]);
  }

  /// <summary>
  /// Attempts to purchase a specific item
  /// </summary>
  public bool PurchaseItem(ShopItem item)
  {
    if (item == null)
    {
      OnPurchaseError?.Invoke("Invalid item");
      return false;
    }

    if (item.isPurchased)
    {
      OnPurchaseError?.Invoke("Item already purchased");
      return false;
    }

    int playerCurrency = GetPlayerCurrency();
    if (playerCurrency < item.cost)
    {
      OnPurchaseError?.Invoke("Not enough currency");
      return false;
    }

    // Deduct currency
    PlayerDataManager.AddCurrencyBalance(-item.cost);

    // Mark item as purchased
    item.isPurchased = true;

    // Apply item effect
    ApplyItemEffect(item);

    // Save purchase data (both persistent and consumable items)
    SavePersistentPurchase(item);

    // Trigger events
    OnItemPurchased?.Invoke(item);
    OnCurrencyChanged?.Invoke(GetPlayerCurrency());

    Debug.Log($"Purchased {item.itemName} for {item.cost} currency!");
    return true;
  }

  /// <summary>
  /// Checks if player can afford an item
  /// </summary>
  public bool CanAffordItem(int itemIndex)
  {
    if (itemIndex < 0 || itemIndex >= availableItems.Count)
      return false;

    ShopItem item = availableItems[itemIndex];
    return !item.isPurchased && GetPlayerCurrency() >= item.cost;
  }

  /// <summary>
  /// Checks if player can afford a specific item
  /// </summary>
  public bool CanAffordItem(ShopItem item)
  {
    if (item == null) return false;
    return !item.isPurchased && GetPlayerCurrency() >= item.cost;
  }

  /// <summary>
  /// Gets the player's current currency
  /// </summary>
  public int GetPlayerCurrency()
  {
    return PlayerDataManager.CurrentPlayerData.currencyBalance;
  }

  /// <summary>
  /// Gets all available shop items
  /// </summary>
  public List<ShopItem> GetAvailableItems()
  {
    return new List<ShopItem>(availableItems);
  }

  /// <summary>
  /// Gets a specific shop item by index
  /// </summary>
  public ShopItem GetItem(int index)
  {
    if (index >= 0 && index < availableItems.Count)
      return availableItems[index];
    return null;
  }

  /// <summary>
  /// Resets consumable purchased items (call this at the start of a new game)
  /// </summary>
  public void ResetPurchasedItems()
  {
    foreach (var item in availableItems)
    {
      // Only reset non-persistent items (consumables)
      if (!item.isPersistent)
      {
        if (item.isPurchased && !string.IsNullOrEmpty(item.itemId))
        {
          // Mark as consumed in persistent data
          ConsumePurchasedItem(item.itemId);
        }
        item.isPurchased = false;
      }
    }

    InitializeActiveEffects();

    // Reapply effects from persistent items
    LoadPersistentPurchases();

    Debug.Log("Shop items reset for new game - consumables cleared, persistent items retained");
  }

  /// <summary>
  /// Awards currency to the player
  /// </summary>
  public void AwardCurrency(int amount)
  {
    PlayerDataManager.AddCurrencyBalance(amount);
    OnCurrencyChanged?.Invoke(GetPlayerCurrency());
    Debug.Log($"Awarded {amount} currency to player");
  }

  /// <summary>
  /// Gets the current active effect value for a specific item type
  /// </summary>
  public int GetActiveEffect(ShopItemType itemType)
  {
    return activeEffects.ContainsKey(itemType) ? activeEffects[itemType] : 0;
  }

  /// <summary>
  /// Gets the total life boost from purchased items
  /// </summary>
  public int GetTotalLifeBoost()
  {
    return GetActiveEffect(ShopItemType.LifeBoost);
  }

  /// <summary>
  /// Gets the total score multiplier from purchased items
  /// </summary>
  public float GetTotalScoreMultiplier()
  {
    int multiplier = GetActiveEffect(ShopItemType.ScoreMultiplier);
    return multiplier > 0 ? multiplier : 1f;
  }

  /// <summary>
  /// Gets a shop item by its ID
  /// </summary>
  public ShopItem GetItemById(string itemId)
  {
    return availableItems.Find(item => item.itemId == itemId);
  }

  /// <summary>
  /// Checks if an item is currently purchased and active
  /// </summary>
  public bool IsItemActive(string itemId)
  {
    var item = GetItemById(itemId);
    return item != null && item.isPurchased;
  }

  /// <summary>
  /// Gets all currently active purchased items
  /// </summary>
  public List<ShopItem> GetActivePurchases()
  {
    return availableItems.FindAll(item => item.isPurchased);
  }

  /// <summary>
  /// Manually consume a purchased item (mark as inactive)
  /// </summary>
  public void ConsumeItem(string itemId)
  {
    var item = GetItemById(itemId);
    if (item != null && item.isPurchased && !item.isPersistent)
    {
      item.isPurchased = false;
      ConsumePurchasedItem(itemId);

      // Recalculate active effects
      InitializeActiveEffects();
      LoadPersistentPurchases();

      Debug.Log($"Consumed item: {item.itemName}");
    }
  }

  #endregion

  #region Private Methods

  private void ApplyItemEffect(ShopItem item)
  {
    switch (item.itemType)
    {
      case ShopItemType.LifeBoost:
        activeEffects[ShopItemType.LifeBoost] += item.value;
        break;
      case ShopItemType.ScoreMultiplier:
        // For multipliers, take the highest value
        if (item.value > activeEffects[ShopItemType.ScoreMultiplier])
        {
          activeEffects[ShopItemType.ScoreMultiplier] = item.value;
        }
        break;
    }
  }

  private void OnPlayerDataChanged(PlayerData data)
  {
    // Update currency display when player data changes
    OnCurrencyChanged?.Invoke(data.currencyBalance);
  }

  #endregion
}