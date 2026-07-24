using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Positions a freshly spawned player on the server using PlayerSpawnManager.
/// Attach this to the network player prefab.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerSpawn : NetworkBehaviour
{
    private bool _spawnApplied;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            return;
        }

        if (!TryApplySpawn())
        {
            StartCoroutine(ApplySpawnWhenReady());
        }
    }

    private bool TryApplySpawn()
    {
        if (_spawnApplied)
        {
            return true;
        }

        PlayerSpawnManager spawnManager = PlayerSpawnManager.Instance;
        if (spawnManager == null)
        {
            return false;
        }

        if (!spawnManager.TryGetSpawnForClient(OwnerClientId, out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            return false;
        }

        // Lift spawn a bit so the capsule/cube is clearly above the floor in greybox tests.
        Vector3 adjustedSpawn = spawnPosition + Vector3.up;
        transform.SetPositionAndRotation(adjustedSpawn, spawnRotation);
        _spawnApplied = true;
        Debug.Log($"NetworkPlayerSpawn: Spawned client {OwnerClientId} at {adjustedSpawn}.");
        return true;
    }

    private IEnumerator ApplySpawnWhenReady()
    {
        const int maxFrames = 300;
        for (int i = 0; i < maxFrames; i++)
        {
            if (TryApplySpawn())
            {
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("NetworkPlayerSpawn: Timed out waiting for PlayerSpawnManager. Keeping current position.");
    }
}
