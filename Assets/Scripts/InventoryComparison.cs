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
        // Find the persistent PlayerInventory if not already assigned
        if (playerInventory == null && PlayerInventory.Instance != null)
        {
            playerInventory = PlayerInventory.Instance;
            Debug.Log("InventoryComparison: Found persistent PlayerInventory");
        }
        
        // Clear the panels initially
        ClearComparison();
    }
    
    public void ShowGearComparison(GearCard selectedGear)
    {
        if (selectedGear == null) return;
        
        Debug.Log($"Showing comparison for {selectedGear.gearName} ({selectedGear.gearType})");
        
        // Check if the selected gear is currently equipped
        GearCard equippedGear = GetEquippedGearOfType(selectedGear.gearType);
        bool selectedGearIsEquipped = IsGearEquipped(selectedGear);
        
        if (selectedGearIsEquipped)
        {
            // If clicked gear is equipped, show it in the top panel only
            Debug.Log($"Selected gear is equipped. Clearing compared panel.");
            ClearComparedDisplay(); // Clear first
            ShowEquippedGear(selectedGear);
            Debug.Log($"Showing equipped gear: {selectedGear.gearName}");
        }
        else
        {
            // If clicked gear is not equipped, show it in the bottom panel
            ShowComparedGear(selectedGear);
            
            // Show the currently equipped gear of the same type in the top panel (if different)
            if (equippedGear != null && equippedGear != selectedGear)
            {
                ShowEquippedGear(equippedGear);
                Debug.Log($"Comparing {selectedGear.gearName} with equipped {equippedGear.gearName}");
            }
            else
            {
                ClearEquippedDisplay();
                Debug.Log($"No {selectedGear.gearType} is currently equipped");
            }
        }
    }
    
    bool IsGearEquipped(GearCard gearCard)
    {
        if (playerInventory == null || gearCard == null) return false;
        
        // Check if this specific gear instance is equipped
        return (playerInventory.equippedRod == gearCard ||
                playerInventory.equippedReel == gearCard ||
                playerInventory.equippedLine == gearCard ||
                playerInventory.equippedLure == gearCard ||
                playerInventory.equippedBait == gearCard ||
                playerInventory.equippedExtra1 == gearCard ||
                playerInventory.equippedExtra2 == gearCard);
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
        Debug.Log("ClearComparedDisplay called");
        if (currentComparedDisplay != null)
        {
            Debug.Log("Destroying compared display object");
            DestroyImmediate(currentComparedDisplay);
            currentComparedDisplay = null;
        }
        else
        {
            Debug.Log("No compared display object to clear");
        }
    }
    
    public void ClearComparison()
    {
        ClearEquippedDisplay();
        ClearComparedDisplay();
    }
}