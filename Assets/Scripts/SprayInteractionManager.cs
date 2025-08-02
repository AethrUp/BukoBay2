using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class SprayInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject sprayAreaPrefab;          // The area that needs to be filled
    public Transform playerTargetArea;          // Where spray areas appear on player
    public Transform fishTargetArea;            // Where spray areas appear on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning
    
    [Header("Spray Detection")]
    public float sprayRadius = 15f;             // Radius of the spray cursor
    public float completionThreshold = 0.9f;   // How much of the area needs to be filled (80%)
    
    [Header("Area Generation")]
    public float areaWidth = 150f;              // Width of areas to fill
    public float areaHeight = 100f;             // Height of areas to fill
    public int fillResolution = 20;             // Grid resolution for tracking fill
    
    [Header("Game References")]
    public FishingManager fishingManager;
    
    // Current spraying session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private int remainingSprays;
    private int totalSprays;
    
    // Active spray tracking
    private List<GameObject> activeSprayAreas = new List<GameObject>();
    private GameObject currentSprayArea;
    private bool[,] fillGrid;                   // 2D grid to track filled areas
    private bool isSpraying = false;
    
    // Network variables for syncing spray state
    public NetworkVariable<bool> isSprayingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> sprayingPlayerId = new NetworkVariable<ulong>(0);
    
    // Spray action card names
    private readonly string[] sprayActionCards = {
        "Bakunawa",
        "BileCow",
        "SalveGipnotizi",
        "Ochkii",
        "Lip Slip",
        "Magno WOW",
        "Mikrowev M3",
        "Molniya K",
        "QuickFire",
        "Red Eye",
        "Zavyshennii ZZ"
    };
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"SprayInteractionManager spawned - IsHost: {IsHost}, IsClient: {IsClient}");
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
        if (isSpraying)
        {
            TrackMouseForSpraying();
        }
    }
    
    /// <summary>
    /// Called when a spray action card is dropped on a target area
    /// </summary>
    public bool StartSpraySequence(ActionCard actionCard, bool targetPlayer, ulong playerId)
    {
        // Only allow if not already spraying and in interactive phase
        if (isSprayingActive.Value || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot start spraying - already in progress or not in interactive phase");
            return false;
        }
        
        currentActionCard = actionCard;
        
        // Check if this card can target the chosen target
        bool canSprayPlayer = actionCard.canTargetPlayer && actionCard.playerEffect != 0;
        bool canSprayFish = actionCard.canTargetFish && actionCard.fishEffect != 0;
        
        if (targetPlayer && !canSprayPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }
        
        if (!targetPlayer && !canSprayFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }
        
        // Use RPC system for networking
        StartSprayServerRpc(actionCard.actionName, targetPlayer, playerId);
        
        return true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void StartSprayServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"HOST: StartSprayServerRpc called - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");
        
        if (!IsHost) 
        {
            Debug.LogWarning("StartSprayServerRpc called but not host!");
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
        
        // Calculate number of sprays needed
        int sprayCount = targetPlayer ? Mathf.Abs(actionCard.playerEffect) : Mathf.Abs(actionCard.fishEffect);
        
        Debug.Log($"HOST: Spray count calculated as {sprayCount}");
        
        if (sprayCount <= 0)
        {
            Debug.LogWarning($"No sprays needed for {actionCardName} on {(targetPlayer ? "player" : "fish")}");
            return;
        }
        
        // Start spraying for all clients
        StartSprayForAllClientsClientRpc(actionCardName, targetPlayer, playerId, sprayCount);
        
        Debug.Log($"HOST: Sent ClientRpc to all players");
    }
    
    [ClientRpc]
    public void StartSprayForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId, int sprayCount)
    {
        Debug.Log($"CLIENT: StartSprayForAllClientsClientRpc received - {actionCardName}, {sprayCount} sprays");
        
        // Set spraying state for all clients
        isSprayingActive.Value = true;
        sprayingPlayerId.Value = playerId;
        targetingPlayer = targetPlayer;
        totalSprays = sprayCount;
        remainingSprays = sprayCount;
        
        Debug.Log($"CLIENT: Set spraying state - targeting {(targetPlayer ? "player" : "fish")}, {remainingSprays} sprays");
        
        // Only create spraying UI for the acting player
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        if (myClientId == playerId)
        {
            Debug.Log($"CLIENT: I am the spraying player ({playerId}) - creating spray areas");
            
            // Find the action card and start spraying
            ActionCard actionCard = FindActionCardByName(actionCardName);
            if (actionCard != null)
            {
                currentActionCard = actionCard;
                StartNextSpray();
            }
            else
            {
                Debug.LogError($"Could not find action card: {actionCardName}");
            }
        }
        else
        {
            Debug.Log($"CLIENT: I am NOT the spraying player (I'm {myClientId}, spraying player is {playerId}) - no spraying UI for me");
        }
    }
    
    void StartNextSpray()
    {
        Debug.Log($"Starting spray {totalSprays - remainingSprays + 1} of {totalSprays}");
        
        // Generate a spray area for this spray
        GenerateSprayArea();
        
        // Start tracking mouse input for spraying
        isSpraying = true;
        
        Debug.Log($"Spray area created with {fillResolution}x{fillResolution} grid");
    }
    
    void GenerateSprayArea()
    {
        // Choose parent based on target
        Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
        if (parentTransform == null)
        {
            Debug.LogError($"No target area found for {(targetingPlayer ? "player" : "fish")}");
            return;
        }
        
        // Initialize the fill grid
        fillGrid = new bool[fillResolution, fillResolution];
        
        // Create the visual spray area
        CreateVisualSprayArea(parentTransform);
        
        Debug.Log($"Generated spray area with {fillResolution}x{fillResolution} fill grid");
    }
    
    void CreateVisualSprayArea(Transform parent)
    {
        if (sprayAreaPrefab == null)
        {
            Debug.LogError("Spray area prefab is null!");
            return;
        }
        
        currentSprayArea = Instantiate(sprayAreaPrefab, parent);
        activeSprayAreas.Add(currentSprayArea);
        
        // Position it randomly within the target area
        RectTransform areaRect = currentSprayArea.GetComponent<RectTransform>();
        if (areaRect != null)
        {
            // Set size
            areaRect.sizeDelta = new Vector2(areaWidth, areaHeight);
            
            // Random position within the panel
            float randomX = Random.Range(-50f, 50f);
            float randomY = Random.Range(-30f, 30f);
            areaRect.anchoredPosition = new Vector2(randomX, randomY);
        }
        
        // Initialize the visual component
        SprayAreaVisual areaVisual = currentSprayArea.GetComponent<SprayAreaVisual>();
        if (areaVisual != null)
        {
            areaVisual.InitializeGrid(fillResolution, areaWidth, areaHeight);
            Debug.Log($"Initialized SprayAreaVisual with {fillResolution}x{fillResolution} grid");
        }
        else
        {
            Debug.LogError("SprayAreaVisual component not found on prefab!");
        }
        
        Debug.Log($"Created visual spray area at random position");
    }
    
    void TrackMouseForSpraying()
    {
        // Get mouse position in screen space
        Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        
        // Convert to local position within the current spray area
        Vector2 localMousePosition;
        if (currentSprayArea != null)
        {
            RectTransform sprayAreaRect = currentSprayArea.GetComponent<RectTransform>();
            
            bool isOverArea = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sprayAreaRect,
                mousePosition,
                gameCanvas.worldCamera,
                out localMousePosition
            );
            
            if (isOverArea)
            {
                // Check if mouse button is held down for spraying
                bool isMouseDown = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
                
                if (isMouseDown)
                {
                    Debug.Log($"Spraying at position: {localMousePosition}");
                    FillAreaAtPosition(localMousePosition);
                }
            }
        }
    }
    
    void FillAreaAtPosition(Vector2 localPosition)
    {
        // Convert local position to grid coordinates
        float normalizedX = (localPosition.x + areaWidth / 2) / areaWidth;
        float normalizedY = (localPosition.y + areaHeight / 2) / areaHeight;
        
        // Clamp to grid bounds
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);
        
        int gridX = Mathf.FloorToInt(normalizedX * fillResolution);
        int gridY = Mathf.FloorToInt(normalizedY * fillResolution);
        
        // Clamp grid coordinates
        gridX = Mathf.Clamp(gridX, 0, fillResolution - 1);
        gridY = Mathf.Clamp(gridY, 0, fillResolution - 1);
        
        // Fill the area around the cursor (spray effect)
        int sprayRadiusInGrid = Mathf.RoundToInt(sprayRadius / (areaWidth / fillResolution));
        
        for (int x = -sprayRadiusInGrid; x <= sprayRadiusInGrid; x++)
        {
            for (int y = -sprayRadiusInGrid; y <= sprayRadiusInGrid; y++)
            {
                int fillX = gridX + x;
                int fillY = gridY + y;
                
                // Check bounds
                if (fillX >= 0 && fillX < fillResolution && fillY >= 0 && fillY < fillResolution)
                {
                    // Check if within spray radius
                    float distance = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                    if (distance <= sprayRadiusInGrid)
                    {
                        if (!fillGrid[fillX, fillY])
                        {
                            fillGrid[fillX, fillY] = true;
                            UpdateSprayVisual(fillX, fillY);
                        }
                    }
                }
            }
        }
        
        // Check completion
        CheckSprayCompletion();
    }
    
    void UpdateSprayVisual(int gridX, int gridY)
    {
        // Update visual feedback for filled area
        // This could be implemented by the spray area prefab component
        if (currentSprayArea != null)
        {
            SprayAreaVisual areaVisual = currentSprayArea.GetComponent<SprayAreaVisual>();
            if (areaVisual != null)
            {
                areaVisual.FillGridCell(gridX, gridY);
            }
        }
    }
    
    void CheckSprayCompletion()
    {
        // Count filled cells
        int filledCount = 0;
        int totalCells = fillResolution * fillResolution;
        
        for (int x = 0; x < fillResolution; x++)
        {
            for (int y = 0; y < fillResolution; y++)
            {
                if (fillGrid[x, y])
                {
                    filledCount++;
                }
            }
        }
        
        float completionPercentage = (float)filledCount / totalCells;
        
        Debug.Log($"Spray completion: {filledCount}/{totalCells} ({completionPercentage:P0})");
        
        // Check if we've filled enough of the area
        if (completionPercentage >= completionThreshold)
        {
            CompleteCurrentSpray();
        }
    }
    
    void CompleteCurrentSpray()
    {
        Debug.Log($"Spray completed! {remainingSprays - 1} sprays remaining");
        
        // Clean up current spray
        isSpraying = false;
        if (currentSprayArea != null)
        {
            SprayAreaVisual areaVisual = currentSprayArea.GetComponent<SprayAreaVisual>();
            if (areaVisual != null)
            {
                areaVisual.MarkAsCompleted();
            }
            
            Destroy(currentSprayArea);
            currentSprayArea = null;
        }
        
        remainingSprays--;
        
        // Check if more sprays needed
        if (remainingSprays > 0)
        {
            // Start next spray after brief delay
            StartCoroutine(NextSprayDelay());
        }
        else
        {
            // All sprays complete - apply effect
            CompleteSpraySequence();
        }
    }
    
    IEnumerator NextSprayDelay()
    {
        yield return new WaitForSeconds(0.5f); // Brief pause between sprays
        StartNextSpray();
    }
    
    void CompleteSpraySequence()
    {
        Debug.Log("All sprays completed!");
        
        // Apply the action card effect through existing system
        if (currentActionCard != null && fishingManager != null)
        {
            bool success = fishingManager.PlayActionCard(currentActionCard, targetingPlayer);
            Debug.Log($"Applied spray effect: {success}");
        }
        
        // Tell all clients to clean up
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            CleanupSprayForAllClientsClientRpc();
        }
        
        // Reset state locally
        ResetSprayState();
    }
    
    [ClientRpc]
    void CleanupSprayForAllClientsClientRpc()
    {
        Debug.Log("All clients: Cleaning up spray sequence");
        ResetSprayState();
    }
    
    void ResetSprayState()
    {
        // Reset state
        isSprayingActive.Value = false;
        sprayingPlayerId.Value = 0;
        currentActionCard = null;
        remainingSprays = 0;
        totalSprays = 0;
        isSpraying = false;
        
        // Clear any remaining spray areas
        foreach (GameObject sprayArea in activeSprayAreas)
        {
            if (sprayArea != null) Destroy(sprayArea);
        }
        activeSprayAreas.Clear();
        
        if (currentSprayArea != null)
        {
            Destroy(currentSprayArea);
            currentSprayArea = null;
        }
        
        fillGrid = null;
    }
    
    // Helper method to check if an action card is a spray type
    public bool IsSprayActionCard(string cardName)
    {
        foreach (string sprayCard in sprayActionCards)
        {
            if (cardName.Equals(sprayCard, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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
        
        Debug.LogWarning($"Could not find ActionCard: {cardName}");
        return null;
    }
}