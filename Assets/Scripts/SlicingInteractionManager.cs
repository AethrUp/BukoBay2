using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class SlicingInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject sliceLinePrefab;          // The line/path the player needs to trace
    public Transform playerTargetArea;          // Where slice lines appear on player
    public Transform fishTargetArea;            // Where slice lines appear on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning
    
    [Header("Slice Detection")]
    public float sliceDetectionRadius = 20f;   // How close the mouse needs to be to the line
    public float completionThreshold = 0.8f;   // How much of the line needs to be traced (80%)
    
    [Header("Line Generation")]
    public float lineLength = 200f;            // Length of lines to draw
    public int lineSegments = 20;              // Number of segments in each line
    
    [Header("Game References")]
    public FishingManager fishingManager;
    
    // Current slicing session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private int remainingSlices;
    private int totalSlices;
    
    // Active slice tracking
    private List<GameObject> activeSliceLines = new List<GameObject>();
    private GameObject currentSliceLine;
    private List<Vector2> currentLinePoints = new List<Vector2>();
    private List<bool> pointsCompleted = new List<bool>();
    private bool isSlicing = false;
    private List<GameObject> progressIndicators = new List<GameObject>();
    
    // Network variables for syncing slicing state
    public NetworkVariable<bool> isSlicingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> slicingPlayerId = new NetworkVariable<ulong>(0);
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"SlicingInteractionManager spawned - IsHost: {IsHost}, IsClient: {IsClient}");
        base.OnNetworkSpawn();
    }
    
    void Start()
    {
        if (fishingManager == null)
            fishingManager = FindFirstObjectByType<FishingManager>();
        
        if (gameCanvas == null)
            gameCanvas = FindFirstObjectByType<Canvas>();
    }
    
    void Update()
    {
        if (isSlicing)
        {
            TrackMouseForSlicing();
        }
    }
    
    /// <summary>
    /// Called when a slicing action card is dropped on a target area
    /// </summary>
    public bool StartSlicingSequence(ActionCard actionCard, bool targetPlayer, ulong playerId)
    {
        // Only allow if not already slicing and in interactive phase
        if (isSlicingActive.Value || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot start slicing - already in progress or not in interactive phase");
            return false;
        }
        
        currentActionCard = actionCard;
        
        // Check if this card can target the chosen target
        bool canSlicePlayer = actionCard.canTargetPlayer && actionCard.playerEffect != 0;
        bool canSliceFish = actionCard.canTargetFish && actionCard.fishEffect != 0;
        
        if (targetPlayer && !canSlicePlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }
        
        if (!targetPlayer && !canSliceFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }
        
        // Use RPC system for networking
        StartSlicingServerRpc(actionCard.actionName, targetPlayer, playerId);
        
        return true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void StartSlicingServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"HOST: StartSlicingServerRpc called - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");
        
        if (!IsHost) 
        {
            Debug.LogWarning("StartSlicingServerRpc called but not host!");
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
        
        // Calculate number of slices needed
        int sliceCount = targetPlayer ? Mathf.Abs(actionCard.playerEffect) : Mathf.Abs(actionCard.fishEffect);
        
        Debug.Log($"HOST: Slice count calculated as {sliceCount}");
        
        if (sliceCount <= 0)
        {
            Debug.LogWarning($"No slices needed for {actionCardName} on {(targetPlayer ? "player" : "fish")}");
            return;
        }
        
        // Start slicing for all clients
        StartSlicingForAllClientsClientRpc(actionCardName, targetPlayer, playerId, sliceCount);
        
        Debug.Log($"HOST: Sent ClientRpc to all players");
    }
    
    [ClientRpc]
