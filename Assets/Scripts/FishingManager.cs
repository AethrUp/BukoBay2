using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class FishingManager : NetworkBehaviour
{
    [Header("Game References")]
    public PlayerInventory currentPlayer;

    [Header("Fishing Setup")]
    public int minCastDepth = 1;
    public int maxCastDepth = 5;
    public FishCard currentFish;
    public int castDepth;

    [Header("Power Calculation")]
    public int totalPlayerPower;
    public int requiredMinDepth;
    public int requiredMaxDepth;

    [Header("Fish Database")]
    public List<FishCard> allFishCards = new List<FishCard>();

    [Header("Round-Based Battle System")]
    public bool isInteractionPhase = false;
    public int currentRound = 0;
    public int playerStamina = 100;
    public int fishStamina = 100;
    public float staminaDrainRate = 1f; // Points per second
    private float lastStaminaUpdate;

    [Header("Action Card Effects")]
    public int totalPlayerBuffs = 0;
    public int totalFishBuffs = 0;
    public List<string> appliedEffects = new List<string>();

    [Header("Battle State")]
    public bool battleEnded = false;

    [Header("UI References")]
    public InteractivePhaseUI interactiveUI;
    public FishingResultsManager resultsManager;
    [Header("Turn Tracking")]
public NetworkVariable<ulong> currentFishingPlayerId = new NetworkVariable<ulong>(0);

    void Start()
    {
        // Debug.Log("FishingManager Start() called");

        LoadAllFishCards();
        // Debug: Check what PlayerInventory objects exist
        PlayerInventory[] allInventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        // Debug.Log($"Found {allInventories.Length} PlayerInventory objects in scene");
        // Debug.Log($"PlayerInventory.Instance = {(PlayerInventory.Instance != null ? PlayerInventory.Instance.name : "NULL")}");

        // Find the persistent PlayerInventory if not assigned
        if (currentPlayer == null)
        {
            currentPlayer = FindFirstObjectByType<PlayerInventory>();
            if (currentPlayer == null && PlayerInventory.Instance != null)
            {
                currentPlayer = PlayerInventory.Instance;
            }
            // Debug.Log($"FishingManager found PlayerInventory: {(currentPlayer != null ? currentPlayer.name : "NOT FOUND")}");
        }
    }

    void LoadAllFishCards()
    {
        // Find all FishCard assets in the project
#if UNITY_EDITOR
        string[] fishGuids = UnityEditor.AssetDatabase.FindAssets("t:FishCard");
        
        allFishCards.Clear();
        
        foreach (string guid in fishGuids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            FishCard fish = UnityEditor.AssetDatabase.LoadAssetAtPath<FishCard>(assetPath);
            
            if (fish != null)
            {
                allFishCards.Add(fish);
                // Debug.Log($"Loaded fish: {fish.fishName} at main depth {fish.mainDepth}, sub depth {fish.subDepth}");
            }
        }
#endif

        // Debug.Log($"Loaded {allFishCards.Count} fish cards total");
    }

    public void SetupFishing()
    {
        currentPlayer = FindFirstObjectByType<PlayerInventory>();

        if (currentPlayer == null) return;

        // Count equipped gear pieces
        int gearCount = CountEquippedGear();

        // Calculate required depth based on gear count
        CalculateRequiredDepthFromGearCount(gearCount);

        // Debug.Log($"Player has {gearCount} gear pieces");
        // Debug.Log($"Must cast at depth {requiredMinDepth}");
    }

    int CountEquippedGear()
    {
        int count = 0;

        if (currentPlayer.equippedRod != null) count++;
        if (currentPlayer.equippedReel != null) count++;
        if (currentPlayer.equippedLine != null) count++;
        if (currentPlayer.equippedLure != null) count++;
        if (currentPlayer.equippedBait != null) count++;
        if (currentPlayer.equippedExtra1 != null) count++;
        if (currentPlayer.equippedExtra2 != null) count++;

        return count;
    }

    void CalculateRequiredDepthFromGearCount(int gearCount)
    {
        // New gear-count-based depth requirements:
        // 2+ pieces: Coast only (depth 1)
        // 3+ pieces: Ocean only (depth 2) 
        // 4+ pieces: Abyss only (depth 3)

        if (gearCount < 2)
        {
            requiredMinDepth = 0; // Cannot fish anywhere
            requiredMaxDepth = 0;
        }
        else if (gearCount == 2)
        {
            requiredMinDepth = 1; // Coast only
            requiredMaxDepth = 1;
        }
        else if (gearCount == 3)
        {
            requiredMinDepth = 2; // Ocean only
            requiredMaxDepth = 2;
        }
        else // 4+ pieces
        {
            requiredMinDepth = 3; // Abyss only
            requiredMaxDepth = 3;
        }
    }

    public bool CanCastAtDepth(int depth)
    {
        return depth >= requiredMinDepth && depth <= requiredMaxDepth;
    }

    public void CastAtDepth(int depth)
    {
        if (!CanCastAtDepth(depth))
        {
            // Debug.LogWarning($"Cannot cast at depth {depth}. Must cast at depth {requiredMinDepth}");
            return;
        }

        castDepth = depth;

        // Handle networking
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Host picks the fish themselves
                currentFish = GetRandomFishAtDepth(depth);

                if (currentFish != null)
                {
                    // Debug.Log($"Host selected fish: {currentFish.fishName} at depth {depth}");

                    // Tell all clients which fish was selected and start battle
                    StartBattleWithSpecificFishServerRpc(currentFish.fishName, currentFish.power, currentFish.coins, currentFish.treasures, depth);
                }
                else
                {
                    Debug.LogWarning($"Host: No fish found at depth {depth}!");
                }
            }
            else
            {
                // CLIENT: Ask the host to pick a fish for me
                Debug.Log($"Client: Asking host to pick fish at depth {depth}...");
                RequestFishFromHostServerRpc(depth);
            }
        }
        else
        {
            // Fallback for single player
            currentFish = GetRandomFishAtDepth(depth);

            if (currentFish != null)
            {
                Debug.Log($"Single player: Cast at depth {depth}! A {currentFish.fishName} appears!");
                StartRoundBasedBattle();
            }
            else
            {
                Debug.LogWarning($"No fish found at depth {depth}!");
            }
        }
    }

    void StartRoundBasedBattle()
    {
        // Check if we should use networked version or single-player version
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // Networked version - delegate to the networked method
            StartRoundBasedBattleForAllClients();
        }
        else
        {
            // Single-player fallback
            battleEnded = false;

            Debug.Log($"=== SINGLE-PLAYER BATTLE STARTED ===");
            Debug.Log($"Fighting {currentFish.fishName} (Power: {currentFish.power})");

            // Reset battle state
            currentRound = 1;
            playerStamina = 100;
            fishStamina = 100;
            totalPlayerBuffs = 0;
            totalFishBuffs = 0;
            appliedEffects.Clear();

            // Clear old cards locally
            ClearAllActionCards();

            // Start first round (old single-player method)
            StartNewRound();
        }
    }
    [ClientRpc]
    void StartInteractivePhaseForAllPlayersClientRpc(string fishName, int fishPower, int playerStamina, int fishStamina, int playerPower, int totalPlayerBuffs, int totalFishBuffs)
    {
        Debug.Log($"=== CLIENT RPC RECEIVED ===");
        Debug.Log($"All players notified: {fishName} battle started (Power: {fishPower})");
        Debug.Log($"My Client ID: {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"InteractiveUI is: {(interactiveUI != null ? "FOUND" : "NULL")}");

        // Update the battle state for all players
        if (currentFish == null)
        {
            // Create a temporary fish object for non-fishing players
            currentFish = ScriptableObject.CreateInstance<FishCard>();
            currentFish.fishName = fishName;
            currentFish.power = fishPower;
        }

        // Sync battle state
        this.playerStamina = playerStamina;
        this.fishStamina = fishStamina;
        this.totalPlayerBuffs = totalPlayerBuffs;
        this.totalFishBuffs = totalFishBuffs;

        // IMPORTANT: Set interactive phase for ALL clients
        this.isInteractionPhase = true;

        // Show interactive UI for ALL players
        if (interactiveUI != null)
        {
            Debug.Log("Calling ShowInteractivePhase() for this client");
            interactiveUI.ShowInteractivePhase();
        }
        else
        {
            Debug.LogError("InteractiveUI is null! Cannot show interactive phase.");
        }
    }

    void StartNewRound()
    {
        Debug.Log($"=== ROUND {currentRound} STARTED ===");
        Debug.Log($"Players can now play action cards or skip their turn");

        // Start interactive phase for this round
        isInteractionPhase = true;
        lastStaminaUpdate = Time.time;

        // ALWAYS show UI for all players via network (not just when host)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"=== SENDING RPC TO ALL PLAYERS ===");
            Debug.Log($"NetworkManager found, IsListening: {NetworkManager.Singleton.IsListening}");
            Debug.Log($"IsHost: {NetworkManager.Singleton.IsHost}");
            Debug.Log($"IsClient: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

            int playerPower = CalculatePlayerPower();

            // Clear old action cards for ALL players BEFORE starting new round
            ClearAllActionCardsClientRpc();

            // Then show the new interactive phase
            StartInteractivePhaseForAllPlayersClientRpc(currentFish.fishName, currentFish.power, playerStamina, fishStamina, playerPower, totalPlayerBuffs, totalFishBuffs);
            Debug.Log("RPC sent to all players!");
        }
        else
        {
            // Fallback for single player
            Debug.Log("=== FALLBACK TO LOCAL ===");
            ClearAllActionCards();
            if (interactiveUI != null)
            {
                interactiveUI.ShowInteractivePhase();
            }
        }

        // Check for auto-win conditions
        CheckAutoWinConditions();
    }

    void CheckAutoWinConditions()
    {
        int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
        int fishPower = CalculateFishPower() + totalFishBuffs;

        Debug.Log($"Checking auto-win: Player Power {playerPower} vs Fish Power {fishPower}");

        // For now, we'll implement the basic check
        // Later we'll add logic for other players helping/hindering

        if (playerPower > fishPower)
        {
            Debug.Log("Player power is higher - potential auto-win if no hindering");
            // TODO: Check if other players want to hinder
        }
        else if (playerPower <= fishPower)
        {
            Debug.Log("Fish power is equal or higher - potential fish win if no helping");
            // TODO: Check if other players want to help or if active player has actions
        }
    }

    // Public function to advance to next round (called by UI button)
    public void NextRound()
    {
        if (!isInteractionPhase)
        {
            Debug.LogWarning("Cannot advance round - not in interaction phase!");
            return;
        }

        EndCurrentRound();
    }

    void EndCurrentRound()
    {
        Debug.Log($"=== ENDING ROUND {currentRound} ===");
        isInteractionPhase = false;

        // Calculate final powers for this round
        int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
        int fishPower = CalculateFishPower() + totalFishBuffs;

        Debug.Log($"Round {currentRound} final powers: Player {playerPower} vs Fish {fishPower}");

        // Apply stamina damage
        if (playerPower > fishPower)
        {
            int damage = playerPower - fishPower;
            fishStamina -= damage;
            //Debug.Log($"Fish takes {damage} damage! Fish stamina: {fishStamina}");
        }
        else if (fishPower > playerPower)
        {
            int damage = fishPower - playerPower;
            playerStamina -= damage;
            ////Debug.Log($"Player takes {damage} damage! Player stamina: {playerStamina}");
        }
        else
        {
            Debug.Log("Powers are equal - no damage dealt this round");
        }

        // Check for battle end
        if (playerStamina <= 0)
        {
            HandleFailure();
            return;
        }
        else if (fishStamina <= 0)
        {
            HandleSuccess();
            return;
        }

        // Battle continues - start next round
        currentRound++;

        // Reset action card effects for next round
        totalPlayerBuffs = 0;
        totalFishBuffs = 0;
        appliedEffects.Clear();

        // Small delay then start next round
        Invoke("StartNewRound", 1f);
    }

    FishCard GetRandomFishAtDepth(int depth)
    {
        List<FishCard> fishAtDepth = new List<FishCard>();

        // Find all fish that live at this main depth
        foreach (FishCard fish in allFishCards)
        {
            if (fish.mainDepth == depth)
            {
                fishAtDepth.Add(fish);
            }
        }

        Debug.Log($"Found {fishAtDepth.Count} fish at main depth {depth}");

        // Return random fish from this depth
        if (fishAtDepth.Count > 0)
        {
            int randomIndex = Random.Range(0, fishAtDepth.Count);
            return fishAtDepth[randomIndex];
        }

        return null;
    }

    void CalculateFishingPowers()
    {
        // This method is for detailed debugging only
        int playerPower = CalculatePlayerPower();
        int fishPower = CalculateFishPower();

        //Debug.Log($"=== POWER CALCULATION ===");
        //Debug.Log($"Player Power: {playerPower}");
        //Debug.Log($"Fish Power: {fishPower}");

        if (playerPower >= fishPower)
        {
            //Debug.Log($"Player advantage by {playerPower - fishPower}");
        }
        else
        {
            //Debug.Log($"Fish advantage by {fishPower - playerPower}");
        }
    }

    // Make these public so UI can access them
    public int CalculatePlayerPower()
    {
       // //Debug.Log($"CalculatePlayerPower called. currentPlayer = {(currentPlayer != null ? currentPlayer.name : "NULL")}");
        if (currentPlayer == null || currentFish == null) return 0;

        // Start with base gear power
        int basePower = currentPlayer.GetTotalPower();

        ////Debug.Log($"GetTotalPower() returned: {basePower}");

        // Apply material bonuses/penalties from fish
        int materialModifier = CalculateMaterialModifier();

        // Apply sub-depth gear effectiveness
        int subDepthModifier = CalculateSubDepthGearModifier();

        int finalPower = basePower + materialModifier + subDepthModifier;

        return finalPower;
    }

    int CalculateMaterialModifier()
    {
        if (currentPlayer == null || currentFish == null) return 0;

        int totalModifier = 0;

        // Check each equipped gear piece for material bonuses
        totalModifier += CheckGearMaterial(currentPlayer.equippedRod, "Rod");
        totalModifier += CheckGearMaterial(currentPlayer.equippedReel, "Reel");
        totalModifier += CheckGearMaterial(currentPlayer.equippedLine, "Line");
        totalModifier += CheckGearMaterial(currentPlayer.equippedLure, "Lure");
        totalModifier += CheckGearMaterial(currentPlayer.equippedBait, "Bait");
        totalModifier += CheckGearMaterial(currentPlayer.equippedExtra1, "Extra1");
        totalModifier += CheckGearMaterial(currentPlayer.equippedExtra2, "Extra2");

        return totalModifier;
    }

    int CheckGearMaterial(GearCard gear, string slotName)
    {
        if (gear == null) return 0;

        // Get the material modifier from the fish for this gear's material
        int modifier = currentFish.GetMaterialModifier(gear.material);

        if (modifier != 0)
        {
            Debug.Log($"{slotName} ({gear.gearName}): {gear.material} material gives {modifier:+0;-#} vs {currentFish.fishName}");
        }

        return modifier;
    }

    int CalculateSubDepthGearModifier()
    {
        if (currentPlayer == null || currentFish == null) return 0;

        int totalModifier = 0;
        int subDepth = currentFish.subDepth;

        // Check each equipped gear piece for sub-depth effectiveness
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedRod, "Rod", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedReel, "Reel", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedLine, "Line", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedLure, "Lure", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedBait, "Bait", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedExtra1, "Extra1", subDepth);
        totalModifier += CheckGearSubDepthEffectiveness(currentPlayer.equippedExtra2, "Extra2", subDepth);

        return totalModifier;
    }

    int CheckGearSubDepthEffectiveness(GearCard gear, string slotName, int subDepth)
    {
        if (gear == null) return 0;

        // For now, use the existing depth effects from gear (depth1Effect, depth2Effect, etc.)
        // We'll map sub-depths to these effects until we update the gear cards

        int modifier = 0;

        // Map sub-depth (1-9) to gear depth effects (1-5)
        // This is temporary until we update gear cards for sub-depth system
        if (subDepth >= 1 && subDepth <= 2)
            modifier = gear.depth1Effect;  // Very shallow
        else if (subDepth >= 3 && subDepth <= 4)
            modifier = gear.depth2Effect;  // Shallow
        else if (subDepth >= 5 && subDepth <= 6)
            modifier = gear.depth3Effect;  // Medium
        else if (subDepth >= 7 && subDepth <= 8)
            modifier = gear.depth4Effect;  // Deep
        else if (subDepth == 9)
            modifier = gear.depth5Effect;  // Very deep

        return modifier;
    }

    public int CalculateFishPower()
    {
        if (currentFish == null) return 0;

        // Fish power is just their base power
        // Sub-depth affects gear effectiveness, not fish difficulty
        int fishPower = currentFish.power;

        return fishPower;
    }

    void Update()
    {
        // Handle continuous stamina drain during interaction phase
        if (isInteractionPhase)
        {
            UpdateStaminaDrain();
        }
    }

    void UpdateStaminaDrain()
    {
        if (battleEnded) return; // Don't continue if battle already ended

        // Calculate how much time has passed since last update
        float deltaTime = Time.time - lastStaminaUpdate;

        if (deltaTime >= staminaDrainRate) // Update every second (or whatever drain rate)
        {
            // Calculate current power difference
            int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
            int fishPower = CalculateFishPower() + totalFishBuffs;
            int powerDifference = playerPower - fishPower;

            ////Debug.Log($"Stamina drain update: Player Power {playerPower} vs Fish Power {fishPower}, Difference: {powerDifference}");

            // Apply stamina damage based on power difference
            if (powerDifference > 0)
            {
                // Player is winning - fish loses stamina
                fishStamina -= Mathf.Abs(powerDifference);
                //Debug.Log($"Fish takes {Mathf.Abs(powerDifference)} damage! Fish stamina: {fishStamina}");
            }
            else if (powerDifference < 0)
            {
                // Fish is winning - player loses stamina
                playerStamina -= Mathf.Abs(powerDifference);
                ////Debug.Log($"Player takes {Mathf.Abs(powerDifference)} damage! Player stamina: {playerStamina}");
            }
            else
            {
                //Debug.Log("Equal power - no damage dealt");
            }

            // Clamp stamina values
            playerStamina = Mathf.Clamp(playerStamina, 0, 100);
            fishStamina = Mathf.Clamp(fishStamina, 0, 100);

            // Send stamina updates to all clients
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                UpdateStaminaForAllPlayersClientRpc(playerStamina, fishStamina, totalPlayerBuffs, totalFishBuffs);
            }

            // UPDATED: Network-synchronized battle end checks
            if (playerStamina <= 0 && !battleEnded)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    EndBattleServerRpc(false); // false = failure
                }
                return;
            }
            else if (fishStamina <= 0 && !battleEnded)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    EndBattleServerRpc(true); // true = success
                }
                return;
            }

            // Update timer for next drain
            lastStaminaUpdate = Time.time;
        }
    }

    void HandleSuccess()
{
    Debug.Log($"SUCCESS! Player catches {currentFish.fishName}!");
    Debug.Log($"Received {currentFish.coins} coins!");

    // IMPORTANT: Only the original fishing player gets coins
    bool isOriginalFishingPlayer = false;
    if (NetworkManager.Singleton != null)
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        isOriginalFishingPlayer = (myClientId == currentFishingPlayerId.Value);
        Debug.Log($"Checking coin rewards: My ID {myClientId}, Fishing Player ID {currentFishingPlayerId}, Am I fishing player? {isOriginalFishingPlayer}");
    }
    else
    {
        // Fallback for single player
        isOriginalFishingPlayer = true;
        Debug.Log("Single player mode - giving coins");
    }

  // Only give coins to the fishing player
