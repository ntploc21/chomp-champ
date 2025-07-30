using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Michsky.UI.Reach;

/// <summary>
/// UI controller for the shop system. Connects ShopManager with UI elements.
/// </summary>
public class ShopUI : MonoBehaviour
{
  [Header("UI References")]
  [SerializeField] private Transform shopItemsContainer;
  [SerializeField] private GameObject shopItemPrefab; // Prefab with ShopButtonManager component
  [SerializeField] private ModalWindowManager purchaseModal; // Optional modal for purchase confirmation
  [SerializeField] private TextMeshProUGUI currencyDisplay;
  [SerializeField] private TextMeshProUGUI errorMessageText;
  [SerializeField] private GameObject errorMessagePanel;

  [Header("Settings")]
  [SerializeField] private float errorDisplayDuration = 3f;

  private List<ShopUIItem> shopUIItems = new List<ShopUIItem>();

  private void Start()
  {
    InitializeShopUI();
    UpdateCurrencyDisplay();

    // Subscribe to shop events
    if (ShopManager.Instance != null)
    {
      ShopManager.Instance.OnCurrencyChanged.AddListener(UpdateCurrencyDisplay);
      ShopManager.Instance.OnPurchaseError.AddListener(ShowErrorMessage);
      ShopManager.Instance.OnItemPurchased.AddListener(OnItemPurchased);
    }
  }

  private void OnDestroy()
  {
    // Unsubscribe from events
    if (ShopManager.Instance != null)
    {
      ShopManager.Instance.OnCurrencyChanged.RemoveListener(UpdateCurrencyDisplay);
      ShopManager.Instance.OnPurchaseError.RemoveListener(ShowErrorMessage);
      ShopManager.Instance.OnItemPurchased.RemoveListener(OnItemPurchased);
    }
  }

  private void InitializeShopUI()
  {
    if (ShopManager.Instance == null)
    {
      Debug.LogError("ShopManager instance not found!");
      return;
    }

    // Clear existing UI items
    foreach (Transform child in shopItemsContainer)
    {
      if (Application.isPlaying)
        Destroy(child.gameObject);
      else
        DestroyImmediate(child.gameObject);
    }
    shopUIItems.Clear();

    // Create UI items for each shop item
    var shopItems = ShopManager.Instance.GetAvailableItems();
    for (int i = 0; i < shopItems.Count; i++)
    {
      CreateShopItemUI(shopItems[i], i);
    }
  }

  private void CreateShopItemUI(ShopItem shopItem, int itemIndex)
  {
    if (shopItemPrefab == null)
    {
      Debug.LogError("Shop item prefab is not assigned!");
      return;
    }

    GameObject itemUI = Instantiate(shopItemPrefab, shopItemsContainer);
    ShopButtonManager buttonManager = itemUI.GetComponent<ShopButtonManager>();

    if (buttonManager == null)
    {
      Debug.LogError("Shop item prefab must have ShopButtonManager component!");
      Destroy(itemUI);
      return;
    }

    // Set up the button data
    buttonManager.buttonTitle = shopItem.itemName;
    buttonManager.buttonDescription = shopItem.description;
    buttonManager.priceText = shopItem.cost.ToString();
    buttonManager.purchaseModal = purchaseModal;

    if (shopItem.itemIcon != null)
    {
      buttonManager.buttonIcon = shopItem.itemIcon;
    }

    // Set up purchase event
    buttonManager.onPurchase.AddListener(() => PurchaseItem(itemIndex));
    buttonManager.InitializePurchaseEvents();

    // Set initial state
    UpdateItemState(buttonManager, shopItem);

    // Store reference
    ShopUIItem uiItem = new ShopUIItem
    {
      shopItem = shopItem,
      buttonManager = buttonManager,
      itemIndex = itemIndex
    };
    shopUIItems.Add(uiItem);

    // Update UI
    buttonManager.UpdateUI();
    buttonManager.InitializePurchaseEvents();
  }

  private void UpdateItemState(ShopButtonManager buttonManager, ShopItem shopItem)
  {
    bool canAfford = ShopManager.Instance.CanAffordItem(shopItem);

    if (shopItem.isPurchased)
    {
      buttonManager.SetState(ShopButtonManager.State.Purchased);
    }
    else
    {
      buttonManager.SetState(ShopButtonManager.State.Default);

      // You might want to add visual indicators for items player can't afford
      if (buttonManager.purchaseButton != null)
      {
        buttonManager.purchaseButton.isInteractable = canAfford;
      }
    }
  }

  private void PurchaseItem(int itemIndex)
  {
    if (ShopManager.Instance == null) return;

    bool success = ShopManager.Instance.PurchaseItem(itemIndex);
    if (success)
    {
      UpdateShopUI();
    }
  }

  private void UpdateShopUI()
  {
    // Update all shop item states
    foreach (var uiItem in shopUIItems)
    {
      UpdateItemState(uiItem.buttonManager, uiItem.shopItem);
    }
  }

  private void UpdateCurrencyDisplay(int currency)
  {
    UpdateCurrencyDisplay();
  }

  private void UpdateCurrencyDisplay()
  {
    if (currencyDisplay != null && ShopManager.Instance != null)
    {
      currencyDisplay.text = ShopManager.Instance.GetPlayerCurrency().ToString();
    }
  }

  private void ShowErrorMessage(string errorMessage)
  {
    if (errorMessageText != null)
    {
      errorMessageText.text = errorMessage;
    }

    if (errorMessagePanel != null)
    {
      errorMessagePanel.SetActive(true);

      // Hide after duration
      if (Application.isPlaying)
      {
        Invoke(nameof(HideErrorMessage), errorDisplayDuration);
      }
    }

    Debug.LogWarning($"Shop Error: {errorMessage}");
  }

  private void HideErrorMessage()
  {
    if (errorMessagePanel != null)
    {
      errorMessagePanel.SetActive(false);
    }
  }

  private void OnItemPurchased(ShopItem item)
  {
    Debug.Log($"Successfully purchased: {item.itemName}");
    UpdateShopUI();
  }

  // Public methods for external use
  public void RefreshShopUI()
  {
    InitializeShopUI();
    UpdateCurrencyDisplay();
  }

  public void ResetShop()
  {
    if (ShopManager.Instance != null)
    {
      ShopManager.Instance.ResetPurchasedItems();
      UpdateShopUI();
    }
  }

  [System.Serializable]
  private class ShopUIItem
  {
    public ShopItem shopItem;
    public ShopButtonManager buttonManager;
    public int itemIndex;
  }
}
