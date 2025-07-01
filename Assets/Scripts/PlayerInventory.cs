using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Equipped Gear")]
    public GearCard equippedRod;
    public GearCard equippedReel;
    public GearCard equippedLine;
    public GearCard equippedLure;
    public GearCard equippedBait;
    public GearCard equippedExtra1;
    public GearCard equippedExtra2;
    
    [Header("Tackle Box")]
    public List<GearCard> extraGear = new List<GearCard>();
    public List<ActionCard> actionCards = new List<ActionCard>();
    
    // Placeholder system for drag and drop
    private GearCard dragPlaceholder = null;
    private int placeholderIndex = -1;
    
    // Function to check if a specific gear type is equipped
    public bool HasGearType(string gearType)
    {
        if (equippedRod != null && equippedRod.gearType == gearType) return true;
        if (equippedReel != null && equippedReel.gearType == gearType) return true;
        if (equippedLine != null && equippedLine.gearType == gearType) return true;
        if (equippedLure != null && equippedLure.gearType == gearType) return true;
        if (equippedBait != null && equippedBait.gearType == gearType) return true;
        if (equippedExtra1 != null && equippedExtra1.gearType == gearType) return true;
        if (equippedExtra2 != null && equippedExtra2.gearType == gearType) return true;
        
        return false;
    }
    
    // Function to get total power from all equipped gear
    public int GetTotalPower()
    {
        int totalPower = 0;
        
        if (equippedRod != null) totalPower += equippedRod.power;
        if (equippedReel != null) totalPower += equippedReel.power;
        if (equippedLine != null) totalPower += equippedLine.power;
        if (equippedLure != null) totalPower += equippedLure.power;
        if (equippedBait != null) totalPower += equippedBait.power;
        if (equippedExtra1 != null) totalPower += equippedExtra1.power;
        if (equippedExtra2 != null) totalPower += equippedExtra2.power;
        
        return totalPower;
    }
    
    // Placeholder system methods
    public void CreatePlaceholder(GearCard gearCard)
    {
        int index = extraGear.IndexOf(gearCard);
        if (index >= 0)
        {
            dragPlaceholder = gearCard;
            placeholderIndex = index;
            extraGear[index] = null; // Replace with null placeholder
            Debug.Log($"Created placeholder for {gearCard.gearName} at index {index}");
        }
    }
    
    // Clean up any null entries in the extra gear list
    public void CleanupNullEntries()
    {
        for (int i = extraGear.Count - 1; i >= 0; i--)
        {
            if (extraGear[i] == null)
            {
                extraGear.RemoveAt(i);
                Debug.Log($"Removed null entry at index {i}");
            }
        }
    }
    
    public void RestorePlaceholder()
    {
        if (dragPlaceholder != null && placeholderIndex >= 0)
        {
            extraGear[placeholderIndex] = dragPlaceholder;
            Debug.Log($"Restored {dragPlaceholder.gearName} to index {placeholderIndex}");
            ClearPlaceholder();
        }
    }
    
    public void CommitPlaceholderRemoval()
    {
        if (dragPlaceholder != null && placeholderIndex >= 0)
        {
            extraGear.RemoveAt(placeholderIndex);
            Debug.Log($"Committed removal of {dragPlaceholder.gearName} from index {placeholderIndex}");
            ClearPlaceholder();
        }
    }
    
    public void AddGearAtPlaceholderPosition(GearCard gearCard)
    {
        if (placeholderIndex >= 0 && placeholderIndex < extraGear.Count)
        {
            extraGear[placeholderIndex] = gearCard;
            Debug.Log($"Added {gearCard.gearName} at placeholder position {placeholderIndex}");
            ClearPlaceholder();
        }
        else
        {
            // No placeholder, add to end
            extraGear.Add(gearCard);
            Debug.Log($"Added {gearCard.gearName} to end of inventory");
        }
    }
    
    void ClearPlaceholder()
    {
        dragPlaceholder = null;
        placeholderIndex = -1;
    }
    
    public bool HasActivePlaceholder()
    {
        return dragPlaceholder != null;
    }
    
    // Helper method to add gear to tackle box (maintains existing functionality)
    public void AddToTackleBox(GearCard gearCard)
    {
        if (!extraGear.Contains(gearCard))
        {
            extraGear.Add(gearCard);
        }
    }
    
    // Helper method to remove gear from tackle box
    public void RemoveFromTackleBox(GearCard gearCard)
    {
        extraGear.Remove(gearCard);
    }
}