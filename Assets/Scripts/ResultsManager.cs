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

[Header("Damage Animation")]
public float damageAnimationDelay = 0.5f;    // Time between each gear damage animation
public GameObject damageNumberPrefab;        // Prefab for floating damage numbers
public Color damageNumberColor = Color.red;  // Color for damage numbers
public Color destroyedColor = Color.black;   // Color when gear is destroyed

private bool damagePhaseComplete = false;
    
[Header("Game References")]
public CameraManager cameraManager;
public PlayerInventory playerInventory;

[Header("Current Results Data")]
public bool fishingSuccess = false;
public FishCard caughtFish = null;
public int coinsEarned = 0;
public string damageReport = "";

[Header("Gear Display")]
public Transform rodPanel;
public Transform reelPanel;
public Transform linePanel;
public Transform lurePanel;
public Transform baitPanel;
public Transform shieldPanel;
public GameObject gearCardDisplayPrefab;    // For gear cards
public GameObject actionCardDisplayPrefab;  // For action cards
public float gearAnimationDelay = 0.1f;

[Header("Coin Animation")]
public GameObject coinIcon;                  // UI coin icon
public TextMeshProUGUI earnedCoinsText;      // Shows earned coins (separate from existing coinsEarnedText)
public TextMeshProUGUI playerTotalCoinsText; // Shows player's total coins
public float coinAnimationSpeed = 50f;       // How fast coins count up
public Button nextButton;                    // Button to proceed to next step

private bool coinAnimationComplete = false;  // Add this line here

[Header("Treasure System")]
public GameObject treasurePanel;              // Panel containing treasure UI
public TextMeshProUGUI treasureCountText;     // Shows remaining treasures
public Button gearChestButton;               // Left chest - gives gear
public Button actionChestButton;             // Right chest - gives action cards
public Transform rewardDisplayPanel;         // Panel to show the reward cards
public Button treasureNextButton;           // Button to proceed to damage phase

[Header("Treasure Data")]
public int remainingTreasures = 0;           // How many treasures left to open
private bool treasurePhaseComplete = false;
    private List<GameObject> rewardCards = new List<GameObject>(); // Track reward cards for cleanup
private List<GameObject> displayedGearCards = new List<GameObject>();
private bool gearDisplayComplete = false;


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
    if (gearCardDisplayPrefab == null || playerInventory == null)
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
    GameObject gearCard = Instantiate(gearCardDisplayPrefab, parentPanel);
    
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
    GameObject effectCard = Instantiate(gearCardDisplayPrefab, parentPanel);
    
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
    
    // Show earned coins (just the number) - even if 0
    if (earnedCoinsText != null)
    {
        earnedCoinsText.gameObject.SetActive(true);
        earnedCoinsText.text = coinsEarned.ToString();
        yield return new WaitForSeconds(0.5f);
    }
    
    // Show player's current total (just the number)
    if (playerTotalCoinsText != null)
    {
        playerTotalCoinsText.gameObject.SetActive(true);
        int currentTotal = playerInventory != null ? playerInventory.coins : 0;
        playerTotalCoinsText.text = currentTotal.ToString();
        yield return new WaitForSeconds(0.5f);
    }
    
    // Transfer animation only if there are coins to transfer
    if (earnedCoinsText != null && playerTotalCoinsText != null && coinsEarned > 0)
    {
        yield return StartCoroutine(TransferCoins());
    }
    
    // Always start treasure phase (even if 0 treasures)
    StartTreasurePhase();
    
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

            // Earned coins count down from coinsEarned to 0 (just numbers)
            int currentEarned = Mathf.RoundToInt(Mathf.Lerp(coinsEarned, 0, progress));
            if (earnedCoinsText != null) earnedCoinsText.text = currentEarned.ToString();

            // Player total counts up (just numbers)
            int currentTotal = Mathf.RoundToInt(Mathf.Lerp(startPlayerTotal, endPlayerTotal, progress));
            if (playerTotalCoinsText != null) playerTotalCoinsText.text = currentTotal.ToString();

            yield return null;
        }

        // Final values (just numbers)
        if (earnedCoinsText != null) earnedCoinsText.text = "0";
        if (playerTotalCoinsText != null) playerTotalCoinsText.text = endPlayerTotal.ToString();
    }

    void StartTreasurePhase()
{
    Debug.Log("Starting treasure phase");
    
    // Use the fish's actual treasure value
    if (caughtFish != null)
    {
        remainingTreasures = caughtFish.treasures;
    }
    else
    {
        remainingTreasures = 0;
    }
    
    if (remainingTreasures > 0)
    {
        // Clear any previous reward cards
        ClearRewardCards();
        
        // Show treasure UI
        if (treasurePanel != null) treasurePanel.SetActive(true);
        UpdateTreasureDisplay();
        
        // Set up chest buttons
        if (gearChestButton != null)
        {
            gearChestButton.onClick.RemoveAllListeners();
            gearChestButton.onClick.AddListener(() => OpenChest("gear"));
        }
        
        if (actionChestButton != null)
        {
            actionChestButton.onClick.RemoveAllListeners();
            actionChestButton.onClick.AddListener(() => OpenChest("action"));
        }
        
        // Hide treasure next button initially
        if (treasureNextButton != null) treasureNextButton.gameObject.SetActive(false);
    }
    else
    {
        // No treasures, go directly to damage phase
        StartDamagePhase();
    }
}
void UpdateTreasureDisplay()
{
    if (treasureCountText != null)
    {
        treasureCountText.text = remainingTreasures.ToString();
    }
    
    // Enable/disable chest buttons based on remaining treasures
    bool hasRemainingTreasures = remainingTreasures > 0;
    if (gearChestButton != null) gearChestButton.interactable = hasRemainingTreasures;
    if (actionChestButton != null) actionChestButton.interactable = hasRemainingTreasures;
}

