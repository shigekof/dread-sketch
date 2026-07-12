using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages MainMenu UI and wires buttons to LobbyManager.
/// Handles host creation and client joining flows.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private const int JoinTimeoutMs = 15000;

    private string GetOrCreatePlayerName()
    {
        string playerName = playerNameInput.text.Trim();
        if (!string.IsNullOrEmpty(playerName))
        {
            return playerName;
        }

        int suffix = Random.Range(1000, 9999);
        string autoName = $"Player{suffix}";
        playerNameInput.text = autoName;
        Debug.LogWarning($"MainMenuUI: Player name was empty. Auto-generated name: {autoName}");
        return autoName;
    }

    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI joinCodeDisplayText;
    [SerializeField] private Button startGameButton;

    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject waitingPanel;

    private LobbyManager _lobbyManager;
    private bool _isCreatingLobby = false;
    private bool _isJoiningLobby = false;

    private void Start()
    {
        Debug.Log("MainMenuUI: Start() called.");

        // Find or create LobbyManager
        _lobbyManager = FindObjectOfType<LobbyManager>();
        if (_lobbyManager == null)
        {
            Debug.LogError("MainMenuUI: LobbyManager not found in scene!");
            statusText.text = "Error: LobbyManager not found";
            return;
        }

        // Hook up button listeners
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // Subscribe to lobby events
        _lobbyManager.OnLobbyCreated += OnLobbyCreatedSuccessfully;
        _lobbyManager.OnLobbyJoined += OnLobbyJoinedSuccessfully;
        _lobbyManager.OnError += OnLobbyError;

        // Show main menu panels
        hostPanel.SetActive(true);
        joinPanel.SetActive(true);
        waitingPanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);

        statusText.text = "Enter your name and create or join a lobby";
        Debug.Log($"MainMenuUI: Wired listeners. CreateButtonInteractable={createLobbyButton.interactable}, JoinButtonInteractable={joinLobbyButton.interactable}");
    }

    private void OnDestroy()
    {
        if (_lobbyManager != null)
        {
            _lobbyManager.OnLobbyCreated -= OnLobbyCreatedSuccessfully;
            _lobbyManager.OnLobbyJoined -= OnLobbyJoinedSuccessfully;
            _lobbyManager.OnError -= OnLobbyError;
        }
    }

    private async void OnCreateLobbyClicked()
    {
        Debug.Log("MainMenuUI: Create button clicked.");
        if (_isCreatingLobby || _isJoiningLobby)
        {
            Debug.LogWarning("MainMenuUI: Create ignored because another operation is in progress.");
            return;
        }

        string playerName = GetOrCreatePlayerName();

        _isCreatingLobby = true;
        statusText.text = "Creating lobby...";
        createLobbyButton.interactable = false;
        joinLobbyButton.interactable = false;

        try
        {
            await _lobbyManager.CreateLobbyAsync(playerName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MainMenuUI: Create failed with exception: {e}");
            _isCreatingLobby = false;
            createLobbyButton.interactable = true;
            joinLobbyButton.interactable = true;
        }
    }

    private async void OnJoinLobbyClicked()
    {
        Debug.Log("MainMenuUI: Join button clicked.");
        if (_isCreatingLobby || _isJoiningLobby)
        {
            Debug.LogWarning("MainMenuUI: Join ignored because another operation is in progress.");
            return;
        }

        string playerName = GetOrCreatePlayerName();
        string joinCode = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(joinCode))
        {
            statusText.text = "Please enter a join code";
            Debug.LogWarning("MainMenuUI: Join aborted - empty join code.");
            return;
        }

        _isJoiningLobby = true;
        statusText.text = "Joining lobby...";
        createLobbyButton.interactable = false;
        joinLobbyButton.interactable = false;
        Debug.Log($"MainMenuUI: Join submitted. Player={playerName}, Code={joinCode}");

        try
        {
            Task joinTask = _lobbyManager.JoinLobbyAsync(joinCode, playerName);
            Task timeoutTask = Task.Delay(JoinTimeoutMs);
            Task completed = await Task.WhenAny(joinTask, timeoutTask);

            if (completed == timeoutTask)
            {
                _isJoiningLobby = false;
                createLobbyButton.interactable = true;
                joinLobbyButton.interactable = true;
                statusText.text = "Join timed out. Check code and try again.";
                Debug.LogWarning("MainMenuUI: Join lobby timed out.");
                return;
            }

            await joinTask;
            Debug.Log("MainMenuUI: Join task completed successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MainMenuUI: Join failed with exception: {e}");
            _isJoiningLobby = false;
            createLobbyButton.interactable = true;
            joinLobbyButton.interactable = true;
        }
    }

    private void OnLobbyCreatedSuccessfully(string joinCode)
    {
        _isCreatingLobby = false;
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        startGameButton.gameObject.SetActive(true);
        startGameButton.interactable = true;

        joinCodeDisplayText.text = $"Join Code: {joinCode}";
        statusText.text = $"Lobby created! Waiting for players... ({_lobbyManager.GetPlayerCount()}/{5})";

        Debug.Log($"Lobby created with code: {joinCode}");
    }

    private void OnLobbyJoinedSuccessfully()
    {
        _isJoiningLobby = false;
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        waitingPanel.SetActive(true);
        startGameButton.gameObject.SetActive(false);

        joinCodeDisplayText.text = "Connected to lobby!";
        statusText.text = $"Joined! Players: {_lobbyManager.GetPlayerCount()}/{5}";

        Debug.Log("Successfully joined lobby");
    }

    private void OnLobbyError(string errorMessage)
    {
        _isCreatingLobby = false;
        _isJoiningLobby = false;
        createLobbyButton.interactable = true;
        joinLobbyButton.interactable = true;

        statusText.text = $"Error: {errorMessage}";
        Debug.LogError($"Lobby error: {errorMessage}");
    }

    private void OnStartGameClicked()
    {
        Debug.Log("MainMenuUI: Start Game button clicked.");
        if (!startGameButton.interactable)
        {
            Debug.LogWarning("MainMenuUI: Start Game clicked while not interactable.");
            return;
        }

        statusText.text = "Starting game...";
        _lobbyManager.StartGameplay();
    }
}
