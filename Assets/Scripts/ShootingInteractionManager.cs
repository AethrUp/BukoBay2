using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ShootingInteractionManager : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject reticlePrefab;            // Your crosshair/reticle sprite prefab
    public GameObject targetPrefab;             // Separate prefab for targets to shoot

    public Transform playerTargetArea;          // Where reticle appears on player
    public Transform fishTargetArea;            // Where reticle appears on fish
    public Canvas gameCanvas;                   // Canvas for UI positioning

    [Header("Reticle Physics Settings")]
    public float reticleWeight = 1f;            // How heavy the reticle feels (higher = more momentum)
    public float followStrength = 5f;           // How strongly reticle follows cursor
    public float dampening = 0.8f;              // How much velocity is reduced each frame (0-1)
    public float maxVelocity = 200f;            // Maximum reticle velocity
    
    [Header("Weapon-Specific Physics")]
    public float defaultWeight = 1f;
    public float defaultFollowStrength = 5f;

    [Header("Game References")]
    public FishingManager fishingManager;

    // Current shooting session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private GameObject activeReticle;
    private Vector2 cursorPosition;
    private bool isAiming = false;
    
    // Physics-based reticle movement
    private Vector2 reticlePosition;            // Current reticle position in local space
    private Vector2 reticleVelocity = Vector2.zero;  // Current reticle velocity
    private Vector2 targetCursorPosition;       // Where the cursor is in local space
    // Multiple shot tracking
    private int shotsRemaining;
    private int totalShots;

    // Target system
    private GameObject activeTarget;
    private Vector2 targetPosition;

    // Network variables for syncing shooting state
    public NetworkVariable<bool> isShootingActive = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> shootingPlayerId = new NetworkVariable<ulong>(0);

    void Start()
    {
        if (fishingManager == null)
            fishingManager = FindFirstObjectByType<FishingManager>();

        if (gameCanvas == null)
            gameCanvas = FindFirstObjectByType<Canvas>();
    }

    void SetPhysicsForWeapon(string weaponName)
    {
        // Set different physics properties for each shooting weapon
        switch (weaponName.ToLower())
        {
            case "aries javeline":
                reticleWeight = 0.8f;        // Light - responsive
                followStrength = 6f;         // High precision - follows cursor well
                break;

            case "cosmorocket":
                reticleWeight = 2.5f;        // Very heavy - lots of momentum
                followStrength = 3f;         // Low responsiveness - hard to control
                break;

            case "elektrika 77":
                reticleWeight = 1.2f;        // Medium weight
                followStrength = 4.5f;       // Medium responsiveness
                break;

            case "lil spittle":
                reticleWeight = 0.5f;        // Very light - easy to control
                followStrength = 8f;         // Very responsive - beginner friendly
                break;

            case "rattler venom":
                reticleWeight = 1.5f;        // Heavy
                followStrength = 4f;         // Lower responsiveness - venom is tricky
                break;

            case "tootitoot":
                reticleWeight = 1f;          // Standard weight
                followStrength = 5f;         // Standard responsiveness
                break;

            case "tranq-o-catch":
                reticleWeight = 0.9f;        // Light - precision weapon
                followStrength = 6.5f;       // High precision for careful shots
                break;

            default:
                reticleWeight = defaultWeight;
                followStrength = defaultFollowStrength;
                break;
        }

        Debug.Log($"Set physics for {weaponName}: Weight={reticleWeight}, Follow={followStrength}");
    }

    void Update()
    {
        if (isAiming)
        {
            UpdatePhysicsBasedReticle();
        }
    }

    void UpdatePhysicsBasedReticle()
    {
        // Get mouse position in screen space using new Input System
        cursorPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        
        // Convert cursor position to local position within the target area
        RectTransform targetAreaRect = targetingPlayer ? 
            playerTargetArea.GetComponent<RectTransform>() : 
            fishTargetArea.GetComponent<RectTransform>();
            
        Vector2 localCursorPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetAreaRect,
            cursorPosition,
            gameCanvas.worldCamera,
            out localCursorPosition
        );
        
        targetCursorPosition = localCursorPosition;
        
        // Apply physics-based movement
        UpdateReticlePhysics();
        
        // Update reticle visual position
        if (activeReticle != null)
        {
            RectTransform reticleRect = activeReticle.GetComponent<RectTransform>();
            if (reticleRect != null)
            {
                reticleRect.anchoredPosition = reticlePosition;
            }
        }
        
        // Check for mouse click to shoot
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (CheckTargetHit())
            {
                FireShot();
            }
            else
            {
                Debug.Log("Missed target!");
            }
        }
    }
    
    void UpdateReticlePhysics()
    {
        // Calculate the force pulling reticle toward cursor
        Vector2 directionToCursor = targetCursorPosition - reticlePosition;
        Vector2 force = directionToCursor * followStrength;
        
        // Apply force to velocity (F = ma, so a = F/m)
        Vector2 acceleration = force / reticleWeight;
        reticleVelocity += acceleration * Time.deltaTime;
        
        // Apply dampening to simulate air resistance/friction
        reticleVelocity *= (1f - (1f - dampening) * Time.deltaTime * 10f);
        
        // Clamp velocity to maximum
        if (reticleVelocity.magnitude > maxVelocity)
        {
            reticleVelocity = reticleVelocity.normalized * maxVelocity;
        }
        
        // Update position based on velocity
        reticlePosition += reticleVelocity * Time.deltaTime;
        
        // Optional: Add slight bounds checking to keep reticle in reasonable area
        reticlePosition.x = Mathf.Clamp(reticlePosition.x, -300f, 300f);
        reticlePosition.y = Mathf.Clamp(reticlePosition.y, -200f, 200f);
    }

    void FireShot()
{
    Debug.Log($"Hit target! {shotsRemaining - 1} shots remaining");
    
    shotsRemaining--;
    
    // Destroy current reticle and target
    if (activeReticle != null)
    {
        Destroy(activeReticle);
        activeReticle = null;
    }
    
    if (activeTarget != null)
    {
        Destroy(activeTarget);
        activeTarget = null;
    }
    
    isAiming = false;
    
    // Check if more shots needed
    if (shotsRemaining > 0)
    {
        // Start next shot after brief delay
        StartCoroutine(NextShotDelay());
    }
    else
    {
        // All shots complete - apply effect
        CompleteShootingSequence();
    }
}