if (isOriginalFishingPlayer)
{
    Debug.Log("I AM the fishing player - attempting to give coins");
    // Use the persistent PlayerInventory.Instance instead of finding any random one
    if (PlayerInventory.Instance != null)
    {
        int coinsBefore = PlayerInventory.Instance.coins;
        PlayerInventory.Instance.AddCoins(currentFish.coins);
        int coinsAfter = PlayerInventory.Instance.coins;
        Debug.Log($"COINS UPDATED: {coinsBefore} -> {coinsAfter} (added {currentFish.coins})");
    }
    else
    {
        Debug.LogError("Could not find PlayerInventory.Instance!");
    }
}
    else
    {
        Debug.Log("I am NOT the fishing player - no coins for me");
        
        // ADD THIS DEBUG FOR NON-FISHING PLAYER TOO:
        Debug.Log($"Non-fishing player sees PlayerInventory.Instance.coins = {PlayerInventory.Instance.coins}");
    }

    // Hide interactive UI for ALL players
    if (interactiveUI != null)
    {
        interactiveUI.OnInteractivePhaseEnd();
    }

    // Hide the fish card panel for ALL players
    FishingUI fishingUI = FindFirstObjectByType<FishingUI>();
    if (fishingUI != null)
    {
        fishingUI.HideFishCard();
    }

    // Show results screen for ALL players
    if (resultsManager != null)
    {
        resultsManager.ShowResults(true, currentFish, currentFish.coins, "");
    }

    // Reset fishing state
    isInteractionPhase = false;

    // Clear played action cards for ALL players
    ActionCardDropZone[] successDropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
    foreach (ActionCardDropZone dropZone in successDropZones)
    {
        dropZone.ClearPlayedCards();
    }

    Debug.Log("Fishing phase ended successfully!");

    // Advance to next player's turn (only host should do this)
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
    {
        NetworkGameManager turnManager = FindFirstObjectByType<NetworkGameManager>();
        if (turnManager != null)
        {
            turnManager.NextTurnServerRpc();
            Debug.Log("Advanced to next player's turn after fishing success");
        }
    }
}

    void HandleFailure()
    {
        Debug.Log("*** HANDLE FAILURE CALLED ***");

        string damageReport = "";

        // Apply damage ONLY to the original fishing player
        bool isOriginalFishingPlayer = false;
        if (NetworkManager.Singleton != null)
        {
            ulong myClientId = NetworkManager.Singleton.LocalClientId;
            isOriginalFishingPlayer = (myClientId == currentFishingPlayerId.Value);
            Debug.Log($"Checking gear damage: My ID {myClientId}, Fishing Player ID {currentFishingPlayerId}, Am I fishing player? {isOriginalFishingPlayer}");
        }
        else
        {
            // Fallback for single player
            isOriginalFishingPlayer = true;
        }

        // Only apply gear damage to the fishing player
        if (isOriginalFishingPlayer && currentPlayer != null && currentFish != null)
        {
            Debug.Log("Applying gear damage to fishing player...");
            damageReport = ApplyGearDamage();
            Debug.Log($"Damage report: '{damageReport}'");

            // Refresh the inventory display to show updated durability
            InventoryDisplay inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
            {
                inventoryDisplay.RefreshDisplay();
                Debug.Log("Refreshed inventory display to show gear damage");
            }
        }
        else
        {
            Debug.Log("Not the fishing player - no gear damage applied");
            damageReport = "No damage (not fishing player)";
        }

        Debug.Log($"FAILURE! Player fails to catch {currentFish.fishName}!");
        Debug.Log("Gear takes damage...");

        // Hide interactive UI for ALL players
        if (interactiveUI != null)
        {
            interactiveUI.OnInteractivePhaseEnd();
        }

        // Hide the fish card panel for ALL players
        FishingUI fishingUI = FindFirstObjectByType<FishingUI>();
        if (fishingUI != null)
        {
            fishingUI.HideFishCard();
        }

        // Show results screen for ALL players
        if (resultsManager != null)
        {
            Debug.Log($"Calling resultsManager.ShowResults with damage report: '{damageReport}'");
            resultsManager.ShowResults(false, currentFish, 0, damageReport);
        }
        else
        {
            Debug.Log("ResultsManager is null!");
        }

        // Reset fishing state
        isInteractionPhase = false;

        // Clear played action cards for ALL players
        ActionCardDropZone[] failureDropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
        foreach (ActionCardDropZone dropZone in failureDropZones)
        {
            dropZone.ClearPlayedCards();
        }

        Debug.Log("Fishing phase ended in failure!");

        // Advance to next player's turn (only host should do this)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkGameManager turnManager = FindFirstObjectByType<NetworkGameManager>();
            if (turnManager != null)
            {
                turnManager.NextTurnServerRpc();
                Debug.Log("Advanced to next player's turn after fishing failure");
            }
        }
    }

    string ApplyGearDamage()
    {
        Debug.Log("=== APPLYING GEAR DAMAGE ===");

        // Get all equipped gear pieces in order
        List<GearCard> equippedGear = GetAllEquippedGear();

        if (equippedGear.Count == 0)
        {
            Debug.Log("No gear equipped to damage!");
            return "No gear to damage";
        }

        Debug.Log($"Player has {equippedGear.Count} gear pieces equipped");

        // Track damage for the report
        List<string> damageMessages = new List<string>();

        // Apply damage based on fish's gear damage values
        int[] fishDamageValues = {
        currentFish.gear1Damage,
        currentFish.gear2Damage,
        currentFish.gear3Damage,
        currentFish.gear4Damage,
        currentFish.gear5Damage
    };

        // Apply damage to each gear piece in order
        for (int i = 0; i < equippedGear.Count && i < fishDamageValues.Length; i++)
        {
            int damageAmount = fishDamageValues[i];

            if (damageAmount > 0)
            {
                GearCard targetGear = equippedGear[i];
                string damageResult = DamageGearPiece(targetGear, damageAmount);
                damageMessages.Add(damageResult);
            }
            else
            {
                Debug.Log($"Gear slot {i + 1}: No damage (0 damage)");
            }
        }

        Debug.Log("=== GEAR DAMAGE COMPLETE ===");

        // Create damage report
        if (damageMessages.Count == 0)
        {
            return "No damage dealt";
        }
        else
        {
            return string.Join(", ", damageMessages);
        }
    }

    List<GearCard> GetAllEquippedGear()
    {
        List<GearCard> gearList = new List<GearCard>();

        if (currentPlayer.equippedRod != null) gearList.Add(currentPlayer.equippedRod);
        if (currentPlayer.equippedReel != null) gearList.Add(currentPlayer.equippedReel);
        if (currentPlayer.equippedLine != null) gearList.Add(currentPlayer.equippedLine);
        if (currentPlayer.equippedLure != null) gearList.Add(currentPlayer.equippedLure);
        if (currentPlayer.equippedBait != null) gearList.Add(currentPlayer.equippedBait);
        if (currentPlayer.equippedExtra1 != null) gearList.Add(currentPlayer.equippedExtra1);
        if (currentPlayer.equippedExtra2 != null) gearList.Add(currentPlayer.equippedExtra2);

        return gearList;
    }

    string DamageGearPiece(GearCard gear, int damageAmount)
    {
        // Check for protection first
        if (gear.hasProtection)
        {
            Debug.Log($"🛡️ {gear.gearName} protection activated! Blocking {damageAmount} damage from {gear.protectionType}");

            // Remove the protection (it's been used)
            gear.hasProtection = false;
            string protectionUsed = gear.protectionType;
            gear.protectionType = "";

            // Update the display to remove the shield icon
            UpdateGearDisplay(gear);

            Debug.Log($"Protection from {protectionUsed} consumed. {gear.gearName} takes no damage.");

            // ADD DEBUG LINE:
            string protectedResult = $"{gear.gearName} PROTECTED";
            Debug.Log($"Returning damage result: '{protectedResult}'");
            return protectedResult;
        }

        // No protection - apply damage normally
        int originalDurability = gear.durability;
        gear.durability = Mathf.Max(0, gear.durability - damageAmount);

        Debug.Log($"Damaged {gear.gearName}: {originalDurability} → {gear.durability} durability (-{damageAmount})");

        // Update the display to show new durability
        UpdateGearDisplay(gear);

        if (gear.durability <= 0)
        {
            Debug.Log($"⚠️ {gear.gearName} is BROKEN! (0 durability)");
            DestroyBrokenGear(gear);

            // ADD DEBUG LINE:
            string brokenResult = $"{gear.gearName} BROKEN";
            Debug.Log($"Returning damage result: '{brokenResult}'");
            return brokenResult;
        }
        else
        {
            // ADD DEBUG LINE:
            string damageResult = $"{gear.gearName} -{damageAmount}";
            Debug.Log($"Returning damage result: '{damageResult}'");
            return damageResult;
        }
    }
    void UpdateGearDisplay(GearCard gear)
    {
        // Find and update all displays showing this gear
        CardDisplay[] allCardDisplays = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

        foreach (CardDisplay cardDisplay in allCardDisplays)
        {
            if (cardDisplay.gearCard == gear)
            {
                cardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    void DestroyBrokenGear(GearCard brokenGear)
    {
        if (currentPlayer == null || brokenGear == null) return;

        Debug.Log($"Destroying broken gear: {brokenGear.gearName}");

        // Remove from equipped slots
        if (currentPlayer.equippedRod == brokenGear)
        {
            currentPlayer.equippedRod = null;
            Debug.Log("Removed broken rod from equipped slot");
        }
        else if (currentPlayer.equippedReel == brokenGear)
        {
            currentPlayer.equippedReel = null;
            Debug.Log("Removed broken reel from equipped slot");
        }
        else if (currentPlayer.equippedLine == brokenGear)
        {
            currentPlayer.equippedLine = null;
            Debug.Log("Removed broken line from equipped slot");
        }
        else if (currentPlayer.equippedLure == brokenGear)
        {
            currentPlayer.equippedLure = null;
            Debug.Log("Removed broken lure from equipped slot");
        }
        else if (currentPlayer.equippedBait == brokenGear)
        {
            currentPlayer.equippedBait = null;
            Debug.Log("Removed broken bait from equipped slot");
        }
        else if (currentPlayer.equippedExtra1 == brokenGear)
        {
            currentPlayer.equippedExtra1 = null;
            Debug.Log("Removed broken gear from extra slot 1");
        }
        else if (currentPlayer.equippedExtra2 == brokenGear)
        {
            currentPlayer.equippedExtra2 = null;
            Debug.Log("Removed broken gear from extra slot 2");
        }

        // Also remove from tackle box if it's there
        if (currentPlayer.extraGear.Contains(brokenGear))
        {
            currentPlayer.extraGear.Remove(brokenGear);
            Debug.Log("Removed broken gear from tackle box");
        }

        Debug.Log($"Successfully destroyed {brokenGear.gearName}");
    }

    // Public function to play action cards during interactive phase
    // Public function to play action cards during interactive phase
    // Public function to play action cards during interactive phase
    public bool PlayActionCard(ActionCard actionCard, bool targetingPlayer)
    {
        if (!isInteractionPhase)
        {
            Debug.LogWarning("Cannot play action cards - not in interactive phase!");
            return false;
        }

        if (actionCard == null)
        {
            Debug.LogWarning("Invalid action card!");
            return false;
        }

        // Check if this player has reached their card limit
        if (NetworkManager.Singleton != null)
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Get the interactive UI to check limits
            InteractivePhaseUI interactiveUI = FindFirstObjectByType<InteractivePhaseUI>();
            if (interactiveUI != null)
            {
                if (!interactiveUI.CanPlayerPlayMoreCards(playerId))
                {
                    Debug.LogWarning($"Player {playerId} has reached their card limit this turn!");
                    return false;
                }
            }
        }

        // Check if card can target the chosen target
        if (targetingPlayer && !actionCard.canTargetPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }

        if (!targetingPlayer && !actionCard.canTargetFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }

        // NEW: Use network RPC to sync across all players
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;
            int effectValue = targetingPlayer ? actionCard.playerEffect : actionCard.fishEffect;

            Debug.Log($"Sending action card play to network: {actionCard.actionName}");
            PlayActionCardServerRpc(actionCard.actionName, targetingPlayer, actionCard.playerEffect, actionCard.fishEffect, playerId);
        }
        else
        {
            // Fallback for single player or when network isn't available
            Debug.Log("Network not available - applying action card locally");
            ApplyActionCardEffect(actionCard.actionName, targetingPlayer, actionCard.playerEffect, actionCard.fishEffect, 0);
        }

        return true;
    }
    [ServerRpc(RequireOwnership = false)]
public void PlayActionCardServerRpc(string cardName, bool targetingPlayer, int playerEffect, int fishEffect, ulong playerId)
{
    Debug.Log($"Server received action card play: {cardName} from player {playerId}");

    // Apply the effect ONLY on the server
    ApplyActionCardEffect(cardName, targetingPlayer, playerEffect, fishEffect, playerId);

    // Tell all clients about this card play (both effect AND visual)
    NotifyActionCardPlayedClientRpc(cardName, targetingPlayer, playerEffect, fishEffect, playerId);

    // Show the visual card for all players
    ShowPlayedActionCardClientRpc(cardName, targetingPlayer, playerId, playerEffect, fishEffect);
}

[ClientRpc]
public void NotifyActionCardPlayedClientRpc(string cardName, bool targetingPlayer, int playerEffect, int fishEffect, ulong playerId)
{
    Debug.Log($"All clients notified: Player {playerId} played {cardName}");

    // DO NOT apply the effect here - that's done on the server only
    // This RPC is just for notification/logging purposes
}
    [ClientRpc]
    public void UpdateStaminaForAllPlayersClientRpc(int newPlayerStamina, int newFishStamina, int playerBuffs, int fishBuffs)
    {
        Debug.Log($"=== STAMINA UPDATE RPC ===");
        Debug.Log($"Player stamina: {newPlayerStamina}, Fish stamina: {newFishStamina}");
        Debug.Log($"Player buffs: {playerBuffs}, Fish buffs: {fishBuffs}");

        // Update local values for all clients
        playerStamina = newPlayerStamina;
        fishStamina = newFishStamina;
        totalPlayerBuffs = playerBuffs;
        totalFishBuffs = fishBuffs;

        // Force update the tug-of-war display on all clients
        if (interactiveUI != null && interactiveUI.tugOfWarBar != null)
        {
            int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
            int fishPower = CalculateFishPower() + totalFishBuffs;
            int powerDifference = playerPower - fishPower;

            interactiveUI.tugOfWarBar.UpdateAll(playerStamina, fishStamina, powerDifference);
            Debug.Log($"Updated tug-of-war for all players: P{playerStamina} F{fishStamina} Diff{powerDifference}");
        }
    }
    [ClientRpc]
    public void ShowPlayedActionCardClientRpc(string cardName, bool targetsPlayer, ulong playerId, int playerEffect, int fishEffect)
    {
        Debug.Log($"All clients: Show {cardName} played by Player {playerId} targeting {(targetsPlayer ? "player" : "fish")}");

        // Find the correct drop zone
        ActionCardDropZone targetDropZone = null;
        ActionCardDropZone[] allDropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);

        foreach (ActionCardDropZone dropZone in allDropZones)
        {
            if (dropZone.targetsPlayer == targetsPlayer)
            {
                targetDropZone = dropZone;
                break;
            }
        }

        if (targetDropZone != null)
        {
            // Create the visual card for all players with more data
            targetDropZone.CreateNetworkedPlayedCard(cardName, playerEffect, fishEffect);
        }
    }
    [ClientRpc]
    public void ClearAllActionCardsClientRpc()
    {
        Debug.Log("=== CLEARING ALL ACTION CARDS FOR ALL PLAYERS ===");
        ClearAllActionCards();
    }

    void ClearAllActionCards()
    {
        ActionCardDropZone[] allDropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
        Debug.Log($"Found {allDropZones.Length} ActionCardDropZone components to clear");

        foreach (ActionCardDropZone dropZone in allDropZones)
        {
            if (dropZone != null)
            {
                Debug.Log($"Clearing cards from drop zone: {dropZone.name}");
                dropZone.ClearPlayedCards();
            }
        }

        Debug.Log("Finished clearing all action cards");
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBattleForAllPlayersServerRpc(string fishName, int fishPower, int depth)
    {
        Debug.Log($"Server: Starting battle for all players - {fishName}");

        // Host finds the fish and starts the battle
        currentFish = GetRandomFishAtDepth(depth);
        if (currentFish == null)
        {
            currentFish = ScriptableObject.CreateInstance<FishCard>();
            currentFish.fishName = fishName;
            currentFish.power = fishPower;
        }

        // Start battle on server
        StartRoundBasedBattleForAllClients();
    }
    // Add these new methods to your FishingManager class

    [ServerRpc(RequireOwnership = false)]
    public void EndBattleServerRpc(bool success)
    {
        if (battleEnded) return; // Prevent multiple calls

        battleEnded = true;
        Debug.Log($"Server: Battle ended with success = {success}");

        // Tell all clients the battle ended
        EndBattleForAllPlayersClientRpc(success);
    }

    // Add this new RPC method to your FishingManager class

    [ServerRpc(RequireOwnership = false)]
    public void StartBattleWithSpecificFishServerRpc(string fishName, int fishPower, int fishCoins, int fishTreasures, int depth)
    {
        Debug.Log($"Server: Starting battle with specific fish - {fishName} (Power: {fishPower}, Coins: {fishCoins}, Treasures: {fishTreasures})");

        // Create the fish object on server (host already has it, but let's be consistent)
        currentFish = GetFishByName(fishName);
        if (currentFish == null)
        {
            // Fallback: create a temporary fish object
            currentFish = ScriptableObject.CreateInstance<FishCard>();
            currentFish.fishName = fishName;
            currentFish.power = fishPower;
            currentFish.coins = fishCoins;
            currentFish.treasures = fishTreasures;
        }

        // Tell all clients about this specific fish and start the battle
        SynchronizeFishForAllPlayersClientRpc(fishName, fishPower, fishCoins, fishTreasures, depth);
    }

    // Add this new RPC method to your FishingManager class

    [ServerRpc(RequireOwnership = false)]
    public void RequestFishFromHostServerRpc(int depth)
    {
        Debug.Log($"Host received fish request for depth {depth}");

        // Host picks the fish
        currentFish = GetRandomFishAtDepth(depth);

        if (currentFish != null)
        {
            Debug.Log($"Host selected fish: {currentFish.fishName} at depth {depth} for client");

            // Tell all clients which fish was selected and start battle
            StartBattleWithSpecificFishServerRpc(currentFish.fishName, currentFish.power, currentFish.coins, currentFish.treasures, depth);
        }
        else
        {
            Debug.LogWarning($"Host: No fish found at depth {depth}!");
        }
    }


    [ClientRpc]
    public void SynchronizeFishForAllPlayersClientRpc(string fishName, int fishPower, int fishCoins, int fishTreasures, int depth)
    {
        Debug.Log($"All clients: Synchronized fish - {fishName} (Power: {fishPower}, Coins: {fishCoins}, Treasures: {fishTreasures})");

        // All clients create the same fish object
        currentFish = GetFishByName(fishName);
        if (currentFish == null)
        {
            // Fallback: create a temporary fish object with the same stats
            currentFish = ScriptableObject.CreateInstance<FishCard>();
            currentFish.fishName = fishName;
            currentFish.power = fishPower;
            currentFish.coins = fishCoins;
            currentFish.treasures = fishTreasures;
        }

        Debug.Log($"Client: Fighting synchronized fish {currentFish.fishName}!");

        // NEW: Show the fish UI for ALL players
        ShowFishUIForAllPlayers();

        // Start the battle for all clients
        StartRoundBasedBattleForAllClients();
    }

    // NEW: Method to show fish UI for everyone
    void ShowFishUIForAllPlayers()
    {
        Debug.Log("Showing fish UI for all players");

        // Find the fishing UI and show the fish card
        FishingUI fishingUI = FindFirstObjectByType<FishingUI>();
        if (fishingUI != null && currentFish != null)
        {
            // Show the fish card panel
            if (fishingUI.fishCardPanel != null)
            {
                fishingUI.fishCardPanel.SetActive(true);
            }

            // Display the fish on the card
            if (fishingUI.fishCardDisplay != null)
            {
                fishingUI.fishCardDisplay.fishCard = currentFish;
                fishingUI.fishCardDisplay.gearCard = null;
                fishingUI.fishCardDisplay.actionCard = null;

                // Force update the display
                fishingUI.fishCardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log($"All players now see fish: {currentFish.fishName}");
        }
        else
        {
            Debug.LogWarning("Could not find FishingUI or currentFish is null");
        }
    }

    // Helper method to find a fish by name
    FishCard GetFishByName(string fishName)
    {
        foreach (FishCard fish in allFishCards)
        {
            if (fish != null && fish.fishName.Equals(fishName, System.StringComparison.OrdinalIgnoreCase))
            {
                return fish;
            }
        }
        return null;
    }

    [ClientRpc]
    public void EndBattleForAllPlayersClientRpc(bool success)
    {
        Debug.Log($"All clients: Battle ended with success = {success}");

        if (success)
        {
            HandleSuccess();
        }
        else
        {
            HandleFailure();
        }
    }

    void StartRoundBasedBattleForAllClients()
    {
        battleEnded = false; // Reset for new battle

        Debug.Log($"=== ROUND-BASED BATTLE STARTED FOR ALL CLIENTS ===");
        Debug.Log($"Fighting {currentFish.fishName} (Power: {currentFish.power})");

        // Reset battle state
        currentRound = 1;
        playerStamina = 100;
        fishStamina = 100;
        totalPlayerBuffs = 0;
        totalFishBuffs = 0;
        appliedEffects.Clear();

        // Clear old cards and start new round for ALL players
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host: Clearing cards and starting new round via RPC");
            ClearAllActionCardsClientRpc();

            // Small delay to ensure cards are cleared, then start the round
            Invoke("StartNewRoundForAllClients", 0.5f);
        }
        else
        {
            // Fallback for single player
            Debug.Log("Single player: Starting round locally");
            ClearAllActionCards();
            StartNewRound();
        }
    }

    void StartNewRoundForAllClients()
    {
        Debug.Log($"=== ROUND {currentRound} STARTED FOR ALL CLIENTS ===");

        // Start interactive phase
        isInteractionPhase = true;
        lastStaminaUpdate = Time.time;

        int playerPower = CalculatePlayerPower();

        Debug.Log($"=== SENDING RPC TO ALL PLAYERS ===");
        Debug.Log($"NetworkManager found, IsListening: {NetworkManager.Singleton.IsListening}");
        Debug.Log($"IsHost: {NetworkManager.Singleton.IsHost}");
        Debug.Log($"IsClient: {NetworkManager.Singleton.IsClient}");
        Debug.Log($"Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Show UI for ALL players via RPC
        StartInteractivePhaseForAllPlayersClientRpc(currentFish.fishName, currentFish.power, playerStamina, fishStamina, playerPower, totalPlayerBuffs, totalFishBuffs);

        Debug.Log("RPC sent to all players!");
    }

    void ApplyActionCardEffect(string cardName, bool targetingPlayer, int playerEffect, int fishEffect, ulong playerId)
    {
        // Apply the effect (existing logic from PlayActionCard)
        if (targetingPlayer)
        {
            int effectToApply = playerEffect;

            // Check for shield absorption if this is a negative effect
            if (effectToApply < 0 && currentPlayer != null && currentPlayer.equippedShield != null)
            {
                int originalEffect = effectToApply;
                effectToApply = ApplyShieldAbsorption(effectToApply);

                if (effectToApply != originalEffect)
                {
                    Debug.Log($"Shield absorbed {originalEffect - effectToApply} damage! Reduced from {originalEffect} to {effectToApply}");
                }
            }

            totalPlayerBuffs += effectToApply;
            Debug.Log($"Player {playerId} played {cardName} on player: {effectToApply:+0;-#} effect");
        }
        else
        {
            totalFishBuffs += fishEffect;
            Debug.Log($"Player {playerId} played {cardName} on fish: {fishEffect:+0;-#} effect");
        }

        appliedEffects.Add($"{cardName} (Player {playerId}): {(targetingPlayer ? playerEffect : fishEffect):+0;-#} to {(targetingPlayer ? "player" : "fish")}");

        // Track that this player used a card
        InteractivePhaseUI interactiveUI = FindFirstObjectByType<InteractivePhaseUI>();
        if (interactiveUI != null)
        {
            interactiveUI.RecordCardPlayed(playerId);
        }

        Debug.Log($"Current totals - Player buffs: {totalPlayerBuffs:+0;-#}, Fish buffs: {totalFishBuffs:+0;-#}");
    }

    int ApplyShieldAbsorption(int negativeEffect)
    {
        if (currentPlayer == null || currentPlayer.equippedShield == null || currentPlayer.shieldStrength <= 0)
        {
            return negativeEffect; // No shield or shield is broken
        }

        // negativeEffect is negative (like -3), so we need to work with absolute values
        int damageAmount = Mathf.Abs(negativeEffect);
        int shieldCanAbsorb = Mathf.Min(damageAmount, currentPlayer.shieldStrength);
        int remainingDamage = damageAmount - shieldCanAbsorb;

        // Reduce shield strength
        currentPlayer.shieldStrength -= shieldCanAbsorb;

        Debug.Log($"🛡️ Shield absorbed {shieldCanAbsorb} damage! Shield strength: {currentPlayer.shieldStrength}");
        // Update the shield display to show new strength
        UpdateShieldDisplay();
        // If shield is depleted, unequip it
        // If shield is depleted, destroy it completely
        if (currentPlayer.shieldStrength <= 0)
        {
            Debug.Log($"💥 Shield {currentPlayer.equippedShield.effectName} is completely destroyed!");

            // DON'T add it back to inventory - just destroy it
            currentPlayer.equippedShield = null;
            currentPlayer.shieldStrength = 0;

            // Update the inventory display
            InventoryDisplay inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
            {
                inventoryDisplay.RefreshDisplay();
            }
        }

        // Return the remaining damage as a negative number
        return remainingDamage > 0 ? -remainingDamage : 0;
    }
    void UpdateShieldDisplay()
    {
        // Find all CardDisplay components in the scene
        CardDisplay[] allCardDisplays = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

        foreach (CardDisplay cardDisplay in allCardDisplays)
        {
            // Check if this card display is showing the equipped shield
            if (cardDisplay.effectCard != null && currentPlayer != null &&
                cardDisplay.effectCard == currentPlayer.equippedShield)
            {
                Debug.Log($"Updating shield display for {cardDisplay.effectCard.effectName}");

                // Force the card display to update
                cardDisplay.SendMessage("DisplayCard", SendMessageOptions.DontRequireReceiver);
            }
        }
    }


    // Public function to manually end round (for UI button)
    [ContextMenu("Next Round")]
    public void ForceNextRound()
    {
        if (isInteractionPhase)
        {
            NextRound();
        }
    }

    // Test functions you can call from the Inspector
    [ContextMenu("Test Setup Fishing")]
    public void TestSetupFishing()
    {
        SetupFishing();
    }

    [ContextMenu("Test Cast at Required Depth")]
    public void TestCastAtRequiredDepth()
    {
        if (requiredMinDepth > 0)
        {
            CastAtDepth(requiredMinDepth);
        }
        else
        {
            Debug.LogWarning("Run Setup Fishing first!");
        }
    }

    [ContextMenu("Debug Fish Depths")]
    public void DebugFishDepths()
    {
        Debug.Log("=== Fish Depth Analysis ===");

        int[] depthCounts = new int[4]; // Index 0-3 for depths 0-3

        foreach (FishCard fish in allFishCards)
        {
            if (fish != null)
            {
                Debug.Log($"{fish.fishName}: main depth {fish.mainDepth}, sub depth {fish.subDepth}");
                if (fish.mainDepth >= 0 && fish.mainDepth <= 3)
                {
                    depthCounts[fish.mainDepth]++;
                }
            }
        }

        Debug.Log("=== Main Depth Summary ===");
        for (int i = 0; i <= 3; i++)
        {
            string depthName = i == 1 ? "Coast" : i == 2 ? "Ocean" : i == 3 ? "Abyss" : "Invalid";
            // Debug.Log($"Depth {i} ({depthName}): {depthCounts[i]} fish");
        }
    }

    [ContextMenu("Test Tug of War UI")]
    public void TestTugOfWarUI()
    {
        // Quick test to verify tug-of-war integration
        if (interactiveUI != null && interactiveUI.tugOfWarBar != null)
        {
            // Debug.Log("Testing tug-of-war display...");

            // Simulate a battle scenario
            playerStamina = 75;
            fishStamina = 60;
            totalPlayerBuffs = 3;
            totalFishBuffs = 1;

            // Force update the UI
            if (interactiveUI.tugOfWarBar != null)
            {
                int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
                int fishPower = CalculateFishPower() + totalFishBuffs;
                int powerDiff = playerPower - fishPower;

                interactiveUI.tugOfWarBar.UpdateAll(playerStamina, fishStamina, powerDiff);
                // Debug.Log($"Updated tug-of-war: Player {playerStamina} stamina, Fish {fishStamina} stamina, Power diff {powerDiff}");
            }
        }
        else
        {
            // Debug.LogWarning("Tug-of-war bar not found! Make sure InteractivePhaseUI.tugOfWarBar is assigned.");
        }
    }

    [ContextMenu("Debug Current Powers")]
    public void DebugCurrentPowers()
    {
        if (currentPlayer == null || currentFish == null)
        {
            // Debug.LogWarning("No active fishing battle to debug!");
            return;
        }

        // Debug.Log("=== DETAILED POWER CALCULATION ===");

        // Calculate player power with detailed logging
        int basePower = currentPlayer.GetTotalPower();
        // Debug.Log($"Base gear power: {basePower}");

        int materialModifier = CalculateMaterialModifier();
        // Debug.Log($"Material modifier: {materialModifier:+0;-#;0}");

        int subDepthModifier = CalculateSubDepthGearModifier();
        // Debug.Log($"Sub-depth gear modifier: {subDepthModifier:+0;-#;0}");

        int playerPower = basePower + materialModifier + subDepthModifier + totalPlayerBuffs;
        Debug.Log($"Final player power: {basePower} + {materialModifier} + {subDepthModifier} + {totalPlayerBuffs} = {playerPower}");

        int fishPower = CalculateFishPower() + totalFishBuffs;
        Debug.Log($"Final fish power: {currentFish.power} + {totalFishBuffs} = {fishPower}");

        Debug.Log($"Power difference: {playerPower - fishPower} (positive = player advantage)");
        Debug.Log($"Current stamina - Player: {playerStamina}, Fish: {fishStamina}");
    }
    public override void OnNetworkSpawn()
    {
        Debug.Log($"=== FISHING MANAGER NETWORK SPAWN ===");
        Debug.Log($"IsHost: {IsHost}");
        Debug.Log($"IsClient: {IsClient}");
        Debug.Log($"IsOwner: {IsOwner}");
        Debug.Log($"NetworkObjectId: {NetworkObjectId}");
    }

// Add this debug method to your FishingManager class

[ContextMenu("Debug Power Sync")]
public void DebugPowerSync()
{
    Debug.Log("=== POWER SYNC DEBUG ===");
    Debug.Log($"My Client ID: {(NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 999)}");
    Debug.Log($"Current Round: {currentRound}");
    Debug.Log($"Battle Ended: {battleEnded}");
    Debug.Log($"Is Interaction Phase: {isInteractionPhase}");
    
    // Base power calculation
    int basePower = CalculatePlayerPower();
    Debug.Log($"Base Player Power: {basePower}");
    
    // Buff tracking
    Debug.Log($"Total Player Buffs: {totalPlayerBuffs}");
    Debug.Log($"Total Fish Buffs: {totalFishBuffs}");
    Debug.Log($"Applied Effects Count: {appliedEffects.Count}");
    
    // Print all applied effects
    for (int i = 0; i < appliedEffects.Count; i++)
    {
        Debug.Log($"  Effect {i}: {appliedEffects[i]}");
    }
    
    // Final powers
    int finalPlayerPower = basePower + totalPlayerBuffs;
    int finalFishPower = CalculateFishPower() + totalFishBuffs;
    Debug.Log($"Final Player Power: {finalPlayerPower} ({basePower} + {totalPlayerBuffs})");
    Debug.Log($"Final Fish Power: {finalFishPower}");
    Debug.Log($"Power Difference: {finalPlayerPower - finalFishPower}");
    
    // Stamina
    Debug.Log($"Player Stamina: {playerStamina}");
    Debug.Log($"Fish Stamina: {fishStamina}");
    Debug.Log("=== END POWER DEBUG ===");
}

    // Simple sync method without string arrays
    [ServerRpc(RequireOwnership = false)]
    public void ForceSyncBattleStateServerRpc()
    {
        Debug.Log("Host: Force syncing battle state to all clients");

        // Send just the critical numbers for now
        SyncBattleNumbersClientRpc(
            currentRound,
            playerStamina,
            fishStamina,
            totalPlayerBuffs,
            totalFishBuffs,
            appliedEffects.Count
        );
    }

[ServerRpc(RequireOwnership = false)]
public void SetFishingPlayerServerRpc(ulong playerId)
{
    Debug.Log($"Host: Setting fishing player to {playerId}");
    currentFishingPlayerId.Value = playerId;
}

    [ClientRpc]
public void SyncBattleNumbersClientRpc(int round, int pStamina, int fStamina, int pBuffs, int fBuffs, int effectCount)
{
    Debug.Log($"Client: Received battle numbers sync");
    Debug.Log($"  Round: {round}, Player Stamina: {pStamina}, Fish Stamina: {fStamina}");
    Debug.Log($"  Player Buffs: {pBuffs}, Fish Buffs: {fBuffs}, Effect Count: {effectCount}");
    
    // Update critical values
    currentRound = round;
    playerStamina = pStamina;
    fishStamina = fStamina;
    totalPlayerBuffs = pBuffs;
    totalFishBuffs = fBuffs;
    
    Debug.Log("Client: Battle numbers synchronized");
}
}