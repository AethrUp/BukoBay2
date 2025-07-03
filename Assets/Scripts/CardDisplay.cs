using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardTypeText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI powerText;      // For gear: power, For actions: player effect
    public TextMeshProUGUI durabilityText; // For gear: durability, For actions: fish effect
    public Image cardBackground;
    public Image cardArtwork;
    
    [Header("Card Data")]
    public GearCard gearCard;
    public FishCard fishCard;
    public ActionCard actionCard;
    
    void Start()
    {
        DisplayCard();
    }
    
    // This will update the display whenever we change the card in the inspector
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            DisplayCard();
        }
    }
    
    void DisplayCard()
    {
        Debug.Log("DisplayCard called");
        
        if (gearCard != null)
        {
            Debug.Log("Displaying gear card: " + gearCard.gearName);
            DisplayGearCard();
        }
        else if (fishCard != null)
        {
            DisplayFishCard();
        }
        else if (actionCard != null)
        {
            Debug.Log("Displaying action card: " + actionCard.actionName);
            DisplayActionCard();
        }
        else
        {
            Debug.Log("No card assigned");
        }
    }
    
    void DisplayGearCard()
    {
        if (cardNameText != null) cardNameText.text = gearCard.gearName;
        if (cardTypeText != null) cardTypeText.text = gearCard.gearType;
        
        // Set individual stat fields
        if (powerText != null) powerText.text = gearCard.power.ToString();
        if (durabilityText != null) durabilityText.text = gearCard.durability.ToString();
        
        // Clear the main stats text since we're using separate fields
        if (statsText != null) statsText.text = "";
        
        if (cardArtwork != null) cardArtwork.sprite = gearCard.gearImage;
    }
    
    void DisplayFishCard()
    {
        if (cardNameText != null) cardNameText.text = fishCard.fishName;
        if (cardTypeText != null) cardTypeText.text = "Fish";
        
        string stats = $"Main Depth: {fishCard.mainDepth} Sub: {fishCard.subDepth}\n";
        stats += $"Power: {fishCard.power}\n";
        stats += $"Coins: {fishCard.coins}\n";
        stats += $"Gear Damage: {fishCard.GetTotalGearDamage()} pieces";
        
        // Add material info if present
        if (!string.IsNullOrEmpty(fishCard.material1))
            stats += $"\n{fishCard.material1}: {fishCard.materialDiff1:+0;-#}";
        if (!string.IsNullOrEmpty(fishCard.material2))
            stats += $"\n{fishCard.material2}: {fishCard.materialDiff2:+0;-#}";
        if (!string.IsNullOrEmpty(fishCard.material3))
            stats += $"\n{fishCard.material3}: {fishCard.materialDiff3:+0;-#}";
        
        if (statsText != null) statsText.text = stats;
        if (cardArtwork != null) cardArtwork.sprite = fishCard.fishImage;
    }
    
    void DisplayActionCard()
    {
        if (cardNameText != null) cardNameText.text = actionCard.actionName;
        if (cardTypeText != null) cardTypeText.text = "Action";
        
        // Use the separate fields for action card effects
        if (powerText != null) 
        {
            powerText.text = $"{actionCard.playerEffect:+0;-#;0}";
        }
        
        if (durabilityText != null) 
        {
            durabilityText.text = $"{actionCard.fishEffect:+0;-#;0}";
        }
        
        // Use statsText for description
        if (statsText != null) 
        {
            statsText.text = actionCard.description;
        }
        
        if (cardArtwork != null) cardArtwork.sprite = actionCard.actionImage;
    }
}