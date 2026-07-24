using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains gameplay spawn points and stable client-to-spawn assignments.
/// Place this in the gameplay scene and assign spawn points in the inspector.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, int> _clientToSpawnIndex = new Dictionary<ulong, int>();
    private int _nextSpawnIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _clientToSpawnIndex.Clear();
        _nextSpawnIndex = 0;
    }

    public bool TryGetSpawnForClient(ulong clientId, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

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
}
