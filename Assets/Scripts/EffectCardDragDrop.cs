using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EffectCardDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Effect Card Reference")]
    public EffectCard effectCard;

    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private PlayerInventory playerInventory;
    private InventoryDisplay inventoryDisplay;

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

        if (raycaster == null && canvas != null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }

        // Find required components
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();

        if (playerInventory == null)
        {
            Debug.LogError("Could not find PlayerInventory in scene!");
        }
        else
        {
            Debug.Log("Found PlayerInventory successfully");
        }
        
        if (inventoryDisplay == null)
        {
            Debug.LogError("Could not find InventoryDisplay in scene!");
        }
        else
        {
            Debug.Log("Found InventoryDisplay successfully");
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (effectCard == null) 
        {
            Debug.LogError("No effect card assigned to drag component!");
            return;
        }
        
        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to top of hierarchy so it renders on top
        transform.SetParent(canvas.transform, true);
        
        Debug.Log($"Started dragging effect card: {effectCard.effectName}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (effectCard == null) return;
        
        // Move the card with the mouse/touch
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (effectCard == null) return;
        
        Debug.Log($"Ended dragging effect card: {effectCard.effectName}");
        
        // Restore appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check what we dropped on
        GearCard targetGear = GetTargetGear(eventData);
        
        if (targetGear != null && CanUseEffectOnGear(effectCard, targetGear))
        {
            // Valid drop - use the effect
            UseEffectOnGear(effectCard, targetGear);
        }
        else
        {
            // Invalid drop - return to original position
            ReturnToOriginalPosition();
            if (targetGear != null)
            {
                Debug.Log($"Cannot use {effectCard.effectName} on {targetGear.gearName}");
            }
            else
            {
                Debug.Log($"Invalid drop target for {effectCard.effectName}");
            }
        }
    }
    
    GearCard GetTargetGear(PointerEventData eventData)
    {
        // Raycast to find what we're over
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);
        
        Debug.Log($"Effect card raycast found {raycastResults.Count} results");
        
        foreach (var raycastResult in raycastResults)
        {
            Debug.Log($"Effect raycast hit: {raycastResult.gameObject.name}");
            
            // Check if we hit a gear card display
            CardDisplay cardDisplay = raycastResult.gameObject.GetComponent<CardDisplay>();
            if (cardDisplay != null && cardDisplay.gearCard != null)
            {
                Debug.Log($"Found gear card: {cardDisplay.gearCard.gearName}");
                return cardDisplay.gearCard;
            }
            
            // Also check parent objects for CardDisplay
            Transform current = raycastResult.gameObject.transform;
            while (current != null)
            {
                CardDisplay parentCardDisplay = current.GetComponent<CardDisplay>();
                if (parentCardDisplay != null && parentCardDisplay.gearCard != null)
                {
                    Debug.Log($"Found gear card on parent: {parentCardDisplay.gearCard.gearName}");
                    return parentCardDisplay.gearCard;
                }
                current = current.parent;
            }
        }
        
        Debug.Log("No gear card found in raycast results");
        return null;
    }
    
    bool CanUseEffectOnGear(EffectCard effect, GearCard gear)
    {
        switch (effect.effectType)
        {
            case EffectType.Repair:
                return CanRepairGear(effect, gear);
            case EffectType.Protection:
                return CanProtectGear(effect, gear);
            case EffectType.Utility:
                return false;
            case EffectType.Persistent:
                return false;
            default:
                return false;
        }
    }
    
    bool CanRepairGear(EffectCard repairCard, GearCard gear)
    {
        // Check if gear is damaged (assuming max durability is higher than current)
        int maxDurability = GetMaxDurability(gear);
        bool isDamaged = gear.durability < maxDurability;
        
        if (!isDamaged)
        {
            Debug.Log($"{gear.gearName} is not damaged (durability: {gear.durability}/{maxDurability})");
            return false;
        }
        
        // Check if gear belongs to the player
        bool isPlayerGear = IsPlayerGear(gear);
        
        if (!isPlayerGear)
        {
            Debug.Log($"{gear.gearName} does not belong to the player");
            return false;
        }
        
        Debug.Log($"{gear.gearName} can be repaired (durability: {gear.durability}/{maxDurability})");
        return true;
    }
    
    bool IsPlayerGear(GearCard gear)
    {
        if (playerInventory == null) return false;
        
        // Check equipped gear
        if (playerInventory.equippedRod == gear) return true;
        if (playerInventory.equippedReel == gear) return true;
        if (playerInventory.equippedLine == gear) return true;
        if (playerInventory.equippedLure == gear) return true;
        if (playerInventory.equippedBait == gear) return true;
        if (playerInventory.equippedExtra1 == gear) return true;
        if (playerInventory.equippedExtra2 == gear) return true;
        
        // Check tackle box
        if (playerInventory.extraGear.Contains(gear)) return true;
        
        return false;
    }
    
    int GetMaxDurability(GearCard gear)
    {
        // Use the stored maxDurability field
        return gear.maxDurability;
    }
    
    void UseEffectOnGear(EffectCard effect, GearCard targetGear)
    {
        Debug.Log($"Using {effect.effectName} on {targetGear.gearName}");
        
        switch (effect.effectType)
        {
            case EffectType.Repair:
                ApplyRepairEffect(effect, targetGear);
                break;
            case EffectType.Protection:
                ApplyProtectionEffect(effect, targetGear);
                break;
            // Add other effect types later
        }
        
        // Remove effect card from inventory if single use
        if (effect.singleUse && playerInventory.effectCards.Contains(effect))
        {
            playerInventory.effectCards.Remove(effect);
            Debug.Log($"Consumed {effect.effectName}");
            
            // Destroy this card display
            Destroy(gameObject);
        }
        
        // Note: We're now updating the specific card display directly instead of refreshing everything
    }
    
    void ApplyRepairEffect(EffectCard repairCard, GearCard gear)
    {
        int repairAmount = CalculateRepairAmount(repairCard, gear);
        int maxDurability = GetMaxDurability(gear);
        
        int oldDurability = gear.durability;
        gear.durability = Mathf.Min(maxDurability, gear.durability + repairAmount);
        int actualRepair = gear.durability - oldDurability;
        
        Debug.Log($"Repaired {gear.gearName}: {oldDurability} → {gear.durability} (+{actualRepair})");
        
        // Show repair feedback and update the specific card display
        ShowRepairFeedback(gear, actualRepair);
        UpdateSpecificGearDisplay(gear);
    }
    
    int CalculateRepairAmount(EffectCard effectCard, GearCard gear)
    {
        if (effectCard.repairHalfDamage)
        {
            // For cards like Sumūzu - repair half of missing durability
            int maxDurability = GetMaxDurability(gear);
            int missingDurability = maxDurability - gear.durability;
            return Mathf.CeilToInt(missingDurability / 2f);
        }
        else
        {
            // For cards like protivoyadiye - repair fixed amount
            return effectCard.repairAmount;
        }
    }
    
    void ApplyProtectionEffect(EffectCard protectionCard, GearCard gear)
    {
        // Apply protection to the gear
        gear.hasProtection = true;
        gear.protectionType = protectionCard.effectName;
        
        Debug.Log($"Applied {protectionCard.effectName} protection to {gear.gearName}");
        
        // For HandsOff, we need to protect ALL equipped gear
        if (protectionCard.effectName.Contains("HandsOff"))
        {
            ApplyProtectionToAllGear(protectionCard);
        }
        
        // Show protection feedback and update the display
        ShowProtectionFeedback(gear, protectionCard.effectName);
        UpdateSpecificGearDisplay(gear);
    }
    
    void ApplyProtectionToAllGear(EffectCard protectionCard)
    {
        if (playerInventory == null) return;
        
        Debug.Log("Applying HandsOff protection to all equipped gear");
        
        // Apply protection to all equipped gear
        if (playerInventory.equippedRod != null && !playerInventory.equippedRod.hasProtection)
        {
            playerInventory.equippedRod.hasProtection = true;
            playerInventory.equippedRod.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedRod);
        }
        
        if (playerInventory.equippedReel != null && !playerInventory.equippedReel.hasProtection)
        {
            playerInventory.equippedReel.hasProtection = true;
            playerInventory.equippedReel.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedReel);
        }
        
        if (playerInventory.equippedLine != null && !playerInventory.equippedLine.hasProtection)
        {
            playerInventory.equippedLine.hasProtection = true;
            playerInventory.equippedLine.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedLine);
        }
        
        if (playerInventory.equippedLure != null && !playerInventory.equippedLure.hasProtection)
        {
            playerInventory.equippedLure.hasProtection = true;
            playerInventory.equippedLure.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedLure);
        }
        
        if (playerInventory.equippedBait != null && !playerInventory.equippedBait.hasProtection)
        {
            playerInventory.equippedBait.hasProtection = true;
            playerInventory.equippedBait.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedBait);
        }
        
        if (playerInventory.equippedExtra1 != null && !playerInventory.equippedExtra1.hasProtection)
        {
            playerInventory.equippedExtra1.hasProtection = true;
            playerInventory.equippedExtra1.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedExtra1);
        }
        
        if (playerInventory.equippedExtra2 != null && !playerInventory.equippedExtra2.hasProtection)
        {
            playerInventory.equippedExtra2.hasProtection = true;
            playerInventory.equippedExtra2.protectionType = protectionCard.effectName;
            UpdateSpecificGearDisplay(playerInventory.equippedExtra2);
        }
        
        Debug.Log("HandsOff protection applied to all equipped gear");
    }
    
    void ShowProtectionFeedback(GearCard gear, string protectionType)
    {
        Debug.Log($"🛡️ {gear.gearName} protected with {protectionType}!");
    }
    
    void ShowRepairFeedback(GearCard gear, int repairAmount)
    {
        // Simple console feedback for now
        // You could add floating text or other visual effects later
        Debug.Log($"✓ {gear.gearName} repaired for +{repairAmount} durability!");
    }
    
    void UpdateSpecificGearDisplay(GearCard updatedGear)
    {
        // Find all CardDisplay components in the scene
        CardDisplay[] allCardDisplays = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);
        
        Debug.Log($"Found {allCardDisplays.Length} CardDisplay components to check");
        
        foreach (CardDisplay cardDisplay in allCardDisplays)
        {
            // Check if this card display is showing the updated gear
            if (cardDisplay.gearCard == updatedGear)
            {
                Debug.Log($"Found CardDisplay showing {updatedGear.gearName}, forcing update");
                
                // Force the card display to update by calling DisplayCard
                cardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
                
                Debug.Log($"Updated CardDisplay for {updatedGear.gearName}");
            }
        }
    }
    
    bool CanProtectGear(EffectCard protectionCard, GearCard gear)
    {
        // Check if gear already has protection
        if (gear.hasProtection)
        {
            Debug.Log($"{gear.gearName} already has protection ({gear.protectionType})");
            return false;
        }
        
        // Check if gear belongs to the player
        bool isPlayerGear = IsPlayerGear(gear);
        
        if (!isPlayerGear)
        {
            Debug.Log($"{gear.gearName} does not belong to the player");
            return false;
        }
        
        Debug.Log($"{gear.gearName} can be protected with {protectionCard.effectName}");
        return true;
    }
    
    void ReturnToOriginalPosition()
    {
        // Return to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }
}