using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card References")]
    public GearCard gearCard;
    public ActionCard actionCard;
    
    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private InventoryDisplay inventoryDisplay;
    private bool draggedFromInventory = false;
    
    void Awake()
    {
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
        
        if (raycaster == null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Find the inventory display
        inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging gear cards, not action cards (for now)
        if (gearCard == null) return;
        
        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Check if we're dragging from the inventory grid
        draggedFromInventory = IsInInventoryGrid();
        
        // Notify inventory display that dragging started
        if (inventoryDisplay != null)
        {
            inventoryDisplay.SetDragging(true);
        }
        
        // If dragging from inventory, create a placeholder
        if (draggedFromInventory && inventoryDisplay.playerInventory != null)
        {
            inventoryDisplay.playerInventory.CreatePlaceholder(gearCard);
        }
        
        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to top of hierarchy so it renders on top
        transform.SetParent(canvas.transform, true);
        
        Debug.Log($"Started dragging {gearCard.gearName} (from inventory: {draggedFromInventory})");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (gearCard == null) return;
        
        // Move the card with the mouse/touch
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (gearCard == null) return;
        
        // Restore appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check what we dropped on
        DropZone dropZone = GetDropZone(eventData);
        
        if (dropZone != null && dropZone.CanAcceptCard(gearCard))
        {
            // Valid drop - handle the gear movement
            HandleValidDrop(dropZone);
            Debug.Log($"Valid drop: {gearCard.gearName} on {dropZone.zoneName}");
        }
        else
        {
            // Invalid drop - restore everything
            HandleInvalidDrop();
            Debug.Log($"Invalid drop for {gearCard.gearName}, restoring to original position");
        }
        
        // Refresh the inventory display after a short delay
        if (inventoryDisplay != null)
        {
            StartCoroutine(RefreshAfterDrop());
        }
    }
    
    System.Collections.IEnumerator RefreshAfterDrop()
    {
        yield return new WaitForEndOfFrame();
        
        // Notify inventory display that dragging ended
        if (inventoryDisplay != null)
        {
            inventoryDisplay.SetDragging(false);
        }
        
        inventoryDisplay.RefreshDisplay();
        
        // Update the gear comparison totals if it exists
        GearComparisonDisplay comparisonDisplay = FindFirstObjectByType<GearComparisonDisplay>();
        if (comparisonDisplay != null)
        {
            comparisonDisplay.UpdateTotalEquippedDisplay();
        }
    }
    
    void HandleValidDrop(DropZone dropZone)
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        if (dropZone.zoneType == DropZone.DropZoneType.TackleBox)
        {
            // Dropping into tackle box
            if (draggedFromInventory)
            {
                // Moving within inventory - commit the placeholder removal
                inventory.CommitPlaceholderRemoval();
                inventory.AddToTackleBox(gearCard);
            }
            else
            {
                // Moving from equipped to inventory
                RemoveFromEquippedSlots();
                inventory.AddToTackleBox(gearCard);
            }
        }
        else
        {
            // Dropping into an equipped slot
            GearCard previouslyEquipped = GetCurrentlyEquippedGear(dropZone);
            
            // Remove from current location first
            if (draggedFromInventory)
            {
                // Don't commit placeholder removal yet - we might need the spot for swapping
                inventory.extraGear.Remove(gearCard); // Remove without affecting placeholder
            }
            else
            {
                RemoveFromEquippedSlots();
            }
            
            // Equip the new gear
            EquipGear(dropZone, gearCard);
            
            // Handle the previously equipped gear
            if (previouslyEquipped != null)
            {
                if (draggedFromInventory && inventory.HasActivePlaceholder())
                {
                    // Put the previously equipped gear in the placeholder spot
                    Debug.Log($"Swapping: {previouslyEquipped.gearName} goes to placeholder position");
                    inventory.AddGearAtPlaceholderPosition(previouslyEquipped);
                }
                else
                {
                    // Add to end of inventory
                    inventory.AddToTackleBox(previouslyEquipped);
                }
            }
            else if (draggedFromInventory && inventory.HasActivePlaceholder())
            {
                // No swap happened, just commit the placeholder removal
                inventory.CommitPlaceholderRemoval();
            }
        }
        
        // Return to original parent temporarily for positioning
        transform.SetParent(originalParent, true);
    }
    
    void HandleInvalidDrop()
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        // If we had a placeholder, restore it
        if (draggedFromInventory && inventory.HasActivePlaceholder())
        {
            inventory.RestorePlaceholder();
        }
        
        // Return to original position
        ReturnToOriginalPosition();
    }
    
    bool IsInInventoryGrid()
    {
        // Check if our parent is the RightPanel (inventory grid)
        Transform checkParent = originalParent;
        while (checkParent != null)
        {
            if (checkParent.name == "RightPanel")
                return true;
            checkParent = checkParent.parent;
        }
        return false;
    }
    
    GearCard GetCurrentlyEquippedGear(DropZone dropZone)
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        switch (dropZone.zoneType)
        {
            case DropZone.DropZoneType.EquippedRod: return inventory.equippedRod;
            case DropZone.DropZoneType.EquippedReel: return inventory.equippedReel;
            case DropZone.DropZoneType.EquippedLine: return inventory.equippedLine;
            case DropZone.DropZoneType.EquippedLure: return inventory.equippedLure;
            case DropZone.DropZoneType.EquippedBait: return inventory.equippedBait;
            case DropZone.DropZoneType.EquippedExtra1: return inventory.equippedExtra1;
            case DropZone.DropZoneType.EquippedExtra2: return inventory.equippedExtra2;
            default: return null;
        }
    }
    
    void EquipGear(DropZone dropZone, GearCard gear)
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        switch (dropZone.zoneType)
        {
            case DropZone.DropZoneType.EquippedRod: inventory.equippedRod = gear; break;
            case DropZone.DropZoneType.EquippedReel: inventory.equippedReel = gear; break;
            case DropZone.DropZoneType.EquippedLine: inventory.equippedLine = gear; break;
            case DropZone.DropZoneType.EquippedLure: inventory.equippedLure = gear; break;
            case DropZone.DropZoneType.EquippedBait: inventory.equippedBait = gear; break;
            case DropZone.DropZoneType.EquippedExtra1: inventory.equippedExtra1 = gear; break;
            case DropZone.DropZoneType.EquippedExtra2: inventory.equippedExtra2 = gear; break;
        }
    }
    
    DropZone GetDropZone(PointerEventData eventData)
    {
        // Raycast to find what we're over
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            DropZone dropZone = result.gameObject.GetComponent<DropZone>();
            if (dropZone != null)
            {
                return dropZone;
            }
        }
        
        return null;
    }
    
    void RemoveFromEquippedSlots()
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
    }
    
    void ReturnToOriginalPosition()
    {
        // Return to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }
}