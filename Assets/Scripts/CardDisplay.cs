using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardTypeText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI powerText;      // For gear: power, For actions: player effect, For effects: effect info
    public TextMeshProUGUI durabilityText; // For gear: durability, For actions: fish effect, For effects: usage info
    public TextMeshProUGUI priceText;      // For shop items: price
    public TextMeshProUGUI quantityText;   // For shop items: quantity
    public Image cardBackground;
    public Image cardArtwork;
    public Image protectionIcon;           // Shield sprite for protection
    
    [Header("Card Data")]
    public GearCard gearCard;
    public FishCard fishCard;
    public ActionCard actionCard;
    public EffectCard effectCard;
    
    [Header("Shop Data")]
    public int itemPrice = 0;
    public int itemQuantity = 0;
    public bool isShopItem = false;
    
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
        else if (effectCard != null)
        {
            Debug.Log("Displaying effect card: " + effectCard.effectName);
            DisplayEffectCard();
        }
        else
        {
            Debug.Log("No card assigned");
        }
        
        // Display shop information if this is a shop item
        if (isShopItem)
        {
            DisplayShopInfo();
        }
    }
    
    void DisplayShopInfo()
    {
        Debug.Log($"DisplayShopInfo called - isShopItem: {isShopItem}, price: {itemPrice}, quantity: {itemQuantity}");
        
        if (priceText != null)
        {
            priceText.text = $"${itemPrice}";
            Debug.Log($"Set price text to: ${itemPrice}");
        }
        else
        {
            Debug.Log("PriceText is null!");
        }
        
        if (quantityText != null)
        {
            quantityText.text = $"Qty: {itemQuantity}";
            Debug.Log($"Set quantity text to: Qty: {itemQuantity}");
        }
        else
        {
            Debug.Log("QuantityText is null!");
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
        
        // Handle protection icon
        UpdateProtectionIcon();
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
    
    void DisplayEffectCard()
    {
        if (cardNameText != null) cardNameText.text = effectCard.effectName;
        if (cardTypeText != null) cardTypeText.text = "Effect";
        
        // Show effect-specific info
        string effectInfo = "";
        switch (effectCard.effectType)
        {
            case EffectType.Repair:
                if (effectCard.repairHalfDamage)
                    effectInfo = "Repairs 50% damage";
                else
                    effectInfo = $"Repairs {effectCard.repairAmount} durability";
                break;
            case EffectType.Protection:
                effectInfo = "Protection Effect";
                break;
            case EffectType.Utility:
                effectInfo = "Utility Effect";
                break;
            case EffectType.Persistent:
                effectInfo = "Persistent Effect";
                break;
        }
        
        // Use powerText and durabilityText for effect info
        if (powerText != null) powerText.text = effectInfo;
        if (durabilityText != null) durabilityText.text = effectCard.singleUse ? "Single Use" : "Reusable";
        
        // Use statsText for description
        if (statsText != null) statsText.text = effectCard.description;
        
        if (cardArtwork != null) cardArtwork.sprite = effectCard.effectImage;
    }
    
    void UpdateProtectionIcon()
    {
        if (protectionIcon == null) return;
        
        // Show/hide protection icon based on gear protection status
        if (gearCard != null && gearCard.hasProtection)
        {
            protectionIcon.gameObject.SetActive(true);
            Debug.Log($"Showing protection icon for {gearCard.gearName}");
        }
        else
        {
            protectionIcon.gameObject.SetActive(false);
        }
    }
}