void OpenChest(string chestType)
{
    if (remainingTreasures <= 0) return;
    
    Debug.Log($"Opening {chestType} chest");
    
    // Decrease treasure count
    remainingTreasures--;
    UpdateTreasureDisplay();
    
    // Give reward based on chest type and show the card
    if (chestType == "gear")
    {
        GiveRandomGear();
    }
    else if (chestType == "action")
    {
        GiveRandomAction();
    }
    
    // Check if treasures are done
    if (remainingTreasures <= 0)
    {
        // Show next button to proceed to damage phase
        if (treasureNextButton != null)
        {
            treasureNextButton.gameObject.SetActive(true);
            treasureNextButton.onClick.RemoveAllListeners();
            treasureNextButton.onClick.AddListener(StartDamagePhase);
        }
    }
}

void GiveRandomGear()
{
    if (playerInventory == null || gearCardDisplayPrefab == null || rewardDisplayPanel == null) return;
    
    // Get a random gear card from all available gear
    GearCard randomGear = GetRandomGearCard();
    
    if (randomGear != null)
    {
        // Add gear to player's inventory
        GearCard gearCopy = Instantiate(randomGear);
        gearCopy.maxDurability = gearCopy.durability;
        playerInventory.extraGear.Add(gearCopy);
        
        // Create and show the card display
        ShowRewardCard(gearCopy, null, null);
        
        Debug.Log($"Player received: {randomGear.gearName}");
    }
}

void GiveRandomAction()
{
    if (playerInventory == null || actionCardDisplayPrefab == null || rewardDisplayPanel == null) return;
    
    // Get a random action card from all available actions
    ActionCard randomAction = GetRandomActionCard();
    
    if (randomAction != null)
    {
        // Add action to player's inventory
        playerInventory.actionCards.Add(randomAction);
        
        // Create and show the card display
        ShowRewardCard(null, randomAction, null);
        
        Debug.Log($"Player received: {randomAction.actionName}");
    }
}

void ShowRewardCard(GearCard gear, ActionCard action, EffectCard effect)
{
    if (rewardDisplayPanel == null) return;
    
    // Choose the correct prefab based on card type
    GameObject prefabToUse = null;
    if (gear != null)
        prefabToUse = gearCardDisplayPrefab;
    else if (action != null)
        prefabToUse = actionCardDisplayPrefab;
    
    if (prefabToUse == null) return;
    
    // Create the card display
    GameObject rewardCard = Instantiate(prefabToUse, rewardDisplayPanel);
    
    // Set up the card display
    CardDisplay cardDisplay = rewardCard.GetComponent<CardDisplay>();
    if (cardDisplay != null)
    {
        cardDisplay.gearCard = gear;
        cardDisplay.actionCard = action;
        cardDisplay.effectCard = effect;
        cardDisplay.fishCard = null;
    }
    
    // Position the card (you might want to arrange multiple cards horizontally)
    RectTransform rectTransform = rewardCard.GetComponent<RectTransform>();
    float cardWidth = 120f;
    float spacing = 10f;
    Vector2 position = new Vector2((rewardCards.Count * (cardWidth + spacing)), 0);
    rectTransform.anchoredPosition = position;
    
    // Add scale-up animation
    rewardCard.transform.localScale = Vector3.zero;
    StartCoroutine(AnimateCardAppear(rewardCard));
    
    // Track for cleanup
    rewardCards.Add(rewardCard);
}

