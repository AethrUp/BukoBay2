using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("Interactive Phase")]
    public float interactionTimer = 30f;
    public bool isInteractionPhase = false;
    public float timeRemaining;
    
    [Header("Action Card Effects")]
    public int totalPlayerBuffs = 0;
    public int totalFishBuffs = 0;
    public List<string> appliedEffects = new List<string>();
    
    [Header("UI References")]
    public InteractivePhaseUI interactiveUI;
    
    void Start()
    {
        LoadAllFishCards();
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
            
            // Start interactive phase
            StartInteractivePhase();
        }
        else
        {
            Debug.LogWarning($"No fish found at depth {depth}!");
        }
    }
    
    void StartInteractivePhase()
    {
        Debug.Log($"=== INTERACTIVE PHASE STARTED ===");
        Debug.Log($"Players have {interactionTimer} seconds to play action cards!");
        
        // Reset action card effects
        totalPlayerBuffs = 0;
        totalFishBuffs = 0;
        appliedEffects.Clear();
        
        // Start timer
        isInteractionPhase = true;
        timeRemaining = interactionTimer;
        
        // Show UI
        if (interactiveUI != null)
        {
            interactiveUI.ShowInteractivePhase();
        }
        
        // Calculate initial powers (without action card effects)
        CalculateFishingPowers();
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
        // Calculate player's effective power
        int playerPower = CalculatePlayerPower();
        
        // Calculate fish's effective power  
        int fishPower = CalculateFishPower();
        
        Debug.Log($"=== POWER CALCULATION ===");
        Debug.Log($"Player Power: {playerPower}");
        Debug.Log($"Fish Power: {fishPower}");
        
        if (playerPower >= fishPower)
        {
            Debug.Log($"SUCCESS! Player wins by {playerPower - fishPower}");
        }
        else
        {
            Debug.Log($"FAILURE! Fish wins by {fishPower - playerPower}");
        }
    }
    
    // Make these public so UI can access them
    public int CalculatePlayerPower()
    {
        if (currentPlayer == null || currentFish == null) return 0;
        
        // Start with base gear power
        int basePower = currentPlayer.GetTotalPower();
        Debug.Log($"Base gear power: {basePower}");
        
        // Apply material bonuses/penalties from fish
        int materialModifier = CalculateMaterialModifier();
        Debug.Log($"Material modifier: {materialModifier:+0;-#;0}");
        
        // Apply sub-depth gear effectiveness
        int subDepthModifier = CalculateSubDepthGearModifier();
        Debug.Log($"Sub-depth gear modifier: {subDepthModifier:+0;-#;0}");
        
        int finalPower = basePower + materialModifier + subDepthModifier;
        Debug.Log($"Final player power: {basePower} + {materialModifier} + {subDepthModifier} = {finalPower}");
        
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
        
        if (modifier != 0)
        {
            Debug.Log($"{slotName} ({gear.gearName}): effectiveness at sub-depth {subDepth} gives {modifier:+0;-#}");
        }
        
        return modifier;
    }
    
    public int CalculateFishPower()
    {
        if (currentFish == null) return 0;
        
        // Fish power is just their base power
        // Sub-depth affects gear effectiveness, not fish difficulty
        int fishPower = currentFish.power;
        
        Debug.Log($"Fish base power: {fishPower}");
        
        return fishPower;
    }
    
    void Update()
    {
        // Handle interaction phase timer
        if (isInteractionPhase)
        {
            timeRemaining -= Time.deltaTime;
            
            if (timeRemaining <= 0)
            {
                EndInteractivePhase();
            }
        }
    }
    
    void EndInteractivePhase()
    {
        Debug.Log($"=== INTERACTIVE PHASE ENDED ===");
        isInteractionPhase = false;
        
        // Notify UI
        if (interactiveUI != null)
        {
            interactiveUI.OnInteractivePhaseEnd();
        }
        
        // Calculate final result with all action card effects
        ResolveFishingAttempt();
    }
    
    void ResolveFishingAttempt()
    {
        // Calculate final powers including action card effects
        int playerPower = CalculatePlayerPower() + totalPlayerBuffs;
        int fishPower = CalculateFishPower() + totalFishBuffs;
        
        Debug.Log($"=== FINAL RESOLUTION ===");
        Debug.Log($"Final Player Power: {playerPower} (including +{totalPlayerBuffs} from actions)");
        Debug.Log($"Final Fish Power: {fishPower} (including +{totalFishBuffs} from actions)");
        
        if (playerPower >= fishPower)
        {
            Debug.Log($"SUCCESS! Player wins by {playerPower - fishPower}");
            HandleSuccess();
        }
        else
        {
            Debug.Log($"FAILURE! Fish wins by {fishPower - playerPower}");
            HandleFailure();
        }
    }
    
    void HandleSuccess()
    {
        Debug.Log($"Player catches {currentFish.fishName}!");
        Debug.Log($"Received {currentFish.coins} coins!");
        
        // TODO: Add coins to player inventory
        // TODO: Distribute rewards to helpers
    }
    
    void HandleFailure()
    {
        Debug.Log($"Player fails to catch {currentFish.fishName}!");
        Debug.Log("Gear takes damage...");
        
        // TODO: Apply gear damage
        // TODO: Distribute rewards to opponents
    }
    
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
        
        // Apply the effect
        if (targetingPlayer)
        {
            totalPlayerBuffs += actionCard.playerEffect;
            Debug.Log($"Played {actionCard.actionName} on player: {actionCard.playerEffect:+0;-#} effect");
        }
        else
        {
            totalFishBuffs += actionCard.fishEffect;
            Debug.Log($"Played {actionCard.actionName} on fish: {actionCard.fishEffect:+0;-#} effect");
        }
        
        appliedEffects.Add($"{actionCard.actionName}: {(targetingPlayer ? actionCard.playerEffect : actionCard.fishEffect):+0;-#} to {(targetingPlayer ? "player" : "fish")}");
        
        Debug.Log($"Current totals - Player buffs: {totalPlayerBuffs:+0;-#}, Fish buffs: {totalFishBuffs:+0;-#}");
        
        return true;
    }
    
    // Function to manually end interaction phase (for testing)
    [ContextMenu("End Interactive Phase")]
    public void ForceEndInteractivePhase()
    {
        if (isInteractionPhase)
        {
            EndInteractivePhase();
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
}