using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;


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

    [Header("Action Card Drop Zones")]
    public ActionCardDropZone targetPlayerDropZone;
    public ActionCardDropZone targetFishDropZone;
    [Header("Multiplayer Action Card Limits")]
    public int maxCardsPerPlayerPerTurn = 3;
    public bool allowMultiplayerInterference = true;
    // Add these new variables at the end of your variable declarations
    private Dictionary<ulong, int> playerCardCounts = new Dictionary<ulong, int>();
    private ulong currentFishingPlayer = 0;

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

        if (tugOfWarBar != null)
            tugOfWarBar.gameObject.SetActive(false);
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

        // Show the tug-of-war bar when interactive phase starts
        if (tugOfWarBar != null)
            tugOfWarBar.gameObject.SetActive(true);

        // NEW: Track who is fishing and reset card counts
        if (NetworkManager.Singleton != null)
        {
            currentFishingPlayer = NetworkManager.Singleton.LocalClientId;
            playerCardCounts.Clear();
            // Debug.Log($"Interactive phase started - Fishing player: {currentFishingPlayer}");
        }

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

        // Hide the tug-of-war bar when interactive phase ends
        if (tugOfWarBar != null)
            tugOfWarBar.gameObject.SetActive(false);

        ClearActionCards();
    }

    void SetupActionCards()
{
    ClearActionCards();
    
    // Action cards are now handled by InventoryDisplay system
    // Just make sure the inventory display is showing current cards
    InventoryDisplay inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
    if (inventoryDisplay != null)
    {
        inventoryDisplay.RefreshDisplay();
        // Debug.Log("Refreshed inventory display for interactive phase");
    }
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
        // Debug.Log($"Selected action card: {actionCard.actionName}");

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
            // Debug.Log($"Successfully played {selectedActionCard.actionName}!");

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

    public void UpdateInteractiveUI()
    {
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
        // Debug.Log("Player skipped their action this round");
        // For now, just advance to next round
        // Later we might want to track who skipped for auto-win logic
        NextRound();
    }

    // Called when interactive phase ends
    public void OnInteractivePhaseEnd()
    {
        // Clear played action cards BEFORE hiding the phase
        // Debug.Log("Clearing played action cards from interactive phase end...");

        if (targetPlayerDropZone != null)
        {
            // Debug.Log("Clearing cards from Target Player drop zone");
            targetPlayerDropZone.ClearPlayedCards();
        }
        else
        {
            // Debug.Log("Target Player drop zone is null!");
        }

        if (targetFishDropZone != null)
        {
            // Debug.Log("Clearing cards from Target Fish drop zone");
            targetFishDropZone.ClearPlayedCards();
        }
        else
        {
            // Debug.Log("Target Fish drop zone is null!");
        }

        // Debug.Log("Finished clearing played action cards");

        // Now hide the phase
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

            // Debug.Log($"Initialized tug-of-war bar: Player Power {playerPower}, Fish Power {fishPower}, Difference {powerDifference}");
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
    // NEW: Multiplayer card limit methods
public bool CanPlayerPlayMoreCards(ulong playerId)
{
    if (!allowMultiplayerInterference)
        return false;
    
    // Get current count for this player
    if (!playerCardCounts.ContainsKey(playerId))
    {
        playerCardCounts[playerId] = 0;
    }
    
    bool canPlay = playerCardCounts[playerId] < maxCardsPerPlayerPerTurn;
    // Debug.Log($"Player {playerId} has played {playerCardCounts[playerId]}/{maxCardsPerPlayerPerTurn} cards this turn. Can play more: {canPlay}");
    
    return canPlay;
}

public void RecordCardPlayed(ulong playerId)
{
    if (!playerCardCounts.ContainsKey(playerId))
    {
        playerCardCounts[playerId] = 0;
    }
    
    playerCardCounts[playerId]++;
    // Debug.Log($"Player {playerId} has now played {playerCardCounts[playerId]}/{maxCardsPerPlayerPerTurn} cards this turn");
}
}