using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractivePhaseUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject interactivePanel;
    public Transform actionCardContainer;
    
    [Header("UI Elements")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI playerPowerText;
    public TextMeshProUGUI fishPowerText;
    public TextMeshProUGUI fishNameText;
    public TextMeshProUGUI playerStaminaText;
    public TextMeshProUGUI fishStaminaText;
    public Button nextRoundButton;
    public Button skipActionButton;
    
    [Header("Tug of War Display")]
    public TugOfWarStaminaBar tugOfWarBar;
    [Header("Action Target Panels")]
    public Transform targetFishPanel;
    public Transform targetPlayerPanel;
    
    [Header("Action Card UI")]
    public GameObject actionCardButtonPrefab;
    public Button targetPlayerButton;
    public Button targetFishButton;
    
    [Header("Game References")]
    public FishingManager fishingManager;
    public PlayerInventory playerInventory;
    
    [Header("Current Selection")]
    public ActionCard selectedActionCard;
    public bool targetingPlayer = true;
    
    private System.Collections.Generic.List<GameObject> actionCardButtons = new System.Collections.Generic.List<GameObject>();
    
    void Start()
    {
        // Set up buttons
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(NextRound);
        
        if (skipActionButton != null)
            skipActionButton.onClick.AddListener(SkipAction);
        
        if (targetPlayerButton != null)
            targetPlayerButton.onClick.AddListener(() => SetTarget(true));
        
        if (targetFishButton != null)
            targetFishButton.onClick.AddListener(() => SetTarget(false));
        
        // Hide panel initially
        if (interactivePanel != null)
            interactivePanel.SetActive(false);
    }
    
    void Update()
    {
        // Update UI if interactive phase is active
        if (fishingManager != null && fishingManager.isInteractionPhase)
        {
            UpdateInteractiveUI();
        }
    }
    
    public void ShowInteractivePhase()
    {
        if (interactivePanel != null)
            interactivePanel.SetActive(true);
        
        SetupActionCards();
        UpdateFishInfo();
        UpdateTargetButtons();
        
        // Initialize the tug-of-war bar when fishing starts
        InitializeTugOfWarBar();
    }
    
    public void HideInteractivePhase()
    {
        if (interactivePanel != null)
            interactivePanel.SetActive(false);
        
        ClearActionCards();
    }
    
    void SetupActionCards()
    {
        ClearActionCards();
        
        if (playerInventory == null) return;
        
        // Create buttons for each action card in player's inventory
        foreach (ActionCard actionCard in playerInventory.actionCards)
        {
            CreateActionCardButton(actionCard);
        }
    }
    
    void CreateActionCardButton(ActionCard actionCard)
    {
        if (actionCardButtonPrefab == null || actionCardContainer == null) return;
        
        GameObject buttonObj = Instantiate(actionCardButtonPrefab, actionCardContainer);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
        {
            string effectText = "";
            if (actionCard.canTargetPlayer && actionCard.playerEffect != 0)
                effectText += $"P:{actionCard.playerEffect:+0;-#} ";
            if (actionCard.canTargetFish && actionCard.fishEffect != 0)
                effectText += $"F:{actionCard.fishEffect:+0;-#}";
            
            buttonText.text = $"{actionCard.actionName}\n{effectText}";
        }
        
        if (button != null)
        {
            button.onClick.AddListener(() => SelectActionCard(actionCard));
        }
        
        actionCardButtons.Add(buttonObj);
    }
    
    void ClearActionCards()
    {
        foreach (GameObject button in actionCardButtons)
        {
            if (button != null)
                Destroy(button);
        }
        actionCardButtons.Clear();
    }
    
    void SelectActionCard(ActionCard actionCard)
    {
        selectedActionCard = actionCard;
        Debug.Log($"Selected action card: {actionCard.actionName}");
        
        // Update target buttons based on what this card can target
        UpdateTargetButtons();
    }
    
    void SetTarget(bool targetPlayer)
    {
        targetingPlayer = targetPlayer;
        UpdateTargetButtons();
        
        // If we have a selected card, try to play it
        if (selectedActionCard != null)
        {
            PlaySelectedCard();
        }
    }
    
    void PlaySelectedCard()
    {
        if (selectedActionCard == null || fishingManager == null) return;
        
        bool success = fishingManager.PlayActionCard(selectedActionCard, targetingPlayer);
        
        if (success)
        {
            Debug.Log($"Successfully played {selectedActionCard.actionName}!");
            
            // TODO: Remove card from player's inventory
            // For now, just clear selection
            selectedActionCard = null;
            UpdateTargetButtons();
        }
    }
    
    void UpdateTargetButtons()
    {
        if (selectedActionCard == null)
        {
            // No card selected - disable target buttons
            if (targetPlayerButton != null)
                targetPlayerButton.interactable = false;
            if (targetFishButton != null)
                targetFishButton.interactable = false;
            return;
        }
        
        // Enable/disable based on what the selected card can target
        if (targetPlayerButton != null)
        {
            targetPlayerButton.interactable = selectedActionCard.canTargetPlayer;
            
            // Highlight if currently targeting player
            ColorBlock colors = targetPlayerButton.colors;
            colors.normalColor = targetingPlayer ? Color.green : Color.white;
            targetPlayerButton.colors = colors;
        }
        
        if (targetFishButton != null)
        {
            targetFishButton.interactable = selectedActionCard.canTargetFish;
            
            // Highlight if currently targeting fish
            ColorBlock colors = targetFishButton.colors;
            colors.normalColor = !targetingPlayer ? Color.green : Color.white;
            targetFishButton.colors = colors;
        }
    }
    
    public void UpdateInteractiveUI()    {
        if (fishingManager == null) return;
        
        // Update round display
        if (roundText != null)
        {
            roundText.text = $"Round: {fishingManager.currentRound}";
        }
        
        // Update stamina displays
        if (playerStaminaText != null)
        {
            playerStaminaText.text = $"Player Stamina: {fishingManager.playerStamina}";
        }
        
        if (fishStaminaText != null)
        {
            fishStaminaText.text = $"Fish Stamina: {fishingManager.fishStamina}";
        }
        
        // Update power displays
        if (playerPowerText != null)
        {
            int basePower = fishingManager.CalculatePlayerPower();
            int totalPower = basePower + fishingManager.totalPlayerBuffs;
            playerPowerText.text = $"Player Power: {totalPower}";
            if (fishingManager.totalPlayerBuffs != 0)
                playerPowerText.text += $" ({basePower} + {fishingManager.totalPlayerBuffs:+0;-#})";
        }
        
        if (fishPowerText != null)
        {
            int basePower = fishingManager.CalculateFishPower();
            int totalPower = basePower + fishingManager.totalFishBuffs;
            fishPowerText.text = $"Fish Power: {totalPower}";
            if (fishingManager.totalFishBuffs != 0)
                fishPowerText.text += $" ({basePower} + {fishingManager.totalFishBuffs:+0;-#})";
        }
        
        // Update tug-of-war display
        UpdateTugOfWarDisplay();
        
        // Show interactive panel if phase is active
        if (!interactivePanel.activeInHierarchy)
        {
            ShowInteractivePhase();
        }
    }
    
    void UpdateFishInfo()
    {
        if (fishingManager == null || fishingManager.currentFish == null) return;
        
        if (fishNameText != null)
        {
            fishNameText.text = $"Fishing for: {fishingManager.currentFish.fishName}";
        }
    }
    
    void NextRound()
    {
        if (fishingManager != null)
        {
            fishingManager.NextRound();
        }
    }
    
    void SkipAction()
    {
        Debug.Log("Player skipped their action this round");
        // For now, just advance to next round
        // Later we might want to track who skipped for auto-win logic
        NextRound();
    }
    
    // Called when interactive phase ends
    public void OnInteractivePhaseEnd()
    {
        HideInteractivePhase();
    }
    
    void InitializeTugOfWarBar()
    {
        if (tugOfWarBar != null && fishingManager != null)
        {
            // Initialize with starting values (100 stamina each, current power difference)
            int playerPower = fishingManager.CalculatePlayerPower() + fishingManager.totalPlayerBuffs;
            int fishPower = fishingManager.CalculateFishPower() + fishingManager.totalFishBuffs;
            int powerDifference = playerPower - fishPower;
            
            tugOfWarBar.UpdateAll(fishingManager.playerStamina, fishingManager.fishStamina, powerDifference);
            
            Debug.Log($"Initialized tug-of-war bar: Player Power {playerPower}, Fish Power {fishPower}, Difference {powerDifference}");
        }
    }
    
    void UpdateTugOfWarDisplay()
    {
        if (tugOfWarBar != null && fishingManager != null)
        {
            // Calculate current powers including action card effects
            int playerPower = fishingManager.CalculatePlayerPower() + fishingManager.totalPlayerBuffs;
            int fishPower = fishingManager.CalculateFishPower() + fishingManager.totalFishBuffs;
            int powerDifference = playerPower - fishPower;
            
            // Update the tug-of-war display
            tugOfWarBar.UpdateAll(fishingManager.playerStamina, fishingManager.fishStamina, powerDifference);
        }
    }
}