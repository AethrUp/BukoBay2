using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class FishingManager : MonoBehaviour
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


    void Start()
    {
         Debug.Log("FishingManager Start() called");

        LoadAllFishCards();
        // Debug: Check what PlayerInventory objects exist
PlayerInventory[] allInventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
Debug.Log($"Found {allInventories.Length} PlayerInventory objects in scene");
Debug.Log($"PlayerInventory.Instance = {(PlayerInventory.Instance != null ? PlayerInventory.Instance.name : "NULL")}");
        
        // Find the persistent PlayerInventory if not assigned
        if (currentPlayer == null)
        {
            currentPlayer = FindFirstObjectByType<PlayerInventory>();
            if (currentPlayer == null && PlayerInventory.Instance != null)
            {
                currentPlayer = PlayerInventory.Instance;
            }
            Debug.Log($"FishingManager found PlayerInventory: {(currentPlayer != null ? currentPlayer.name : "NOT FOUND")}");
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
                Debug.Log($"Loaded fish: {fish.fishName} at main depth {fish.mainDepth}, sub depth {fish.subDepth}");
            }
        }
        #endif
        
        Debug.Log($"Loaded {allFishCards.Count} fish cards total");
    }
    
    public void SetupFishing()
    {
        currentPlayer = FindFirstObjectByType<PlayerInventory>();

        if (currentPlayer == null) return;
        
        // Count equipped gear pieces
        int gearCount = CountEquippedGear();
        
        // Calculate required depth based on gear count
        CalculateRequiredDepthFromGearCount(gearCount);
        
        Debug.Log($"Player has {gearCount} gear pieces");
        Debug.Log($"Must cast at depth {requiredMinDepth}");
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
            Debug.LogWarning($"Cannot cast at depth {depth}. Must cast at depth {requiredMinDepth}");
            return;
        }
        
        castDepth = depth;
        
        // Get random fish from this depth
        currentFish = GetRandomFishAtDepth(depth);
        
        if (currentFish != null)
        {
            Debug.Log($"Cast at depth {depth}! A {currentFish.fishName} appears!");
            Debug.Log($"Fish power: {currentFish.power}, Coins: {currentFish.coins}");
            
            // Start round-based battle
            StartRoundBasedBattle();
        }
        else
        {
            Debug.LogWarning($"No fish found at depth {depth}!");
        }
    }
    
    void StartRoundBasedBattle()
    {
        battleEnded = false; // Reset for new battle

        Debug.Log($"=== ROUND-BASED BATTLE STARTED ===");
        Debug.Log($"Fighting {currentFish.fishName} (Power: {currentFish.power})");
        
        // Reset battle state
        currentRound = 1;
        playerStamina = 100;
        fishStamina = 100;
        totalPlayerBuffs = 0;
        totalFishBuffs = 0;
        appliedEffects.Clear();
        
        // Start first round
        StartNewRound();
    }
    
    void StartNewRound()
    {
        Debug.Log($"=== ROUND {currentRound} STARTED ===");
        Debug.Log($"Players can now play action cards or skip their turn");
        
        // Start interactive phase for this round
        isInteractionPhase = true;
        lastStaminaUpdate = Time.time;
        
        // Show UI
        if (interactiveUI != null)
        {
            interactiveUI.ShowInteractivePhase();
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
            Debug.Log($"Fish takes {damage} damage! Fish stamina: {fishStamina}");
        }
        else if (fishPower > playerPower)
        {
            int damage = fishPower - playerPower;
            playerStamina -= damage;
            Debug.Log($"Player takes {damage} damage! Player stamina: {playerStamina}");
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
        
        Debug.Log($"=== POWER CALCULATION ===");
        Debug.Log($"Player Power: {playerPower}");
        Debug.Log($"Fish Power: {fishPower}");
        
        if (playerPower >= fishPower)
        {
            Debug.Log($"Player advantage by {playerPower - fishPower}");
        }
        else
        {
            Debug.Log($"Fish advantage by {fishPower - playerPower}");
        }
    }
    
    // Make these public so UI can access them
    public int CalculatePlayerPower()
    {
        Debug.Log($"CalculatePlayerPower called. currentPlayer = {(currentPlayer != null ? currentPlayer.name : "NULL")}");
        if (currentPlayer == null || currentFish == null) return 0;
        
        // Start with base gear power
        int basePower = currentPlayer.GetTotalPower();

        Debug.Log($"GetTotalPower() returned: {basePower}");
        
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
            
            Debug.Log($"Stamina drain update: Player Power {playerPower} vs Fish Power {fishPower}, Difference: {powerDifference}");
            
            // Apply stamina damage based on power difference
            if (powerDifference > 0)
            {
                // Player is winning - fish loses stamina
                fishStamina -= Mathf.Abs(powerDifference);
                Debug.Log($"Fish takes {Mathf.Abs(powerDifference)} damage! Fish stamina: {fishStamina}");
            }
            else if (powerDifference < 0)
            {
                // Fish is winning - player loses stamina
                playerStamina -= Mathf.Abs(powerDifference);
                Debug.Log($"Player takes {Mathf.Abs(powerDifference)} damage! Player stamina: {playerStamina}");
            }
            else
            {
                Debug.Log("Equal power - no damage dealt");
            }
            
            // Clamp stamina values
            playerStamina = Mathf.Clamp(playerStamina, 0, 100);
            fishStamina = Mathf.Clamp(fishStamina, 0, 100);
            
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
            
            // Update timer for next drain
            lastStaminaUpdate = Time.time;
        }
    }

    // Remove the old Update method that handled timer
    // void Update() - REMOVED

    void HandleSuccess()
    {
        if (battleEnded) return; // Prevent multiple calls

        battleEnded = true; // Mark battle as ended
        Debug.Log($"SUCCESS! Player catches {currentFish.fishName}!");
        Debug.Log($"Received {currentFish.coins} coins!");

        // Add coins to player inventory
        if (currentPlayer != null)
        {
            currentPlayer.AddCoins(currentFish.coins);
        }

        // Hide interactive UI
        if (interactiveUI != null)
        {
            interactiveUI.OnInteractivePhaseEnd();
        }

        // Hide the fish card panel
        FishingUI fishingUI = FindFirstObjectByType<FishingUI>();
        if (fishingUI != null)
        {
            fishingUI.HideFishCard();
        }

        // Show results screen
        if (resultsManager != null)
        {
            resultsManager.ShowResults(true, currentFish, currentFish.coins, "");
        }

        // Reset fishing state
        isInteractionPhase = false;

        // Clear played action cards
        ActionCardDropZone[] failureDropZones  = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
        foreach (ActionCardDropZone dropZone in failureDropZones)
        {
            dropZone.ClearPlayedCards();
        }

        Debug.Log("Fishing phase ended successfully!");
    
    // Clear played action cards
Debug.Log("Attempting to clear played action cards...");
ActionCardDropZone[] dropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
Debug.Log($"Found {dropZones.Length} ActionCardDropZone components");

foreach (ActionCardDropZone dropZone in dropZones)
{
    if (dropZone != null)
    {
        Debug.Log($"Clearing cards from drop zone: {dropZone.name}");
        dropZone.ClearPlayedCards();
    }
}
        Debug.Log("Finished clearing played action cards");
NetworkGameManager gameManager = FindFirstObjectByType<NetworkGameManager>();
if (gameManager != null)
{
    gameManager.NextTurnServerRpc();
    Debug.Log("Advanced to next player's turn after fishing success");
}
}

    void HandleFailure()
    {
        Debug.Log("*** ACTUAL HANDLE FAILURE CALLED ***");
        Debug.Log($"battleEnded at start: {battleEnded}");

        if (battleEnded)
        {
            Debug.Log("battleEnded is true, but we still need to apply damage!");
            // DON'T return early - we need to apply damage even if battleEnded is true
            // This might be a second call, but we need to make sure damage happens
        }

        // Apply damage BEFORE setting battleEnded to true
        string damageReport = "";

        Debug.Log("About to call ApplyGearDamage()...");

        if (currentPlayer != null && currentFish != null)
        {
            Debug.Log("Calling ApplyGearDamage() now...");
            damageReport = ApplyGearDamage();

            Debug.Log($"=== DAMAGE REPORT GENERATED ===");
            Debug.Log($"Damage report: '{damageReport}'");
            Debug.Log($"Damage report length: {damageReport.Length}");
            Debug.Log($"=== END DAMAGE REPORT ===");
        }
        else
        {
            Debug.Log("Skipping ApplyGearDamage because currentPlayer or currentFish is null");
        }

        // NOW set battleEnded to true
        if (!battleEnded)
        {
            battleEnded = true;
            Debug.Log("Set battleEnded to true");

            Debug.Log("=== HANDLE FAILURE CALLED ===");
            Debug.Log($"CurrentPlayer: {(currentPlayer != null ? currentPlayer.name : "NULL")}");
            Debug.Log($"CurrentFish: {(currentFish != null ? currentFish.fishName : "NULL")}");

            // TEST FISH DAMAGE VALUES:
            if (currentFish != null)
            {
                Debug.Log($"=== FISH DAMAGE VALUES TEST ===");
                Debug.Log($"Fish: {currentFish.fishName}");
                Debug.Log($"Gear 1 Damage: {currentFish.gear1Damage}");
                Debug.Log($"Gear 2 Damage: {currentFish.gear2Damage}");
                Debug.Log($"Gear 3 Damage: {currentFish.gear3Damage}");
                Debug.Log($"Gear 4 Damage: {currentFish.gear4Damage}");
                Debug.Log($"Gear 5 Damage: {currentFish.gear5Damage}");
                Debug.Log($"Total Damage: {currentFish.GetTotalGearDamage()}");
                Debug.Log($"=== END FISH DAMAGE TEST ===");
            }

            Debug.Log($"FAILURE! Player fails to catch {currentFish.fishName}!");
            Debug.Log("Gear takes damage...");

            // Refresh the inventory display to show updated durability
            InventoryDisplay inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
            if (inventoryDisplay != null)
            {
                inventoryDisplay.RefreshDisplay();
                Debug.Log("Refreshed inventory display to show gear damage");
            }

            // Hide interactive UI
            if (interactiveUI != null)
            {
                interactiveUI.OnInteractivePhaseEnd();
            }

            // Hide the fish card panel
            FishingUI fishingUI = FindFirstObjectByType<FishingUI>();
            if (fishingUI != null)
            {
                fishingUI.HideFishCard();
            }

            // Show results screen
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

            // Clear played action cards
            ActionCardDropZone[] failureDropZones = FindObjectsByType<ActionCardDropZone>(FindObjectsSortMode.None);
            foreach (ActionCardDropZone dropZone in failureDropZones)
            {
                dropZone.ClearPlayedCards();
            }
        }

        Debug.Log("Fishing phase ended in failure!");
    // At the end of HandleSuccess() method:
NetworkGameManager turnManager = FindFirstObjectByType<NetworkGameManager>();
if (turnManager != null)
{
    turnManager.NextTurnServerRpc();
    Debug.Log("Advanced to next player's turn after fishing success");
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
    
    // NEW: Check if this player has reached their card limit
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
    
    // Apply the effect (existing code stays the same)
    if (targetingPlayer)
    {
        int effectToApply = actionCard.playerEffect;
        
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
        Debug.Log($"Played {actionCard.actionName} on player: {effectToApply:+0;-#} effect (original: {actionCard.playerEffect:+0;-#})");
    }
    else
    {
        totalFishBuffs += actionCard.fishEffect;
        Debug.Log($"Played {actionCard.actionName} on fish: {actionCard.fishEffect:+0;-#} effect");
    }
    
    appliedEffects.Add($"{actionCard.actionName}: {(targetingPlayer ? actionCard.playerEffect : actionCard.fishEffect):+0;-#} to {(targetingPlayer ? "player" : "fish")}");
    
    // NEW: Track that this player used a card
    if (NetworkManager.Singleton != null)
    {
        ulong playerId = NetworkManager.Singleton.LocalClientId;
        InteractivePhaseUI interactiveUI = FindFirstObjectByType<InteractivePhaseUI>();
        if (interactiveUI != null)
        {
            interactiveUI.RecordCardPlayed(playerId);
        }
    }
    
    Debug.Log($"Current totals - Player buffs: {totalPlayerBuffs:+0;-#}, Fish buffs: {totalFishBuffs:+0;-#}");
    
    return true;
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
            Debug.Log($"Depth {i} ({depthName}): {depthCounts[i]} fish");
        }
    }
    
    [ContextMenu("Test Tug of War UI")]
    public void TestTugOfWarUI()
    {
        // Quick test to verify tug-of-war integration
        if (interactiveUI != null && interactiveUI.tugOfWarBar != null)
        {
            Debug.Log("Testing tug-of-war display...");
            
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
                Debug.Log($"Updated tug-of-war: Player {playerStamina} stamina, Fish {fishStamina} stamina, Power diff {powerDiff}");
            }
        }
        else
        {
            Debug.LogWarning("Tug-of-war bar not found! Make sure InteractivePhaseUI.tugOfWarBar is assigned.");
        }
    }
    
    [ContextMenu("Debug Current Powers")]
    public void DebugCurrentPowers()
    {
        if (currentPlayer == null || currentFish == null)
        {
            Debug.LogWarning("No active fishing battle to debug!");
            return;
        }
        
        Debug.Log("=== DETAILED POWER CALCULATION ===");
        
        // Calculate player power with detailed logging
        int basePower = currentPlayer.GetTotalPower();
        Debug.Log($"Base gear power: {basePower}");
        
        int materialModifier = CalculateMaterialModifier();
        Debug.Log($"Material modifier: {materialModifier:+0;-#;0}");
        
        int subDepthModifier = CalculateSubDepthGearModifier();
        Debug.Log($"Sub-depth gear modifier: {subDepthModifier:+0;-#;0}");
        
        int playerPower = basePower + materialModifier + subDepthModifier + totalPlayerBuffs;
        Debug.Log($"Final player power: {basePower} + {materialModifier} + {subDepthModifier} + {totalPlayerBuffs} = {playerPower}");
        
        int fishPower = CalculateFishPower() + totalFishBuffs;
        Debug.Log($"Final fish power: {currentFish.power} + {totalFishBuffs} = {fishPower}");
        
        Debug.Log($"Power difference: {playerPower - fishPower} (positive = player advantage)");
        Debug.Log($"Current stamina - Player: {playerStamina}, Fish: {fishStamina}");
    }
}