bool CheckTargetHit()
{
    if (activeTarget == null) return false;
    
    Vector2 targetPos = activeTarget.GetComponent<RectTransform>().anchoredPosition;
    
    float distance = Vector2.Distance(reticlePosition, targetPos);
    float hitRadius = 50f; // How close you need to be to hit
    
    return distance <= hitRadius;
}

IEnumerator NextShotDelay()
{
    yield return new WaitForSeconds(0.5f); // Brief pause between shots
    StartNextShot();
}

void CompleteShootingSequence()
{
    Debug.Log("All shots completed!");
    
    // Network the shot if in multiplayer
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
    {
        FireShotServerRpc(currentActionCard.actionName, targetingPlayer);
    }
    else
    {
        // Single player - apply effect directly
        ApplyShotEffect();
    }
    
    // End the shooting sequence
    EndShootingSequence();
}

    void EndShootingSequence()
{
    isAiming = false;
    
    if (activeReticle != null)
    {
        Destroy(activeReticle);
        activeReticle = null;
    }
    
    // Only reset local aiming state - network state is handled by CleanupShooting
}

    // This will be called by ActionCardDropZone
    public bool StartShootingSequence(ActionCard actionCard, bool targetPlayer, ulong playerId)
    {
        // Only allow if not already shooting and in interactive phase
        if (isShootingActive.Value || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot start shooting - already in progress or not in interactive phase");
            return false;
        }

        currentActionCard = actionCard;

        // Check if this card can target the chosen target
        bool canShootPlayer = actionCard.canTargetPlayer && actionCard.playerEffect != 0;
        bool canShootFish = actionCard.canTargetFish && actionCard.fishEffect != 0;

        if (targetPlayer && !canShootPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return false;
        }

        if (!targetPlayer && !canShootFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return false;
        }

        // Use networking if available
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            StartShootingServerRpc(actionCard.actionName, targetPlayer, playerId);
        }
        else
        {
            // Single player fallback
            StartShootingLocally(actionCard, targetPlayer);
        }

        return true;
    }

    void StartAiming()
    {
        Debug.Log("Starting aim phase");
        
        // Set weapon-specific physics settings
        if (currentActionCard != null)
        {
            SetPhysicsForWeapon(currentActionCard.actionName);
        }
        
        // Initialize reticle physics
        InitializeReticlePhysics();
        
        isAiming = true;
        
        // Create reticle
        Transform targetArea = targetingPlayer ? playerTargetArea : fishTargetArea;
        activeReticle = Instantiate(reticlePrefab, targetArea);
    }
    
    void InitializeReticlePhysics()
    {
        // Start reticle at center of target area
        reticlePosition = Vector2.zero;
        reticleVelocity = Vector2.zero;
        
        // Initialize cursor position
        cursorPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        RectTransform targetAreaRect = targetingPlayer ? 
            playerTargetArea.GetComponent<RectTransform>() : 
            fishTargetArea.GetComponent<RectTransform>();
            
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetAreaRect,
            cursorPosition,
            gameCanvas.worldCamera,
            out targetCursorPosition
        );
        
        Debug.Log($"Initialized reticle physics - Weight: {reticleWeight}, Follow: {followStrength}");
    }

   
    [ServerRpc(RequireOwnership = false)]
    public void StartShootingServerRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"HOST: StartShootingServerRpc called - {actionCardName}, targeting {(targetPlayer ? "player" : "fish")}");

        if (!IsHost)
        {
            Debug.LogWarning("StartShootingServerRpc called but not host!");
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

        // Start shooting for all clients
        StartShootingForAllClientsClientRpc(actionCardName, targetPlayer, playerId);

        Debug.Log($"HOST: Sent ClientRpc to all players");
    }

    [ClientRpc]
    public void StartShootingForAllClientsClientRpc(string actionCardName, bool targetPlayer, ulong playerId)
    {
        Debug.Log($"CLIENT: StartShootingForAllClientsClientRpc received - {actionCardName}");

        // Set shooting state
        isShootingActive.Value = true;
        shootingPlayerId.Value = playerId;
        targetingPlayer = targetPlayer;

        Debug.Log($"CLIENT: Set shooting state - targeting {(targetPlayer ? "player" : "fish")}");

       // ONLY CREATE AIMING UI FOR THE ACTING PLAYER
ulong myClientId = NetworkManager.Singleton.LocalClientId;
if (myClientId == playerId)
{
    Debug.Log($"CLIENT: I am the shooting player ({playerId}) - starting aim sequence");
    
    // Find the action card and start shooting sequence
    ActionCard actionCard = FindActionCardByName(actionCardName);
    if (actionCard != null)
    {
        currentActionCard = actionCard;
        StartMultiShotSequence(actionCard, targetPlayer);
    }
    else
    {
        Debug.LogError($"Could not find action card: {actionCardName}");
    }
}
        else
        {
            Debug.Log($"CLIENT: I am NOT the shooting player (I'm {myClientId}, shooting player is {playerId}) - no aiming UI for me");
        }
    }

    void StartShootingLocally(ActionCard actionCard, bool targetPlayer)
{
    Debug.Log($"Single player shooting: {actionCard.actionName}");
    
    isShootingActive.Value = true;
    currentActionCard = actionCard;
    targetingPlayer = targetPlayer;
    
    StartMultiShotSequence(actionCard, targetPlayer);
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

    void StartMultiShotSequence(ActionCard actionCard, bool targetPlayer)
{
    // Calculate number of shots needed
    int effectValue = targetPlayer ? actionCard.playerEffect : actionCard.fishEffect;
    totalShots = Mathf.Abs(effectValue);
    shotsRemaining = totalShots;
    
    Debug.Log($"Starting {totalShots} shot sequence with {actionCard.actionName}");
    
    if (totalShots <= 0)
    {
        Debug.LogWarning($"No shots needed for {actionCard.actionName}");
        EndShootingSequence();
        return;
    }
    
    // Start first shot
    StartNextShot();
}

void StartNextShot()
{
    Debug.Log($"Shot {totalShots - shotsRemaining + 1} of {totalShots}");
    
    // Set weapon-specific physics settings
    if (currentActionCard != null)
    {
        SetPhysicsForWeapon(currentActionCard.actionName);
    }
    
    // Initialize reticle physics for this shot
    InitializeReticlePhysics();
    
    // Create target at random position
    CreateShootingTarget();
    
    isAiming = true;
    
    // Create reticle
    Transform targetArea = targetingPlayer ? playerTargetArea : fishTargetArea;
    activeReticle = Instantiate(reticlePrefab, targetArea);
}

void CreateShootingTarget()
{
    Transform targetArea = targetingPlayer ? playerTargetArea : fishTargetArea;
    
    // Create target using separate prefab
    GameObject prefabToUse = targetPrefab != null ? targetPrefab : reticlePrefab;
    activeTarget = Instantiate(prefabToUse, targetArea);
    
    // Make sure it's visually distinct if using same prefab
    if (targetPrefab == null)
    {
        UnityEngine.UI.Image targetImage = activeTarget.GetComponent<UnityEngine.UI.Image>();
        if (targetImage != null)
        {
            targetImage.color = Color.red; // Red target if no separate prefab
        }
    }
    
    // Position target randomly within the target area
    RectTransform targetRect = activeTarget.GetComponent<RectTransform>();
    if (targetRect != null)
    {
        float randomX = Random.Range(-100f, 100f);
        float randomY = Random.Range(-100f, 100f);
        targetPosition = new Vector2(randomX, randomY);
        targetRect.anchoredPosition = targetPosition;
    }
    
    Debug.Log($"Created target at position: {targetPosition}");
}

[ServerRpc(RequireOwnership = false)]
void FireShotServerRpc(string actionCardName, bool targetingPlayer)
{
    Debug.Log($"SERVER: Shot fired with {actionCardName}");
    
    // Apply the effect on the server
    ActionCard actionCard = FindActionCardByName(actionCardName);
    if (actionCard != null && fishingManager != null)
    {
        bool success = fishingManager.PlayActionCard(actionCard, targetingPlayer);
        Debug.Log($"SERVER: Applied shooting effect: {success}");
    }
    
    // Tell all clients the shot was fired
    NotifyShotFiredClientRpc(actionCardName, targetingPlayer);
}

[ClientRpc]
void NotifyShotFiredClientRpc(string actionCardName, bool targetingPlayer)
{
    Debug.Log($"ALL CLIENTS: {actionCardName} shot fired targeting {(targetingPlayer ? "player" : "fish")}");
    
    // All clients reset their shooting state
    CleanupShooting();
}

void ApplyShotEffect()
{
    // Single player effect application
    if (currentActionCard != null && fishingManager != null)
    {
        bool success = fishingManager.PlayActionCard(currentActionCard, targetingPlayer);
        Debug.Log($"Applied shooting effect: {success}");
    }
}

void CleanupShooting()
{
    // Reset state for all clients
    isShootingActive.Value = false;
    shootingPlayerId.Value = 0;
    currentActionCard = null;
}
}