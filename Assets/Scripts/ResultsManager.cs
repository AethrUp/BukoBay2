using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

[Header("Gear Display")]
[Header("Gear Display")]
public Transform rodPanel;
public Transform reelPanel;
public Transform linePanel;
public Transform lurePanel;
public Transform baitPanel;
public Transform shieldPanel;
public GameObject cardDisplayPrefab;
public float gearAnimationDelay = 0.1f;
private List<GameObject> displayedGearCards = new List<GameObject>();
private bool gearDisplayComplete = false;

[Header("Coin Animation")]
public GameObject coinIcon;                  // UI coin icon
public TextMeshProUGUI earnedCoinsText;      // Shows earned coins (separate from existing coinsEarnedText)
public TextMeshProUGUI playerTotalCoinsText; // Shows player's total coins
public float coinAnimationSpeed = 50f;       // How fast coins count up
public Button nextButton;                    // Button to proceed to next step

private bool coinAnimationComplete = false;
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

        StartCoroutine(AnimateGearDisplay());
        
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

    System.Collections.IEnumerator AnimateGearDisplay()
{
    if (cardDisplayPrefab == null || playerInventory == null)
    {
        Debug.LogWarning("Missing components for gear display!");
        gearDisplayComplete = true;
        yield break;
    }
    
    // Clear any existing gear displays
    ClearGearDisplay();
    
    Vector2 centerPosition = Vector2.zero; // Center the card in each individual panel
    
    // Display each equipped gear in its specific panel with animation delay
    if (playerInventory.equippedRod != null && rodPanel != null)
    {
        CreateGearDisplay(playerInventory.equippedRod, rodPanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    if (playerInventory.equippedReel != null && reelPanel != null)
    {
        CreateGearDisplay(playerInventory.equippedReel, reelPanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    if (playerInventory.equippedLine != null && linePanel != null)
    {
        CreateGearDisplay(playerInventory.equippedLine, linePanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    if (playerInventory.equippedLure != null && lurePanel != null)
    {
        CreateGearDisplay(playerInventory.equippedLure, lurePanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    if (playerInventory.equippedBait != null && baitPanel != null)
    {
        CreateGearDisplay(playerInventory.equippedBait, baitPanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    if (playerInventory.equippedExtra1 != null)
    {
        // For extra gear, you might want to create additional panels or handle differently
        Debug.Log("Extra gear 1 present but no panel assigned");
    }
    
    if (playerInventory.equippedExtra2 != null)
    {
        Debug.Log("Extra gear 2 present but no panel assigned");
    }
    
    // Handle equipped shield if you have a shield panel
    if (playerInventory.equippedShield != null && shieldPanel != null)
    {
        CreateEffectDisplay(playerInventory.equippedShield, shieldPanel, centerPosition);
        yield return new WaitForSeconds(gearAnimationDelay);
    }
    
    gearDisplayComplete = true;
    Debug.Log("Gear display animation complete");
}

    List<GearCard> GetEquippedGear()
{
    List<GearCard> gearList = new List<GearCard>();
    
    if (playerInventory.equippedRod != null) gearList.Add(playerInventory.equippedRod);
    if (playerInventory.equippedReel != null) gearList.Add(playerInventory.equippedReel);
    if (playerInventory.equippedLine != null) gearList.Add(playerInventory.equippedLine);
    if (playerInventory.equippedLure != null) gearList.Add(playerInventory.equippedLure);
    if (playerInventory.equippedBait != null) gearList.Add(playerInventory.equippedBait);
    if (playerInventory.equippedExtra1 != null) gearList.Add(playerInventory.equippedExtra1);
    if (playerInventory.equippedExtra2 != null) gearList.Add(playerInventory.equippedExtra2);
    
    return gearList;
}

void CreateGearDisplay(GearCard gear, Transform parentPanel, Vector2 position)
{
    GameObject gearCard = Instantiate(cardDisplayPrefab, parentPanel);
    
    // Set up the card display
    CardDisplay cardDisplay = gearCard.GetComponent<CardDisplay>();
    if (cardDisplay != null)
    {
        cardDisplay.gearCard = gear;
        cardDisplay.fishCard = null;
        cardDisplay.actionCard = null;
        cardDisplay.effectCard = null;
    }
    
    // Position the card (centered in the panel)
    RectTransform rectTransform = gearCard.GetComponent<RectTransform>();
    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
    rectTransform.anchoredPosition = position;
    
    // Start small and scale up for animation
    gearCard.transform.localScale = Vector3.zero;
    
    // Animate the card appearing
    StartCoroutine(AnimateCardAppear(gearCard));
    
    displayedGearCards.Add(gearCard);
}

// Add this method for shield display
void CreateEffectDisplay(EffectCard effect, Transform parentPanel, Vector2 position)
{
    GameObject effectCard = Instantiate(cardDisplayPrefab, parentPanel);
    
    // Set up the card display
    CardDisplay cardDisplay = effectCard.GetComponent<CardDisplay>();
    if (cardDisplay != null)
    {
        cardDisplay.gearCard = null;
        cardDisplay.fishCard = null;
        cardDisplay.actionCard = null;
        cardDisplay.effectCard = effect;
    }
    
    // Position the card (centered in the panel)
    RectTransform rectTransform = effectCard.GetComponent<RectTransform>();
    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
    rectTransform.anchoredPosition = position;
    
    // Start small and scale up for animation
    effectCard.transform.localScale = Vector3.zero;
    
    // Animate the card appearing
    StartCoroutine(AnimateCardAppear(effectCard));
    
    displayedGearCards.Add(effectCard);
}

System.Collections.IEnumerator AnimateCardAppear(GameObject card)
{
    float animationTime = 0.3f;
    float elapsed = 0f;
    
    while (elapsed < animationTime)
    {
        elapsed += Time.deltaTime;
        float scale = Mathf.Lerp(0f, 1f, elapsed / animationTime);
        card.transform.localScale = Vector3.one * scale;
        yield return null;
    }
    
    card.transform.localScale = Vector3.one;
}

void ClearGearDisplay()
{
    foreach (GameObject card in displayedGearCards)
    {
        if (card != null)
            Destroy(card);
    }
    displayedGearCards.Clear();
}

System.Collections.IEnumerator AnimateCoinReward()
{
    Debug.Log("Starting coin animation");
    
    // Hide continue button, show coin elements
    if (continueButton != null) continueButton.gameObject.SetActive(false);
    if (coinIcon != null) coinIcon.SetActive(true);
    
    // Show earned coins counting up
    if (earnedCoinsText != null)
    {
        earnedCoinsText.gameObject.SetActive(true);
        yield return StartCoroutine(CountUpCoins(earnedCoinsText, 0, coinsEarned));
    }
    
    // Show player's current total
    if (playerTotalCoinsText != null)
    {
        playerTotalCoinsText.gameObject.SetActive(true);
        int currentTotal = playerInventory != null ? playerInventory.coins : 0;
        playerTotalCoinsText.text = $"Total: {currentTotal}";
        yield return new WaitForSeconds(0.5f);
    }
    
    // Transfer animation (earned coins go to total)
    if (earnedCoinsText != null && playerTotalCoinsText != null && coinsEarned > 0)
    {
        yield return StartCoroutine(TransferCoins());
    }
    
    // Show next button
    if (nextButton != null) nextButton.gameObject.SetActive(true);
    
    coinAnimationComplete = true;
    Debug.Log("Coin animation complete");
}

System.Collections.IEnumerator CountUpCoins(TextMeshProUGUI textField, int startValue, int endValue)
{
    float elapsed = 0f;
    float duration = Mathf.Abs(endValue - startValue) / coinAnimationSpeed;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, elapsed / duration));
        textField.text = $"Earned: {currentValue}";
        yield return null;
    }
    
    textField.text = $"Earned: {endValue}";
}

System.Collections.IEnumerator TransferCoins()
{
    // Get current values
    int startPlayerTotal = playerInventory != null ? playerInventory.coins : 0;
    int endPlayerTotal = startPlayerTotal + coinsEarned;
    
    // Animate earned coins going down to 0 and player total going up
    float duration = 1f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        
        // Earned coins count down
        int currentEarned = Mathf.RoundToInt(Mathf.Lerp(coinsEarned, 0, progress));
        if (earnedCoinsText != null) earnedCoinsText.text = $"Earned: {currentEarned}";
        
        // Player total counts up
        int currentTotal = Mathf.RoundToInt(Mathf.Lerp(startPlayerTotal, endPlayerTotal, progress));
        if (playerTotalCoinsText != null) playerTotalCoinsText.text = $"Total: {currentTotal}";
        
        yield return null;
    }
    
    // Final values
    if (earnedCoinsText != null) earnedCoinsText.text = "Earned: 0";
    if (playerTotalCoinsText != null) playerTotalCoinsText.text = $"Total: {endPlayerTotal}";
}
    void OnContinueClicked()
{
    Debug.Log("Continue button clicked");
    
    if (!gearDisplayComplete)
    {
        Debug.Log("Waiting for gear display to complete...");
        return; // Don't continue until gear animation is done
    }
    
    // Start coin animation instead of returning to camera
    StartCoroutine(AnimateCoinReward());
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