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
    
    [Header("Card Display Prefab")]
    public GameObject cardDisplayPrefab;
    
    [Header("Player Reference")]
    public PlayerInventory playerInventory;
    
    [Header("Card Size")]
    public float cardWidth = 200;
    public float cardHeight = 200f;
    public float cardSpacing = 10f;
    
    private List<GameObject> displayedCards = new List<GameObject>();
    
    void Start()
    {
        if (cardDisplayPrefab == null)
        {
            CreateCardDisplayPrefab();
        }
        
        // Use the persistent PlayerInventory if available
        if (PlayerInventory.Instance != null)
        {
            playerInventory = PlayerInventory.Instance;
        }
        
        UpdateDisplay();
    }
    
    void CreateCardDisplayPrefab()
    {
        // Create a card display prefab programmatically
        GameObject prefab = new GameObject("CardDisplayPrefab");
        
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
        
        // Create text elements
        GameObject nameText = CreateTextElement(prefab, "CardName", new Vector2(0, 70), "Card Name");
        GameObject typeText = CreateTextElement(prefab, "CardType", new Vector2(0, 40), "Type");
        GameObject statsText = CreateTextElement(prefab, "Stats", new Vector2(0, -20), "Stats");
        
        // Connect to CardDisplay component
        cardDisplay.cardNameText = nameText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.cardTypeText = typeText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.statsText = statsText.GetComponent<TMPro.TextMeshProUGUI>();
        cardDisplay.cardBackground = bgImage;
        
        // Store as prefab reference
        cardDisplayPrefab = prefab;
        
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
        // Clear existing displayed cards
        ClearDisplay();
        
        if (playerInventory == null) return;
        
        // Display equipped gear
        DisplayEquippedGear();
        
        // Display tackle box gear (only if panel exists)
        if (tackleBoxPanel != null)
        {
            DisplayTackleBoxGear();
        }
        
        // Display action cards
        DisplayActionCards();
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
        Debug.Log($"DisplayTackleBoxGear called. Extra gear count: {playerInventory.extraGear.Count}");
        
        Vector2 position = new Vector2(10, -10); // Start from top-left
        
        foreach (GearCard gear in playerInventory.extraGear)
        {
            Debug.Log($"Displaying gear in tackle box: {gear.gearName}");
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
        Vector2 position = new Vector2(10, -10); // Start from top-left
        
        foreach (ActionCard action in playerInventory.actionCards)
        {
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
        // Skip if no card to display
        if (gearCard == null && fishCard == null && actionCard == null) return;
        
        // Create card display instance
        GameObject cardObj = Instantiate(cardDisplayPrefab, parent);
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
        
        // Add drag and drop functionality for gear cards
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
        UpdateDisplay();
    }
}