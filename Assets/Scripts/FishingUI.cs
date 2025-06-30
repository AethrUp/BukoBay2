using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingUI : MonoBehaviour
{
    [Header("UI References")]
    public Button startFishingButton;
    public TextMeshProUGUI gearCountText;
    public TextMeshProUGUI depthInfoText;
    public GameObject fishCardPanel;
    public CardDisplay fishCardDisplay;
    
    [Header("Game References")]
    public FishingManager fishingManager;
    public PlayerInventory playerInventory;
    
    void Start()
    {
        // Set up button
        if (startFishingButton != null)
        {
            startFishingButton.onClick.AddListener(StartFishing);
        }
        
        // Hide fish card initially
        if (fishCardPanel != null)
        {
            fishCardPanel.SetActive(false);
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        // Update UI every frame to show current gear status
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (playerInventory == null || fishingManager == null) return;
        
        // Count equipped gear
        int gearCount = CountEquippedGear();
        
        // Update gear count display
        if (gearCountText != null)
        {
            gearCountText.text = $"Equipped Gear: {gearCount} pieces";
        }
        
        // Update depth info and button state
        UpdateDepthInfo(gearCount);
    }
    
    int CountEquippedGear()
    {
        int count = 0;
        
        if (playerInventory.equippedRod != null) count++;
        if (playerInventory.equippedReel != null) count++;
        if (playerInventory.equippedLine != null) count++;
        if (playerInventory.equippedLure != null) count++;
        if (playerInventory.equippedBait != null) count++;
        if (playerInventory.equippedExtra1 != null) count++;
        if (playerInventory.equippedExtra2 != null) count++;
        
        return count;
    }
    
    void UpdateDepthInfo(int gearCount)
    {
        string depthInfo = "";
        bool canFish = false;
        
        if (gearCount < 2)
        {
            depthInfo = "Need at least 2 gear pieces to fish";
            canFish = false;
        }
        else if (gearCount == 2)
        {
            depthInfo = "Can fish: Coast (Depth 1)";
            canFish = true;
        }
        else if (gearCount == 3)
        {
            depthInfo = "Can fish: Ocean (Depth 2)";
            canFish = true;
        }
        else // 4+ pieces
        {
            depthInfo = "Can fish: Abyss (Depth 3)";
            canFish = true;
        }
        
        // Update depth info text
        if (depthInfoText != null)
        {
            depthInfoText.text = depthInfo;
        }
        
        // Enable/disable fishing button
        if (startFishingButton != null)
        {
            startFishingButton.interactable = canFish;
        }
    }
    
    public void StartFishing()
    {
        if (fishingManager == null)
        {
            Debug.LogError("No FishingManager assigned!");
            return;
        }
        
        Debug.Log("=== STARTING FISHING ===");
        
        // Setup fishing (calculates depth based on gear count)
        fishingManager.SetupFishing();
        
        // Cast at the required depth
        if (fishingManager.requiredMinDepth > 0)
        {
            fishingManager.CastAtDepth(fishingManager.requiredMinDepth);
            
            // Show the fish that appeared
            ShowCaughtFish();
        }
        else
        {
            Debug.LogWarning("Cannot fish - not enough gear!");
        }
    }
    
    void ShowCaughtFish()
    {
        if (fishingManager.currentFish == null)
        {
            Debug.LogWarning("No fish caught!");
            return;
        }
        
        // Show the fish card panel
        if (fishCardPanel != null)
        {
            fishCardPanel.SetActive(true);
        }
        
        // Display the fish on the card
        if (fishCardDisplay != null)
        {
            fishCardDisplay.fishCard = fishingManager.currentFish;
            fishCardDisplay.gearCard = null;
            fishCardDisplay.actionCard = null;
            
            // Force update the display
            fishCardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
        }
        
        Debug.Log($"Showing fish: {fishingManager.currentFish.fishName}");
    }
    
    // Public method to hide fish card (can be called by a close button)
    public void HideFishCard()
    {
        if (fishCardPanel != null)
        {
            fishCardPanel.SetActive(false);
        }
    }
}