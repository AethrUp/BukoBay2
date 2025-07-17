using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Game State")]
    public NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0);
    public NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(false);

    [Header("UI References")]
    public TextMeshProUGUI turnStatusText;
    public TextMeshProUGUI connectedPlayersText;

    [Header("Turn Management")]
    private int totalPlayers = 0;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            Debug.Log("NetworkGameManager: Host started game management");
            currentPlayerTurn.Value = 0; // Host goes first
            gameInProgress.Value = false; // Game starts paused until players are ready
        }

        // Subscribe to player turn changes
        currentPlayerTurn.OnValueChanged += OnTurnChanged;

        UpdateUI();
    }

    void Update()
    {
        // Update player count and UI - with null checks
        if (IsHost && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients != null)
        {
            int connectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;
            if (totalPlayers != connectedPlayers)
            {
                totalPlayers = connectedPlayers;
                UpdateUI();
            }
        }
    }

    void OnTurnChanged(int previousValue, int newValue)
    {
        Debug.Log($"Turn changed from player {previousValue} to player {newValue}");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (turnStatusText != null && NetworkManager.Singleton != null)
        {
            ulong myClientId = NetworkManager.Singleton.LocalClientId;
            bool isMyTurn = currentPlayerTurn.Value == (int)myClientId;

            if (isMyTurn)
            {
                turnStatusText.text = "YOUR TURN - You can fish!";
                turnStatusText.color = Color.green;
            }
            else
            {
                turnStatusText.text = $"Player {currentPlayerTurn.Value}'s turn - Please wait";
                turnStatusText.color = Color.yellow;
            }
        }

        if (connectedPlayersText != null && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            connectedPlayersText.text = $"Connected Players: {playerCount}";
        }
    }

    // Check if it's the local player's turn
    public bool IsMyTurn()
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        return currentPlayerTurn.Value == (int)myClientId;
    }

    // Move to next player's turn (only host can do this)
    [ServerRpc(RequireOwnership = false)]
public void NextTurnServerRpc()
{
    if (!IsHost) return;
    
    // For local simulation, assume 2 players
    int simulatedPlayerCount = 2;
    int nextPlayer = (currentPlayerTurn.Value + 1) % simulatedPlayerCount;
    currentPlayerTurn.Value = nextPlayer;
    
    Debug.Log($"Host: Advanced turn to player {nextPlayer}");
}

    // Start the game (only host can do this)
    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!IsHost) return;

        gameInProgress.Value = true;
        Debug.Log("Host: Game started!");
    }
    
    // Test method to manually advance turns
[ContextMenu("Advance Turn")]
public void TestAdvanceTurn()
{
    if (IsHost)
    {
        NextTurnServerRpc();
    }
}
}