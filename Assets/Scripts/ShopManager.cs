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
    public GameObject gearCardPrefab;
    public GameObject actionCardPrefab;
    public float itemSpacing = 10f;
    
    [Header("Player Info")]
    public TextMeshProUGUI playerCoinsText;
    public PlayerInventory playerInventory;
    
    [Header("Shop Inventory")]
    public List<ShopItem> shopGearItems = new List<ShopItem>();
    public List<ShopItem> shopActionItems = new List<ShopItem>();
    
    [Header("Shopping Cart")]
    public List<ShopItem> cartItems = new List<ShopItem>();
    public List<GameObject> cartItemDisplays = new List<GameObject>();
    public Button checkoutButton;
    public TextMeshProUGUI cartTotalText;
    public TextMeshProUGUI cartItemCountText;
    
    void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        if (checkoutButton != null)
        {
            checkoutButton.onClick.AddListener(Checkout);
        }
        
        SetupShop();
        UpdatePlayerInfo();
    }
    
  void SetupShop()
    {
        Debug.Log("Setting up shop...");
        LoadRealGearItems();
        LoadRealActionItems();
        DisplayShopItems();
    }
    
    void LoadRealGearItems()
    {
        #if UNITY_EDITOR
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
        
        List<GearCard> selectedGear = new List<GearCard>();
        while (selectedGear.Count < 8 && allGearCards.Count > 0)
        {
            int randomIndex = Random.Range(0, allGearCards.Count);
            selectedGear.Add(allGearCards[randomIndex]);
            allGearCards.RemoveAt(randomIndex);
        }
        
        foreach (GearCard gear in selectedGear)
        {
            ShopItem shopItem = new ShopItem();
            shopItem.itemName = gear.gearName;
            shopItem.price = Mathf.RoundToInt(gear.price);
            shopItem.quantity = Random.Range(1, 4);
            shopItem.itemType = ShopItem.ItemType.Gear;
            shopItem.gearCard = gear;
            shopGearItems.Add(shopItem);
        }
        #else
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
        
        List<ActionCard> selectedActions = new List<ActionCard>();
        while (selectedActions.Count < 8 && allActionCards.Count > 0)
        {
            int randomIndex = Random.Range(0, allActionCards.Count);
            selectedActions.Add(allActionCards[randomIndex]);
            allActionCards.RemoveAt(randomIndex);
        }
        
        foreach (ActionCard action in selectedActions)
        {
            ShopItem shopItem = new ShopItem();
            shopItem.itemName = action.actionName;
            shopItem.price = 25;
            shopItem.quantity = Random.Range(2, 6);
            shopItem.itemType = ShopItem.ItemType.Action;
            shopItem.actionCard = action;
            shopActionItems.Add(shopItem);
        }
        #else
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
        ClearShopDisplay();
        Vector2 currentPosition = Vector2.zero;
        
        for (int i = 0; i < shopGearItems.Count; i++)
        {
            CreateShopItemStack(shopGearItems[i], currentPosition);
            
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
        
        currentPosition.y -= 140;
        currentPosition.x = 0;
        
        for (int i = 0; i < shopActionItems.Count; i++)
        {
            CreateShopItemStack(shopActionItems[i], currentPosition);
            
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
        GameObject stackContainer = new GameObject($"{item.itemName}_Stack");
        stackContainer.transform.SetParent(shopItemsPanel, false);
        
        RectTransform containerRect = stackContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(140, 100);
        
        int cardsToShow = Mathf.Min(item.quantity, 5);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            GameObject cardDisplay = null;
            
            if (item.itemType == ShopItem.ItemType.Gear && gearCardPrefab != null)
            {
                cardDisplay = Instantiate(gearCardPrefab, stackContainer.transform);
            }
            else if (item.itemType == ShopItem.ItemType.Action && actionCardPrefab != null)
            {
                cardDisplay = Instantiate(actionCardPrefab, stackContainer.transform);
            }
            
            if (cardDisplay == null) return;
            
            cardDisplay.name = $"{item.itemName}_Card_{i}";
            
            RectTransform cardRect = cardDisplay.GetComponent<RectTransform>();
            Vector2 stackOffset = new Vector2(i * 3f, -i * 3f);
            cardRect.anchoredPosition = stackOffset;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = Vector2.zero;
            
            bool isTopCard = (i == cardsToShow - 1);
            
            SetupCardDisplay(cardDisplay, item, isTopCard);
            
            if (!isTopCard)
            {
                AddShadowEffect(cardDisplay);
                MakeCardNonInteractable(cardDisplay);
            }
        }
    }
    
    void SetupCardDisplay(GameObject cardDisplay, ShopItem item, bool isDraggable)
    {
        CardDisplay cardDisplayComponent = cardDisplay.GetComponent<CardDisplay>();
        if (cardDisplayComponent != null)
        {
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
            
            cardDisplayComponent.isShopItem = true;
            cardDisplayComponent.itemPrice = item.price;
            cardDisplayComponent.itemQuantity = item.quantity;
            
            cardDisplayComponent.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
        }
        
        if (isDraggable)
        {
            SetupDragFunctionality(cardDisplay, item);
        }
    }
    
    void SetupDragFunctionality(GameObject cardDisplay, ShopItem item)
    {
        ShopItemData shopData = cardDisplay.GetComponent<ShopItemData>();
        if (shopData == null)
            shopData = cardDisplay.AddComponent<ShopItemData>();
        shopData.shopItem = item;
        
        CardDragDrop existingCardDrag = cardDisplay.GetComponent<CardDragDrop>();
        if (existingCardDrag != null)
        {
            DestroyImmediate(existingCardDrag);
        }
        
        ShopItemDragDrop existingShopDrag = cardDisplay.GetComponent<ShopItemDragDrop>();
        if (existingShopDrag != null)
        {
            DestroyImmediate(existingShopDrag);
        }
        
        ShopItemDragDrop shopDragDrop = cardDisplay.AddComponent<ShopItemDragDrop>();
        shopDragDrop.shopItem = item;
        
        Canvas parentCanvas = shopItemsPanel.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        
        if (parentCanvas != null)
        {
            shopDragDrop.canvas = parentCanvas;
            shopDragDrop.raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        }
        
        CanvasGroup canvasGroup = cardDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardDisplay.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
    
    void AddShadowEffect(GameObject cardDisplay)
    {
        Image cardBg = cardDisplay.GetComponent<Image>();
        if (cardBg != null)
        {
            Color shadowColor = cardBg.color;
            shadowColor.r *= 0.7f;
            shadowColor.g *= 0.7f;
            shadowColor.b *= 0.7f;
            cardBg.color = shadowColor;
        }
        
        Image[] childImages = cardDisplay.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            if (img != cardBg)
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
        CanvasGroup canvasGroup = cardDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardDisplay.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
    }
    
    void ClearShopDisplay()
    {
        for (int i = shopItemsPanel.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(shopItemsPanel.GetChild(i).gameObject);
        }
    }
    
    public bool AddToCart(ShopItem item, GameObject itemDisplay)
    {
        if (item.quantity <= 0)
        {
            Debug.Log($"{item.itemName} is out of stock!");
            return false;
        }
        
        Debug.Log($"Adding {item.itemName} to cart");
        
        cartItems.Add(item);
        cartItemDisplays.Add(itemDisplay);
        
        RemoveTopCardFromStack(item, itemDisplay);
        MoveCardToCart(itemDisplay);
        UpdateShoppingCart();
        
        Debug.Log($"Successfully added {item.itemName} to cart");
        return true;
    }
    
    void RemoveTopCardFromStack(ShopItem item, GameObject cardDisplay)
    {
        ShopItemDragDrop dragComponent = cardDisplay.GetComponent<ShopItemDragDrop>();
        if (dragComponent != null && dragComponent.OriginalParent != null)
        {
            Transform stackContainer = dragComponent.OriginalParent;
            Debug.Log($"Removing card from stack: {stackContainer.name}");
            
            StartCoroutine(ActivateNextCardAfterMove(stackContainer, item));
        }
        else
        {
            Debug.LogWarning("Could not find original parent to activate next card");
        }
    }
    
    System.Collections.IEnumerator ActivateNextCardAfterMove(Transform stackContainer, ShopItem item)
    {
        yield return null;
        
        Debug.Log($"Checking for next card in stack: {stackContainer.name}, children: {stackContainer.childCount}");
        
        if (stackContainer.childCount > 0)
        {
            GameObject newTopCard = FindTopCard(stackContainer);
            
            if (newTopCard != null)
            {
                Debug.Log($"Found new top card: {newTopCard.name}");
                
                RemoveShadowEffect(newTopCard);
                MakeCardInteractable(newTopCard);
                
                ShopItemDragDrop existingDrag = newTopCard.GetComponent<ShopItemDragDrop>();
                if (existingDrag != null)
                {
                    DestroyImmediate(existingDrag);
                }
                
                SetupDragFunctionality(newTopCard, item);
                
                Debug.Log($"Next card {newTopCard.name} is now draggable");
            }
        }
        else
        {
            Debug.Log($"No more cards in stack {stackContainer.name}");
        }
    }
    
    void MoveCardToCart(GameObject cardDisplay)
    {
        if (shoppingCartPanel != null)
        {
            cardDisplay.transform.SetParent(shoppingCartPanel, false);
            
            RectTransform cardRect = cardDisplay.GetComponent<RectTransform>();
            int cardIndex = cartItemDisplays.Count - 1;
            
            float cardWidth = 120f;
            float cardHeight = 80f;
            float spacing = 10f;
            
            int row = cardIndex / 2;
            int col = cardIndex % 2;
            
            Vector2 cartPosition = new Vector2(
                col * (cardWidth + spacing) + spacing,
                -(row * (cardHeight + spacing) + spacing)
            );
            
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(0, 1);
            cardRect.pivot = new Vector2(0, 1);
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            cardRect.anchoredPosition = cartPosition;
            
            ShopItemDragDrop shopDrag = cardDisplay.GetComponent<ShopItemDragDrop>();
            if (shopDrag != null) Destroy(shopDrag);
            
            CartItemDragDrop cartDrag = cardDisplay.AddComponent<CartItemDragDrop>();
            Canvas parentCanvas = shoppingCartPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                cartDrag.canvas = parentCanvas;
                cartDrag.raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            }
            
            Debug.Log($"Moved {cardDisplay.name} to cart at position {cartPosition}");
        }
        else
        {
            Debug.LogError("Shopping cart panel not assigned!");
        }
    }
    
    public void RemoveFromCart(ShopItem item, GameObject itemDisplay)
    {
        Debug.Log($"Removing {item.itemName} from cart");
        
        int itemIndex = -1;
        for (int i = 0; i < cartItems.Count; i++)
        {
            if (cartItems[i] == item && cartItemDisplays[i] == itemDisplay)
            {
                itemIndex = i;
                break;
            }
        }
        
        if (itemIndex >= 0)
        {
            cartItems.RemoveAt(itemIndex);
            cartItemDisplays.RemoveAt(itemIndex);
            
            ReturnCardToStack(item, itemDisplay);
            UpdateShoppingCart();
            RearrangeCartItems();
            
            Debug.Log($"Successfully removed {item.itemName} from cart");
        }
        else
        {
            Debug.LogWarning($"Could not find {item.itemName} in cart to remove");
        }
    }
    
    void ReturnCardToStack(ShopItem item, GameObject cardDisplay)
    {
        Debug.Log($"Returning {cardDisplay.name} to its original stack");
        
        string stackName = $"{item.itemName}_Stack";
        Transform stackContainer = shopItemsPanel.Find(stackName);
        
        if (stackContainer != null)
        {
            CartItemDragDrop cartDrag = cardDisplay.GetComponent<CartItemDragDrop>();
            if (cartDrag != null)
            {
                Destroy(cartDrag);
            }
            
            cardDisplay.transform.SetParent(stackContainer, false);
            
            int currentStackCount = stackContainer.childCount - 1;
            Vector2 stackOffset = new Vector2(currentStackCount * 3f, -currentStackCount * 3f);
            
            RectTransform cardRect = cardDisplay.GetComponent<RectTransform>();
            cardRect.anchoredPosition = stackOffset;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = Vector2.zero;
            cardRect.sizeDelta = new Vector2(140, 100);
            
            SetupDragFunctionality(cardDisplay, item);
            
            Debug.Log($"Returned {cardDisplay.name} to stack {stackName}");
        }
        else
        {
            Debug.LogWarning($"Could not find stack container {stackName}");
            Destroy(cardDisplay);
        }
    }
    
    void RearrangeCartItems()
    {
        for (int i = 0; i < cartItemDisplays.Count; i++)
        {
            GameObject cardDisplay = cartItemDisplays[i];
            if (cardDisplay != null)
            {
                RectTransform cardRect = cardDisplay.GetComponent<RectTransform>();
                
                float cardWidth = 120f;
                float cardHeight = 80f;
                float spacing = 10f;
                
                int row = i / 2;
                int col = i % 2;
                
                Vector2 cartPosition = new Vector2(
                    col * (cardWidth + spacing) + spacing,
                    -(row * (cardHeight + spacing) + spacing)
                );
                
                cardRect.anchoredPosition = cartPosition;
            }
        }
        
        Debug.Log($"Rearranged {cartItemDisplays.Count} items in cart");
    }
    
    GameObject FindTopCard(Transform stackContainer)
    {
        GameObject topCard = null;
        float highestX = float.MinValue;
        
        for (int i = 0; i < stackContainer.childCount; i++)
        {
            Transform child = stackContainer.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                float cardX = childRect.anchoredPosition.x;
                if (cardX > highestX)
                {
                    highestX = cardX;
                    topCard = child.gameObject;
                }
            }
        }
        
        return topCard;
    }
    
    void MakeCardInteractable(GameObject cardDisplay)
    {
        CanvasGroup canvasGroup = cardDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardDisplay.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = true;
    }
    
    void RemoveShadowEffect(GameObject cardDisplay)
    {
        Image cardBg = cardDisplay.GetComponent<Image>();
        if (cardBg != null)
        {
            Color normalColor = cardBg.color;
            normalColor.r = Mathf.Min(1f, normalColor.r / 0.7f);
            normalColor.g = Mathf.Min(1f, normalColor.g / 0.7f);
            normalColor.b = Mathf.Min(1f, normalColor.b / 0.7f);
            cardBg.color = normalColor;
        }
        
        Image[] childImages = cardDisplay.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            if (img != cardBg)
            {
                Color normalColor = img.color;
                normalColor.r = Mathf.Min(1f, normalColor.r / 0.8f);
                normalColor.g = Mathf.Min(1f, normalColor.g / 0.8f);
                normalColor.b = Mathf.Min(1f, normalColor.b / 0.8f);
                img.color = normalColor;
            }
        }
    }
    
    
    
    void UpdatePlayerInfo()
    {
        if (playerCoinsText != null && playerInventory != null)
        {
            playerCoinsText.text = $"Coins: {playerInventory.coins}";
        }
    }
    
    void UpdateShoppingCart()
    {
        if (cartItemCountText != null)
        {
            cartItemCountText.text = $"Items: {cartItems.Count}";
        }
        
        int totalCost = 0;
        foreach (ShopItem item in cartItems)
        {
            totalCost += item.price;
        }
        
        if (cartTotalText != null)
        {
            cartTotalText.text = $"Total: ${totalCost}";
        }
        
        if (checkoutButton != null)
        {
            bool canCheckout = cartItems.Count > 0 && playerInventory.coins >= totalCost;
            checkoutButton.interactable = canCheckout;
        }
        
        Debug.Log($"Cart updated: {cartItems.Count} items, total cost: ${totalCost}");
    }
    
    public void Checkout()
    {
        if (cartItems.Count == 0)
        {
            Debug.Log("Cart is empty!");
            return;
        }
        
        int totalCost = 0;
        foreach (ShopItem item in cartItems)
        {
            totalCost += item.price;
        }
        
        if (playerInventory.coins < totalCost)
        {
            Debug.Log($"Not enough coins! Need {totalCost}, have {playerInventory.coins}");
            return;
        }
        
        Debug.Log($"Processing checkout: {cartItems.Count} items for ${totalCost}");
        
        playerInventory.coins -= totalCost;
        
        foreach (ShopItem item in cartItems)
        {
            item.quantity--;
            
            if (item.itemType == ShopItem.ItemType.Gear && item.gearCard != null)
{
    // Create a copy of the gear to avoid modifying the original asset
    GearCard gearCopy = Instantiate(item.gearCard);
    gearCopy.maxDurability = gearCopy.durability; // Store original durability as max
    
    playerInventory.extraGear.Add(gearCopy);
    Debug.Log($"Added {gearCopy.gearName} copy to player's gear (Durability: {gearCopy.durability}/{gearCopy.maxDurability})");
}
            else if (item.itemType == ShopItem.ItemType.Action && item.actionCard != null)
            {
                playerInventory.actionCards.Add(item.actionCard);
                Debug.Log($"Added {item.actionCard.actionName} to player's actions");
            }
        }
        
        ClearCart();
        UpdatePlayerInfo();
        UpdateShoppingCart();
        
        Debug.Log($"Checkout successful! Player now has {playerInventory.coins} coins");
    }
    
    public void ClearCart()
    {
        foreach (GameObject cardDisplay in cartItemDisplays)
        {
            if (cardDisplay != null)
            {
                Destroy(cardDisplay);
            }
        }
        
        cartItems.Clear();
        cartItemDisplays.Clear();
        
        Debug.Log("Shopping cart cleared");
    }
    
    public bool PurchaseItem(ShopItem item, GameObject itemDisplay, Transform originalStackContainer = null)
    {
        Debug.Log($"PurchaseItem called for {item.itemName}, redirecting to AddToCart");
        return AddToCart(item, itemDisplay);
    }
}

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
    public int quantity;
    public ItemType itemType;
    public GearCard gearCard;
    public ActionCard actionCard;
    
    public enum ItemType
    {
        Gear,
        Action
    }
}