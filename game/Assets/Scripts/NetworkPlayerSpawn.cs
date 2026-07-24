using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Positions a freshly spawned player on the server using PlayerSpawnManager.
/// Attach this to the network player prefab.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerSpawn : NetworkBehaviour
{
    private const string GameplaySceneName = "ArtSchool_Greybox";

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

        string roleLabel = "Unassigned";
        if (spawnManager.TryGetRole(OwnerClientId, out PlayerSpawnManager.PlayerRole role))
        {
            roleLabel = role.ToString();
        }

        Debug.Log($"NetworkPlayerSpawn: Spawned client {OwnerClientId} as {roleLabel} at {adjustedSpawn}.");
        return true;
    }

    private IEnumerator ApplySpawnWhenReady()
    {
        const int maxGameplayFrames = 300;
        int gameplayFramesWaited = 0;

        while (IsServer && IsSpawned && !_spawnApplied)
        {
            if (TryApplySpawn())
            {
                yield break;
            }

            if (IsGameplaySceneLoaded())
            {
                gameplayFramesWaited++;
                if (gameplayFramesWaited >= maxGameplayFrames)
                {
                    Debug.LogWarning("NetworkPlayerSpawn: Timed out waiting for PlayerSpawnManager after gameplay scene loaded. Keeping current position.");
                    yield break;
                }
            }

            yield return null;
        }
    }

    private static bool IsGameplaySceneLoaded()
    {
        Scene gameplayScene = SceneManager.GetSceneByName(GameplaySceneName);
        return gameplayScene.IsValid() && gameplayScene.isLoaded;
    }
}
