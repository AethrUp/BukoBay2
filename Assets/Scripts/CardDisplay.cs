using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardTypeText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI powerText;      // Separate field for power
    public TextMeshProUGUI durabilityText; // Separate field for durability
    public Image cardBackground;
    public Image cardArtwork;
    
    [Header("Card Data")]
    public GearCard gearCard;
    public FishCard fishCard;
    public ActionCard actionCard;
    
    [Header("Comparison System")]
    public static GearComparisonDisplay gearComparison;
    
    void Start()
    {
        DisplayCard();
        
        // Find the comparison system if not set
        if (gearComparison == null)
        {
            gearComparison = FindFirstObjectByType<GearComparisonDisplay>();
        }
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
        
        string stats = $"Player Effect: {actionCard.playerEffect}\n";
        stats += $"Fish Effect: {actionCard.fishEffect}\n";
        stats += $"Description: {actionCard.description}";
        
        if (statsText != null) statsText.text = stats;
        if (cardArtwork != null) cardArtwork.sprite = actionCard.actionImage;
    }
    
    // Handle clicks on the card
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle gear card clicks for now
        if (gearCard != null && gearComparison != null)
        {
            Debug.Log($"Clicked on {gearCard.gearName}");
            gearComparison.ShowGearComparison(gearCard);
        }
    }
}