GearCard GetRandomGearCard()
{
    // Load all gear cards from the project
    #if UNITY_EDITOR
    string[] gearGuids = UnityEditor.AssetDatabase.FindAssets("t:GearCard");
    
    if (gearGuids.Length > 0)
    {
        int randomIndex = Random.Range(0, gearGuids.Length);
        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(gearGuids[randomIndex]);
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GearCard>(assetPath);
    }
    #endif
    
    return null;
}

ActionCard GetRandomActionCard()
{
    // Load all action cards from the project
    #if UNITY_EDITOR
    string[] actionGuids = UnityEditor.AssetDatabase.FindAssets("t:ActionCard");
    
    if (actionGuids.Length > 0)
    {
        int randomIndex = Random.Range(0, actionGuids.Length);
        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(actionGuids[randomIndex]);
        return UnityEditor.AssetDatabase.LoadAssetAtPath<ActionCard>(assetPath);
    }
    #endif
    
    return null;
}

void ClearRewardCards()
{
    foreach (GameObject card in rewardCards)
    {
        if (card != null) Destroy(card);
    }
    rewardCards.Clear();
}

    void StartDamagePhase()
    {
        Debug.Log("Starting damage phase");

        // Hide treasure UI
        if (treasurePanel != null) treasurePanel.SetActive(false);

        // Clear reward cards
        ClearRewardCards();

        // Start gear damage animations if there was damage
        if (!string.IsNullOrEmpty(damageReport) && damageReport != "No damage dealt")
        {
            StartCoroutine(AnimateGearDamage());
        }
        else
        {
            // No damage to animate, just show finish button
            ShowFinishButton();
        }
    }
System.Collections.IEnumerator AnimateGearDamage()
{
    Debug.Log("Starting gear damage animations");
    
    // We need to get the actual damage info from the fishing manager
    // For now, let's parse the damage report string
    string[] damageEntries = damageReport.Split(',');
    
    foreach (string entry in damageEntries)
    {
        if (string.IsNullOrEmpty(entry.Trim())) continue;
        
        // Parse the damage entry (e.g., "Rod -2" or "Reel BROKEN")
        yield return StartCoroutine(AnimateGearPieceDamage(entry.Trim()));
        
        // Wait before next damage animation
        yield return new WaitForSeconds(damageAnimationDelay);
    }
    
    // All damage animations complete
    ShowFinishButton();
    damagePhaseComplete = true;
}

System.Collections.IEnumerator AnimateGearPieceDamage(string damageEntry)
{
    Debug.Log($"Animating damage for: {damageEntry}");
    
    // Parse the damage entry to get gear name and damage amount
    string[] parts = damageEntry.Split(' ');
    if (parts.Length < 2) yield break;
    
    string gearName = parts[0];
    string damageInfo = parts[1];
    
    // Find the gear card that matches this damage entry
    GameObject gearCardObj = FindGearCardByName(gearName);
    if (gearCardObj == null) yield break;
    
    // Animate the gear taking damage
    if (damageInfo == "BROKEN")
    {
        yield return StartCoroutine(AnimateGearDestroyed(gearCardObj));
    }
    else if (damageInfo == "PROTECTED")
    {
        yield return StartCoroutine(AnimateGearProtected(gearCardObj));
    }
    else
    {
        // Regular damage (like "-2")
        int damageAmount = 0;
        if (int.TryParse(damageInfo, out damageAmount))
        {
            yield return StartCoroutine(AnimateGearDamaged(gearCardObj, Mathf.Abs(damageAmount)));
        }
    }
}

GameObject FindGearCardByName(string gearName)
{
    // Look through all displayed gear cards to find the one with matching gear name
    foreach (GameObject gearCard in displayedGearCards)
    {
        if (gearCard == null) continue;
        
        CardDisplay cardDisplay = gearCard.GetComponent<CardDisplay>();
        if (cardDisplay != null && cardDisplay.gearCard != null)
        {
            if (cardDisplay.gearCard.gearName.Contains(gearName) || gearName.Contains(cardDisplay.gearCard.gearName))
            {
                return gearCard;
            }
        }
    }
    
    return null;
}

