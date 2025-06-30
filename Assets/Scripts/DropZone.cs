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
        
        playerInventory = FindFirstObjectByType<PlayerInventory>();
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
        if (playerInventory == null) return;
        
        switch (zoneType)
        {
            case DropZoneType.EquippedRod:
                playerInventory.equippedRod = gearCard;
                break;
            case DropZoneType.EquippedReel:
                playerInventory.equippedReel = gearCard;
                break;
            case DropZoneType.EquippedLine:
                playerInventory.equippedLine = gearCard;
                break;
            case DropZoneType.EquippedLure:
                playerInventory.equippedLure = gearCard;
                break;
            case DropZoneType.EquippedBait:
                playerInventory.equippedBait = gearCard;
                break;
            case DropZoneType.EquippedExtra1:
                playerInventory.equippedExtra1 = gearCard;
                break;
            case DropZoneType.EquippedExtra2:
                playerInventory.equippedExtra2 = gearCard;
                break;
            case DropZoneType.TackleBox:
                if (!playerInventory.extraGear.Contains(gearCard))
                {
                    playerInventory.extraGear.Add(gearCard);
                }
                break;
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