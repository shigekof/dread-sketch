using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Maintains gameplay spawn points and stable client-to-spawn assignments.
/// Place this in the gameplay scene and assign spawn points in the inspector.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    public enum PlayerRole
    {
        Monster = 0,
        Survivor = 1
    }

    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, int> _clientToSpawnIndex = new Dictionary<ulong, int>();
    private readonly Dictionary<ulong, PlayerRole> _clientToRole = new Dictionary<ulong, PlayerRole>();
    private int _nextSpawnIndex;

    private bool _monsterAssigned;
    private ulong _monsterClientId;
    private bool _disconnectCallbackRegistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _clientToSpawnIndex.Clear();
        _clientToRole.Clear();
        _nextSpawnIndex = 0;
        _monsterAssigned = false;
        _monsterClientId = 0;
        _disconnectCallbackRegistered = false;
    }

    private void OnEnable()
    {
        TryRegisterDisconnectCallback();
    }

    private void OnDisable()
    {
        TryUnregisterDisconnectCallback();
    }

    /// <summary>
    /// Call this when a new match starts to ensure clean slot assignment for all clients.
    /// </summary>
    public void ResetForNewMatch()
    {
        _clientToSpawnIndex.Clear();
        _clientToRole.Clear();
        _nextSpawnIndex = 0;
        _monsterAssigned = false;
        _monsterClientId = 0;
        Debug.Log("PlayerSpawnManager: Reset for new match.");
    }

    public bool TryGetSpawnForClient(ulong clientId, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        TryRegisterDisconnectCallback();
        EnsureRoleAssigned(clientId);

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("PlayerSpawnManager: No spawn points assigned.");
            return false;
        }

        if (!_clientToSpawnIndex.TryGetValue(clientId, out int spawnIndex))
        {
            spawnIndex = _nextSpawnIndex % spawnPoints.Length;
            _clientToSpawnIndex[clientId] = spawnIndex;
            _nextSpawnIndex++;
        }

        Transform spawn = spawnPoints[spawnIndex];
        if (spawn == null)
        {
            Debug.LogWarning($"PlayerSpawnManager: Spawn point index {spawnIndex} is null.");
            return false;
        }

        position = spawn.position;
        rotation = spawn.rotation;
        return true;
    }

    public bool TryGetRole(ulong clientId, out PlayerRole role)
    {
        return _clientToRole.TryGetValue(clientId, out role);
    }

    public bool IsMonster(ulong clientId)
    {
        return _clientToRole.TryGetValue(clientId, out PlayerRole role) && role == PlayerRole.Monster;
    }

    private void EnsureRoleAssigned(ulong clientId)
    {
        if (_clientToRole.ContainsKey(clientId))
        {
            return;
        }

        if (!_monsterAssigned)
        {
            _clientToRole[clientId] = PlayerRole.Monster;
            _monsterAssigned = true;
            _monsterClientId = clientId;
            Debug.Log($"PlayerSpawnManager: Assigned Monster role to client {clientId}.");
            return;
        }

        _clientToRole[clientId] = PlayerRole.Survivor;
        Debug.Log($"PlayerSpawnManager: Assigned Survivor role to client {clientId}.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _clientToSpawnIndex.Remove(clientId);
        _clientToRole.Remove(clientId);

        if (_monsterAssigned && _monsterClientId == clientId)
        {
            _monsterAssigned = false;
            _monsterClientId = 0;
            Debug.LogWarning("PlayerSpawnManager: Monster disconnected. Next new connection will be assigned Monster.");
        }
    }

    private void TryRegisterDisconnectCallback()
    {
        if (_disconnectCallbackRegistered)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        _disconnectCallbackRegistered = true;
    }

    private void TryUnregisterDisconnectCallback()
    {
        if (!_disconnectCallbackRegistered)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        _disconnectCallbackRegistered = false;
    }
}
