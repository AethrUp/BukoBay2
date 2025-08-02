using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class HittingInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject crosshairPrefab;          // Your crosshair sprite prefab
    public GameObject timingBarPrefab;          // Timing bar UI prefab
    public Transform playerTargetArea;          // Where crosshairs appear on player
    public Transform fishTargetArea;            // Where crosshairs appear on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning
    
    [Header("Drop Zone Detection")]
    public ActionCardDropZone playerDropZone;   // Drop zone for hitting player
    public ActionCardDropZone fishDropZone;    // Drop zone for hitting fish
    
    [Header("Settings")]
    public float targetAreaWidth = 200f;        // How wide the target area is
    public float targetAreaHeight = 300f;       // How tall the target area is
    public float crosshairSize = 80f;           // Size of crosshair targets (increased from 30f)
    
    [Header("Timing Settings")]
    public float timingBarSpeed = 2f;           // Speed of the timing indicator
    
    [Header("Game References")]
    public FishingManager fishingManager;
    
    // Current hitting session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private int remainingTargets;
    private List<GameObject> activeCrosshairs = new List<GameObject>();
    
    // Timing system data
    private GameObject currentTimingBar;
    private HittingTimingBar timingBarScript;
    private GameObject currentCrosshair;
    private bool waitingForTiming = false;
    
    // Network variables for syncing hitting state
    public NetworkVariable<bool> isHittingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> hittingPlayerId = new NetworkVariable<ulong>(0);
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"HittingInteractionManager spawned - IsHost: {IsHost}, IsClient: {IsClient}");
        base.OnNetworkSpawn();
    }
    
    void Start()
    {
        if (fishingManager == null)
            fishingManager = FindFirstObjectByType<FishingManager>();
        
        if (gameCanvas == null)
            gameCanvas = FindFirstObjectByType<Canvas>();
    }
    
    /// <summary>
    /// Called when a hitting action card is dropped on a target area
    /// </summary>
    public bool StartHittingSequence(ActionCard actionCard, bool targetPlayer, ulong playerId)
    {
        // Only allow if not already hitting and in interactive phase
        if (isHittingActive.Value || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot start hitting - already in progress or not in interactive phase");
            return false;
        }
        
        currentActionCard = actionCard;
        
        // Check if this card can target the chosen target
        bool canHitPlayer = actionCard.canTargetPlayer && actionCard.playerEffect != 0;
        bool canHitFish = actionCard.canTargetFish && actionCard.fishEffect != 0;
        
        if (targetPlayer && !canHitPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }
        
        if (!targetPlayer && !canHitFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }
        
        // Use RPC system for networking
        StartHittingServerRpc(actionCard.actionName, targetPlayer, playerId);
        
        return true;
    }
    
    /// <summary>
    /// Handle hitting sequence locally for single player
    /// </summary>
    void StartHittingLocally(ActionCard actionCard, bool targetPlayer)
    {
        // Calculate number of targets needed
        int targetCount = targetPlayer ? Mathf.Abs(actionCard.playerEffect) : Mathf.Abs(actionCard.fishEffect);
        
        if (targetCount <= 0)
        {
            Debug.LogWarning($"No targets needed for {actionCard.actionName}");
            return;
        }
        
        // Generate random target positions
        Vector2[] targetPositions = GenerateTargetPositions(targetCount, targetPlayer);
        
        // Set hitting state locally
        isHittingActive.Value = true;
        currentActionCard = actionCard;
        targetingPlayer = targetPlayer;
        remainingTargets = targetPositions.Length;
        
        // Create first crosshair
        if (targetPositions.Length > 0)
        {
            CreateNextCrosshair(targetPositions[0]);
        }
        
        // Start sequence
        StartCoroutine(HittingSequence(targetPositions));
        
        Debug.Log($"Single player hitting sequence started - {remainingTargets} targets to hit");
    }
    

    
    [ServerRpc(RequireOwnership = false)]
    public void StartHittingServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"HOST: StartHittingServerRpc called - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");
        
        if (!IsHost) 
        {
            Debug.LogWarning("StartHittingServerRpc called but not host!");
            return;
        }
        
        // Find the action card
        ActionCard actionCard = FindActionCardByName(actionCardName);
        if (actionCard == null)
        {
            Debug.LogError($"Could not find action card: {actionCardName}");
            return;
        }
        
        Debug.Log($"HOST: Found action card {actionCard.actionName}");
        
        // Calculate number of targets needed
        int targetCount = targetPlayer ? Mathf.Abs(actionCard.playerEffect) : Mathf.Abs(actionCard.fishEffect);
        
        Debug.Log($"HOST: Target count calculated as {targetCount}");
        
        if (targetCount <= 0)
        {
            Debug.LogWarning($"No targets needed for {actionCardName} on {(targetPlayer ? "player" : "fish")}");
            return;
        }
        
        // Generate random target positions
        Vector2[] targetPositions = GenerateTargetPositions(targetCount, targetPlayer);
        
        Debug.Log($"HOST: Generated {targetPositions.Length} target positions");
        
        // Start hitting for all clients
        StartHittingForAllClientsClientRpc(actionCardName, targetPlayer, playerId, targetPositions, targetCount);
        
        Debug.Log($"HOST: Sent ClientRpc to all players");
    }
    
    Vector2[] GenerateTargetPositions(int count, bool onPlayer)
    {
        Vector2[] positions = new Vector2[count];
        
        for (int i = 0; i < count; i++)
        {
            // Generate random position within target area
            float x = Random.Range(-targetAreaWidth / 2, targetAreaWidth / 2);
            float y = Random.Range(-targetAreaHeight / 2, targetAreaHeight / 2);
            positions[i] = new Vector2(x, y);
        }
        
        return positions;
    }
    
