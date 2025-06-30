using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractivePhaseUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject interactivePanel;
    public Transform actionCardContainer;
    
    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerPowerText;
    public TextMeshProUGUI fishPowerText;
    public TextMeshProUGUI fishNameText;
    public Button endPhaseButton;
    
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
        if (endPhaseButton != null)
            endPhaseButton.onClick.AddListener(EndPhase);
        
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
    
    void UpdateInteractiveUI()
    {
        if (fishingManager == null) return;
        
        // Update timer
        if (timerText != null)
        {
            timerText.text = $"Time: {fishingManager.timeRemaining:F1}s";
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
    
    void EndPhase()
    {
        if (fishingManager != null)
        {
            fishingManager.ForceEndInteractivePhase();
        }
    }
    
    // Called when interactive phase ends
    public void OnInteractivePhaseEnd()
    {
        HideInteractivePhase();
    }
}