System.Collections.IEnumerator AnimateGearDamaged(GameObject gearCard, int damageAmount)
{
    Debug.Log($"Animating gear damaged: {damageAmount} damage");
    
    // Simple hit animation - shake the card
    Vector3 originalPosition = gearCard.transform.localPosition;
    
    // Shake animation
    float shakeTime = 0.5f;
    float shakeIntensity = 10f;
    float elapsed = 0f;
    
    while (elapsed < shakeTime)
    {
        elapsed += Time.deltaTime;
        Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * (1f - elapsed / shakeTime);
        gearCard.transform.localPosition = originalPosition + shakeOffset;
        yield return null;
    }
    
    // Return to original position
    gearCard.transform.localPosition = originalPosition;
    
    // Flash red briefly
    yield return StartCoroutine(FlashCardColor(gearCard, damageNumberColor, 0.3f));
    
    // Show floating damage number
    ShowFloatingDamageNumber(gearCard, $"-{damageAmount}");
}

System.Collections.IEnumerator AnimateGearDestroyed(GameObject gearCard)
{
    Debug.Log("Animating gear destroyed");
    
    // More intense shake
    Vector3 originalPosition = gearCard.transform.localPosition;
    
    float shakeTime = 0.8f;
    float shakeIntensity = 20f;
    float elapsed = 0f;
    
    while (elapsed < shakeTime)
    {
        elapsed += Time.deltaTime;
        Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity;
        gearCard.transform.localPosition = originalPosition + shakeOffset;
        yield return null;
    }
    
    // Flash black and fade out
    yield return StartCoroutine(FlashCardColor(gearCard, destroyedColor, 0.5f));
    
    // Show "BROKEN" text
    ShowFloatingDamageNumber(gearCard, "BROKEN");
    
    // Fade out the card
    yield return StartCoroutine(FadeOutCard(gearCard));
}

System.Collections.IEnumerator AnimateGearProtected(GameObject gearCard)
{
    Debug.Log("Animating gear protected");
    
    // Flash green/blue to show protection
    yield return StartCoroutine(FlashCardColor(gearCard, Color.cyan, 0.5f));
    
    // Show "PROTECTED" text
    ShowFloatingDamageNumber(gearCard, "PROTECTED");
}

System.Collections.IEnumerator FlashCardColor(GameObject gearCard, Color flashColor, float duration)
{
    UnityEngine.UI.Image cardImage = gearCard.GetComponent<UnityEngine.UI.Image>();
    if (cardImage == null) yield break;
    
    Color originalColor = cardImage.color;
    
    // Flash to damage color
    float elapsed = 0f;
    while (elapsed < duration / 2)
    {
        elapsed += Time.deltaTime;
        cardImage.color = Color.Lerp(originalColor, flashColor, elapsed / (duration / 2));
        yield return null;
    }
    
    // Flash back to original
    elapsed = 0f;
    while (elapsed < duration / 2)
    {
        elapsed += Time.deltaTime;
        cardImage.color = Color.Lerp(flashColor, originalColor, elapsed / (duration / 2));
        yield return null;
    }
    
    cardImage.color = originalColor;
}

System.Collections.IEnumerator FadeOutCard(GameObject gearCard)
{
    CanvasGroup canvasGroup = gearCard.GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
        canvasGroup = gearCard.AddComponent<CanvasGroup>();
    }
    
    float fadeTime = 1f;
    float elapsed = 0f;
    
    while (elapsed < fadeTime)
    {
        elapsed += Time.deltaTime;
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
        yield return null;
    }
    
    canvasGroup.alpha = 0f;
}

void ShowFloatingDamageNumber(GameObject gearCard, string damageText)
{
    // For now, just log the damage. We'll implement floating text in the next step if needed
    Debug.Log($"Damage number: {damageText}");
}

void ShowFinishButton()
{
    Debug.Log("Showing finish button");
    
    if (continueButton != null)
    {
        continueButton.gameObject.SetActive(true);
        
        // Update button to return to main camera
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => {
            if (cameraManager != null)
            {
                cameraManager.SwitchToMainCamera();
            }
        });
    }
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