[ClientRpc]
public void StartHittingForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId, Vector2[] targetPositions, int targetCount)
{
    Debug.Log($"CLIENT: StartHittingForAllClientsClientRpc received - {actionCardName}, {targetPositions.Length} targets");
    
    // Set hitting state using the provided data instead of trying to find the action card
    isHittingActive.Value = true;
    hittingPlayerId.Value = playerId;
    targetingPlayer = targetPlayer;
    remainingTargets = targetPositions.Length;
    
    Debug.Log($"CLIENT: Set hitting state - targeting {(targetPlayer ? "player" : "fish")}, {remainingTargets} targets");
    
    // ONLY CREATE CROSSHAIRS FOR THE ACTING PLAYER
    ulong myClientId = NetworkManager.Singleton.LocalClientId;
    if (myClientId == playerId)
    {
        Debug.Log($"CLIENT: I am the acting player ({playerId}) - creating crosshairs");
        // Create first crosshair
        if (targetPositions.Length > 0)
        {
            Debug.Log($"CLIENT: About to create first crosshair at position {targetPositions[0]}");
            CreateNextCrosshair(targetPositions[0]);
            Debug.Log($"CLIENT: First crosshair creation attempted");
        }
        else
        {
            Debug.LogError("CLIENT: No target positions provided!");
        }
    }
    else
    {
        Debug.Log($"CLIENT: I am NOT the acting player (I'm {myClientId}, acting player is {playerId}) - no crosshairs for me");
    }
    
    // ONLY START THE HITTING SEQUENCE FOR THE ACTING PLAYER
    if (myClientId == playerId)
    {
        // Store remaining positions for sequence
        StartCoroutine(HittingSequence(targetPositions));
        Debug.Log($"CLIENT: Hitting sequence started - {remainingTargets} targets to hit");
    }
    else
    {
        Debug.Log($"CLIENT: I am not the acting player - no hitting sequence for me");
    }
}
    
    IEnumerator HittingSequence(Vector2[] positions)
    {
        // First crosshair is already created, start from index 1
        int currentTargetIndex = 1; // Start from 1 since first crosshair is already created
        
        while (currentTargetIndex <= positions.Length)
        {
            // Wait for current crosshair to be hit or missed
            while (waitingForTiming)
            {
                yield return null;
            }
            
            // Create next crosshair if there are more targets
            if (currentTargetIndex < positions.Length)
            {
                CreateNextCrosshair(positions[currentTargetIndex]);
            }
            
            currentTargetIndex++;
        }
        
        // All targets processed - finish sequence
        FinishHittingSequence();
    }
    
    void CreateNextCrosshair(Vector2 localPosition)
    {
        Debug.Log($"CLIENT: CreateNextCrosshair called at position {localPosition}");
        
        if (crosshairPrefab == null) 
        {
            Debug.LogError("CLIENT: Crosshair prefab is null!");
            return;
        }
        
        // Choose parent based on target
        Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
        if (parentTransform == null)
        {
            Debug.LogError($"CLIENT: No target area found for {(targetingPlayer ? "player" : "fish")}");
            return;
        }
        
        Debug.Log($"CLIENT: Using parent transform: {parentTransform.name}");
        
        // Create crosshair
        GameObject crosshair = Instantiate(crosshairPrefab, parentTransform);
        currentCrosshair = crosshair;
        
        Debug.Log($"CLIENT: Created crosshair object: {crosshair.name}");
        
        // Position it
        RectTransform rectTransform = crosshair.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = localPosition;
            rectTransform.sizeDelta = Vector2.one * crosshairSize;
            Debug.Log($"CLIENT: Positioned crosshair at {localPosition} with size {crosshairSize}");
        }
        else
        {
            Debug.LogWarning("CLIENT: Crosshair has no RectTransform!");
        }
        
        // Create timing bar near the crosshair
        CreateTimingBar(localPosition, parentTransform);
        
        // Add click handler with timing requirement
        TimingCrosshairTarget targetScript = crosshair.GetComponent<TimingCrosshairTarget>();
        if (targetScript == null)
        {
            targetScript = crosshair.AddComponent<TimingCrosshairTarget>();
            Debug.Log($"CLIENT: Added TimingCrosshairTarget component to {crosshair.name}");
        }
        targetScript.Initialize(this, timingBarScript);
        
        // Make sure it's clickable
        UnityEngine.UI.Image image = crosshair.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            Debug.Log($"CLIENT: Set raycastTarget = true on crosshair image");
        }
        else
        {
            Debug.LogWarning($"CLIENT: No Image component found on crosshair!");
        }
        
        activeCrosshairs.Add(crosshair);
        
        // Start waiting for timing (no auto-fail timer)
        waitingForTiming = true;
        
        Debug.Log($"CLIENT: Crosshair setup complete. Active crosshairs: {activeCrosshairs.Count}");
    }
    
    void CreateTimingBar(Vector2 crosshairPosition, Transform parent)
    {
        if (timingBarPrefab == null)
        {
            Debug.LogError("CLIENT: Timing bar prefab is null!");
            return;
        }
        
        // Create timing bar
        currentTimingBar = Instantiate(timingBarPrefab, parent);
        timingBarScript = currentTimingBar.GetComponent<HittingTimingBar>();
        
        if (timingBarScript == null)
        {
            timingBarScript = currentTimingBar.AddComponent<HittingTimingBar>();
        }
        
        // Position timing bar near crosshair (above it)
        RectTransform timingBarRect = currentTimingBar.GetComponent<RectTransform>();
        if (timingBarRect != null)
        {
            Vector2 barPosition = crosshairPosition + Vector2.up * (crosshairSize + 20f);
            timingBarRect.anchoredPosition = barPosition;
            timingBarRect.sizeDelta = new Vector2(120f, 20f); // Timing bar size
        }
        
        // Configure timing bar
        timingBarScript.barSpeed = timingBarSpeed;
        timingBarScript.Initialize(this);
        timingBarScript.StartTiming();
        
        Debug.Log($"CLIENT: Created timing bar at position {crosshairPosition}");
    }
    
    // Removed auto-fail timing window - players must take action
    
    public void OnCrosshairHit(GameObject crosshair)
    {
        Debug.Log("Crosshair hit!");
        
        // Check if timing is correct (this will be checked by TimingCrosshairTarget)
        if (!waitingForTiming)
        {
            Debug.Log("Not waiting for timing - ignoring hit");
            return;
        }
        
        // Remove from active list
        activeCrosshairs.Remove(crosshair);
        
        // Clean up timing UI
        CleanupCurrentTarget();
        
        remainingTargets--;
        
        // Notify all clients of the hit
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            CrosshairHitClientRpc();
        }
        
        Debug.Log($"Remaining targets: {remainingTargets}");
    }
    
    public void OnTimingMissed()
    {
        Debug.Log("Bad timing - target missed but stays active!");
        
        // Don't clean up target - let it stay active for another attempt
        // Players must hit with correct timing to proceed
    }
    
    void CleanupCurrentTarget()
    {
        waitingForTiming = false;
        
        // Destroy crosshair
        if (currentCrosshair != null)
        {
            Destroy(currentCrosshair);
            currentCrosshair = null;
        }
        
        // Destroy timing bar
        if (currentTimingBar != null)
        {
            if (timingBarScript != null)
            {
                timingBarScript.StopTiming();
            }
            Destroy(currentTimingBar);
            currentTimingBar = null;
            timingBarScript = null;
        }
    }
    
    [ClientRpc]
    public void CrosshairHitClientRpc()
    {
        // Play hit effect, update UI, etc.
        Debug.Log("All clients: Crosshair was hit");
    }

    void FinishHittingSequence()
{
    Debug.Log("Hitting sequence complete!");
    
    // Apply the action card effect through existing system
    if (currentActionCard != null && fishingManager != null)
    {
        bool success = fishingManager.PlayActionCard(currentActionCard, targetingPlayer);
        Debug.Log($"Applied hitting effect: {success}");
    }
    
    // Tell all clients to clean up
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
    {
        CleanupHittingForAllClientsClientRpc();
    }
    
    // Reset state locally
    ResetHittingState();
}

