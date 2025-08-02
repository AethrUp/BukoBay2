using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class DrinkingInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject containerPrefab;          // Container sprite with liquid
    public GameObject liquidGaugePrefab;        // Fuel gauge UI
    public GameObject liquidParticlesPrefab;    // Pouring particle effects
    public Transform playerTargetArea;          // Where containers appear on player
    public Transform fishTargetArea;            // Where containers appear on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning

    [Header("Physics Settings")]
    public float tiltSensitivity = 1f;          // How responsive tilting is to mouse movement
    public float gravityStrength = 1f;          // Base gravity affecting liquid flow
    public float pourThreshold = 30f;           // Degrees of tilt needed to start pouring
    public float maxTiltAngle = 180f;           // Maximum container tilt (upside down)
    public float emptyTiltThreshold = 120f;     // Angle needed to drain last drops
    
    [Header("Visual Settings")]
    public float particleSpawnRate = 10f;       // Particles per second when pouring
    public Color defaultLiquidColor = Color.blue;

    [Header("Game References")]
    public FishingManager fishingManager;

    // Current drinking session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private GameObject activeContainer;
    private GameObject activeLiquidGauge;
    private GameObject activePourEffect;
    private bool isDrinking = false;

    // Container physics
    private float currentTiltAngle = 0f;
    private float targetTiltAngle = 0f;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    
    // Liquid properties
    private LiquidProperties currentLiquid;
    private float remainingLiquid = 1f;         // 0-1 range
    private bool isPouring = false;

    // Network variables
    public NetworkVariable<bool> isDrinkingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> drinkingPlayerId = new NetworkVariable<ulong>(0);

    // Drinking action card names
    private readonly string[] drinkingActionCards = {
        "CoCoa Kola",
        "Coffee", 
        "Elixir of Strength",
        "Elixir of Weakness",
        "Moon Kombucha",
        "Prairie Dew",
        "Rybak Vodka",
        "Sarsasparilla"
    };

    public override void OnNetworkSpawn()
    {
        Debug.Log($"DrinkingInteractionManager spawned - IsHost: {IsHost}, IsClient: {IsClient}");
        base.OnNetworkSpawn();
    }

    void Start()
    {
        if (fishingManager == null)
            fishingManager = FindFirstObjectByType<FishingManager>();

        if (gameCanvas == null)
            gameCanvas = FindFirstObjectByType<Canvas>();
            
        // Ensure tilt angle is set correctly
        if (maxTiltAngle < 180f)
        {
            Debug.LogWarning($"maxTiltAngle was {maxTiltAngle}, setting to 180 for full rotation");
            maxTiltAngle = 180f;
        }
    }

    void Update()
    {
        if (isDrinking)
        {
            UpdateContainerTilting();
            UpdateLiquidFlow();
        }
    }

    /// <summary>
    /// Called when a drinking action card is dropped on a target area
    /// </summary>
    public bool StartDrinkingSequence(ActionCard actionCard, bool targetPlayer, ulong playerId)
    {
        // Only allow if not already drinking and in interactive phase
        if (isDrinkingActive.Value || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot start drinking - already in progress or not in interactive phase");
            return false;
        }

        currentActionCard = actionCard;

        // Check if this card can target the chosen target
        bool canDrinkPlayer = actionCard.canTargetPlayer && actionCard.playerEffect != 0;
        bool canDrinkFish = actionCard.canTargetFish && actionCard.fishEffect != 0;

        if (targetPlayer && !canDrinkPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }

        if (!targetPlayer && !canDrinkFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }

        // Use RPC system for networking if available
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            StartDrinkingServerRpc(actionCard.actionName, targetPlayer, playerId);
        }
        else
        {
            // Single player fallback
            StartDrinkingLocally(actionCard, targetPlayer);
        }

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartDrinkingServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"HOST: StartDrinkingServerRpc called - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");

        if (!IsHost)
        {
            Debug.LogWarning("StartDrinkingServerRpc called but not host!");
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

        // Start drinking for all clients
        StartDrinkingForAllClientsClientRpc(actionCardName, targetPlayer, playerId);

        Debug.Log($"HOST: Sent ClientRpc to all players");
    }

    [ClientRpc]
    public void StartDrinkingForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"CLIENT: StartDrinkingForAllClientsClientRpc received - {actionCardName}");

        // Set drinking state for all clients
        isDrinkingActive.Value = true;
        drinkingPlayerId.Value = playerId;
        targetingPlayer = targetPlayer;

        Debug.Log($"CLIENT: Set drinking state - targeting {(targetPlayer ? "player" : "fish")}");

        // Only create drinking UI for the acting player
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        if (myClientId == playerId)
        {
            Debug.Log($"CLIENT: I am the drinking player ({playerId}) - creating container");

            // Find the action card and start drinking
            ActionCard actionCard = FindActionCardByName(actionCardName);
            if (actionCard != null)
            {
                currentActionCard = actionCard;
                StartDrinkingSession();
            }
            else
            {
                Debug.LogError($"Could not find action card: {actionCardName}");
            }
        }
        else
        {
            Debug.Log($"CLIENT: I am NOT the drinking player (I'm {myClientId}, drinking player is {playerId}) - no drinking UI for me");
        }
    }

    void StartDrinkingLocally(ActionCard actionCard, bool targetPlayer)
    {
        Debug.Log($"Single player drinking: {actionCard.actionName}");
        
        isDrinkingActive.Value = true;
        currentActionCard = actionCard;
        targetingPlayer = targetPlayer;
        
        StartDrinkingSession();
    }

    void StartDrinkingSession()
    {
        Debug.Log("Starting drinking session");

        // Set liquid properties based on the drink
        SetLiquidProperties(currentActionCard.actionName);

        // Initialize drinking state
        remainingLiquid = 1f;
        currentTiltAngle = 0f;
        targetTiltAngle = 0f;
        isPouring = false;
        isDrinking = true;
        isDragging = false;

        // Create container and UI
        CreateDrinkingContainer();
        CreateLiquidGauge();

        Debug.Log($"Drinking session started for {currentActionCard.actionName}");
    }

    void CreateDrinkingContainer()
    {
        if (containerPrefab == null)
        {
            Debug.LogError("Container prefab is null!");
            return;
        }

        // Choose parent based on target
        Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
        if (parentTransform == null)
        {
            Debug.LogError($"No target area found for {(targetingPlayer ? "player" : "fish")}");
            return;
        }

        // Create container
        activeContainer = Instantiate(containerPrefab, parentTransform);

        // Position container in center
        RectTransform containerRect = activeContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(currentLiquid.containerWidth, currentLiquid.containerHeight);
        }

        // Set up container visual (liquid color, etc.)
        SetupContainerVisual();

        Debug.Log($"Created drinking container for {currentActionCard.actionName}");
    }

    void SetupContainerVisual()
    {
        if (activeContainer == null) return;

        // Find liquid sprite in container and set color
        Transform liquidTransform = activeContainer.transform.Find("Liquid");
        if (liquidTransform != null)
        {
            UnityEngine.UI.Image liquidImage = liquidTransform.GetComponent<UnityEngine.UI.Image>();
            if (liquidImage != null)
            {
                liquidImage.color = currentLiquid.liquidColor;
            }
        }
    }

    void CreateLiquidGauge()
    {
        if (liquidGaugePrefab == null)
        {
            Debug.LogWarning("Liquid gauge prefab is null - no gauge will be shown");
            return;
        }

        // Choose parent based on target
        Transform parentTransform = targetingPlayer ? playerTargetArea : fishTargetArea;
        if (parentTransform == null) return;

        // Create gauge
        activeLiquidGauge = Instantiate(liquidGaugePrefab, parentTransform);

        // Position gauge next to container
        RectTransform gaugeRect = activeLiquidGauge.GetComponent<RectTransform>();
        if (gaugeRect != null)
        {
            Vector2 gaugePosition = new Vector2(currentLiquid.containerWidth / 2 + 60f, 0f);
            gaugeRect.anchoredPosition = gaugePosition;
        }

        Debug.Log("Created liquid gauge");
    }

    void UpdateContainerTilting()
    {
        Vector2 currentMousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        bool mousePressed = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
        bool mouseJustPressed = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseReleased = UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame;

        // Handle mouse input for dragging
        if (mouseJustPressed && !isDragging)
        {
            // Start dragging - check if mouse is over container area
            if (IsMouseOverContainer(currentMousePosition))
            {
                isDragging = true;
                dragStartPosition = currentMousePosition;
                Debug.Log("Started dragging container");
            }
        }
        else if (mouseReleased && isDragging)
        {
            // Stop dragging - container returns to upright
            isDragging = false;
            targetTiltAngle = 0f;
            Debug.Log("Stopped dragging container");
        }

        // Update tilt angle based on drag
        if (isDragging && mousePressed)
        {
            // Calculate tilt based on vertical mouse movement (up = more tilt)
            Vector2 dragDelta = currentMousePosition - dragStartPosition;
            
            // Convert vertical mouse movement to tilt angle
            // Test: Try positive dragDelta.y (if this feels wrong, we'll switch back)
            float verticalMovement = dragDelta.y; 
            targetTiltAngle = (verticalMovement * tiltSensitivity);
            
            // Clamp tilt angle
            targetTiltAngle = Mathf.Clamp(targetTiltAngle, 0f, maxTiltAngle);
            
            Debug.Log($"Drag: {dragDelta.y:F1}, Vertical: {verticalMovement:F1}, Target: {targetTiltAngle:F1}, Max: {maxTiltAngle}");
        }
        else if (!isDragging)
        {
            // Return to upright when not dragging
            targetTiltAngle = Mathf.Lerp(targetTiltAngle, 0f, Time.deltaTime * 3f);
        }

        // Smoothly interpolate to target angle
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, Time.deltaTime * 8f);

        // Apply rotation to container (positive angle = clockwise rotation)
        if (activeContainer != null)
        {
            activeContainer.transform.rotation = Quaternion.Euler(0, 0, currentTiltAngle);
        }

        // Check if we should start/stop pouring
        bool shouldPour = currentTiltAngle > pourThreshold && remainingLiquid > 0f;
        
        if (shouldPour && !isPouring)
        {
            StartPouring();
        }
        else if (!shouldPour && isPouring)
        {
            StopPouring();
        }
    }

    bool IsMouseOverContainer(Vector2 screenPosition)
    {
        if (activeContainer == null) return false;

        // Convert screen position to local position within the target area
        Vector2 localPosition;
        RectTransform targetAreaRect = targetingPlayer ? 
            playerTargetArea.GetComponent<RectTransform>() : 
            fishTargetArea.GetComponent<RectTransform>();

        bool isOver = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetAreaRect,
            screenPosition,
            gameCanvas.worldCamera,
            out localPosition
        );

        if (!isOver) return false;

        // Check if within a reasonable distance of the container
        RectTransform containerRect = activeContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            float distance = Vector2.Distance(localPosition, containerRect.anchoredPosition);
            return distance < (currentLiquid.containerWidth + currentLiquid.containerHeight) / 2f;
        }

        return true; // Fallback - allow dragging anywhere in target area
    }

    void UpdateLiquidFlow()
    {
        if (!isPouring || remainingLiquid <= 0f) return;

        // Calculate base tilt factor (how much the container is tilted)
        float tiltFactor = (currentTiltAngle - pourThreshold) / (maxTiltAngle - pourThreshold);
        tiltFactor = Mathf.Clamp01(tiltFactor);

        // Calculate liquid accessibility factor based on remaining liquid and tilt
        // Less liquid requires more extreme tilting to reach the spout
        float liquidAccessibilityFactor = CalculateLiquidAccessibility(remainingLiquid, currentTiltAngle);

        // Viscosity affects flow speed (higher viscosity = slower flow)
        float viscosityFactor = 1f / currentLiquid.viscosity;

        // Gravity and base flow rate
        float baseFlowRate = currentLiquid.flowRate * gravityStrength;

        // Combine all factors
        float actualFlowRate = baseFlowRate * tiltFactor * liquidAccessibilityFactor * viscosityFactor;

        // Apply flow rate
        remainingLiquid -= actualFlowRate * Time.deltaTime;
        remainingLiquid = Mathf.Max(0f, remainingLiquid);

        // Update visuals
        UpdateLiquidGauge();
        UpdateContainerLiquidLevel();

        // Check if finished
        if (remainingLiquid <= 0f)
        {
            CompleteDrinking();
        }

        Debug.Log($"Pouring: {remainingLiquid:F2} remaining, tilt: {currentTiltAngle:F1}°, flow: {actualFlowRate:F3}");
    }

    float CalculateLiquidAccessibility(float liquidAmount, float tiltAngle)
    {
        // When container is full, liquid pours easily at low angles
        // When container is nearly empty, need extreme tilting to get last drops
        
        // Calculate minimum angle needed to pour at this liquid level
        float minAngleForThisLevel = Mathf.Lerp(pourThreshold, emptyTiltThreshold, 1f - liquidAmount);
        
        if (tiltAngle < minAngleForThisLevel)
        {
            // Not tilted enough for this liquid level - no flow
            return 0f;
        }
        
        // Calculate how much "extra" tilt we have beyond the minimum needed
        float excessTilt = tiltAngle - minAngleForThisLevel;
        float maxExcessTilt = maxTiltAngle - minAngleForThisLevel;
        
        if (maxExcessTilt <= 0f) return 1f;
        
        // Return a factor based on how much extra tilt we have
        float accessibilityFactor = Mathf.Clamp01(excessTilt / maxExcessTilt);
        
        // Apply curve to make it feel more realistic
        // Square root makes it easier to start flowing, but harder to get last drops
        return Mathf.Sqrt(accessibilityFactor);
    }

    void StartPouring()
    {
        isPouring = true;
        
        // Create pour effect
        CreatePourEffect();
        
        Debug.Log($"Started pouring at {currentTiltAngle:F1} degrees");
    }

    void StopPouring()
    {
        isPouring = false;
        
        // Stop pour effect
        if (activePourEffect != null)
        {
            Destroy(activePourEffect);
            activePourEffect = null;
        }
        
        Debug.Log("Stopped pouring");
    }

    void CreatePourEffect()
    {
        if (liquidParticlesPrefab == null || activeContainer == null) return;

        // Create pour effect at container spout
        activePourEffect = Instantiate(liquidParticlesPrefab, activeContainer.transform);
        
        // Position at container spout based on tilt angle
        RectTransform effectRect = activePourEffect.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            // Calculate spout position based on tilt angle
            // As container tilts, the spout moves to the "low" corner
            float containerHalfWidth = currentLiquid.containerWidth / 2f;
            float containerHalfHeight = currentLiquid.containerHeight / 2f;
            
            // Position at the corner that would be lowest when tilted
            Vector2 spoutPosition = new Vector2(containerHalfWidth, -containerHalfHeight);
            effectRect.anchoredPosition = spoutPosition;
            
            // Adjust particle direction based on tilt
            effectRect.rotation = Quaternion.Euler(0, 0, -currentTiltAngle + 90f);
        }

        // Set particle color to match liquid
        ParticleSystem particles = activePourEffect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main;
            main.startColor = currentLiquid.liquidColor;
            
            // Adjust particle speed based on flow rate
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-(50f + currentTiltAngle));
        }
    }

    void UpdateLiquidGauge()
    {
        if (activeLiquidGauge == null) return;

        // Find gauge fill image
        Transform fillTransform = activeLiquidGauge.transform.Find("Fill");
        if (fillTransform != null)
        {
            UnityEngine.UI.Image fillImage = fillTransform.GetComponent<UnityEngine.UI.Image>();
            if (fillImage != null)
            {
                fillImage.fillAmount = remainingLiquid;
                fillImage.color = currentLiquid.liquidColor;
            }
        }
    }

    void UpdateContainerLiquidLevel()
    {
        if (activeContainer == null) return;

        // Find liquid sprite in container
        Transform liquidTransform = activeContainer.transform.Find("Liquid");
        if (liquidTransform != null)
        {
            UnityEngine.UI.Image liquidImage = liquidTransform.GetComponent<UnityEngine.UI.Image>();
            if (liquidImage != null)
            {
                liquidImage.fillAmount = remainingLiquid;
            }
        }
    }

    void CompleteDrinking()
    {
        Debug.Log("Drinking completed!");

        // Stop pouring
        StopPouring();
        isDrinking = false;

        // Apply the action card effect through existing system
        if (currentActionCard != null && fishingManager != null)
        {
            bool success = fishingManager.PlayActionCard(currentActionCard, targetingPlayer);
            Debug.Log($"Applied drinking effect: {success}");
        }

        // Tell all clients to clean up (networking) or clean up locally (single player)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            CleanupDrinkingForAllClientsClientRpc();
        }
        else
        {
            // Single player - clean up directly
            ResetDrinkingState();
        }
    }

    [ClientRpc]
    void CleanupDrinkingForAllClientsClientRpc()
    {
        Debug.Log("All clients: Cleaning up drinking sequence");
        ResetDrinkingState();
    }

    void ResetDrinkingState()
    {
        // Reset state
        isDrinkingActive.Value = false;
        drinkingPlayerId.Value = 0;
        currentActionCard = null;
        isDrinking = false;
        isPouring = false;
        remainingLiquid = 1f;
        currentTiltAngle = 0f;
        targetTiltAngle = 0f;

        // Clean up UI elements
        if (activeContainer != null)
        {
            Destroy(activeContainer);
            activeContainer = null;
        }

        if (activeLiquidGauge != null)
        {
            Destroy(activeLiquidGauge);
            activeLiquidGauge = null;
        }

        if (activePourEffect != null)
        {
            Destroy(activePourEffect);
            activePourEffect = null;
        }
    }

    // Helper method to check if an action card is a drinking type
    public bool IsDrinkingActionCard(string cardName)
    {
        foreach (string drinkCard in drinkingActionCards)
        {
            if (cardName.Equals(drinkCard, System.StringComparison.OrdinalIgnoreCase))
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

    void SetLiquidProperties(string drinkName)
    {
        currentLiquid = GetLiquidProperties(drinkName);
        Debug.Log($"Set liquid properties for {drinkName}: Flow={currentLiquid.flowRate}, Viscosity={currentLiquid.viscosity}");
    }

    LiquidProperties GetLiquidProperties(string drinkName)
    {
        // Configure properties for each drink
        switch (drinkName.ToLower())
        {
            case "cocoa kola":
                return new LiquidProperties
                {
                    containerWidth = 60f,   // Reduced from 120f
                    containerHeight = 90f,  // Reduced from 180f
                    flowRate = 0.8f,        // Medium flow - carbonated
                    viscosity = 1.2f,       // Slightly thick
                    liquidColor = new Color(0.4f, 0.2f, 0.1f, 1f) // Dark brown
                };

            case "coffee":
                return new LiquidProperties
                {
                    containerWidth = 80f,    // Reduced from 100f
                    containerHeight = 100f,  // Reduced from 140f
                    flowRate = 1.2f,        // Fast flow - hot liquid
                    viscosity = 0.9f,       // Thin
                    liquidColor = new Color(0.3f, 0.15f, 0.05f, 1f) // Dark coffee brown
                };

            case "elixir of strength":
                return new LiquidProperties
                {
                    containerWidth = 40f,   // Small vial
                    containerHeight = 60f,
                    flowRate = 0.3f,        // Very slow - thick magical potion
                    viscosity = 3f,         // Very viscous
                    liquidColor = new Color(1f, 0.2f, 0.2f, 1f) // Bright red
                };

            case "elixir of weakness":
                return new LiquidProperties
                {
                    containerWidth = 40f,   // Small vial
                    containerHeight = 60f,
                    flowRate = 0.3f,        // Very slow - thick magical potion
                    viscosity = 3f,         // Very viscous
                    liquidColor = new Color(0.5f, 0.2f, 0.8f, 1f) // Purple
                };

            case "moon kombucha":
                return new LiquidProperties
                {
                    containerWidth = 55f,   // Medium bottle
                    containerHeight = 80f,
                    flowRate = 0.9f,        // Medium-fast flow
                    viscosity = 1.1f,       // Slightly thick - fermented
                    liquidColor = new Color(0.8f, 0.9f, 0.3f, 1f) // Pale yellow-green
                };

            case "prairie dew":
                return new LiquidProperties
                {
                    containerWidth = 65f,   // Large can
                    containerHeight = 100f,
                    flowRate = 1.5f,        // Fast flow - thin soda
                    viscosity = 0.8f,       // Very thin
                    liquidColor = new Color(0.4f, 0.9f, 0.2f, 1f) // Bright green
                };

            case "rybak vodka":
                return new LiquidProperties
                {
                    containerWidth = 45f,   // Tall thin bottle
                    containerHeight = 110f,
                    flowRate = 1.3f,        // Fast flow - alcohol
                    viscosity = 0.7f,       // Very thin
                    liquidColor = new Color(0.9f, 0.9f, 0.9f, 0.8f) // Clear/transparent
                };

            case "sarsasparilla":
                return new LiquidProperties
                {
                    containerWidth = 50f,   // Standard bottle
                    containerHeight = 80f,
                    flowRate = 1f,          // Standard flow
                    viscosity = 1f,         // Standard viscosity
                    liquidColor = new Color(0.6f, 0.3f, 0.1f, 1f) // Root beer brown
                };

            default:
                return new LiquidProperties
                {
                    containerWidth = 50f,   // Reasonable default size
                    containerHeight = 75f,
                    flowRate = 1f,
                    viscosity = 1f,
                    liquidColor = defaultLiquidColor
                };
        }
    }
}

[System.Serializable]
public class LiquidProperties
{
    public float containerWidth = 100f;
    public float containerHeight = 150f;
    public float flowRate = 1f;         // How fast liquid pours (higher = faster)
    public float viscosity = 1f;        // How thick the liquid is (higher = slower)
    public Color liquidColor = Color.blue;
}