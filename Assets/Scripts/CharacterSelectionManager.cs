using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterSelectionManager : NetworkBehaviour
{
    [Header("Available Characters")]
    public List<PlayerCharacterData> availableCharacters = new List<PlayerCharacterData>();
    
    [Header("Default Colors (Fallback)")]
    public Color[] defaultPlayerColors = {
        Color.red,
        Color.blue, 
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };
    
    // Network variables to track player selections
    private NetworkList<PlayerSelection> playerSelections;
    
    // Local tracking
    private Dictionary<ulong, PlayerCharacterData> playerCharacters = new Dictionary<ulong, PlayerCharacterData>();
    
    public static CharacterSelectionManager Instance { get; private set; }
    
    public struct PlayerSelection : INetworkSerializable, System.IEquatable<PlayerSelection>
    {
        public ulong playerId;
        public int characterIndex; // -1 means no selection
        public FixedString64Bytes playerName;
        public bool isReady;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref characterIndex);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref isReady);
        }
        
        public bool Equals(PlayerSelection other)
        {
            return playerId == other.playerId &&
                   characterIndex == other.characterIndex &&
                   playerName.Equals(other.playerName) &&
                   isReady == other.isReady;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PlayerSelection other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return System.HashCode.Combine(playerId, characterIndex, playerName, isReady);
        }
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize NetworkList early
            if (playerSelections == null)
            {
                playerSelections = new NetworkList<PlayerSelection>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (playerSelections == null)
        {
            playerSelections = new NetworkList<PlayerSelection>();
        }
        
        playerSelections.OnListChanged += OnPlayerSelectionsChanged;
        
        // Initialize with current players if host
        if (IsHost)
        {
            InitializePlayerList();
        }
        
        Debug.Log($"CharacterSelectionManager spawned - IsHost: {IsHost}");
    }
    
    public override void OnNetworkDespawn()
    {
        if (playerSelections != null)
        {
            playerSelections.OnListChanged -= OnPlayerSelectionsChanged;
        }
    }
    
    void InitializePlayerList()
    {
        if (!IsHost) return;
        
        // Add all connected players to the selection list
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            AddPlayerToSelection(clientId);
        }
    }
    
    void AddPlayerToSelection(ulong playerId)
    {
        // Check if player already exists
        for (int i = 0; i < playerSelections.Count; i++)
        {
            if (playerSelections[i].playerId == playerId)
                return; // Already exists
        }
        
        // Add new player with no selection
        PlayerSelection newSelection = new PlayerSelection
        {
            playerId = playerId,
            characterIndex = -1,
            playerName = $"Player {playerId}",
            isReady = false
        };
        
        playerSelections.Add(newSelection);
        Debug.Log($"Added player {playerId} to character selection");
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SelectCharacterServerRpc(int characterIndex, ulong playerId)
    {
        if (!IsHost) return;
        
        // Validate character index
        if (characterIndex < 0 || characterIndex >= availableCharacters.Count)
        {
            Debug.LogWarning($"Invalid character index: {characterIndex}");
            return;
        }
        
        // Check if character is already taken
        for (int i = 0; i < playerSelections.Count; i++)
        {
            var selection = playerSelections[i];
            if (selection.characterIndex == characterIndex && selection.playerId != playerId)
            {
                Debug.LogWarning($"Character {characterIndex} already taken by player {selection.playerId}");
                return;
            }
        }
        
        // Update player's selection
        for (int i = 0; i < playerSelections.Count; i++)
        {
            var selection = playerSelections[i];
            if (selection.playerId == playerId)
            {
                selection.characterIndex = characterIndex;
                playerSelections[i] = selection;
                Debug.Log($"Player {playerId} selected character {characterIndex}");
                return;
            }
        }
        
        // If player not found, add them
        AddPlayerToSelection(playerId);
        // Try again
        SelectCharacterServerRpc(characterIndex, playerId);
    }
    
    void OnPlayerSelectionsChanged(NetworkListEvent<PlayerSelection> changeEvent)
    {
        RefreshLocalPlayerCharacters();
        
        // Notify UI if it exists
        CharacterSelectionUI selectionUI = FindFirstObjectByType<CharacterSelectionUI>();
        if (selectionUI != null)
        {
            selectionUI.RefreshSelectionDisplay();
        }
    }
    
    void RefreshLocalPlayerCharacters()
    {
        playerCharacters.Clear();
        
        for (int i = 0; i < playerSelections.Count; i++)
        {
            var selection = playerSelections[i];
            if (selection.characterIndex >= 0 && selection.characterIndex < availableCharacters.Count)
            {
                playerCharacters[selection.playerId] = availableCharacters[selection.characterIndex];
            }
        }
        
        Debug.Log($"Refreshed local player characters: {playerCharacters.Count} players have selections");
    }
    
    // Public methods for getting player data
    public PlayerCharacterData GetPlayerCharacter(ulong playerId)
    {
        if (playerCharacters.ContainsKey(playerId))
        {
            return playerCharacters[playerId];
        }
        return null;
    }
    
    public Color GetPlayerColor(ulong playerId)
    {
        PlayerCharacterData character = GetPlayerCharacter(playerId);
        if (character != null)
        {
            return character.tokenColor;
        }
        
        // Fallback to default colors based on player ID
        int colorIndex = (int)(playerId % (ulong)defaultPlayerColors.Length);
        return defaultPlayerColors[colorIndex];
    }
    
    public string GetPlayerName(ulong playerId)
    {
        PlayerCharacterData character = GetPlayerCharacter(playerId);
        if (character != null)
        {
            return character.characterName;
        }
        
        return $"Player {playerId}";
    }
    
    public bool IsCharacterAvailable(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= availableCharacters.Count)
            return false;
        
        // Check if playerSelections is initialized
        if (playerSelections == null)
            return true; // If not initialized yet, assume available
        
        // Check if any player has selected this character
        for (int i = 0; i < playerSelections.Count; i++)
        {
            if (playerSelections[i].characterIndex == characterIndex)
                return false;
        }
        
        return true;
    }
    
    public List<PlayerSelection> GetAllPlayerSelections()
    {
        List<PlayerSelection> selections = new List<PlayerSelection>();
        if (playerSelections != null)
        {
            for (int i = 0; i < playerSelections.Count; i++)
            {
                selections.Add(playerSelections[i]);
            }
        }
        return selections;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(bool ready, ulong playerId)
    {
        if (!IsHost) return;
        
        // Update player's ready state
        for (int i = 0; i < playerSelections.Count; i++)
        {
            var selection = playerSelections[i];
            if (selection.playerId == playerId)
            {
                selection.isReady = ready;
                playerSelections[i] = selection;
                Debug.Log($"Player {playerId} ready state: {ready}");
                return;
            }
        }
    }
    
    public bool AllPlayersReady()
    {
        if (playerSelections.Count == 0) return false;
        
        for (int i = 0; i < playerSelections.Count; i++)
        {
            var selection = playerSelections[i];
            if (selection.characterIndex == -1 || !selection.isReady)
                return false;
        }
        
        return true;
    }
    
    // Called when a new player joins
    public void OnPlayerJoined(ulong playerId)
    {
        if (IsHost)
        {
            AddPlayerToSelection(playerId);
        }
    }
    
    // Called when a player leaves
    public void OnPlayerLeft(ulong playerId)
    {
        if (!IsHost) return;
        
        for (int i = 0; i < playerSelections.Count; i++)
        {
            if (playerSelections[i].playerId == playerId)
            {
                playerSelections.RemoveAt(i);
                Debug.Log($"Removed player {playerId} from character selection");
                break;
            }
        }
    }
}