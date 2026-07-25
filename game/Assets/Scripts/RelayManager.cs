using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Unity Relay connection logic for multiplayer sessions.
/// Handles both host (server) and client join flows.
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private const string GameplaySceneName = "ArtSchool_Greybox";
    private const string BuildMarker = "DS_NETCFG_MARKER_2026-07-25_01";
    private const string SurvivorPrefabResourcePath = "Prefabs/SurvivorPrefab";
    private const string MonsterPrefabResourcePath = "Prefabs/MonsterPrefab";

    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private GameObject survivorPrefab;
    [SerializeField] private GameObject monsterPrefab;

    private UnityTransport _unityTransport;
    private NetworkManager _networkManager;
    private readonly List<ulong> _connectionOrder = new List<ulong>();
    private bool _networkSceneCallbackRegistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        _unityTransport = GetComponent<UnityTransport>();

        if (_networkManager == null)
        {
            Debug.LogError("RelayManager: NetworkManager component not found on this GameObject!");
            return;
        }

        if (_unityTransport == null)
        {
            Debug.LogError("RelayManager: UnityTransport component not found on this GameObject!");
            return;
        }

        // Normalize key config fields at runtime so host and clients hash the same values.
        _networkManager.NetworkConfig.PlayerPrefab = null;
        _networkManager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        _networkManager.NetworkConfig.ForceSamePrefabs = false;

        TryResolveMissingPrefabReferences();

        if (survivorPrefab == null || monsterPrefab == null)
        {
            Debug.LogError("RelayManager: Survivor and Monster prefabs must be assigned.");
            return;
        }

        if (survivorPrefab.GetComponent<NetworkObject>() == null || monsterPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("RelayManager: Assigned prefabs must include a NetworkObject component.");
            return;
        }

        _networkManager.OnServerStarted += OnServerStarted;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRegisterNetworkSceneCallback();

        Debug.LogWarning($"RelayManager: {BuildMarker}");
        LogNetworkConfig("Start");
        Debug.Log($"RelayManager initialized successfully. MaxPlayers={maxPlayers}");
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.OnServerStarted -= OnServerStarted;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;

            if (_networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoadEventCompleted -= OnNetworkSceneLoadCompleted;
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Host creates a relay allocation and starts the session.
    /// Returns the join code that clients can use to connect.
    /// </summary>
    public async Task<string> CreateHostAsync()
    {
        try
        {
            Debug.Log("RelayManager: Creating host allocation...");
            LogNetworkConfig("BeforeStartHost");

            // Create allocation on Relay server
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            Debug.Log($"RelayManager: Allocation created. Region={allocation.Region}, AllocationId={allocation.AllocationId}");
            
            // Get join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Configure transport with relay allocation
            _unityTransport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            Debug.Log($"RelayManager: Host created successfully. Join code: {joinCode}");

            // Start as host
            bool hostStarted = _networkManager.StartHost();
            Debug.Log($"RelayManager: StartHost returned {hostStarted}");
            
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"RelayManager: Failed to create host: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Client joins a relay session using the host's join code.
    /// </summary>
    public async Task JoinClientAsync(string joinCode)
    {
        try
        {
            Debug.Log($"RelayManager: Joining with code: {joinCode}");
            LogNetworkConfig("BeforeStartClient");

            // Join the relay allocation using the join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"RelayManager: Join allocation received. Region={joinAllocation.Region}, AllocationId={joinAllocation.AllocationId}");

            // Configure transport with joined relay allocation
            _unityTransport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            Debug.Log("RelayManager: Client joined successfully.");

            // Start as client
            bool clientStarted = _networkManager.StartClient();
            Debug.Log($"RelayManager: StartClient returned {clientStarted}");
        }
        catch (Exception e)
        {
            Debug.LogError($"RelayManager: Failed to join client: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Disconnect from relay and shut down the session.
    /// </summary>
    public void Disconnect()
    {
        if (_networkManager != null && _networkManager.IsListening)
        {
            _networkManager.Shutdown();
            Debug.Log("RelayManager: Disconnected.");
        }
    }

    private void OnServerStarted()
    {
        TryRegisterNetworkSceneCallback();
        _connectionOrder.Clear();

        foreach (ulong clientId in _networkManager.ConnectedClientsIds)
        {
            AddClientToConnectionOrder(clientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (_networkManager == null || !_networkManager.IsServer)
        {
            return;
        }

        AddClientToConnectionOrder(clientId);

        // Late-joining client: server is already in gameplay, spawn immediately.
        // (Normal flow: spawn happens via OnNetworkSceneLoadCompleted)
        if (SceneManager.GetActiveScene().name == GameplaySceneName)
        {
            StartCoroutine(SpawnSingleClientWhenReady(clientId));
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _connectionOrder.Remove(clientId);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Intentionally empty — spawning is handled by OnNetworkSceneLoadCompleted
        // which fires only after ALL clients have loaded the scene, preventing
        // deferred-spawn race conditions.
    }

    private void OnNetworkSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!_networkManager.IsServer)
        {
            return;
        }

        if (!string.Equals(sceneName, GameplaySceneName, StringComparison.Ordinal))
        {
            return;
        }

        if (clientsTimedOut != null && clientsTimedOut.Count > 0)
        {
            Debug.LogWarning($"RelayManager: Scene load completed with {clientsTimedOut.Count} timed-out clients.");
        }

        StartCoroutine(SpawnPlayersWhenReady());
    }

    private void TryRegisterNetworkSceneCallback()
    {
        if (_networkSceneCallbackRegistered)
        {
            return;
        }

        if (_networkManager == null || _networkManager.SceneManager == null)
        {
            return;
        }

        _networkManager.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadCompleted;
        _networkSceneCallbackRegistered = true;
    }

    private IEnumerator SpawnPlayersWhenReady()
    {
        const int maxFrames = 300;

        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (PlayerSpawnManager.Instance != null)
            {
                PlayerSpawnManager.Instance.ResetForNewMatch();

                for (int i = 0; i < _connectionOrder.Count; i++)
                {
                    TrySpawnPlayerForClient(_connectionOrder[i]);
                }

                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("RelayManager: Timed out waiting for PlayerSpawnManager before role-based spawning.");
    }

    private IEnumerator SpawnSingleClientWhenReady(ulong clientId)
    {
        const int maxFrames = 300;

        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (PlayerSpawnManager.Instance != null)
            {
                TrySpawnPlayerForClient(clientId);
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning($"RelayManager: Timed out spawning late-joining client {clientId}.");
    }

    private void AddClientToConnectionOrder(ulong clientId)
    {
        if (_connectionOrder.Contains(clientId))
        {
            return;
        }

        _connectionOrder.Add(clientId);
        Debug.Log($"RelayManager: Connection order recorded for client {clientId} at slot {_connectionOrder.Count - 1}.");
    }

    private bool TrySpawnPlayerForClient(ulong clientId)
    {
        if (_networkManager == null || !_networkManager.IsServer)
        {
            return false;
        }

        if (!_networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient))
        {
            return false;
        }

        if (networkClient.PlayerObject != null)
        {
            return true;
        }

        PlayerSpawnManager spawnManager = PlayerSpawnManager.Instance;
        if (spawnManager == null)
        {
            return false;
        }

        if (!spawnManager.TryGetSpawnForClient(clientId, out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            return false;
        }

        if (!spawnManager.TryGetRole(clientId, out PlayerSpawnManager.PlayerRole role))
        {
            return false;
        }

        GameObject prefab = role == PlayerSpawnManager.PlayerRole.Monster ? monsterPrefab : survivorPrefab;
        if (prefab == null)
        {
            Debug.LogError($"RelayManager: Missing prefab for role {role}.");
            return false;
        }

        GameObject playerObject = Instantiate(prefab, spawnPosition, spawnRotation);
        NetworkObject playerInstance = playerObject.GetComponent<NetworkObject>();
        if (playerInstance == null)
        {
            Debug.LogError($"RelayManager: Spawned prefab for role {role} has no NetworkObject component.");
            Destroy(playerObject);
            return false;
        }

        playerInstance.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"RelayManager: Spawned {role} prefab for client {clientId}.");
        return true;
    }

    private void LogNetworkConfig(string context)
    {
        if (_networkManager == null)
        {
            return;
        }

        string playerPrefabName = _networkManager.NetworkConfig.PlayerPrefab != null
            ? _networkManager.NetworkConfig.PlayerPrefab.name
            : "null";

        string survivorName = survivorPrefab != null ? survivorPrefab.name : "null";
        string monsterName = monsterPrefab != null ? monsterPrefab.name : "null";

        Debug.LogWarning(
            $"RelayManager[{context}]: scene={SceneManager.GetActiveScene().name}, " +
            $"playerPrefab={playerPrefabName}, autoSpawnClientSide={_networkManager.NetworkConfig.AutoSpawnPlayerPrefabClientSide}, " +
            $"forceSamePrefabs={_networkManager.NetworkConfig.ForceSamePrefabs}, survivorPrefab={survivorName}, monsterPrefab={monsterName}");
    }

    private void TryResolveMissingPrefabReferences()
    {
        if (survivorPrefab == null)
        {
            survivorPrefab = Resources.Load<GameObject>(SurvivorPrefabResourcePath);
            Debug.LogWarning($"RelayManager: survivorPrefab was null. Fallback load from Resources returned '{(survivorPrefab != null ? survivorPrefab.name : "null")}'.");
        }

        if (monsterPrefab == null)
        {
            monsterPrefab = Resources.Load<GameObject>(MonsterPrefabResourcePath);
            Debug.LogWarning($"RelayManager: monsterPrefab was null. Fallback load from Resources returned '{(monsterPrefab != null ? monsterPrefab.name : "null")}'.");
        }
    }
}
