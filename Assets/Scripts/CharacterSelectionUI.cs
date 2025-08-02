using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterSelectionPanel;
    public Transform characterButtonContainer;
    public GameObject characterButtonPrefab;
    public Button readyButton;
    public Button startGameButton; // Only visible for host
    public TextMeshProUGUI statusText;
    
    [Header("Player Status Display")]
    public Transform playerStatusContainer;
    public GameObject playerStatusPrefab;
    
    [Header("Local Player Info")]
    public Image selectedCharacterPortrait;
    public TextMeshProUGUI selectedCharacterName;
    public TextMeshProUGUI selectedCharacterDescription;
    
    private CharacterSelectionManager selectionManager;
    private List<CharacterButton> characterButtons = new List<CharacterButton>();
    private List<PlayerStatusDisplay> playerStatusDisplays = new List<PlayerStatusDisplay>();
    private int selectedCharacterIndex = -1;
    private bool isReady = false;
    
    void Start()
    {
        // Wait for CharacterSelectionManager to be ready
        StartCoroutine(WaitForManagerAndInitialize());
    }
    
    System.Collections.IEnumerator WaitForManagerAndInitialize()
    {
        // Wait until CharacterSelectionManager is available and initialized
        while (CharacterSelectionManager.Instance == null)
        {
            yield return null;
        }
        
        selectionManager = CharacterSelectionManager.Instance;
        
        // Wait until NetworkManager is ready if needed
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }
        
        SetupUI();
        RefreshSelectionDisplay();
        
        // Show/hide host-only buttons
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReady);
        }
    }
    
    void SetupUI()
    {
        if (selectionManager.availableCharacters == null || characterButtonContainer == null)
            return;
        
        // Clear existing buttons
        foreach (var button in characterButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }
        characterButtons.Clear();
        
        // Create character selection buttons
        for (int i = 0; i < selectionManager.availableCharacters.Count; i++)
        {
            CreateCharacterButton(i);
        }
    }
    
    void CreateCharacterButton(int characterIndex)
    {
        if (characterButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(characterButtonPrefab, characterButtonContainer);
        CharacterButton characterButton = buttonObj.GetComponent<CharacterButton>();
        
        if (characterButton == null)
        {
            characterButton = buttonObj.AddComponent<CharacterButton>();
        }
        
        characterButton.Initialize(characterIndex, selectionManager.availableCharacters[characterIndex], this);
        characterButtons.Add(characterButton);
    }
    
    public void OnCharacterSelected(int characterIndex)
    {
        if (isReady) return; // Can't change selection when ready
        
        selectedCharacterIndex = characterIndex;
        
        // Send selection to server
        ulong myPlayerId = NetworkManager.Singleton.LocalClientId;
        selectionManager.SelectCharacterServerRpc(characterIndex, myPlayerId);
        
        // Update local UI
        UpdateSelectedCharacterDisplay();
        RefreshSelectionDisplay();
    }
    
    void UpdateSelectedCharacterDisplay()
    {
        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= selectionManager.availableCharacters.Count)
        {
            // Clear selection display
            if (selectedCharacterPortrait != null)
                selectedCharacterPortrait.sprite = null;
            if (selectedCharacterName != null)
                selectedCharacterName.text = "No Character Selected";
            if (selectedCharacterDescription != null)
                selectedCharacterDescription.text = "";
            return;
        }
        
        PlayerCharacterData character = selectionManager.availableCharacters[selectedCharacterIndex];
        
        if (selectedCharacterPortrait != null)
            selectedCharacterPortrait.sprite = character.characterPortrait;
        if (selectedCharacterName != null)
            selectedCharacterName.text = character.characterName;
        if (selectedCharacterDescription != null)
            selectedCharacterDescription.text = character.characterDescription;
    }
    
    public void RefreshSelectionDisplay()
    {
        // Update character button states
        foreach (var button in characterButtons)
        {
            if (button != null)
            {
                button.RefreshState();
            }
        }
        
        // Update player status displays
        RefreshPlayerStatusDisplays();
        
        // Update ready button state
        UpdateReadyButton();
        
        // Update status text
        UpdateStatusText();
        
        // Update start game button for host
        if (startGameButton != null && NetworkManager.Singleton.IsHost)
        {
            startGameButton.interactable = selectionManager.AllPlayersReady();
        }
    }
    
    void RefreshPlayerStatusDisplays()
    {
        if (playerStatusContainer == null || playerStatusPrefab == null) return;
        
        // Clear existing displays
        foreach (var display in playerStatusDisplays)
        {
            if (display != null && display.gameObject != null)
                Destroy(display.gameObject);
        }
        playerStatusDisplays.Clear();
        
        // Create new displays for all players
        var playerSelections = selectionManager.GetAllPlayerSelections();
        foreach (var selection in playerSelections)
        {
            CreatePlayerStatusDisplay(selection);
        }
    }
    
    void CreatePlayerStatusDisplay(CharacterSelectionManager.PlayerSelection selection)
    {
        GameObject displayObj = Instantiate(playerStatusPrefab, playerStatusContainer);
        PlayerStatusDisplay statusDisplay = displayObj.GetComponent<PlayerStatusDisplay>();
        
        if (statusDisplay == null)
        {
            statusDisplay = displayObj.AddComponent<PlayerStatusDisplay>();
        }
        
        statusDisplay.UpdateDisplay(selection, selectionManager);
        playerStatusDisplays.Add(statusDisplay);
    }
    
    void UpdateReadyButton()
    {
        if (readyButton == null) return;
        
        bool canReady = selectedCharacterIndex >= 0;
        readyButton.interactable = canReady;
        
        TextMeshProUGUI buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isReady ? "Not Ready" : "Ready";
        }
    }
    
    void UpdateStatusText()
    {
        if (statusText == null) return;
        
        if (selectedCharacterIndex < 0)
        {
            statusText.text = "Select a character to continue";
        }
        else if (!isReady)
        {
            statusText.text = "Click Ready when you're satisfied with your selection";
        }
        else
        {
            statusText.text = "Waiting for other players...";
        }
    }
    
    void ToggleReady()
    {
        if (selectedCharacterIndex < 0) return;
        
        isReady = !isReady;
        
        // Send ready state to server
        ulong myPlayerId = NetworkManager.Singleton.LocalClientId;
        selectionManager.SetPlayerReadyServerRpc(isReady, myPlayerId);
        
        UpdateReadyButton();
        UpdateStatusText();
        
        // Enable/disable character selection when ready
        foreach (var button in characterButtons)
        {
            if (button != null)
            {
                button.SetInteractable(!isReady);
            }
        }
    }
    
    void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        
        if (!selectionManager.AllPlayersReady())
        {
            Debug.LogWarning("Cannot start game - not all players are ready");
            return;
        }
        
        Debug.Log("Starting game with character selections!");
        
        // Hide character selection UI
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }
        
        // TODO: Proceed to main game
        // For now, just log the selections
        var selections = selectionManager.GetAllPlayerSelections();
        foreach (var selection in selections)
        {
            Debug.Log($"Player {selection.playerId}: Character {selection.characterIndex}");
        }
    }
    
    public void ShowCharacterSelection()
    {
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
        }
    }
    
    public void HideCharacterSelection()
    {
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }
    }
}

