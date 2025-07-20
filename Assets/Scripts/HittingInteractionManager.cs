using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class HittingInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject crosshairPrefab;          // Your crosshair sprite prefab
    public Transform playerTargetArea;          // Where crosshairs appear on player
    public Transform fishTargetArea;            // Where crosshairs appear on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning
    
    [Header("Drop Zone Detection")]
    public ActionCardDropZone playerDropZone;   // Drop zone for hitting player
    public ActionCardDropZone fishDropZone;    // Drop zone for hitting fish
    
    [Header("Settings")]
    public float targetAreaWidth = 200f;        // How wide the target area is
    public float targetAreaHeight = 300f;       // How tall the target area is
    public float crosshairSize = 30f;           // Size of crosshair targets
    
    [Header("Game References")]
    public FishingManager fishingManager;
    
    // Current hitting session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private int remainingTargets;
    private List<GameObject> activeCrosshairs = new List<GameObject>();
    
    // Network variables for syncing hitting state
    public NetworkVariable<bool> isHittingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> hittingPlayerId = new NetworkVariable<ulong>(0);
    
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
        
        // Start hitting sequence
        StartHittingServerRpc(actionCard.actionName, targetPlayer, playerId);
        return true;
    }
    

    
    [ServerRpc(RequireOwnership = false)]
    public void StartHittingServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        if (!IsHost) return;
        
        Debug.Log($"Host: Starting hitting sequence - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");
        
        // Find the action card
        ActionCard actionCard = FindActionCardByName(actionCardName);
        if (actionCard == null)
        {
            Debug.LogError($"Could not find action card: {actionCardName}");
            return;
        }
        
        // Calculate number of targets needed
        int targetCount = targetPlayer ? Mathf.Abs(actionCard.playerEffect) : Mathf.Abs(actionCard.fishEffect);
        
        if (targetCount <= 0)
        {
            Debug.LogWarning($"No targets needed for {actionCardName} on {(targetPlayer ? "player" : "fish")}");
            return;
        }
        
        // Generate random target positions
        Vector2[] targetPositions = GenerateTargetPositions(targetCount, targetPlayer);
        
        // Start hitting for all clients
        StartHittingForAllClientsClientRpc(actionCardName, targetPlayer, playerId, targetPositions);
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
    public void StartHittingForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId, Vector2[] targetPositions)
    {
        Debug.Log($"All clients: Starting hitting sequence for {actionCardName}");
        
        // Find the action card
        ActionCard actionCard = FindActionCardByName(actionCardName);
        if (actionCard == null) return;
        
        // Set hitting state
        isHittingActive.Value = true;
        hittingPlayerId.Value = playerId;
        currentActionCard = actionCard;
        targetingPlayer = targetPlayer;
        remainingTargets = targetPositions.Length;
        
        // Create first crosshair
        if (targetPositions.Length > 0)
        {
            CreateNextCrosshair(targetPositions[0]);
        }
        
        // Store remaining positions for sequence
        StartCoroutine(HittingSequence(targetPositions));
        
        Debug.Log($"Hitting sequence started - {remainingTargets} targets to hit");
    }
    
    IEnumerator HittingSequence(Vector2[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            // Wait for current crosshair to be hit
            while (activeCrosshairs.Count > 0)
            {
                yield return null;
            }
            
            // Create next crosshair if there are more
            if (i + 1 < positions.Length)
            {
                CreateNextCrosshair(positions[i + 1]);
            }
        }
        
        // All targets hit - finish sequence
        FinishHittingSequence();
    }
    
    void CreateNextCrosshair(Vector2 localPosition)
    {
        if (crosshairPrefab == null) return;
        
        // Choose parent based on target
        Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
        if (parentTransform == null)
        {
            Debug.LogError($"No target area found for {(targetingPlayer ? "player" : "fish")}");
            return;
        }
        
        // Create crosshair
        GameObject crosshair = Instantiate(crosshairPrefab, parentTransform);
        
        // Position it
        RectTransform rectTransform = crosshair.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = localPosition;
            rectTransform.sizeDelta = Vector2.one * crosshairSize;
        }
        
        // Add click handler
        CrosshairTarget targetScript = crosshair.GetComponent<CrosshairTarget>();
        if (targetScript == null)
        {
            targetScript = crosshair.AddComponent<CrosshairTarget>();
        }
        targetScript.Initialize(this);
        
        activeCrosshairs.Add(crosshair);
        
        Debug.Log($"Created crosshair at {localPosition}");
    }
    
    public void OnCrosshairHit(GameObject crosshair)
    {
        Debug.Log("Crosshair hit!");
        
        // Remove from active list
        activeCrosshairs.Remove(crosshair);
        
        // Destroy the crosshair
        Destroy(crosshair);
        
        remainingTargets--;
        
        // Notify all clients of the hit
        if (NetworkManager.Singleton.IsHost)
        {
            CrosshairHitClientRpc();
        }
        
        Debug.Log($"Remaining targets: {remainingTargets}");
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
        
        // Reset state
        isHittingActive.Value = false;
        hittingPlayerId.Value = 0;
        currentActionCard = null;
        remainingTargets = 0;
        
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
        #if UNITY_EDITOR
        string[] actionGuids = UnityEditor.AssetDatabase.FindAssets($"{cardName} t:ActionCard");
        
        if (actionGuids.Length > 0)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(actionGuids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ActionCard>(assetPath);
        }
        #endif
        
        return null;
    }
}

// Simple component for crosshair click detection
public class CrosshairTarget : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    private HittingInteractionManager manager;
    
    public void Initialize(HittingInteractionManager hittingManager)
    {
        manager = hittingManager;
        
        // Make sure we have a graphic for raycasting
        if (GetComponent<UnityEngine.UI.Image>() == null)
        {
            gameObject.AddComponent<UnityEngine.UI.Image>();
        }
    }
    
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.OnCrosshairHit(gameObject);
        }
    }
}