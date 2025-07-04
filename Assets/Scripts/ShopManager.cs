using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform shopItemsPanel;
    public Transform shoppingCartPanel;
    public Transform playerInfoPanel;
    
    [Header("Item Display")]
    public GameObject gearCardPrefab;     // Prefab for displaying gear cards
    public GameObject actionCardPrefab;   // Prefab for displaying action cards
    public float itemSpacing = 10f;
    
    [Header("Player Info")]
    public TextMeshProUGUI playerCoinsText;
    public PlayerInventory playerInventory;
    
    [Header("Shop Inventory")]
    public List<ShopItem> shopGearItems = new List<ShopItem>();
    public List<ShopItem> shopActionItems = new List<ShopItem>();
    
    [Header("Shopping Cart")]
    public List<ShopItem> cartItems = new List<ShopItem>();
    public Button checkoutButton;
    
    void Start()
    {
        // Find player inventory if not assigned
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        // Set up checkout button
        if (checkoutButton != null)
        {
            checkoutButton.onClick.AddListener(Checkout);
        }
        
        // Initialize shop
        SetupShop();
        UpdatePlayerInfo();
    }
    
    void SetupShop()
    {
        Debug.Log("Setting up shop...");
        
        // Load real items from assets
        LoadRealGearItems();
        LoadRealActionItems();
        
        // Display items in the shop
        DisplayShopItems();
    }
    
    void LoadRealGearItems()
    {
        #if UNITY_EDITOR
        // Find all GearCard assets in the project
        string[] gearGuids = UnityEditor.AssetDatabase.FindAssets("t:GearCard");
        
        List<GearCard> allGearCards = new List<GearCard>();
        foreach (string guid in gearGuids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GearCard gear = UnityEditor.AssetDatabase.LoadAssetAtPath<GearCard>(assetPath);
            if (gear != null)
            {
                allGearCards.Add(gear);
            }
        }
        
        // Randomly select 8 gear cards for the shop
        List<GearCard> selectedGear = new List<GearCard>();
        while (selectedGear.Count < 8 && allGearCards.Count > 0)
        {
            int randomIndex = Random.Range(0, allGearCards.Count);
            selectedGear.Add(allGearCards[randomIndex]);
            allGearCards.RemoveAt(randomIndex);
        }
        
        // Convert to shop items
        foreach (GearCard gear in selectedGear)
        {
            ShopItem shopItem = new ShopItem();
            shopItem.itemName = gear.gearName;
            shopItem.price = Mathf.RoundToInt(gear.price); // Use the gear's price
            shopItem.quantity = Random.Range(1, 4); // Random quantity 1-3
            shopItem.itemType = ShopItem.ItemType.Gear;
            shopItem.gearCard = gear;
            shopGearItems.Add(shopItem);
        }
        #else
        // For builds, create fallback test items
        for (int i = 0; i < 8; i++)
        {
            ShopItem gearItem = new ShopItem();
            gearItem.itemName = $"Gear {i + 1}";
            gearItem.price = 50 + (i * 10);
            gearItem.quantity = Random.Range(1, 4);
            gearItem.itemType = ShopItem.ItemType.Gear;
            shopGearItems.Add(gearItem);
        }
        #endif
    }
    
    void LoadRealActionItems()
    {
        #if UNITY_EDITOR
        // Find all ActionCard assets in the project
        string[] actionGuids = UnityEditor.AssetDatabase.FindAssets("t:ActionCard");
        
        List<ActionCard> allActionCards = new List<ActionCard>();
        foreach (string guid in actionGuids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ActionCard action = UnityEditor.AssetDatabase.LoadAssetAtPath<ActionCard>(assetPath);
            if (action != null)
            {
                allActionCards.Add(action);
            }
        }
        
        // Randomly select 8 action cards for the shop
        List<ActionCard> selectedActions = new List<ActionCard>();
        while (selectedActions.Count < 8 && allActionCards.Count > 0)
        {
            int randomIndex = Random.Range(0, allActionCards.Count);
            selectedActions.Add(allActionCards[randomIndex]);
            allActionCards.RemoveAt(randomIndex);
        }
        
        // Convert to shop items
        foreach (ActionCard action in selectedActions)
        {
            ShopItem shopItem = new ShopItem();
            shopItem.itemName = action.actionName;
            shopItem.price = 25; // Fixed price for action cards, or add price field to ActionCard
            shopItem.quantity = Random.Range(2, 6); // Random quantity 2-5
            shopItem.itemType = ShopItem.ItemType.Action;
            shopItem.actionCard = action;
            shopActionItems.Add(shopItem);
        }
        #else
        // For builds, create fallback test items
        for (int i = 0; i < 8; i++)
        {
            ShopItem actionItem = new ShopItem();
            actionItem.itemName = $"Action {i + 1}";
            actionItem.price = 25 + (i * 5);
            actionItem.quantity = Random.Range(2, 6);
            actionItem.itemType = ShopItem.ItemType.Action;
            shopActionItems.Add(actionItem);
        }
        #endif
    }
    
    void DisplayShopItems()
    {
        // Clear existing displays
        ClearShopDisplay();
        
        Vector2 currentPosition = Vector2.zero;
        
        // Display gear items in a grid
        for (int i = 0; i < shopGearItems.Count; i++)
        {
            CreateShopItemStack(shopGearItems[i], currentPosition);
            
            // Move to next position (4 items per row)
            if ((i + 1) % 4 == 0)
            {
                currentPosition.x = 0;
                currentPosition.y -= 120;
            }
            else
            {
                currentPosition.x += 150;
            }
        }
        
        // Continue with action items
        currentPosition.y -= 140; // Add space between gear and actions
        currentPosition.x = 0;
        
        for (int i = 0; i < shopActionItems.Count; i++)
        {
            CreateShopItemStack(shopActionItems[i], currentPosition);
            
            // Move to next position (4 items per row)
            if ((i + 1) % 4 == 0)
            {
                currentPosition.x = 0;
                currentPosition.y -= 120;
            }
            else
            {
                currentPosition.x += 150;
            }
        }
    }
    
    void CreateShopItemStack(ShopItem item, Vector2 position)
    {
        Debug.Log($"*** CREATING STACKED SHOP ITEM: {item.itemName} with quantity: {item.quantity} ***");
        
        // Create a container for this item's stack
        GameObject stackContainer = new GameObject($"{item.itemName}_Stack");
        stackContainer.transform.SetParent(shopItemsPanel, false);
        
        // The Grid Layout will position this container
        RectTransform containerRect = stackContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(140, 100); // Match grid cell size
        
        // Create multiple card displays based on quantity (max 5 visual cards)
        int cardsToShow = Mathf.Min(item.quantity, 5);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            GameObject cardDisplay = null;
            
            // Use the appropriate prefab based on item type
            if (item.itemType == ShopItem.ItemType.Gear && gearCardPrefab != null)
            {
                cardDisplay = Instantiate(gearCardPrefab, stackContainer.transform);
            }
            else if (item.itemType == ShopItem.ItemType.Action && actionCardPrefab != null)
            {
                cardDisplay = Instantiate(actionCardPrefab, stackContainer.transform);
            }
            
            if (cardDisplay == null)
            {
                Debug.LogError("No prefab assigned for item type: " + item.itemType);
                return;
            }
            
            // Position each card with slight offset for stacking effect
            RectTransform cardRect = cardDisplay.GetComponent<RectTransform>();
            Vector2 stackOffset = new Vector2(i * 3f, -i * 3f); // Offset each card slightly
            cardRect.anchoredPosition = stackOffset;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = Vector2.zero;
            
            // Set up the card display data
            SetupCardDisplay(cardDisplay, item, i == cardsToShow - 1); // Only top card is draggable
            
            // Add shadow effect to cards behind the top one
            if (i < cardsToShow - 1)
            {
                AddShadowEffect(cardDisplay);
                MakeCardNonInteractable(cardDisplay);
            }
            
            Debug.Log($"Created card {i + 1} of {cardsToShow} for {item.itemName} at offset {stackOffset}");
        }
    }
    
    void SetupCardDisplay(GameObject cardDisplay, ShopItem item, bool isDraggable)
    {
        // Get the CardDisplay component and set the card data
        CardDisplay cardDisplayComponent = cardDisplay.GetComponent<CardDisplay>();
        if (cardDisplayComponent != null)
        {
            // Set the appropriate card based on item type
            if (item.itemType == ShopItem.ItemType.Gear)
            {
                cardDisplayComponent.gearCard = item.gearCard;
                cardDisplayComponent.fishCard = null;
                cardDisplayComponent.actionCard = null;
            }
            else if (item.itemType == ShopItem.ItemType.Action)
            {
                cardDisplayComponent.gearCard = null;
                cardDisplayComponent.fishCard = null;
                cardDisplayComponent.actionCard = item.actionCard;
            }
            
            // Set shop-specific data
            cardDisplayComponent.isShopItem = true;
            cardDisplayComponent.itemPrice = item.price;
            cardDisplayComponent.itemQuantity = item.quantity;
            
            // Force the card to display
            cardDisplayComponent.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
        }
        
        // Only add drag functionality to the top card
        if (isDraggable)
        {
            SetupDragFunctionality(cardDisplay, item);
        }
    }
    
    void SetupDragFunctionality(GameObject cardDisplay, ShopItem item)
    {
        // Store the shop item data FIRST
        ShopItemData shopData = cardDisplay.GetComponent<ShopItemData>();
        if (shopData == null)
            shopData = cardDisplay.AddComponent<ShopItemData>();
        shopData.shopItem = item;
        
        // Remove any existing CardDragDrop component (from inventory prefabs)
        CardDragDrop existingDragDrop = cardDisplay.GetComponent<CardDragDrop>();
        if (existingDragDrop != null)
        {
            DestroyImmediate(existingDragDrop);
        }
        
        // Add shop-specific drag and drop functionality
        ShopItemDragDrop shopDragDrop = cardDisplay.GetComponent<ShopItemDragDrop>();
        if (shopDragDrop == null)
            shopDragDrop = cardDisplay.AddComponent<ShopItemDragDrop>();
        
        // Manually set the shop item reference
        shopDragDrop.shopItem = item;
        
        // Set up canvas references for dragging
        Canvas parentCanvas = shopItemsPanel.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        
        shopDragDrop.canvas = parentCanvas;
        if (shopDragDrop.canvas != null)
        {
            shopDragDrop.raycaster = shopDragDrop.canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Make sure required components exist
        if (cardDisplay.GetComponent<CanvasGroup>() == null)
        {
            cardDisplay.AddComponent<CanvasGroup>();
        }
        
        Debug.Log($"Shop item will use canvas: {(parentCanvas != null ? parentCanvas.name : "NULL")}");
    }
    
    void AddShadowEffect(GameObject cardDisplay)
    {
        // Darken the card to create shadow effect
        Image cardBg = cardDisplay.GetComponent<Image>();
        if (cardBg != null)
        {
            Color shadowColor = cardBg.color;
            shadowColor.r *= 0.7f;
            shadowColor.g *= 0.7f;
            shadowColor.b *= 0.7f;
            cardBg.color = shadowColor;
        }
        
        // Also darken any child images
        Image[] childImages = cardDisplay.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            if (img != cardBg) // Don't double-darken the main background
            {
                Color shadowColor = img.color;
                shadowColor.r *= 0.8f;
                shadowColor.g *= 0.8f;
                shadowColor.b *= 0.8f;
                img.color = shadowColor;
            }
        }
    }
    
    void MakeCardNonInteractable(GameObject cardDisplay)
    {
        // Remove any interaction components from shadow cards
        CanvasGroup canvasGroup = cardDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardDisplay.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
    }
    
    void ClearShopDisplay()
    {
        // Clear all child objects from shop items panel
        for (int i = shopItemsPanel.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(shopItemsPanel.GetChild(i).gameObject);
        }
    }
    
    public bool PurchaseItem(ShopItem item, GameObject itemDisplay)
    {
        // Check if item is available
        if (item.quantity <= 0)
        {
            Debug.Log($"{item.itemName} is out of stock!");
            return false;
        }
        
        // Check if player has enough coins
        if (playerInventory.coins < item.price)
        {
            Debug.Log($"Not enough coins! Need {item.price}, have {playerInventory.coins}");
            return false;
        }
        
        Debug.Log($"Purchasing {item.itemName} for {item.price} coins");
        
        // Deduct coins
        playerInventory.coins -= item.price;
        
        // Reduce quantity
        item.quantity--;
        
        // Add item to player inventory
        if (item.itemType == ShopItem.ItemType.Gear && item.gearCard != null)
        {
            playerInventory.extraGear.Add(item.gearCard);
            Debug.Log($"Added {item.gearCard.gearName} to player's gear");
        }
        else if (item.itemType == ShopItem.ItemType.Action && item.actionCard != null)
        {
            playerInventory.actionCards.Add(item.actionCard);
            Debug.Log($"Added {item.actionCard.actionName} to player's actions");
        }
        
        // Update player info
        UpdatePlayerInfo();
        
        // Remove just the purchased card (top card) and make the next one draggable
        RemoveTopCardAndActivateNext(item, itemDisplay);
        
        Debug.Log($"Purchase successful! Player now has {playerInventory.coins} coins");
        return true;
    }
    
    void RemoveTopCardAndActivateNext(ShopItem item, GameObject purchasedCard)
    {
        // Find the stack container for this item
        Transform stackContainer = purchasedCard.transform.parent;
        
        if (stackContainer == null)
        {
            Debug.LogError("Could not find stack container!");
            return;
        }
        
        // Destroy the purchased card (should be the top one)
        Destroy(purchasedCard);
        
        // If there are still cards in the stack, make the new top card draggable
        if (stackContainer.childCount > 0)
        {
            // Find the new top card (highest index, since we stack from 0 up)
            GameObject newTopCard = null;
            float highestZ = float.MinValue;
            
            for (int i = 0; i < stackContainer.childCount; i++)
            {
                Transform child = stackContainer.GetChild(i);
                if (child.localPosition.z > highestZ)
                {
                    highestZ = child.localPosition.z;
                    newTopCard = child.gameObject;
                }
            }
            
            // Actually, let's use a simpler approach - the last child should be the top card
            if (stackContainer.childCount > 0)
            {
                newTopCard = stackContainer.GetChild(stackContainer.childCount - 1).gameObject;
            }
            
            if (newTopCard != null)
            {
                // Remove shadow effect from the new top card
                RemoveShadowEffect(newTopCard);
                
                // Make it draggable
                MakeCardDraggable(newTopCard, item);
                
                Debug.Log($"Made next card draggable for {item.itemName}. Remaining quantity: {item.quantity}");
            }
        }
        else
        {
            Debug.Log($"No more {item.itemName} cards available - out of stock!");
        }
    }
    
    void RemoveShadowEffect(GameObject cardDisplay)
    {
        // Restore normal colors
        Image cardBg = cardDisplay.GetComponent<Image>();
        if (cardBg != null)
        {
            Color normalColor = cardBg.color;
            normalColor.r /= 0.7f; // Reverse the darkening
            normalColor.g /= 0.7f;
            normalColor.b /= 0.7f;
            cardBg.color = normalColor;
        }
        
        // Restore child images
        Image[] childImages = cardDisplay.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            if (img != cardBg)
            {
                Color normalColor = img.color;
                normalColor.r /= 0.8f; // Reverse the darkening
                normalColor.g /= 0.8f;
                normalColor.b /= 0.8f;
                img.color = normalColor;
            }
        }
    }
    
    void MakeCardDraggable(GameObject cardDisplay, ShopItem item)
    {
        // Enable raycast blocking
        CanvasGroup canvasGroup = cardDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
        
        // Add drag functionality if it doesn't exist
        ShopItemDragDrop shopDragDrop = cardDisplay.GetComponent<ShopItemDragDrop>();
        if (shopDragDrop == null)
        {
            SetupDragFunctionality(cardDisplay, item);
        }
        
        Debug.Log($"Card is now draggable: {item.itemName}");
    }
    
    void UpdateShoppingCart()
    {
        // TODO: Display cart items in shopping cart panel
        Debug.Log($"Cart has {cartItems.Count} items");
    }
    
    void UpdatePlayerInfo()
    {
        if (playerCoinsText != null && playerInventory != null)
        {
            playerCoinsText.text = $"Coins: {playerInventory.coins}";
        }
    }
    
    public void Checkout()
    {
        int totalCost = 0;
        foreach (ShopItem item in cartItems)
        {
            totalCost += item.price;
        }
        
        if (playerInventory.coins >= totalCost)
        {
            // TODO: Add items to player inventory
            // TODO: Deduct coins
            Debug.Log($"Checkout successful! Total cost: {totalCost}");
            cartItems.Clear();
            UpdateShoppingCart();
            UpdatePlayerInfo();
        }
        else
        {
            Debug.Log($"Not enough coins! Need {totalCost}, have {playerInventory.coins}");
        }
    }
}

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
    public int quantity;
    public ItemType itemType;
    public GearCard gearCard;      // For gear items
    public ActionCard actionCard;  // For action items
    
    public enum ItemType
    {
        Gear,
        Action
    }
}