// Component for individual character selection buttons
public class CharacterButton : MonoBehaviour
{
    private Button button;
    private Image characterImage;
    private TextMeshProUGUI characterNameText;
    private GameObject selectedIndicator;
    private GameObject takenIndicator;
    
    private int characterIndex;
    private PlayerCharacterData characterData;
    private CharacterSelectionUI selectionUI;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();
        
        // Find child components (assuming they exist in prefab)
        characterImage = GetComponentInChildren<Image>();
        characterNameText = GetComponentInChildren<TextMeshProUGUI>();
        
        // Look for indicators (optional)
        Transform selectedTransform = transform.Find("SelectedIndicator");
        if (selectedTransform != null)
            selectedIndicator = selectedTransform.gameObject;
            
        Transform takenTransform = transform.Find("TakenIndicator");
        if (takenTransform != null)
            takenIndicator = takenTransform.gameObject;
    }
    
    public void Initialize(int index, PlayerCharacterData data, CharacterSelectionUI ui)
    {
        characterIndex = index;
        characterData = data;
        selectionUI = ui;
        
        // Set up button
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
        
        // Set up visual data
        if (characterImage != null && characterData.characterPortrait != null)
        {
            characterImage.sprite = characterData.characterPortrait;
        }
        
        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }
        
        RefreshState();
    }
    
    public void RefreshState()
    {
        if (CharacterSelectionManager.Instance == null) return;
        
        bool isAvailable = CharacterSelectionManager.Instance.IsCharacterAvailable(characterIndex);
        bool isMySelection = false;
        
        // Check if this is my current selection
        ulong myPlayerId = NetworkManager.Singleton.LocalClientId;
        PlayerCharacterData myCharacter = CharacterSelectionManager.Instance.GetPlayerCharacter(myPlayerId);
        if (myCharacter == characterData)
        {
            isMySelection = true;
        }
        
        // Update button interactability
        if (button != null)
        {
            button.interactable = isAvailable || isMySelection;
        }
        
        // Update indicators
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isMySelection);
        }
        
        if (takenIndicator != null)
        {
            takenIndicator.SetActive(!isAvailable && !isMySelection);
        }
        
        // Update visual feedback
        if (characterImage != null)
        {
            Color imageColor = characterImage.color;
            imageColor.a = (isAvailable || isMySelection) ? 1f : 0.5f;
            characterImage.color = imageColor;
        }
    }
    
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable && CharacterSelectionManager.Instance.IsCharacterAvailable(characterIndex);
        }
    }
    
    void OnButtonClicked()
    {
        if (selectionUI != null)
        {
            selectionUI.OnCharacterSelected(characterIndex);
        }
    }
}

