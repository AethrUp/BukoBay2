using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearComparisonDisplay : MonoBehaviour
{
    [Header("Equipped Gear UI (Top Panel)")]
    public TextMeshProUGUI equippedGearName;
    public TextMeshProUGUI equippedGearType;
    public TextMeshProUGUI equippedPower;
    public TextMeshProUGUI equippedDurability;
    public Image equippedGearImage;
    public DepthEffectsDisplay equippedDepthDisplay;
    
    [Header("Compared Gear UI (Bottom Panel)")]
    public TextMeshProUGUI comparedGearName;
    public TextMeshProUGUI comparedGearType;
    public TextMeshProUGUI comparedPower;
    public TextMeshProUGUI comparedDurability;
    public Image comparedGearImage;
    public DepthEffectsDisplay comparedDepthDisplay;
    
    [Header("Total Equipped Gear UI")]
    public TextMeshProUGUI totalEquippedPower;
    public DepthEffectsDisplay totalDepthDisplay;
    
    [Header("Player Inventory")]
    public PlayerInventory playerInventory;
    
    void Start()
    {
        // Find the persistent PlayerInventory if not already assigned
        if (playerInventory == null && PlayerInventory.Instance != null)
        {
            playerInventory = PlayerInventory.Instance;
            Debug.Log("GearComparisonDisplay: Found persistent PlayerInventory");
        }
        
        // Clear both panels initially
        ClearEquippedDisplay();
        ClearComparedDisplay();
    }
    
    public void ShowGearComparison(GearCard clickedGear)
    {
        if (clickedGear == null) return;
        
        Debug.Log($"GearComparisonDisplay: Showing comparison for {clickedGear.gearName}");
        
        // Check if the clicked gear is currently equipped
        bool clickedGearIsEquipped = IsGearEquipped(clickedGear);
        GearCard equippedGear = GetEquippedGearOfType(clickedGear.gearType);
        
        if (clickedGearIsEquipped)
        {
            // If clicked gear is equipped, show it in the top panel only
            Debug.Log($"GearComparisonDisplay: Selected gear is equipped. Clearing compared panel.");
            ClearComparedDisplay(); // Clear first
            DisplayEquippedGear(clickedGear);
            Debug.Log($"GearComparisonDisplay: Showing equipped gear: {clickedGear.gearName}");
        }
        else
        {
            // If clicked gear is not equipped, show it in the bottom panel
            DisplayComparedGear(clickedGear);
            
            // Show the currently equipped gear of the same type in the top panel (if different)
            if (equippedGear != null && equippedGear != clickedGear)
            {
                DisplayEquippedGear(equippedGear);
                Debug.Log($"GearComparisonDisplay: Comparing {clickedGear.gearName} with equipped {equippedGear.gearName}");
            }
            else
            {
                ClearEquippedDisplay();
                Debug.Log($"GearComparisonDisplay: No {clickedGear.gearType} is currently equipped");
            }
        }
    }
    
    bool IsGearEquipped(GearCard gearCard)
    {
        if (playerInventory == null || gearCard == null) return false;
        
        Debug.Log($"GearComparisonDisplay: Checking if {gearCard.gearName} is equipped");
        
        // Check if this specific gear instance is equipped
        bool isEquipped = (playerInventory.equippedRod == gearCard ||
                          playerInventory.equippedReel == gearCard ||
                          playerInventory.equippedLine == gearCard ||
                          playerInventory.equippedLure == gearCard ||
                          playerInventory.equippedBait == gearCard ||
                          playerInventory.equippedExtra1 == gearCard ||
                          playerInventory.equippedExtra2 == gearCard);
        
        Debug.Log($"GearComparisonDisplay: {gearCard.gearName} is equipped: {isEquipped}");
        return isEquipped;
    }
    
    void DisplayEquippedGear(GearCard gear)
    {
        if (equippedGearName != null) equippedGearName.text = gear.gearName;
        if (equippedGearType != null) equippedGearType.text = gear.gearType;
        if (equippedPower != null) equippedPower.text = "Power: " + gear.power;
        if (equippedDurability != null) equippedDurability.text = "Durability: " + gear.durability;
        if (equippedGearImage != null) equippedGearImage.sprite = gear.gearImage;
        if (equippedDepthDisplay != null) equippedDepthDisplay.DisplayGearDepthEffects(gear);
    }
    
    void DisplayComparedGear(GearCard gear)
    {
        if (comparedGearName != null) comparedGearName.text = gear.gearName;
        if (comparedGearType != null) comparedGearType.text = gear.gearType;
        if (comparedPower != null) comparedPower.text = "Power: " + gear.power;
        if (comparedDurability != null) comparedDurability.text = "Durability: " + gear.durability;
        if (comparedGearImage != null) comparedGearImage.sprite = gear.gearImage;
        if (comparedDepthDisplay != null) comparedDepthDisplay.DisplayGearDepthEffects(gear);
    }
    
    void ClearEquippedDisplay()
    {
        if (equippedGearName != null) equippedGearName.text = "No Gear Equipped";
        if (equippedGearType != null) equippedGearType.text = "";
        if (equippedPower != null) equippedPower.text = "";
        if (equippedDurability != null) equippedDurability.text = "";
        if (equippedGearImage != null) equippedGearImage.sprite = null;
        if (equippedDepthDisplay != null) equippedDepthDisplay.ClearDisplay();
    }
    
    void ClearComparedDisplay()
    {
        Debug.Log("GearComparisonDisplay: ClearComparedDisplay called");
        if (comparedGearName != null) comparedGearName.text = "Click gear to compare";
        if (comparedGearType != null) comparedGearType.text = "";
        if (comparedPower != null) comparedPower.text = "";
        if (comparedDurability != null) comparedDurability.text = "";
        if (comparedGearImage != null) comparedGearImage.sprite = null;
        if (comparedDepthDisplay != null) comparedDepthDisplay.ClearDisplay();
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
    
    public void UpdateTotalEquippedDisplay()
    {
        if (playerInventory == null) return;
        
        // Calculate total power from all equipped gear
        int totalPower = playerInventory.GetTotalPower();
        
        // Display total power
        if (totalEquippedPower != null)
        {
            totalEquippedPower.text = "Total Power: " + totalPower;
        }
        
        // Calculate total depth effects
        if (totalDepthDisplay != null)
        {
            int[] totalDepthEffects = CalculateTotalDepthEffects();
            DisplayTotalDepthEffects(totalDepthEffects);
        }
    }
    
    int[] CalculateTotalDepthEffects()
    {
        int[] totals = new int[9]; // Initialize to 0
        
        // Add effects from each equipped gear piece
        AddGearDepthEffects(totals, playerInventory.equippedRod);
        AddGearDepthEffects(totals, playerInventory.equippedReel);
        AddGearDepthEffects(totals, playerInventory.equippedLine);
        AddGearDepthEffects(totals, playerInventory.equippedLure);
        AddGearDepthEffects(totals, playerInventory.equippedBait);
        AddGearDepthEffects(totals, playerInventory.equippedExtra1);
        AddGearDepthEffects(totals, playerInventory.equippedExtra2);
        
        return totals;
    }
    
    void AddGearDepthEffects(int[] totals, GearCard gear)
    {
        if (gear == null) return;
        
        for (int depth = 1; depth <= 9; depth++)
        {
            totals[depth - 1] += gear.GetDepthEffect(depth);
        }
    }
    
    void DisplayTotalDepthEffects(int[] totalEffects)
    {
        // Create a temporary gear card to hold the totals for display
        GearCard tempCard = ScriptableObject.CreateInstance<GearCard>();
        
        // Set the total effects
        for (int depth = 1; depth <= 9; depth++)
        {
            tempCard.SetDepthEffect(depth, totalEffects[depth - 1]);
        }
        
        // Display using the existing depth display component
        totalDepthDisplay.DisplayGearDepthEffects(tempCard);
        
        // Clean up the temporary object
        DestroyImmediate(tempCard);
    }
}