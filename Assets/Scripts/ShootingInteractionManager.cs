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

    [Header("Default Sway Settings")]
    public float defaultSwayRadius = 50f;
    public float defaultSwaySpeed = 2f;

    [Header("Current Sway (Runtime)")]
    public float swayRadius = 50f;              // Current sway radius
    public float swaySpeed = 2f;                // Current sway speed
    public float aimTime = 3f;                  // Current aim time

    [Header("Game References")]
    public FishingManager fishingManager;

    // Current shooting session data
    private ActionCard currentActionCard;
    private bool targetingPlayer;
    private GameObject activeReticle;
    private Vector2 cursorPosition;
    private Vector2 swayOffset;
    private bool isAiming = false;
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

    void SetSwayForWeapon(string weaponName)
    {
        // Set different sway patterns for each shooting weapon
        switch (weaponName.ToLower())
        {
            case "aries javeline":
                swayRadius = 30f;    // Small sway - precise javelin
                swaySpeed = 1.5f;    // Slow movement - easier to aim
                break;

            case "cosmorocket":
                swayRadius = 80f;    // Large sway - heavy rocket
                swaySpeed = 3f;      // Fast movement - harder to control
                break;

            case "elektrika 77":
                swayRadius = 40f;    // Medium sway
                swaySpeed = 2.5f;    // Medium speed
                break;

            case "lil spittle":
                swayRadius = 20f;    // Very small sway - easy to aim
                swaySpeed = 1f;      // Very slow - simple weapon
                break;

            case "rattler venom":
                swayRadius = 45f;    // Medium-large sway
                swaySpeed = 4f;      // Fast - venom is quick
                break;

            case "tootitoot":
                swayRadius = 35f;    // Small-medium sway
                swaySpeed = 2f;      // Standard speed
                break;

            case "tranq-o-catch":
                swayRadius = 25f;    // Small sway - precision tranquilizer
                swaySpeed = 1.8f;    // Slow-medium - careful aim
                break;

            default:
                swayRadius = defaultSwayRadius;
                swaySpeed = defaultSwaySpeed;
                break;
        }

        Debug.Log($"Set sway for {weaponName}: Radius={swayRadius}, Speed={swaySpeed}, Time={aimTime}");
    }

    void Update()
    {
        if (isAiming)
        {
            UpdateSwayAndCursor();
        }
    }

    void UpdateSwayAndCursor()
{
    // Get mouse position in screen space using new Input System
    cursorPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
    
    // Calculate sway offset using sine waves for smooth movement
    float time = Time.time * swaySpeed;
    swayOffset = new Vector2(
        Mathf.Sin(time) * swayRadius,
        Mathf.Cos(time * 1.3f) * swayRadius  // Different frequency for more organic movement
    );
    
    // Update reticle position if it exists - RETICLE FOLLOWS CURSOR + SWAY
    if (activeReticle != null)
    {
        RectTransform reticleRect = activeReticle.GetComponent<RectTransform>();
        if (reticleRect != null)
        {
            // Convert cursor position to local position within the target area
            Vector2 localCursorPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetingPlayer ? playerTargetArea.GetComponent<RectTransform>() : fishTargetArea.GetComponent<RectTransform>(),
                cursorPosition,  // Use actual cursor position
                gameCanvas.worldCamera,
                out localCursorPosition
            );
            
            // Apply sway offset to cursor position
            reticleRect.anchoredPosition = localCursorPosition + swayOffset;
        }
    }
    
    // Check for mouse click to shoot - BUT ONLY IF OVER TARGET
    if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
    {
        if (CheckTargetHit())
        {
            FireShot();
        }
        else
        {
            Debug.Log("Missed target!");
            // Could add miss penalty or feedback here
        }
    }
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
    if (activeReticle == null || activeTarget == null) return false;
    
    Vector2 reticlePos = activeReticle.GetComponent<RectTransform>().anchoredPosition;
    Vector2 targetPos = activeTarget.GetComponent<RectTransform>().anchoredPosition;
    
    float distance = Vector2.Distance(reticlePos, targetPos);
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
    
    // Set weapon-specific sway settings
    if (currentActionCard != null)
    {
        SetSwayForWeapon(currentActionCard.actionName);
    }
    
    isAiming = true;
    
    // Create reticle
    Transform targetArea = targetingPlayer ? playerTargetArea : fishTargetArea;
    activeReticle = Instantiate(reticlePrefab, targetArea);
    
    // REMOVED: Start aim timer with weapon-specific time
    // StartCoroutine(AimTimer());
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
    
    // Set weapon-specific sway settings
    if (currentActionCard != null)
    {
        SetSwayForWeapon(currentActionCard.actionName);
    }
    
    // Create target at random position
    CreateShootingTarget();
    
    isAiming = true;
    
    // Create reticle
    Transform targetArea = targetingPlayer ? playerTargetArea : fishTargetArea;
    activeReticle = Instantiate(reticlePrefab, targetArea);
    
    // REMOVED: Start aim timer with weapon-specific time
    // StartCoroutine(AimTimer());
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