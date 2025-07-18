using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class CardDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Card References")]
    public GearCard gearCard;
    public ActionCard actionCard;
    public EffectCard effectCard;

    [Header("Game References")]  // Add this new section
    private PlayerInventory playerInventory;

    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private InventoryDisplay inventoryDisplay;

    void Awake()
    {
        Debug.Log($"CardDragDrop Awake called on {gameObject.name}");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Find the canvas and raycaster automatically
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (raycaster == null && canvas != null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }

        // Find the PlayerInventory component in the scene
        playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (playerInventory == null)
        {
            Debug.LogError("Could not find PlayerInventory in scene!");
        }
        else
        {
            Debug.Log("Found PlayerInventory successfully!");
        }

        // Find the inventory display - but don't fail if it's not found yet
        if (inventoryDisplay == null)
        {
            inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
            if (inventoryDisplay == null)
            {
                Debug.LogWarning("InventoryDisplay not found in Awake, will try again later");
            }
        }

        Debug.Log($"After Awake - GearCard: {(gearCard != null ? gearCard.gearName : "NULL")}");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle clicks on gear cards (not action cards)
        if (gearCard == null) return;

        Debug.Log($"Clicked on gear: {gearCard.gearName}");

        // Find the gear comparison display
        GearComparisonDisplay comparisonDisplay = FindFirstObjectByType<GearComparisonDisplay>();

        if (comparisonDisplay != null)
        {
            comparisonDisplay.ShowGearComparison(gearCard);
            Debug.Log($"Sent {gearCard.gearName} to comparison display");
        }
        else
        {
            Debug.LogError("Could not find GearComparisonDisplay component!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Allow dragging both gear cards and action cards
        if (gearCard == null && actionCard == null) return;

        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to top of hierarchy so it renders on top
        transform.SetParent(canvas.transform, true);

        if (gearCard != null)
        {
            Debug.Log($"Started dragging gear: {gearCard.gearName}");
        }
        else if (actionCard != null)
        {
            Debug.Log($"Started dragging action: {actionCard.actionName}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gearCard == null && actionCard == null) return;

        // Move the card with the mouse/touch
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("=== OnEndDrag called ===");
        Debug.Log($"GearCard: {(gearCard != null ? gearCard.gearName : "NULL")}");
        Debug.Log($"ActionCard: {(actionCard != null ? actionCard.actionName : "NULL")}");

        if (gearCard == null && actionCard == null) return;

        // Restore appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        bool shouldRefresh = true; // Track if we should refresh inventory

        // Handle different card types
        if (gearCard != null)
        {
            HandleGearCardDrop(eventData);
        }
        else if (actionCard != null)
        {
            // Check if action card was successfully played
            ActionCardDropZone actionDropZone = GetActionDropZone(eventData);
            if (actionDropZone != null)
            {
                Debug.Log($"Attempting to drop {actionCard.actionName} on action drop zone");

                // Validate the drop first
                bool canDrop = ValidateActionCardDrop(actionDropZone);

                if (canDrop)
                {
                    // Let the ActionCardDropZone handle the drop
                    Debug.Log($"Letting ActionCardDropZone handle {actionCard.actionName}");
                    actionDropZone.OnDrop(eventData);
                    shouldRefresh = false; // Don't refresh if card was successfully played
                }
                else
                {
                    // Invalid drop - return to original position
                    Debug.Log($"Invalid drop for {actionCard.actionName}, returning to original position");
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                // Invalid drop - return to original position
                ReturnToOriginalPosition();
                Debug.Log($"Invalid drop for {actionCard.actionName}, returning to original position");
            }
        }

        // Only refresh the inventory display if needed
        if (shouldRefresh && inventoryDisplay != null)
        {
            StartCoroutine(RefreshAfterDrop());
        }
        else if (shouldRefresh)
        {
            // Try to find it again
            inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
            {
                StartCoroutine(RefreshAfterDrop());
            }
        }
    }

    void HandleGearCardDrop(PointerEventData eventData)
    {
        // Check what we dropped on for gear cards (existing functionality)
        DropZone dropZone = GetDropZone(eventData);

        if (dropZone != null && dropZone.CanAcceptCard(gearCard))
        {
            // Valid drop - handle the gear movement
            HandleGearMove(dropZone);
            Debug.Log($"Dropped {gearCard.gearName} on {dropZone.zoneName}");
        }
        else
        {
            // Invalid drop - return to original position
            ReturnToOriginalPosition();
            Debug.Log($"Invalid drop for {gearCard.gearName}, returning to original position");
        }
    }



    System.Collections.IEnumerator RefreshAfterDrop()
    {
        yield return new WaitForEndOfFrame();
        inventoryDisplay.RefreshDisplay();
    }

    DropZone GetDropZone(PointerEventData eventData)
    {
        // Raycast to find what we're over (for gear cards)
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);

        Debug.Log($"Raycast found {raycastResults.Count} objects");

        foreach (var result in raycastResults)
        {
            Debug.Log($"Raycast hit: {result.gameObject.name}");
            DropZone dropZone = result.gameObject.GetComponent<DropZone>();
            if (dropZone != null)
            {
                Debug.Log($"Found DropZone: {dropZone.zoneName}");
                return dropZone;
            }
        }

        Debug.Log("No DropZone found in raycast results");
        return null;
    }

    ActionCardDropZone GetActionDropZone(PointerEventData eventData)
    {
        // Raycast to find what we're over (for action cards)
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);

        Debug.Log($"Raycast found {raycastResults.Count} results");

        foreach (var raycastResult in raycastResults)
        {
            Debug.Log($"Raycast hit: {raycastResult.gameObject.name}");
            ActionCardDropZone actionDropZone = raycastResult.gameObject.GetComponent<ActionCardDropZone>();
            if (actionDropZone != null)
            {
                Debug.Log("Found ActionCardDropZone component!");
                return actionDropZone;
            }
        }

        Debug.Log("No ActionCardDropZone found in raycast results");
        return null;
    }

    void HandleGearMove(DropZone dropZone)
    {
        Debug.Log("=== HandleGearMove called ===");
        Debug.Log($"PlayerInventory is: {(playerInventory != null ? "FOUND" : "NULL")}");
        Debug.Log($"GearCard is: {(gearCard != null ? gearCard.gearName : "NULL")}");
        Debug.Log($"DropZone is: {(dropZone != null ? dropZone.zoneName : "NULL")}");
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned to CardDragDrop!");
            ReturnToOriginalPosition();
            return;
        }

        // Remove from current location
        RemoveFromCurrentLocation();

        // Add to new location
        dropZone.AcceptCard(gearCard);

        // Return to original parent temporarily to fix positioning
        transform.SetParent(originalParent, true);
    }

    void RemoveFromCurrentLocation()
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;

        // Check equipped slots
        if (inventory.equippedRod == gearCard) inventory.equippedRod = null;
        else if (inventory.equippedReel == gearCard) inventory.equippedReel = null;
        else if (inventory.equippedLine == gearCard) inventory.equippedLine = null;
        else if (inventory.equippedLure == gearCard) inventory.equippedLure = null;
        else if (inventory.equippedBait == gearCard) inventory.equippedBait = null;
        else if (inventory.equippedExtra1 == gearCard) inventory.equippedExtra1 = null;
        else if (inventory.equippedExtra2 == gearCard) inventory.equippedExtra2 = null;

        // Check tackle box
        else if (inventory.extraGear.Contains(gearCard))
        {
            inventory.extraGear.Remove(gearCard);
        }
    }

    void ReturnToOriginalPosition()
    {
        // Return to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }
    bool ValidateActionCardDrop(ActionCardDropZone dropZone)
{
    if (actionCard == null || dropZone == null) return false;
    
    // Check if we're in the interactive phase
    FishingManager fishingManager = FindFirstObjectByType<FishingManager>();
    if (fishingManager == null || !fishingManager.isInteractionPhase)
    {
        Debug.LogWarning("Cannot play action cards - not in interactive phase!");
        return false;
    }
    
    // Check if this card can target what this drop zone affects
    if (dropZone.targetsPlayer && !actionCard.canTargetPlayer)
    {
        Debug.LogWarning($"{actionCard.actionName} cannot target players!");
        return false;
    }
    
    if (!dropZone.targetsPlayer && !actionCard.canTargetFish)
    {
        Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
        return false;
    }
    
    // Check for zero effects - this fixes your main issue
    int effectValue = dropZone.targetsPlayer ? actionCard.playerEffect : actionCard.fishEffect;
    if (effectValue == 0)
    {
        Debug.LogWarning($"{actionCard.actionName} has no effect on {(dropZone.targetsPlayer ? "players" : "fish")}!");
        return false;
    }
    
    // Check max action card limit
    if (NetworkManager.Singleton != null)
    {
        ulong playerId = NetworkManager.Singleton.LocalClientId;
        
        // Get the interactive UI to check limits
        InteractivePhaseUI interactiveUI = FindFirstObjectByType<InteractivePhaseUI>();
        if (interactiveUI != null)
        {
            if (!interactiveUI.CanPlayerPlayMoreCards(playerId))
            {
                Debug.LogWarning($"Player {playerId} has reached their card limit this turn!");
                return false;
            }
        }
    }
    
    Debug.Log($"Action card {actionCard.actionName} passed all validation checks");
    return true;
}
}