using UnityEngine;
using UnityEngine.UI;

public class DropZone : MonoBehaviour
{
    [Header("Drop Zone Settings")]
    public string zoneName;
    public DropZoneType zoneType;
    public string allowedGearType = ""; // For specific gear slots like "Rod", "Reel", etc.
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.green;
    public Color invalidColor = Color.red;
    
    private Image backgroundImage;
    private PlayerInventory playerInventory;
    
    public enum DropZoneType
    {
        EquippedRod,
        EquippedReel,
        EquippedLine,
        EquippedLure,
        EquippedBait,
        EquippedExtra1,
        EquippedExtra2,
        TackleBox
    }
    
    void Awake()
{
    backgroundImage = GetComponent<Image>();
    if (backgroundImage == null)
    {
        backgroundImage = gameObject.AddComponent<Image>();
    }
    
    // Find PlayerInventory the same way other scripts do
    playerInventory = FindFirstObjectByType<PlayerInventory>();
    if (playerInventory == null)
    {
        // Debug.LogError("DropZone: Could not find PlayerInventory!");
    }
    else
    {
        // Debug.Log("DropZone: Found PlayerInventory successfully!");
    }
}
    
    void Start()
    {
        // Set initial color
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
    
    public bool CanAcceptCard(GearCard gearCard)
    {
        if (gearCard == null) return false;
        
        // Tackle box accepts any gear
        if (zoneType == DropZoneType.TackleBox)
        {
            return true;
        }
        
        // Equipped slots check for matching gear type
        if (!string.IsNullOrEmpty(allowedGearType))
        {
            return gearCard.gearType.Equals(allowedGearType, System.StringComparison.OrdinalIgnoreCase);
        }
        
        // Extra slots accept any gear
        if (zoneType == DropZoneType.EquippedExtra1 || zoneType == DropZoneType.EquippedExtra2)
        {
            return true;
        }
        
        return false;
    }
    
    public void AcceptCard(GearCard gearCard)
{
    // Make sure we can find PlayerInventory
    if (playerInventory == null) 
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory == null)
        {
            // Debug.LogError("DropZone: Still cannot find PlayerInventory!");
            return;
        }
    }
    
    // Debug.Log($"DropZone: Accepting {gearCard.gearName} in {zoneName}");
    
    // Store the currently equipped gear (if any) before replacing it
    GearCard previousGear = null;
    
    switch (zoneType)
    {
        case DropZoneType.EquippedRod:
            previousGear = playerInventory.equippedRod;
            playerInventory.equippedRod = gearCard;
            break;
        case DropZoneType.EquippedReel:
            previousGear = playerInventory.equippedReel;
            playerInventory.equippedReel = gearCard;
            break;
        case DropZoneType.EquippedLine:
            previousGear = playerInventory.equippedLine;
            playerInventory.equippedLine = gearCard;
            break;
        case DropZoneType.EquippedLure:
            previousGear = playerInventory.equippedLure;
            playerInventory.equippedLure = gearCard;
            break;
        case DropZoneType.EquippedBait:
            previousGear = playerInventory.equippedBait;
            playerInventory.equippedBait = gearCard;
            break;
        case DropZoneType.EquippedExtra1:
            previousGear = playerInventory.equippedExtra1;
            playerInventory.equippedExtra1 = gearCard;
            break;
        case DropZoneType.EquippedExtra2:
            previousGear = playerInventory.equippedExtra2;
            playerInventory.equippedExtra2 = gearCard;
            break;
        case DropZoneType.TackleBox:
            if (!playerInventory.extraGear.Contains(gearCard))
            {
                playerInventory.extraGear.Add(gearCard);
            }
            break;
    }
    
    // If we replaced an equipped item, move the old one to tackle box
    if (previousGear != null && zoneType != DropZoneType.TackleBox)
    {
        if (!playerInventory.extraGear.Contains(previousGear))
        {
            playerInventory.extraGear.Add(previousGear);
            // Debug.Log($"Moved {previousGear.gearName} to tackle box");
        }
    }
    
    // Update the gear comparison display if it exists
    GearComparisonDisplay comparisonDisplay = FindFirstObjectByType<GearComparisonDisplay>();
    if (comparisonDisplay != null)
    {
        // Debug.Log("Found GearComparisonDisplay, updating totals...");
        comparisonDisplay.UpdateTotalEquippedDisplay();
    }
    else
    {
        // Debug.LogWarning("Could not find GearComparisonDisplay component!");
    }
}
    
    // Visual feedback methods (can be called by other scripts)
    public void HighlightAsValid()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
        }
    }
    
    public void HighlightAsInvalid()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = invalidColor;
        }
    }
    
    public void RemoveHighlight()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
}