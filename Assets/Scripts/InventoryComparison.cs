using UnityEngine;
using TMPro;

public class InventoryComparison : MonoBehaviour
{
    [Header("Comparison Panels")]
    public Transform equippedStatsPanel;    // Top center panel
    public Transform comparedStatsPanel;    // Bottom center panel
    
    [Header("Player Inventory")]
    public PlayerInventory playerInventory;
    
    [Header("Card Display Prefab")]
    public GameObject cardDisplayPrefab;
    
    private GameObject currentEquippedDisplay;
    private GameObject currentComparedDisplay;
    
    void Start()
    {
        // Clear the panels initially
        ClearComparison();
    }
    
    public void ShowGearComparison(GearCard selectedGear)
    {
        if (selectedGear == null) return;
        
        Debug.Log($"Showing comparison for {selectedGear.gearName} ({selectedGear.gearType})");
        
        // Show the selected gear in the bottom panel
        ShowComparedGear(selectedGear);
        
        // Find and show the equipped gear of the same type in the top panel
        GearCard equippedGear = GetEquippedGearOfType(selectedGear.gearType);
        if (equippedGear != null)
        {
            ShowEquippedGear(equippedGear);
        }
        else
        {
            ClearEquippedDisplay();
            Debug.Log($"No {selectedGear.gearType} is currently equipped");
        }
    }
    
    void ShowEquippedGear(GearCard gearCard)
    {
        // Clear previous display
        ClearEquippedDisplay();
        
        // Create new card display
        if (cardDisplayPrefab != null && equippedStatsPanel != null)
        {
            currentEquippedDisplay = Instantiate(cardDisplayPrefab, equippedStatsPanel);
            
            // Set up the card display
            CardDisplay cardDisplay = currentEquippedDisplay.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.gearCard = gearCard;
                cardDisplay.fishCard = null;
                cardDisplay.actionCard = null;
            }
            
            Debug.Log($"Showing equipped gear: {gearCard.gearName}");
        }
    }
    
    void ShowComparedGear(GearCard gearCard)
    {
        // Clear previous display
        ClearComparedDisplay();
        
        // Create new card display
        if (cardDisplayPrefab != null && comparedStatsPanel != null)
        {
            currentComparedDisplay = Instantiate(cardDisplayPrefab, comparedStatsPanel);
            
            // Set up the card display
            CardDisplay cardDisplay = currentComparedDisplay.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.gearCard = gearCard;
                cardDisplay.fishCard = null;
                cardDisplay.actionCard = null;
            }
            
            Debug.Log($"Showing compared gear: {gearCard.gearName}");
        }
    }
    
    GearCard GetEquippedGearOfType(string gearType)
    {
        if (playerInventory == null) return null;
        
        // Check each equipped slot for matching gear type
        if (playerInventory.equippedRod != null && playerInventory.equippedRod.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedRod;
        
        if (playerInventory.equippedReel != null && playerInventory.equippedReel.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedReel;
        
        if (playerInventory.equippedLine != null && playerInventory.equippedLine.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedLine;
        
        if (playerInventory.equippedLure != null && playerInventory.equippedLure.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedLure;
        
        if (playerInventory.equippedBait != null && playerInventory.equippedBait.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedBait;
        
        if (playerInventory.equippedExtra1 != null && playerInventory.equippedExtra1.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedExtra1;
        
        if (playerInventory.equippedExtra2 != null && playerInventory.equippedExtra2.gearType.Equals(gearType, System.StringComparison.OrdinalIgnoreCase))
            return playerInventory.equippedExtra2;
        
        return null;
    }
    
    void ClearEquippedDisplay()
    {
        if (currentEquippedDisplay != null)
        {
            DestroyImmediate(currentEquippedDisplay);
            currentEquippedDisplay = null;
        }
    }
    
    void ClearComparedDisplay()
    {
        if (currentComparedDisplay != null)
        {
            DestroyImmediate(currentComparedDisplay);
            currentComparedDisplay = null;
        }
    }
    
    public void ClearComparison()
    {
        ClearEquippedDisplay();
        ClearComparedDisplay();
    }
}