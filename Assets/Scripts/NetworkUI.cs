using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;



public class NetworkUI : MonoBehaviour
{
    [Header("Connection UI")]
    public Button hostButton;
    public Button joinButton;
    public TextMeshProUGUI statusText;

    [Header("Game UI")]
    public Button startGameButton;

    private NetworkGameManager gameManager;

    [Header("Local Testing")]
    public Button localMultiplayerButton;
    
    [Header("Connection Settings")]
    public TMP_InputField ipAddressInput;  // Add this line

    void Start()
{
    hostButton.onClick.AddListener(StartHost);
    joinButton.onClick.AddListener(StartClient);
    
    // Add local multiplayer test
    if (localMultiplayerButton != null)
        localMultiplayerButton.onClick.AddListener(StartLocalMultiplayer);
    
    if (startGameButton != null)
    {
        startGameButton.onClick.AddListener(StartGame);
        startGameButton.gameObject.SetActive(false);
    }
    
    statusText.text = "Not Connected";
    
    // Add connection event listeners
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
}

void OnClientConnected(ulong clientId)
{
    Debug.Log($"Client {clientId} connected!");
    if (statusText != null)
        statusText.text = $"Connected! Client ID: {clientId}";
}

void OnClientDisconnected(ulong clientId)
{
    Debug.Log($"Client {clientId} disconnected!");
    if (statusText != null)
        statusText.text = "Disconnected";
}

    void StartLocalMultiplayer()
    {
        Debug.Log("Starting local multiplayer simulation...");

        // Start as host first
        NetworkManager.Singleton.StartHost();

        // Simulate a second player joining after a short delay
        StartCoroutine(SimulateSecondPlayer());
    }

    System.Collections.IEnumerator SimulateSecondPlayer()
    {
        yield return new WaitForSeconds(1f);

        // This simulates having 2 players for turn management
        Debug.Log("Simulated second player joined!");
        statusText.text = "Local Multiplayer - 2 Players";
    }

    void Update()
    {
        // Show start game button only for host
        if (startGameButton != null && NetworkManager.Singleton != null)
        {
            bool shouldShow = NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsListening;
            startGameButton.gameObject.SetActive(shouldShow);
        }

        // Update connection status
        if (statusText != null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                statusText.text = "Hosting - Waiting for clients...";
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                statusText.text = "Connected as Client";
            }
            else if (NetworkManager.Singleton.IsConnectedClient)
            {
                statusText.text = "Connected!";
            }
            else
            {
                statusText.text = "Connecting...";
            }
        }
    }

    void StartHost()
{
    Debug.Log("Starting Host...");
    
    // Force the host to bind to all interfaces
    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    transport.ConnectionData.Address = "0.0.0.0";
    transport.ConnectionData.Port = 7779;
    
    NetworkManager.Singleton.StartHost();
    statusText.text = "Hosting...";
    
    hostButton.gameObject.SetActive(false);
    joinButton.gameObject.SetActive(false);
}

    void StartClient()
{
    Debug.Log("Starting Client...");
    
    // Get IP address from input field
    string ipAddress = "127.0.0.1"; // default
    if (ipAddressInput != null && !string.IsNullOrEmpty(ipAddressInput.text))
    {
        ipAddress = ipAddressInput.text;
    }
    
    // Set the connection IP
    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    transport.ConnectionData.Address = ipAddress;

     Debug.Log($"Attempting to connect to: {ipAddress}:{transport.ConnectionData.Port}");
    Debug.Log($"Transport Address is now: {transport.ConnectionData.Address}");
    
    NetworkManager.Singleton.StartClient();
    statusText.text = "Connecting...";
    
    hostButton.gameObject.SetActive(false);
    joinButton.gameObject.SetActive(false);
}

    void StartGame()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<NetworkGameManager>();
        }

        if (gameManager != null)
        {
            gameManager.StartGameServerRpc();
        }
    }
}