// Component for displaying player status
public class PlayerStatusDisplay : MonoBehaviour
{
    private TextMeshProUGUI playerNameText;
    private Image characterPortrait;
    private TextMeshProUGUI statusText;
    private Image colorIndicator;
    
    void Awake()
    {
        // Find child components (assuming they exist in prefab)
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0) playerNameText = texts[0];
        if (texts.Length > 1) statusText = texts[1];
        
        Image[] images = GetComponentsInChildren<Image>();
        if (images.Length > 0) characterPortrait = images[0];
        if (images.Length > 1) colorIndicator = images[1];
    }
    
    public void UpdateDisplay(CharacterSelectionManager.PlayerSelection selection, CharacterSelectionManager manager)
    {
        // Update player name
        if (playerNameText != null)
        {
            playerNameText.text = selection.playerName.ToString();
        }
        
        // Update character info
        if (selection.characterIndex >= 0 && selection.characterIndex < manager.availableCharacters.Count)
        {
            PlayerCharacterData character = manager.availableCharacters[selection.characterIndex];
            
            if (characterPortrait != null)
            {
                characterPortrait.sprite = character.characterPortrait;
            }
            
            if (statusText != null)
            {
                string readyText = selection.isReady ? " (Ready)" : " (Not Ready)";
                statusText.text = character.characterName + readyText;
            }
            
            if (colorIndicator != null)
            {
                colorIndicator.color = character.tokenColor;
            }
        }
        else
        {
            // No selection yet
            if (characterPortrait != null)
            {
                characterPortrait.sprite = null;
            }
            
            if (statusText != null)
            {
                statusText.text = "Selecting...";
            }
            
            if (colorIndicator != null)
            {
                colorIndicator.color = Color.gray;
            }
        }
    }
}