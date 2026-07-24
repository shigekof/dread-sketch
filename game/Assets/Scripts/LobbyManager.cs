using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages lobby creation, joining, and player session coordination.
/// Works with UGS Lobbies and coordinates with RelayManager for networking.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private string gameVersion = "0.1.0";

    private Lobby _currentLobby;
    private RelayManager _relayManager;
    private bool _isHost = false;
    private Task _servicesInitializationTask;
    private float _heartbeatTimer;
    private const float HeartbeatIntervalSeconds = 15f;
    private string _runtimeAuthProfile;

    // Events for UI to subscribe to
    public event Action<string> OnLobbyCreated; // Passes join code
    public event Action OnLobbyJoined;
    public event Action<string> OnError; // Passes error message

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure separate local instances (Editor/built app) don't reuse the same cached anonymous profile.
        // Auth profile must be <= 30 chars and only use [a-zA-Z0-9_-].
        string instanceTag = Application.isEditor ? "ed" : "app";
        string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 12);
        _runtimeAuthProfile = $"ds_{instanceTag}_{shortGuid}";
    }

    private void Start()
    {
        Debug.Log($"LobbyManager: Start() called. Runtime profile={_runtimeAuthProfile}");

        _relayManager = GetComponent<RelayManager>();
        if (_relayManager == null)
        {
            Debug.LogError("LobbyManager: RelayManager component not found!");
        }

        _servicesInitializationTask = EnsureServicesInitializedAsync();
    }

    private async void Update()
    {
        if (!_isHost || _currentLobby == null)
        {
            return;
        }

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer > 0f)
        {
            return;
        }

        _heartbeatTimer = HeartbeatIntervalSeconds;
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LobbyManager: Heartbeat failed: {e.Message}");
        }
    }

    private async Task EnsureServicesInitializedAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("LobbyManager: Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("LobbyManager: Unity Services initialized.");
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SwitchProfile(_runtimeAuthProfile);
            Debug.Log("LobbyManager: Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"LobbyManager: Signed in. PlayerId={AuthenticationService.Instance.PlayerId}");
        }

        Debug.Log("LobbyManager: Unity Services ready.");
    }

    /// <summary>
    /// Host creates a new lobby and relay session.
    /// </summary>
    public async Task<string> CreateLobbyAsync(string playerName)
    {
        try
        {
            await _servicesInitializationTask;
            Debug.Log($"LobbyManager: Creating lobby as host {playerName}...");

            _isHost = true;
            _heartbeatTimer = HeartbeatIntervalSeconds;

            // Create relay allocation and get join code
            string relayJoinCode = await _relayManager.CreateHostAsync();
            Debug.Log($"LobbyManager: Relay join code: {relayJoinCode}");

            // Create lobby data
            var lobbyData = new Dictionary<string, DataObject>
            {
                { "RelayJoinCode", new DataObject(visibility: DataObject.VisibilityOptions.Public, value: relayJoinCode) },
                { "GameVersion", new DataObject(visibility: DataObject.VisibilityOptions.Public, value: gameVersion) }
            };

            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Public, value: playerName) }
            };

            // Create the lobby
            var createLobbyOptions = new CreateLobbyOptions
            {
                Data = lobbyData,
                Player = new Player { Data = playerData }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("DreadSketch", maxPlayers, createLobbyOptions);

            Debug.Log($"LobbyManager: Lobby created! ID: {_currentLobby.Id}");
            Debug.Log($"LobbyManager: Lobby join code for UI: {_currentLobby.LobbyCode}");

            OnLobbyCreated?.Invoke(_currentLobby.LobbyCode);

            return _currentLobby.LobbyCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyManager: Failed to create lobby: {e.Message}");
            OnError?.Invoke($"Failed to create lobby: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Client joins an existing lobby using the lobby code.
    /// </summary>
    public async Task JoinLobbyAsync(string lobbyCode, string playerName)
    {
        try
        {
            await _servicesInitializationTask;
            lobbyCode = lobbyCode.Trim().ToUpperInvariant();
            Debug.Log($"LobbyManager: Joining lobby {lobbyCode} as {playerName}...");

            _isHost = false;

            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Public, value: playerName) }
            };

            var joinLobbyOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player { Data = playerData }
            };

            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyOptions);

            Debug.Log($"LobbyManager: Joined lobby! ID: {_currentLobby.Id}");
            Debug.Log($"LobbyManager: Lobby has {_currentLobby.Players.Count} players and {_currentLobby.Data.Count} data entries.");

            // Get relay join code from lobby data
            if (_currentLobby.Data.TryGetValue("RelayJoinCode", out var relayCodeData))
            {
                string relayJoinCode = relayCodeData.Value;
                Debug.Log($"LobbyManager: Got relay join code from lobby: {relayJoinCode}");

                // Join relay session
                await _relayManager.JoinClientAsync(relayJoinCode);
                Debug.Log("LobbyManager: Relay join completed.");

                // Wait until Netcode reports this local instance is actually connected as a client.
                await WaitForLocalClientConnectedAsync();
                Debug.Log("LobbyManager: Netcode client connection confirmed.");
            }
            else
            {
                throw new Exception("Relay join code not found in lobby data!");
            }

            OnLobbyJoined?.Invoke();
            Debug.Log("LobbyManager: OnLobbyJoined event invoked.");
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyManager: Failed to join lobby: {e.Message}");
            OnError?.Invoke($"Failed to join lobby: {e.Message}");
            throw;
        }
    }

    private async Task WaitForLocalClientConnectedAsync(int timeoutMs = 15000)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            throw new Exception("NetworkManager.Singleton is null while waiting for client connection.");
        }

        float timeoutSeconds = timeoutMs / 1000f;
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (networkManager.IsClient && networkManager.IsConnectedClient)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Timed out waiting for Netcode client connection.");
    }

    /// <summary>
    /// Get the current lobby code (for displaying to host).
    /// </summary>
    public string GetLobbyCode()
    {
        return _currentLobby?.LobbyCode ?? "N/A";
    }

    /// <summary>
    /// Get current player count in lobby.
    /// </summary>
    public int GetPlayerCount()
    {
        return _currentLobby?.Players.Count ?? 0;
    }

    /// <summary>
    /// Get list of player names in lobby.
    /// </summary>
    public List<string> GetPlayerNames()
    {
        var names = new List<string>();
        if (_currentLobby != null)
        {
            foreach (var player in _currentLobby.Players)
            {
                if (player.Data.TryGetValue("PlayerName", out var nameData))
                {
                    names.Add(nameData.Value);
                }
            }
        }
        return names;
    }

    /// <summary>
    /// Load the gameplay scene after session is ready.
    /// </summary>
    public async void StartGameplay()
    {
        try
        {
            if (!_isHost)
            {
                Debug.LogWarning("LobbyManager: StartGameplay ignored - only host can start gameplay.");
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError("LobbyManager: Cannot start gameplay - NetworkManager.Singleton is null.");
                return;
            }

            if (!networkManager.IsHost)
            {
                Debug.LogError("LobbyManager: Cannot start gameplay - this instance is not the active host.");
                return;
            }

            if (!networkManager.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogError("LobbyManager: Scene management is disabled in NetworkManager.");
                return;
            }

            // Refresh lobby snapshot first, then verify Netcode-side connected clients are ready.
            if (_currentLobby != null)
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            }

            int lobbyPlayers = _currentLobby?.Players?.Count ?? 1;
            int connectedClients = networkManager.ConnectedClientsIds.Count;
            if (connectedClients < lobbyPlayers)
            {
                string msg = $"Start blocked: Netcode connected clients ({connectedClients}) is less than lobby players ({lobbyPlayers}). Wait a moment and try again.";
                Debug.LogWarning($"LobbyManager: {msg}");
                OnError?.Invoke(msg);
                return;
            }

            Debug.Log("LobbyManager: Starting synchronized gameplay scene load...");
            SceneEventProgressStatus status = networkManager.SceneManager.LoadScene("ArtSchool_Greybox", LoadSceneMode.Single);
            Debug.Log($"LobbyManager: Network scene load status: {status}");
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyManager: Failed to start gameplay: {e.Message}");
            OnError?.Invoke($"Failed to start gameplay: {e.Message}");
        }
    }

    /// <summary>
    /// Leave lobby and disconnect.
    /// </summary>
    public async void LeaveLobby()
    {
        try
        {
            if (_servicesInitializationTask != null)
            {
                await _servicesInitializationTask;
            }

            if (_currentLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("LobbyManager: Left lobby.");
            }

            _currentLobby = null;
            _isHost = false;
            _relayManager.Disconnect();
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyManager: Error leaving lobby: {e.Message}");
        }
    }
}
