using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingResultsManager : MonoBehaviour
{
[Header("UI References")]
public TextMeshProUGUI resultsTitle;
public TextMeshProUGUI fishResultText;
public TextMeshProUGUI coinsEarnedText;
public TextMeshProUGUI gearDamageText;
public UnityEngine.UI.Image fishImage;
public Button continueButton;
    
    [Header("Game References")]
    public CameraManager cameraManager;
    public PlayerInventory playerInventory;
    
    [Header("Current Results Data")]
    public bool fishingSuccess = false;
    public FishCard caughtFish = null;
    public int coinsEarned = 0;
    public string damageReport = "";
    
    void Start()
    {
        // Find game references if not assigned
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();
        
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        // Set up the continue button
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }
    
    public void ShowResults(bool success, FishCard fish, int coins, string damage)
    {
        // Store the results data
        fishingSuccess = success;
        caughtFish = fish;
        coinsEarned = coins;
        damageReport = damage;
        
        // Update the UI
        UpdateResultsDisplay();
        
        // Switch to results camera
        if (cameraManager != null)
        {
            cameraManager.SwitchToResultsCamera();
        }
        
        Debug.Log($"Showing results - Success: {success}, Fish: {(fish != null ? fish.fishName : "None")}, Coins: {coins}");
    }
    
    void UpdateResultsDisplay()
    {
        // Update title
        if (resultsTitle != null)
        {
            resultsTitle.text = fishingSuccess ? "FISHING SUCCESS!" : "FISHING FAILED!";
            resultsTitle.color = fishingSuccess ? Color.green : Color.red;
        }
        
        // Update fish result
        if (fishResultText != null)
        {
            if (fishingSuccess && caughtFish != null)
            {
                fishResultText.text = $"You caught a {caughtFish.fishName}!";
                fishResultText.color = Color.white;
            }
            else
            {
                fishResultText.text = "The fish got away...";
                fishResultText.color = Color.gray;
            }
        }
        
        // Update coins earned
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"Coins earned: {coinsEarned}";
            coinsEarnedText.color = coinsEarned > 0 ? Color.yellow : Color.gray;
        }

        // Update fish image
if (fishImage != null)
{
    if (fishingSuccess && caughtFish != null && caughtFish.fishImage != null)
    {
        fishImage.sprite = caughtFish.fishImage;
        fishImage.color = Color.white;
    }
    else
    {
        fishImage.sprite = null;
        fishImage.color = Color.clear;
    }
}
        
        // Update gear damage
        if (gearDamageText != null)
        {
            if (string.IsNullOrEmpty(damageReport))
            {
                gearDamageText.text = "Gear damage: None";
                gearDamageText.color = Color.green;
            }
            else
            {
                gearDamageText.text = $"Gear damage: {damageReport}";
                gearDamageText.color = Color.red;
            }
        }
    }
    
    void OnContinueClicked()
    {
        Debug.Log("Continue button clicked - returning to main screen");
        
        // Return to main camera
        if (cameraManager != null)
        {
            cameraManager.SwitchToMainCamera();
        }
    }
    
    // Test method you can call from the inspector
    [ContextMenu("Test Success Results")]
    public void TestSuccessResults()
    {
        // Create some fake test data
        ShowResults(true, null, 50, "");
    }
    
    [ContextMenu("Test Failure Results")]
    public void TestFailureResults()
    {
        ShowResults(false, null, 0, "Rod damaged (-2 durability)");
    }
}