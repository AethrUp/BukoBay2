using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class RelayNetworkUI : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI joinCodeDisplay;
    
    [Header("Game UI")]
    public Button startGameButton;
    
    private string currentJoinCode;
    
    async void Start()
    {
        // Initialize Unity Services
        await InitializeUnityServices();
        
        // Set up buttons
        hostButton.onClick.AddListener(() => StartHost());
        joinButton.onClick.AddListener(() => StartClient());
        
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.gameObject.SetActive(false);
        }
        
        statusText.text = "Ready to connect";
        
        // Add connection event listeners
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }
    
    async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Unity Services initialized successfully");
            statusText.text = "Services ready";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e}");
            statusText.text = "Service initialization failed";
        }
    }
    
    async void StartHost()
    {
        try
        {
            statusText.text = "Creating lobby...";
            Debug.Log("Starting Host with Relay...");
            
            // Create a relay allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2); // Max 2 players
            
            // Get the join code
            currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // Configure transport
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, 
                                       allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            
            // Start hosting
            NetworkManager.Singleton.StartHost();
            
            // Display the join code for others
            if (joinCodeDisplay != null)
            {
                joinCodeDisplay.text = $"Join Code: {currentJoinCode}";
                joinCodeDisplay.gameObject.SetActive(true);
            }
            
            statusText.text = $"Hosting! Join Code: {currentJoinCode}";
            Debug.Log($"Host started with join code: {currentJoinCode}");
            
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start host: {e}");
            statusText.text = "Failed to start hosting";
        }
    }
    
    async void StartClient()
    {
        try
        {
            string joinCode = joinCodeInput.text.Trim().ToUpper();
            
            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Enter a join code first";
                return;
            }
            
            statusText.text = "Joining...";
            Debug.Log($"Starting Client with join code: {joinCode}");
            
            // Join the relay
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            // Configure transport
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                                       allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, 
                                       allocation.HostConnectionData);
            
            // Start client
            NetworkManager.Singleton.StartClient();
            
            statusText.text = "Connecting...";
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join: {e}");
            statusText.text = "Failed to join - check code";
        }
    }
    
    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
        if (statusText != null)
        {
            if (NetworkManager.Singleton.IsHost)
                statusText.text = $"Player joined! Connected: {NetworkManager.Singleton.ConnectedClients.Count}";
            else
                statusText.text = "Connected successfully!";
        }
    }
    
    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
        if (statusText != null)
            statusText.text = "Disconnected";
    }
    
    void StartGame()
    {
        NetworkGameManager gameManager = FindFirstObjectByType<NetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.StartGameServerRpc();
        }
    }
    
    void Update()
    {
        // Show start game button only for host
        if (startGameButton != null && NetworkManager.Singleton != null)
        {
            bool shouldShow = NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsListening;
            startGameButton.gameObject.SetActive(shouldShow);
        }
    }
}