public void StartSlicingForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId, int sliceCount)
{
    Debug.Log($"CLIENT: StartSlicingForAllClientsClientRpc received - {actionCardName}, {sliceCount} slices");
    
    // Set slicing state for all clients
    isSlicingActive.Value = true;
    slicingPlayerId.Value = playerId;
    targetingPlayer = targetPlayer;
    totalSlices = sliceCount;
    remainingSlices = sliceCount;
    
    Debug.Log($"CLIENT: Set slicing state - targeting {(targetPlayer ? "player" : "fish")}, {remainingSlices} slices");
    
    // ADD THIS DEBUG LINE
    ulong myClientId = NetworkManager.Singleton.LocalClientId;
    Debug.Log($"DEBUG: My Client ID = {myClientId}, Slicing Player ID = {playerId}");
    
    // ONLY CREATE SLICING UI FOR THE ACTING PLAYER
    if (myClientId == playerId)
    {
        Debug.Log($"CLIENT: I am the slicing player ({playerId}) - creating slice lines");
        
        // Find the action card and start slicing
        ActionCard actionCard = FindActionCardByName(actionCardName);
        if (actionCard != null)
        {
            currentActionCard = actionCard;
            StartNextSlice();
        }
        else
        {
            Debug.LogError($"Could not find action card: {actionCardName}");
        }
    }
    else
    {
        Debug.Log($"CLIENT: I am NOT the slicing player (I'm {myClientId}, slicing player is {playerId}) - no slicing UI for me");
    }
}
    
    void StartNextSlice()
    {
        Debug.Log($"Starting slice {totalSlices - remainingSlices + 1} of {totalSlices}");
        
        // Generate a random line pattern for this slice
        GenerateSliceLine();
        
        // Start tracking mouse input for slicing
        isSlicing = true;
        
        Debug.Log($"Slice line created with {currentLinePoints.Count} points");
    }
    
    void GenerateSliceLine()
{
    // Choose parent based on target
    Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
    if (parentTransform == null)
    {
        Debug.LogError($"No target area found for {(targetingPlayer ? "player" : "fish")}");
        return;
    }
    
    // Generate a simple zigzag pattern
    currentLinePoints.Clear();
    pointsCompleted.Clear();
    
    // Simpler zigzag parameters
    float totalWidth = lineLength;
    float zigzagHeight = 30f; // Reduced height
    int numZigzags = 2; // Only 2 zigzag cycles instead of 3
    
    // Calculate points for simpler zigzag
    float stepX = totalWidth / lineSegments;
    float zigzagCycleLength = totalWidth / numZigzags;
    
    for (int i = 0; i <= lineSegments; i++)
    {
        float x = (i * stepX) - (totalWidth / 2); // Center the line
        
        // Simpler zigzag pattern - just up and down
        float cycleProgress = (i * stepX) / zigzagCycleLength;
        float zigzagPhase = (cycleProgress % 1.0f) * 2.0f; // Only 2 phases per cycle
        
        float y = 0;
        if (zigzagPhase < 1.0f)
        {
            // Going up
            y = Mathf.Lerp(0, zigzagHeight, zigzagPhase);
        }
        else
        {
            // Going down
            y = Mathf.Lerp(zigzagHeight, 0, zigzagPhase - 1.0f);
        }
        
        Vector2 point = new Vector2(x, y);
        currentLinePoints.Add(point);
        pointsCompleted.Add(false);
    }
    
    // Create the visual line
    CreateVisualSliceLine(parentTransform);
    
    Debug.Log($"Generated simple zigzag slice line with {currentLinePoints.Count} points");
}
    
    void CreateVisualSliceLine(Transform parent)
{
    if (sliceLinePrefab == null)
    {
        Debug.LogError("Slice line prefab is null!");
        return;
    }
    
    currentSliceLine = Instantiate(sliceLinePrefab, parent);
    activeSliceLines.Add(currentSliceLine);
    
    // Set up the visual line with the generated points
    SliceLineVisual lineVisual = currentSliceLine.GetComponent<SliceLineVisual>();
    if (lineVisual != null)
    {
        lineVisual.SetupLine(currentLinePoints.ToArray());
    }
    
    // Position it randomly within the target area and rotate it
    RectTransform lineRect = currentSliceLine.GetComponent<RectTransform>();
    if (lineRect != null)
    {
        // Random position within the panel (not too close to edges)
        float randomX = Random.Range(-50f, 50f);
        float randomY = Random.Range(-30f, 30f);
        lineRect.anchoredPosition = new Vector2(randomX, randomY);
        
        // Random rotation
        float randomRotation = Random.Range(-30f, 30f); // Up to 30 degrees rotation
        lineRect.rotation = Quaternion.Euler(0, 0, randomRotation);
    }
    
    Debug.Log($"Created visual slice line at random position and rotation");
}
    
    void TrackMouseForSlicing()
    {
        // Get mouse position in screen space
        Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        
        // Convert to local position within the target area
        Vector2 localMousePosition;
        RectTransform targetRect = (targetingPlayer ? playerTargetArea : fishTargetArea).GetComponent<RectTransform>();
        
        bool isOverTarget = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            mousePosition,
            gameCanvas.worldCamera,
            out localMousePosition
        );
        
        if (!isOverTarget) return;
        
        // Check if mouse is near any uncompleted points on the current line
        for (int i = 0; i < currentLinePoints.Count; i++)
        {
            if (pointsCompleted[i]) continue; // Skip already completed points
            
            float distance = Vector2.Distance(localMousePosition, currentLinePoints[i]);
            
            if (distance <= sliceDetectionRadius)
            {
                // Mark this point as completed
                pointsCompleted[i] = true;
                Debug.Log($"Completed point {i} of {currentLinePoints.Count}");
                
                // Update visual feedback
                UpdateSliceVisual(i);
                
                // Check if slice is complete
                CheckSliceCompletion();
                break; // Only complete one point per frame
            }
        }
    }
    
    void UpdateSliceVisual(int completedPointIndex)
{
    if (currentSliceLine != null)
    {
        SliceLineVisual lineVisual = currentSliceLine.GetComponent<SliceLineVisual>();
        if (lineVisual != null)
        {
            lineVisual.UpdateProgress(completedPointIndex);
        }
    }
}
    
    void CheckSliceCompletion()
    {
        // Count completed points
        int completedCount = 0;
        foreach (bool completed in pointsCompleted)
        {
            if (completed) completedCount++;
        }
        
        float completionPercentage = (float)completedCount / currentLinePoints.Count;
        
        Debug.Log($"Slice completion: {completedCount}/{currentLinePoints.Count} ({completionPercentage:P0})");
        
        // Check if we've completed enough of the line
        if (completionPercentage >= completionThreshold)
        {
            CompleteCurrentSlice();
        }
    }
    
    void CompleteCurrentSlice()
    {
        Debug.Log($"Slice completed! {remainingSlices - 1} slices remaining");
        
        // Clean up current slice
        isSlicing = false;
        if (currentSliceLine != null)
        {
            Destroy(currentSliceLine);
            currentSliceLine = null;
        }
        
        remainingSlices--;
        
        // Check if more slices needed
        if (remainingSlices > 0)
        {
            // Start next slice after brief delay
            StartCoroutine(NextSliceDelay());
        }
        else
        {
            // All slices complete - apply effect
            CompleteSlicingSequence();
        }
    }
    
    IEnumerator NextSliceDelay()
    {
        yield return new WaitForSeconds(0.5f); // Brief pause between slices
        StartNextSlice();
    }
    
    void CompleteSlicingSequence()
    {
        Debug.Log("All slices completed!");
        
        // Apply the action card effect through existing system
        if (currentActionCard != null && fishingManager != null)
        {
            bool success = fishingManager.PlayActionCard(currentActionCard, targetingPlayer);
            Debug.Log($"Applied slicing effect: {success}");
        }
        
        // Tell all clients to clean up
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            CleanupSlicingForAllClientsClientRpc();
        }
        
        // Reset state locally
        ResetSlicingState();
    }
    
    [ClientRpc]
    void CleanupSlicingForAllClientsClientRpc()
    {
        Debug.Log("All clients: Cleaning up slicing sequence");
        ResetSlicingState();
    }
    
    void ResetSlicingState()
    {
        // Reset state
        isSlicingActive.Value = false;
        slicingPlayerId.Value = 0;
        currentActionCard = null;
        remainingSlices = 0;
        totalSlices = 0;
        isSlicing = false;
        
        // Clear any remaining slice lines
        foreach (GameObject sliceLine in activeSliceLines)
        {
            if (sliceLine != null) Destroy(sliceLine);
        }
        activeSliceLines.Clear();
        
        currentLinePoints.Clear();
        pointsCompleted.Clear();
        
        if (currentSliceLine != null)
        {
            Destroy(currentSliceLine);
            currentSliceLine = null;
        }
    }
    
    // Helper method to find ActionCard by name (same as other managers)
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
        
        Debug.LogWarning($"Could not find ActionCard: {cardName}");
        return null;
    }
}