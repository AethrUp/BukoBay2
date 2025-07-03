using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryDisplay : MonoBehaviour
{
    [Header("UI Panels")]
    public Transform equippedGearPanel;
    public Transform tackleBoxPanel;
    public Transform actionCardsPanel;
    
    [Header("Individual Gear Panels")]
    public Transform rodPanel;
    public Transform reelPanel;
    public Transform linePanel;
    public Transform lurePanel;
    public Transform baitPanel;
    
    [Header("Card Display Prefabs")]
    public GameObject cardDisplayPrefab;  // For gear cards
    public GameObject actionCardDisplayPrefab;  // For action cards
    
    [Header("Player Reference")]
    public PlayerInventory playerInventory;
    
    [Header("Card Size")]
    public float cardWidth = 200;
    public float cardHeight = 200f;
    public float cardSpacing = 10f;
    
    private List<GameObject> displayedCards = new List<GameObject>();
    private bool hasDisplayedOnce = false;
    
    void Start()
    {
        if (cardDisplayPrefab == null)
        {
            CreateGearCardDisplayPrefab();
        }
        
        if (actionCardDisplayPrefab == null)
        {
            CreateActionCardDisplayPrefab();
        }
        
        // Don't call UpdateDisplay() here - wait for PlayerInventory to exist
        StartCoroutine(WaitForPlayerInventory());
    }
    
    System.Collections.IEnumerator WaitForPlayerInventory()
    {
        Debug.Log("Waiting for PlayerInventory...");
        
        // Wait until PlayerInventory exists
        while (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            yield return null; // Wait one frame
        }
        
        Debug.Log("PlayerInventory found! Updating display...");
        hasDisplayedOnce = true;
        UpdateDisplay();
    }
    
    void Update()
    {
        // Continuously check for PlayerInventory (like FishingUI does)
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (playerInventory != null)
            {
                Debug.Log("InventoryDisplay: Connected to persistent PlayerInventory in Update");
                UpdateDisplay();
            }
        }
    }
    
    void OnEnable()
    {
        // Refresh display when this GameObject becomes active (like when returning from setup scene)
        if (playerInventory != null)
        {
            Debug.Log("OnEnable called - refreshing display...");
            UpdateDisplay();
        }
    }
    
    void CreateGearCardDisplayPrefab()
    {
        // Create a gear card display prefab programmatically
        GameObject prefab = new GameObject("GearCardDisplayPrefab");
        
        // Add RectTransform and set it up properly
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add CardDisplay component
        CardDisplay cardDisplay = prefab.AddComponent<CardDisplay>();
        
        // Add background image
        UnityEngine.UI.Image bgImage = prefab.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.8f, 0.8f, 0.8f, 0.9f); // Light gray background
        
        // Create text elements for gear cards
        GameObject nameText = CreateTextElement(prefab, "CardName", new Vector2(0, 70), "Card Name");
        GameObject typeText = CreateTextElement(prefab, "CardType", new Vector2(0, 40), "Type");
        GameObject powerText = CreateTextElement(prefab, "Power", new Vector2(-50, 10), "0");
        GameObject durabilityText = CreateTextElement(prefab, "Durability", new Vector2(50, 10), "0");
        
        // Connect to CardDisplay component
        cardDisplay.cardNameText = nameText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.cardTypeText = typeText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.powerText = powerText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.durabilityText = durabilityText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.cardBackground = bgImage;
        
        // Store as prefab reference
        cardDisplayPrefab = prefab;
        
        // Deactivate the original
        prefab.SetActive(false);
    }
    
    void CreateActionCardDisplayPrefab()
    {
        // Create an action card display prefab programmatically
        GameObject prefab = new GameObject("ActionCardDisplayPrefab");
        
        // Add RectTransform and set it up properly
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add CardDisplay component
        CardDisplay cardDisplay = prefab.AddComponent<CardDisplay>();
        
        // Add background image with different color for action cards
        UnityEngine.UI.Image bgImage = prefab.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.6f, 0.8f, 1f, 0.9f); // Light blue background for action cards
        
        // Create text elements for action cards (different layout)
        GameObject nameText = CreateTextElement(prefab, "CardName", new Vector2(0, 70), "Action Name");
        GameObject typeText = CreateTextElement(prefab, "CardType", new Vector2(0, 45), "Action");
        GameObject playerEffectText = CreateTextElement(prefab, "PlayerEffect", new Vector2(-50, 15), "P: 0");
        GameObject fishEffectText = CreateTextElement(prefab, "FishEffect", new Vector2(50, 15), "F: 0");
        GameObject descriptionText = CreateTextElement(prefab, "Description", new Vector2(0, -30), "Description");
        
        // Make description text smaller and multi-line
        TMPro.TextMeshProUGUI descComponent = descriptionText.GetComponent<TMPro.TextMeshProUGUI>();
        descComponent.fontSize = 8;
        descComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
        RectTransform descRect = descriptionText.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(cardWidth - 20, 60);
        
        // Connect to CardDisplay component
        cardDisplay.cardNameText = nameText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.cardTypeText = typeText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.powerText = playerEffectText.GetComponent<TMPro.TextMeshProUGUI>(); // Reuse powerText for player effect
        cardDisplay.durabilityText = fishEffectText.GetComponent<TMPro.TextMeshProUGUI>(); // Reuse durabilityText for fish effect
        cardDisplay.statsText = descriptionText.GetComponent<TMPro.TextMeshProUGUI>(); // Use statsText for description
        cardDisplay.cardBackground = bgImage;
        
        // Store as prefab reference
        actionCardDisplayPrefab = prefab;
        
        // Deactivate the original
        prefab.SetActive(false);
    }
    
    GameObject CreateTextElement(GameObject parent, string name, Vector2 position, string text)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        TMPro.TextMeshProUGUI textComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 12;
        textComponent.color = Color.black;
        textComponent.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchoredPosition = position;
        textRect.sizeDelta = new Vector2(cardWidth - 20, 20);
        
        return textObj;
    }
    
    public void UpdateDisplay()
    {
        Debug.Log("=== UpdateDisplay called ===");
        
        // Clear existing displayed cards
        ClearDisplay();
        
        if (playerInventory == null) 
        {
            Debug.Log("ERROR: PlayerInventory is null!");
            return;
        }
        
        Debug.Log("PlayerInventory found, checking gear...");
        
        // Debug check each equipped item
        Debug.Log($"Equipped Rod: {(playerInventory.equippedRod != null ? playerInventory.equippedRod.gearName : "NULL")}");
        Debug.Log($"Equipped Reel: {(playerInventory.equippedReel != null ? playerInventory.equippedReel.gearName : "NULL")}");
        Debug.Log($"Equipped Line: {(playerInventory.equippedLine != null ? playerInventory.equippedLine.gearName : "NULL")}");
        Debug.Log($"Extra Gear Count: {playerInventory.extraGear.Count}");
        Debug.Log($"Action Cards Count: {playerInventory.actionCards.Count}");
        
        // Display equipped gear
        Debug.Log("About to display equipped gear...");
        try
        {
            DisplayEquippedGear();
            Debug.Log("Equipped gear display completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error displaying equipped gear: {e.Message}");
        }
        
        // Display tackle box gear
        Debug.Log("About to display tackle box gear...");
        try
        {
            DisplayTackleBoxGear();
            Debug.Log("Tackle box display completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error displaying tackle box gear: {e.Message}");
        }
        
        // Display action cards
        Debug.Log("About to display action cards...");
        try
        {
            DisplayActionCards();
            Debug.Log("Action cards display completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error displaying action cards: {e.Message}");
        }
        
        Debug.Log($"=== UpdateDisplay complete. Total cards displayed: {displayedCards.Count} ===");
    }
    
    void ClearDisplay()
    {
        foreach (GameObject card in displayedCards)
        {
            if (card != null)
                DestroyImmediate(card);
        }
        displayedCards.Clear();
    }
    
    void DisplayEquippedGear()
    {
        Vector2 centerPosition = Vector2.zero; // Center the card in each individual panel
        
        // Display each equipped gear in its specific panel (centered)
        if (rodPanel != null)
            CreateCardDisplay(playerInventory.equippedRod, null, null, rodPanel, centerPosition, false);
            
        if (reelPanel != null)
            CreateCardDisplay(playerInventory.equippedReel, null, null, reelPanel, centerPosition, false);
            
        if (linePanel != null)
            CreateCardDisplay(playerInventory.equippedLine, null, null, linePanel, centerPosition, false);
            
        if (lurePanel != null)
            CreateCardDisplay(playerInventory.equippedLure, null, null, lurePanel, centerPosition, false);
            
        if (baitPanel != null)
            CreateCardDisplay(playerInventory.equippedBait, null, null, baitPanel, centerPosition, false);
        
        // Handle extra slots (these might still go in the main equipped panel or separate panels)
        Vector2 extraPosition = new Vector2(10, -10);
        CreateCardDisplay(playerInventory.equippedExtra1, null, null, equippedGearPanel, extraPosition, true);
        extraPosition.x += cardWidth + cardSpacing;
        CreateCardDisplay(playerInventory.equippedExtra2, null, null, equippedGearPanel, extraPosition, true);
    }
    
    void DisplayTackleBoxGear()
    {
        Vector2 position = new Vector2(10, -10); // Start from top-left
        
        foreach (GearCard gear in playerInventory.extraGear)
        {
            CreateCardDisplay(gear, null, null, tackleBoxPanel, position, true); // Use grid positioning
            position.x += cardWidth + cardSpacing;
            
            // Wrap to next row if needed
            if (position.x > 400)
            {
                position.x = 10;
                position.y -= cardHeight + cardSpacing;
            }
        }
    }
    
    void DisplayActionCards()
    {
        Debug.Log($"DisplayActionCards called - actionCardsPanel is {(actionCardsPanel != null ? "ASSIGNED" : "NULL")}");
        
        if (actionCardsPanel == null)
        {
            Debug.LogError("Action Cards Panel is not assigned in the Inspector!");
            return;
        }
        
        Vector2 position = new Vector2(10, -10); // Start from top-left
        
        foreach (ActionCard action in playerInventory.actionCards)
        {
            Debug.Log($"Found action card: {action.actionName}");
            CreateCardDisplay(null, null, action, actionCardsPanel, position, true); // Use grid positioning
            position.x += cardWidth + cardSpacing;
            
            // Wrap to next row if needed
            if (position.x > 400)
            {
                position.x = 10;
                position.y -= cardHeight + cardSpacing;
            }
        }
    }
    
    void CreateCardDisplay(GearCard gearCard, FishCard fishCard, ActionCard actionCard, Transform parent, Vector2 position, bool useGridPositioning = false)
    {
        Debug.Log($"CreateCardDisplay called - Gear: {(gearCard?.gearName ?? "NULL")}, Fish: {(fishCard?.fishName ?? "NULL")}, Action: {(actionCard?.actionName ?? "NULL")}, Parent: {(parent?.name ?? "NULL")}");
        
        // Skip if no card to display
        if (gearCard == null && fishCard == null && actionCard == null) 
        {
            Debug.Log("Skipping - no card to display");
            return;
        }
        
        // Choose the right prefab based on card type
        GameObject prefabToUse;
        if (actionCard != null)
        {
            prefabToUse = actionCardDisplayPrefab;
        }
        else
        {
            prefabToUse = cardDisplayPrefab;  // Use for gear cards
        }
        
        // Create card display instance
        GameObject cardObj = Instantiate(prefabToUse, parent);
        cardObj.SetActive(true);
        
        // Ensure proper RectTransform setup for UI parenting
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        
        if (useGridPositioning)
        {
            // Grid positioning for tackle box (top-left anchoring)
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(0, 1);
            cardRect.pivot = new Vector2(0, 1);
        }
        else
        {
            // Center positioning for individual slots
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
        }
        
        // Position relative to parent
        cardRect.anchoredPosition = position;
        
        // Set up the card display
        CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
        cardDisplay.gearCard = gearCard;
        cardDisplay.fishCard = fishCard;
        cardDisplay.actionCard = actionCard;
        
        // Add drag and drop functionality for gear cards only
        if (gearCard != null)
        {
            CardDragDrop dragDrop = cardObj.GetComponent<CardDragDrop>();
            if (dragDrop == null)
            {
                dragDrop = cardObj.AddComponent<CardDragDrop>();
            }
            
            dragDrop.gearCard = gearCard;
            dragDrop.canvas = GetComponentInParent<Canvas>();
            dragDrop.raycaster = dragDrop.canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Force update the display
        cardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
        
        // Track for cleanup
        displayedCards.Add(cardObj);
    }
    
    // Function to refresh display when inventory changes
    [ContextMenu("Refresh Display")]
    public void RefreshDisplay()
    {
        // Always try to find PlayerInventory first
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        if (playerInventory != null)
        {
            Debug.Log("Manual refresh called - PlayerInventory found");
            UpdateDisplay();
        }
        else
        {
            Debug.Log("Cannot refresh - PlayerInventory still not found");
        }
    }
    
    // Public method to force a refresh (can be called from other scripts)
    public void ForceRefresh()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        if (playerInventory != null)
        {
            Debug.Log("Force refresh called");
            UpdateDisplay();
        }
    }
}