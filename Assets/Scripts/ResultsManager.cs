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
    public Button continueButton;  // SINGLE BUTTON FOR EVERYTHING

    [Header("Damage Animation")]
    public float damageAnimationDelay = 0.5f;
    public GameObject damageNumberPrefab;
    public Color damageNumberColor = Color.red;
    public Color destroyedColor = Color.black;

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
    public GameObject gearCardDisplayPrefab;
    public GameObject actionCardDisplayPrefab;
    public float gearAnimationDelay = 0.1f;

    [Header("Coin Animation")]
    public GameObject coinIcon;
    public TextMeshProUGUI earnedCoinsText;
    public TextMeshProUGUI playerTotalCoinsText;
    public float coinAnimationSpeed = 50f;

    [Header("Treasure System")]
    public GameObject treasurePanel;
    public TextMeshProUGUI treasureCountText;
    public Button gearChestButton;
    public Button actionChestButton;
    public Transform rewardDisplayPanel;

    [Header("Treasure Data")]
    public int remainingTreasures = 0;
    private List<GameObject> rewardCards = new List<GameObject>();
    private List<GameObject> displayedGearCards = new List<GameObject>();

    // State tracking
    private bool gearDisplayComplete = false;
    private bool coinAnimationComplete = false;
    private bool treasurePhaseComplete = false;
    private bool damagePhaseComplete = false;
    private int currentPhase = 0; // 0=gear, 1=coins, 2=treasures, 3=damage, 4=finish

    void Start()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();
        
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }
    
    public void ShowResults(bool success, FishCard fish, int coins, string damage)
{
    Debug.Log($"=== SHOWING RESULTS ===");
    Debug.Log($"Success: {success}");
    Debug.Log($"Fish: {(fish != null ? fish.fishName : "None")}");
    Debug.Log($"Coins: {coins}");
    Debug.Log($"Damage: {damage}");
    
    // NEW: Check if this player should get coins
    bool shouldGetCoins = false;
    if (Unity.Netcode.NetworkManager.Singleton != null)
    {
        FishingManager fishingManager = FindFirstObjectByType<FishingManager>();
        if (fishingManager != null)
        {
            ulong myClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            shouldGetCoins = (myClientId == fishingManager.currentFishingPlayerId.Value);
            Debug.Log($"COIN CHECK - My ID: {myClientId}, Fishing Player ID: {fishingManager.currentFishingPlayerId.Value}, Should I get coins? {shouldGetCoins}");

        }
        else
        {
            Debug.LogError("Could not find FishingManager!");
        }
    }
    else
    {
        // Single player - always get coins
        shouldGetCoins = true;
        Debug.Log("Single player mode - getting coins");
    }
    
    // Store the results data
    fishingSuccess = success;
    caughtFish = fish;
    coinsEarned = shouldGetCoins ? coins : 0; // Only show coins if this player should get them
    damageReport = damage;
    
    // Reset all phases
    currentPhase = 0;
    gearDisplayComplete = false;
    coinAnimationComplete = false;
    treasurePhaseComplete = false;
    damagePhaseComplete = false;
    
    // Update the UI
    UpdateResultsDisplay();
    
    // Switch to results camera
    if (cameraManager != null)
    {
        cameraManager.SwitchToResultsCamera();
    }

    // Start the first phase
    StartCoroutine(AnimateGearDisplay());
}
    
    void UpdateResultsDisplay()
    {
        if (resultsTitle != null)
        {
            resultsTitle.text = fishingSuccess ? "FISHING SUCCESS!" : "FISHING FAILED!";
            resultsTitle.color = fishingSuccess ? Color.green : Color.red;
        }
        
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
        
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"Coins earned: {coinsEarned}";
            coinsEarnedText.color = coinsEarned > 0 ? Color.yellow : Color.gray;
        }

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
        
        if (gearDamageText != null)
        {
            if (string.IsNullOrEmpty(damageReport) || damageReport == "No damage dealt")
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

        // Set up continue button text for first phase
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            UpdateContinueButtonText();
        }
    }

    void UpdateContinueButtonText()
    {
        if (continueButton == null) return;
        
        TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText == null) return;
        
        switch (currentPhase)
        {
            case 0:
                buttonText.text = "Continue";
                break;
            case 1:
                buttonText.text = "Collect Coins";
                break;
            case 2:
                buttonText.text = fishingSuccess && caughtFish != null && caughtFish.treasures > 0 ? "Open Treasures" : "Continue";
                break;
            case 3:
                buttonText.text = (!string.IsNullOrEmpty(damageReport) && damageReport != "No damage dealt") ? "Show Damage" : "Continue";
                break;
            case 4:
                buttonText.text = "Return to Fishing";
                break;
        }
    }
    // ADD THIS TO YOUR ResultsManager class - Part 2

    System.Collections.IEnumerator AnimateGearDisplay()
    {
        Debug.Log("Starting gear display animation");
        
        if (gearCardDisplayPrefab == null || playerInventory == null)
        {
            gearDisplayComplete = true;
            yield break;
        }
        
        ClearGearDisplay();
        Vector2 centerPosition = Vector2.zero;
        
        // Display each equipped gear with animation delay
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
        
        if (playerInventory.equippedShield != null && shieldPanel != null)
        {
            CreateEffectDisplay(playerInventory.equippedShield, shieldPanel, centerPosition);
            yield return new WaitForSeconds(gearAnimationDelay);
        }
        
        gearDisplayComplete = true;
        Debug.Log("Gear display animation complete");
    }

    void CreateGearDisplay(GearCard gear, Transform parentPanel, Vector2 position)
    {
        GameObject gearCard = Instantiate(gearCardDisplayPrefab, parentPanel);
        
        CardDisplay cardDisplay = gearCard.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardDisplay.gearCard = gear;
            cardDisplay.fishCard = null;
            cardDisplay.actionCard = null;
            cardDisplay.effectCard = null;
        }
        
        RectTransform rectTransform = gearCard.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        
        gearCard.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateCardAppear(gearCard));
        
        displayedGearCards.Add(gearCard);
    }

    void CreateEffectDisplay(EffectCard effect, Transform parentPanel, Vector2 position)
    {
        GameObject effectCard = Instantiate(gearCardDisplayPrefab, parentPanel);
        
        CardDisplay cardDisplay = effectCard.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardDisplay.gearCard = null;
            cardDisplay.fishCard = null;
            cardDisplay.actionCard = null;
            cardDisplay.effectCard = effect;
        }
        
        RectTransform rectTransform = effectCard.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        
        effectCard.transform.localScale = Vector3.zero;
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
    // ADD THIS TO YOUR ResultsManager class - Part 3

    System.Collections.IEnumerator AnimateCoinReward()
    {
        Debug.Log("Starting coin animation");
        
        if (coinIcon != null) coinIcon.SetActive(true);
        
        if (earnedCoinsText != null)
        {
            earnedCoinsText.gameObject.SetActive(true);
            earnedCoinsText.text = coinsEarned.ToString();
            yield return new WaitForSeconds(0.5f);
        }
        
        if (playerTotalCoinsText != null)
        {
            playerTotalCoinsText.gameObject.SetActive(true);
            int currentTotal = playerInventory != null ? playerInventory.coins : 0;
            playerTotalCoinsText.text = currentTotal.ToString();
            yield return new WaitForSeconds(0.5f);
        }
        
        if (earnedCoinsText != null && playerTotalCoinsText != null && coinsEarned > 0)
        {
            yield return StartCoroutine(TransferCoins());
        }
        
        coinAnimationComplete = true;
        currentPhase = 2;
        UpdateContinueButtonText();
        Debug.Log("Coin animation complete");
    }

    System.Collections.IEnumerator TransferCoins()
{
    // Get the starting values for animation (don't modify actual inventory)
    int startPlayerTotal = playerInventory != null ? playerInventory.coins : 0;
    int targetPlayerTotal = startPlayerTotal + coinsEarned;

    float duration = 1f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;

        // Animate the earned coins counting down
        int currentEarned = Mathf.RoundToInt(Mathf.Lerp(coinsEarned, 0, progress));
        if (earnedCoinsText != null) earnedCoinsText.text = currentEarned.ToString();

        // Animate the total coins counting up (but don't actually modify inventory)
        int animatedTotal = Mathf.RoundToInt(Mathf.Lerp(startPlayerTotal, targetPlayerTotal, progress));
        if (playerTotalCoinsText != null) playerTotalCoinsText.text = animatedTotal.ToString();

        yield return null;
    }

    // Final animation state
    if (earnedCoinsText != null) earnedCoinsText.text = "0";
    
    // Show the actual current coins (which may have been updated by FishingManager)
    int actualCurrentCoins = playerInventory != null ? playerInventory.coins : targetPlayerTotal;
    if (playerTotalCoinsText != null) playerTotalCoinsText.text = actualCurrentCoins.ToString();
}

    void StartTreasurePhase()
{
    Debug.Log("Starting treasure phase");
    
    // NEW: Check if this player should get treasures (same logic as coins)
    bool shouldGetTreasures = false;
    if (Unity.Netcode.NetworkManager.Singleton != null)
    {
        FishingManager fishingManager = FindFirstObjectByType<FishingManager>();
        if (fishingManager != null)
        {
            ulong myClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            shouldGetTreasures = (myClientId == fishingManager.currentFishingPlayerId.Value);
            Debug.Log($"TREASURE CHECK - My ID: {myClientId}, Fishing Player ID: {fishingManager.currentFishingPlayerId.Value}, Should I get treasures? {shouldGetTreasures}");
        }
        else
        {
            Debug.LogError("Could not find FishingManager!");
        }
    }
    else
    {
        // Single player - always get treasures
        shouldGetTreasures = true;
        Debug.Log("Single player mode - getting treasures");
    }
    
    // Only give treasures if fishing was successful AND this is the fishing player
    if (fishingSuccess && caughtFish != null && shouldGetTreasures)
    {
        remainingTreasures = caughtFish.treasures;
    }
    else
    {
        remainingTreasures = 0;
    }
    
    Debug.Log($"Remaining treasures: {remainingTreasures}");
    
    if (remainingTreasures > 0)
    {
        ClearRewardCards();
        
        if (treasurePanel != null) treasurePanel.SetActive(true);
        UpdateTreasureDisplay();
        
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
        
        // Hide continue button during treasure phase
        if (continueButton != null) continueButton.gameObject.SetActive(false);
    }
    else
    {
        treasurePhaseComplete = true;
        currentPhase = 3;
        UpdateContinueButtonText();
    }
}

    void UpdateTreasureDisplay()
    {
        if (treasureCountText != null)
        {
            treasureCountText.text = remainingTreasures.ToString();
        }
        
        bool hasRemainingTreasures = remainingTreasures > 0;
        if (gearChestButton != null) gearChestButton.interactable = hasRemainingTreasures;
        if (actionChestButton != null) actionChestButton.interactable = hasRemainingTreasures;
    }

    void OpenChest(string chestType)
    {
        if (remainingTreasures <= 0) return;
        
        Debug.Log($"Opening {chestType} chest");
        
        remainingTreasures--;
        UpdateTreasureDisplay();
        
        if (chestType == "gear")
        {
            GiveRandomGear();
        }
        else if (chestType == "action")
        {
            GiveRandomAction();
        }
        
        if (remainingTreasures <= 0)
        {
            if (treasurePanel != null) treasurePanel.SetActive(false);
            
            treasurePhaseComplete = true;
            currentPhase = 3;
            UpdateContinueButtonText();
            
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }
    }

    void GiveRandomGear()
    {
        if (playerInventory == null || gearCardDisplayPrefab == null || rewardDisplayPanel == null) return;
        
        GearCard randomGear = GetRandomGearCard();
        
        if (randomGear != null)
        {
            GearCard gearCopy = Instantiate(randomGear);
            gearCopy.maxDurability = gearCopy.durability;
            playerInventory.extraGear.Add(gearCopy);
            
            ShowRewardCard(gearCopy, null, null);
            
            Debug.Log($"Player received: {randomGear.gearName}");
        }
    }

    void GiveRandomAction()
    {
        if (playerInventory == null || actionCardDisplayPrefab == null || rewardDisplayPanel == null) return;
        
        ActionCard randomAction = GetRandomActionCard();
        
        if (randomAction != null)
        {
            playerInventory.actionCards.Add(randomAction);
            ShowRewardCard(null, randomAction, null);
            
            Debug.Log($"Player received: {randomAction.actionName}");
        }
    }

    void ShowRewardCard(GearCard gear, ActionCard action, EffectCard effect)
    {
        if (rewardDisplayPanel == null) return;
        
        GameObject prefabToUse = null;
        if (gear != null)
            prefabToUse = gearCardDisplayPrefab;
        else if (action != null)
            prefabToUse = actionCardDisplayPrefab;
        
        if (prefabToUse == null) return;
        
        GameObject rewardCard = Instantiate(prefabToUse, rewardDisplayPanel);
        
        CardDisplay cardDisplay = rewardCard.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardDisplay.gearCard = gear;
            cardDisplay.actionCard = action;
            cardDisplay.effectCard = effect;
            cardDisplay.fishCard = null;
        }
        
        RectTransform rectTransform = rewardCard.GetComponent<RectTransform>();
        float cardWidth = 120f;
        float spacing = 10f;
        Vector2 position = new Vector2((rewardCards.Count * (cardWidth + spacing)), 0);
        rectTransform.anchoredPosition = position;
        
        rewardCard.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateCardAppear(rewardCard));
        
        rewardCards.Add(rewardCard);
    }

    GearCard GetRandomGearCard()
    {
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
    // ADD THIS TO YOUR ResultsManager class - Part 5

    void StartDamagePhase()
    {
        Debug.Log("Starting damage phase");
        
        if (treasurePanel != null) treasurePanel.SetActive(false);
        ClearRewardCards();
        
        // FIX: Only show damage animations if there was actual damage
        if (!string.IsNullOrEmpty(damageReport) && damageReport != "No damage dealt")
        {
            StartCoroutine(AnimateGearDamage());
        }
        else
        {
            damagePhaseComplete = true;
            currentPhase = 4;
            UpdateContinueButtonText();
        }
    }

    System.Collections.IEnumerator AnimateGearDamage()
    {
        Debug.Log($"Starting gear damage animations for: {damageReport}");
        
        // FIX: Better damage parsing
        string[] damageEntries = damageReport.Split(',');
        
        foreach (string entry in damageEntries)
        {
            if (string.IsNullOrEmpty(entry.Trim())) continue;
            
            Debug.Log($"Processing damage entry: '{entry.Trim()}'");
            yield return StartCoroutine(AnimateGearPieceDamage(entry.Trim()));
            yield return new WaitForSeconds(damageAnimationDelay);
        }
        
        damagePhaseComplete = true;
        currentPhase = 4;
        UpdateContinueButtonText();
    }

    // REPLACE the AnimateGearPieceDamage method with this fixed version:

    System.Collections.IEnumerator AnimateGearPieceDamage(string damageEntry)
{
    Debug.Log($"Animating damage for: '{damageEntry}'");
    
    // The damage format is "GearName -DamageAmount" or "GearName BROKEN" or "GearName PROTECTED"
    // Examples: "Basic Rod -2", "Starter Reel BROKEN", "Simple Line PROTECTED"
    
    if (damageEntry.Contains(" BROKEN"))
    {
        string gearName = damageEntry.Replace(" BROKEN", "").Trim();
        Debug.Log($"Gear BROKEN: '{gearName}'");
        
        GameObject gearCardObj = FindGearCardByName(gearName);
        if (gearCardObj != null)
        {
            yield return StartCoroutine(AnimateGearDestroyed(gearCardObj));
        }
        else
        {
            Debug.LogWarning($"Could not find gear card for: {gearName}");
        }
    }
    else if (damageEntry.Contains(" PROTECTED"))
    {
        string gearName = damageEntry.Replace(" PROTECTED", "").Trim();
        Debug.Log($"Gear PROTECTED: '{gearName}'");
        
        GameObject gearCardObj = FindGearCardByName(gearName);
        if (gearCardObj != null)
        {
            yield return StartCoroutine(AnimateGearProtected(gearCardObj));
        }
        else
        {
            Debug.LogWarning($"Could not find gear card for: {gearName}");
        }
    }
    else if (damageEntry.Contains(" -"))
    {
        // Format: "GearName -DamageAmount"
        string[] parts = damageEntry.Split(new string[] { " -" }, System.StringSplitOptions.None);
        if (parts.Length == 2)
        {
            string gearName = parts[0].Trim();
            string damageAmountStr = parts[1].Trim();
            
            Debug.Log($"Gear DAMAGED: '{gearName}' damage: '{damageAmountStr}'");
            
            if (int.TryParse(damageAmountStr, out int damageAmount))
            {
                GameObject gearCardObj = FindGearCardByName(gearName);
                if (gearCardObj != null)
                {
                    yield return StartCoroutine(AnimateGearDamaged(gearCardObj, damageAmount));
                }
                else
                {
                    Debug.LogWarning($"Could not find gear card for: {gearName}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not parse damage amount: {damageAmountStr}");
            }
        }
        else
        {
            Debug.LogWarning($"Unexpected damage format: {damageEntry}");
        }
    }
    else
    {
        Debug.LogWarning($"Unknown damage format: {damageEntry}");
    }
}

    GameObject FindGearCardByName(string gearName)
    {
        Debug.Log($"Searching for gear card with name: '{gearName}'");
        
        foreach (GameObject gearCard in displayedGearCards)
        {
            if (gearCard == null) continue;
            
            CardDisplay cardDisplay = gearCard.GetComponent<CardDisplay>();
            if (cardDisplay != null && cardDisplay.gearCard != null)
            {
                Debug.Log($"Checking against: '{cardDisplay.gearCard.gearName}'");
                
                // FIX: Better name matching
                if (cardDisplay.gearCard.gearName.Equals(gearName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"Found exact match for: {gearName}");
                    return gearCard;
                }
                
                // Also try partial matching
                if (cardDisplay.gearCard.gearName.Contains(gearName) || gearName.Contains(cardDisplay.gearCard.gearName))
                {
                    Debug.Log($"Found partial match for: {gearName}");
                    return gearCard;
                }
            }
        }
        
        Debug.LogWarning($"No gear card found for: {gearName}");
        return null;
    }

    System.Collections.IEnumerator AnimateGearDamaged(GameObject gearCard, int damageAmount)
    {
        Debug.Log($"Animating gear damaged: {damageAmount} damage");
        
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
        
        gearCard.transform.localPosition = originalPosition;
        yield return StartCoroutine(FlashCardColor(gearCard, damageNumberColor, 0.3f));
        
        Debug.Log($"Gear took {damageAmount} damage");
    }

    System.Collections.IEnumerator AnimateGearDestroyed(GameObject gearCard)
    {
        Debug.Log("Animating gear destroyed");
        
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
        
        yield return StartCoroutine(FlashCardColor(gearCard, destroyedColor, 0.5f));
        yield return StartCoroutine(FadeOutCard(gearCard));
        
        Debug.Log("Gear destroyed animation complete");
    }

    System.Collections.IEnumerator AnimateGearProtected(GameObject gearCard)
    {
        Debug.Log("Animating gear protected");
        
        yield return StartCoroutine(FlashCardColor(gearCard, Color.cyan, 0.5f));
        
        Debug.Log("Gear protection animation complete");
    }
    // ADD THIS TO YOUR ResultsManager class - Part 6 (Final Part)

    System.Collections.IEnumerator FlashCardColor(GameObject gearCard, Color flashColor, float duration)
    {
        UnityEngine.UI.Image cardImage = gearCard.GetComponent<UnityEngine.UI.Image>();
        if (cardImage == null) yield break;
        
        Color originalColor = cardImage.color;
        
        float elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            cardImage.color = Color.Lerp(originalColor, flashColor, elapsed / (duration / 2));
            yield return null;
        }
        
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

    void OnContinueClicked()
    {
        Debug.Log($"Continue button clicked - Current phase: {currentPhase}");
        
        switch (currentPhase)
        {
            case 0: // Gear display complete -> start coin animation
                if (gearDisplayComplete)
                {
                    currentPhase = 1;
                    UpdateContinueButtonText();
                    StartCoroutine(AnimateCoinReward());
                }
                break;
                
            case 1: // Coin animation should be automatic
                break;
                
            case 2: // Coin complete -> start treasure phase
                if (coinAnimationComplete)
                {
                    StartTreasurePhase();
                }
                break;
                
            case 3: // Treasure complete -> start damage phase
                if (treasurePhaseComplete)
                {
                    StartDamagePhase();
                }
                break;
                
            case 4: // Damage complete -> return to main camera
                if (damagePhaseComplete)
                {
                    if (cameraManager != null)
                    {
                        cameraManager.SwitchToMainCamera();
                    }
                }
                break;
        }
    }

    [ContextMenu("Test Success Results")]
    public void TestSuccessResults()
    {
        ShowResults(true, null, 50, "");
    }

    [ContextMenu("Test Failure Results")]
    public void TestFailureResults()
    {
        ShowResults(false, null, 0, "Basic Rod -2, Starter Reel BROKEN");
    }
}