[ClientRpc]
void CleanupHittingForAllClientsClientRpc()
{
    Debug.Log("All clients: Cleaning up hitting sequence");
    ResetHittingState();
}

void ResetHittingState()
{
    // Reset state
    isHittingActive.Value = false;
    hittingPlayerId.Value = 0;
    currentActionCard = null;
    remainingTargets = 0;
    waitingForTiming = false;
    
    // Clean up current timing elements
    CleanupCurrentTarget();
    
    // Clear any remaining crosshairs
    foreach (GameObject crosshair in activeCrosshairs)
    {
        if (crosshair != null) Destroy(crosshair);
    }
    activeCrosshairs.Clear();
}
    // Helper method to find ActionCard by name
    ActionCard FindActionCardByName(string cardName)
    {
        // First try to get it from the current action card (passed from client)
        if (currentActionCard != null && currentActionCard.actionName == cardName)
        {
            return currentActionCard;
        }
        
        #if UNITY_EDITOR
        string[] actionGuids = UnityEditor.AssetDatabase.FindAssets($"{cardName} t:ActionCard");
        
        if (actionGuids.Length > 0)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(actionGuids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ActionCard>(assetPath);
        }
        #endif
        
        Debug.LogWarning($"Could not find ActionCard: {cardName}");
        return null;
    }
}

