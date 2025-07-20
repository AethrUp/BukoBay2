using UnityEngine;
using System.Collections.Generic;

public class EffectCardManager : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;
    public InventoryDisplay inventoryDisplay;
    
    [Header("Gear Selection UI")]
    public GameObject gearSelectionPanel;
    public Transform gearSelectionContainer;
    public GameObject cardDisplayPrefab;  // Use your existing card prefab
    public UnityEngine.UI.Button confirmButton;
    public TMPro.TextMeshProUGUI confirmButtonText;
    
    private EffectCard currentEffectCard;
    private GearCard selectedGear;
    private List<GameObject> gearDisplays = new List<GameObject>();
    private List<GearCard> selectableGear = new List<GearCard>();
    
    void Start()
    {
        // Find components if not assigned
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        if (inventoryDisplay == null)
            inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
        
        // Hide gear selection panel initially
        if (gearSelectionPanel != null)
            gearSelectionPanel.SetActive(false);
        
        // Set up confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmRepair);
            confirmButton.interactable = false; // Disabled until gear is selected
        }
    }
    
    public void UseEffectCard(EffectCard effectCard)
    {
        if (effectCard == null || playerInventory == null)
        {
            // Debug.LogError("Cannot use effect card - missing components!");
            return;
        }
        
        // Debug.Log($"Using effect card: {effectCard.effectName}");
        
        switch (effectCard.effectType)
        {
            case EffectType.Repair:
                HandleRepairEffect(effectCard);
                break;
            case EffectType.Protection:
                HandleProtectionEffect(effectCard);
                break;
            case EffectType.Utility:
                HandleUtilityEffect(effectCard);
                break;
            case EffectType.Persistent:
                HandlePersistentEffect(effectCard);
                break;
        }
    }
    
    void HandleRepairEffect(EffectCard effectCard)
    {
        // Store the current effect card for use in gear selection
        currentEffectCard = effectCard;
        
        // Get all damaged gear
        List<GearCard> damagedGear = GetDamagedGear();
        
        if (damagedGear.Count == 0)
        {
            // Debug.Log("No damaged gear to repair!");
            return;
        }
        
        // Show gear selection UI
        ShowGearSelection(damagedGear);
    }
    
    List<GearCard> GetDamagedGear()
    {
        List<GearCard> damagedGear = new List<GearCard>();
        
        // Check all equipped gear for damage
        if (playerInventory.equippedRod != null && playerInventory.equippedRod.durability < GetMaxDurability(playerInventory.equippedRod))
            damagedGear.Add(playerInventory.equippedRod);
        
        if (playerInventory.equippedReel != null && playerInventory.equippedReel.durability < GetMaxDurability(playerInventory.equippedReel))
            damagedGear.Add(playerInventory.equippedReel);
        
        if (playerInventory.equippedLine != null && playerInventory.equippedLine.durability < GetMaxDurability(playerInventory.equippedLine))
            damagedGear.Add(playerInventory.equippedLine);
        
        if (playerInventory.equippedLure != null && playerInventory.equippedLure.durability < GetMaxDurability(playerInventory.equippedLure))
            damagedGear.Add(playerInventory.equippedLure);
        
        if (playerInventory.equippedBait != null && playerInventory.equippedBait.durability < GetMaxDurability(playerInventory.equippedBait))
            damagedGear.Add(playerInventory.equippedBait);
        
        if (playerInventory.equippedExtra1 != null && playerInventory.equippedExtra1.durability < GetMaxDurability(playerInventory.equippedExtra1))
            damagedGear.Add(playerInventory.equippedExtra1);
        
        if (playerInventory.equippedExtra2 != null && playerInventory.equippedExtra2.durability < GetMaxDurability(playerInventory.equippedExtra2))
            damagedGear.Add(playerInventory.equippedExtra2);
        
        // Also check tackle box gear for damage
        foreach (GearCard gear in playerInventory.extraGear)
        {
            if (gear != null && gear.durability < GetMaxDurability(gear))
                damagedGear.Add(gear);
        }
        
        return damagedGear;
    }
    
    int GetMaxDurability(GearCard gear)
    {
        // For now, we'll assume max durability is based on the gear's current durability + any missing
        // You might want to store original max durability in the GearCard later
        // For testing, let's assume max durability is 100 or current durability if higher
        return Mathf.Max(gear.durability, 100);
    }
    
    void ShowGearSelection(List<GearCard> damagedGear)
    {
        if (gearSelectionPanel == null || gearSelectionContainer == null)
        {
            // Debug.LogError("Gear selection UI not set up!");
            return;
        }
        
        // Store selectable gear and clear previous displays
        selectableGear = damagedGear;
        ClearGearDisplays();
        
        // Reset selection state
        selectedGear = null;
        UpdateConfirmButton();
        
        // Create display for each damaged gear
        foreach (GearCard gear in damagedGear)
        {
            CreateGearDisplay(gear);
        }
        
        // Show the panel
        gearSelectionPanel.SetActive(true);
        
        // Debug.Log($"Showing {damagedGear.Count} damaged gear options for repair");
    }
    
    void CreateGearDisplay(GearCard gear)
    {
        if (cardDisplayPrefab == null) return;
        
        GameObject displayObj = Instantiate(cardDisplayPrefab, gearSelectionContainer);
        
        // Set up the card display
        CardDisplay cardDisplay = displayObj.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardDisplay.gearCard = gear;
            cardDisplay.fishCard = null;
            cardDisplay.actionCard = null;
            
            // Force update the display
            cardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
        }
        
        // Add click detection
        GearSelectionHandler selectionHandler = displayObj.GetComponent<GearSelectionHandler>();
        if (selectionHandler == null)
        {
            selectionHandler = displayObj.AddComponent<GearSelectionHandler>();
        }
        selectionHandler.Initialize(gear, this);
        
        // Visual feedback component
        UnityEngine.UI.Image background = displayObj.GetComponent<UnityEngine.UI.Image>();
        if (background != null)
        {
            selectionHandler.backgroundImage = background;
        }
        
        gearDisplays.Add(displayObj);
    }
    
    public void SelectGear(GearCard gear)
    {
        // Update selection
        selectedGear = gear;
        
        // Update visual feedback on all gear displays
        foreach (GameObject display in gearDisplays)
        {
            GearSelectionHandler handler = display.GetComponent<GearSelectionHandler>();
            if (handler != null)
            {
                handler.UpdateSelection(gear == handler.gearCard);
            }
        }
        
        // Update confirm button
        UpdateConfirmButton();
        
        // Debug.Log($"Selected {gear.gearName} for repair");
    }
    
    void UpdateConfirmButton()
    {
        if (confirmButton == null) return;
        
        bool hasSelection = selectedGear != null;
        confirmButton.interactable = hasSelection;
        
        if (confirmButtonText != null)
        {
            if (hasSelection)
            {
                int repairAmount = CalculateRepairAmount(currentEffectCard, selectedGear);
                confirmButtonText.text = $"Repair {selectedGear.gearName} (+{repairAmount})";
            }
            else
            {
                confirmButtonText.text = "Select gear to repair";
            }
        }
    }
    
    public void ConfirmRepair()
    {
        if (selectedGear == null || currentEffectCard == null)
        {
            // Debug.LogError("Cannot confirm repair - no gear selected!");
            return;
        }
        
        // Debug.Log($"Confirming repair of {selectedGear.gearName} with {currentEffectCard.effectName}");
        
        // Apply the repair effect
        int repairAmount = CalculateRepairAmount(currentEffectCard, selectedGear);
        int maxDurability = GetMaxDurability(selectedGear);
        
        int oldDurability = selectedGear.durability;
        selectedGear.durability = Mathf.Min(maxDurability, selectedGear.durability + repairAmount);
        int actualRepair = selectedGear.durability - oldDurability;
        
        // Debug.Log($"Repaired {selectedGear.gearName}: {oldDurability} → {selectedGear.durability} (+{actualRepair})");
        
        // Remove the effect card from inventory (most are single use)
        if (currentEffectCard.singleUse && playerInventory.effectCards.Contains(currentEffectCard))
        {
            playerInventory.effectCards.Remove(currentEffectCard);
            // Debug.Log($"Consumed {currentEffectCard.effectName}");
        }
        
        // Hide the gear selection panel
        HideGearSelection();
        
        // Refresh inventory display
        if (inventoryDisplay != null)
        {
            inventoryDisplay.RefreshDisplay();
        }
        
        // Clear current effect card and selection
        currentEffectCard = null;
        selectedGear = null;
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
    
    void ClearGearDisplays()
    {
        foreach (GameObject display in gearDisplays)
        {
            if (display != null)
                Destroy(display);
        }
        gearDisplays.Clear();
    }
    
    void HideGearSelection()
    {
        if (gearSelectionPanel != null)
            gearSelectionPanel.SetActive(false);
        
        ClearGearDisplays();
        selectedGear = null;
        selectableGear.Clear();
    }
    
    public void CancelGearSelection()
    {
        // Debug.Log("Gear selection cancelled");
        HideGearSelection();
        currentEffectCard = null;
    }
    
    // Placeholder methods for other effect types
    void HandleProtectionEffect(EffectCard effectCard)
    {
        // Debug.Log($"Protection effect not implemented yet: {effectCard.effectName}");
    }
    
    void HandleUtilityEffect(EffectCard effectCard)
    {
        // Debug.Log($"Utility effect not implemented yet: {effectCard.effectName}");
    }
    
    void HandlePersistentEffect(EffectCard effectCard)
    {
        // Debug.Log($"Persistent effect not implemented yet: {effectCard.effectName}");
    }
}