using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GearSelectionHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("Selection State")]
    public GearCard gearCard;
    public bool isSelected = false;
    
    [Header("Visual Feedback")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color hoverColor = Color.yellow;
    
    private EffectCardManager effectManager;
    
    public void Initialize(GearCard gear, EffectCardManager manager)
    {
        gearCard = gear;
        effectManager = manager;
        
        // Get background image if not assigned
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        // Set initial color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
        
        isSelected = false;
        
        Debug.Log($"Initialized gear selection for {gear.gearName}");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (effectManager != null && gearCard != null)
        {
            Debug.Log($"Clicked on {gearCard.gearName} for selection");
            effectManager.SelectGear(gearCard);
        }
    }
    
    public void UpdateSelection(bool selected)
    {
        isSelected = selected;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
        
        Debug.Log($"{gearCard.gearName} selection state: {isSelected}");
    }
    
    // Optional: Add hover effects
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null && !isSelected)
        {
            backgroundImage.color = hoverColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null && !isSelected)
        {
            backgroundImage.color = normalColor;
        }
    }
}