// Timing-based component for crosshair click detection
public class TimingCrosshairTarget : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    private HittingInteractionManager manager;
    private HittingTimingBar timingBar;
    
    public void Initialize(HittingInteractionManager hittingManager, HittingTimingBar timing)
    {
        manager = hittingManager;
        timingBar = timing;
        
        Debug.Log($"TimingCrosshairTarget initialized on {gameObject.name}");
        
        // Make sure we have a graphic for raycasting
        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<UnityEngine.UI.Image>();
            Debug.Log($"Added Image component to {gameObject.name}");
        }
        
        // Ensure the image can receive raycast events
        image.raycastTarget = true;
        
        Debug.Log($"TimingCrosshairTarget setup complete on {gameObject.name}");
    }
    
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Debug.Log($"CROSSHAIR CLICKED! GameObject: {gameObject.name}");
        
        if (manager == null)
        {
            Debug.LogError($"Manager is null on TimingCrosshairTarget!");
            return;
        }
        
        if (timingBar == null)
        {
            Debug.LogError($"TimingBar is null on TimingCrosshairTarget!");
            return;
        }
        
        // Check if the timing is in the sweet spot
        if (timingBar.IsInSweetSpot)
        {
            Debug.Log($"Perfect timing! Hit successful");
            manager.OnCrosshairHit(gameObject);
        }
        else
        {
            Debug.Log($"Bad timing! Hit missed - indicator not in sweet spot");
            manager.OnTimingMissed();
        }
    }
}