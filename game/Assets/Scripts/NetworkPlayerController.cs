using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Very simple prototype movement controller for a networked player.
/// Movement is owner-driven and replicated via NetworkTransform.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    private const string GameplaySceneName = "ArtSchool_Greybox";

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3f, -6f);

    private Camera _mainCamera;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    private bool _isInGameplayScene;

    private void Update()
    {
        if (!IsOwner || !_isInGameplayScene)
        {
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
        }
        
        Vector3 input = new Vector3(horizontal, 0f, vertical);

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        transform.position += input * (moveSpeed * Time.deltaTime);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        RefreshSceneState();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        if (!IsOwner)
        {
            return;
        }

        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!IsOwner || !_isInGameplayScene || _mainCamera == null)
        {
            return;
        }

        _mainCamera.transform.position = transform.position + cameraOffset;
        _mainCamera.transform.LookAt(transform.position + Vector3.up);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        RefreshSceneState();
    }

    private void RefreshSceneState()
    {
        _isInGameplayScene = SceneManager.GetActiveScene().name == GameplaySceneName;

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = _isInGameplayScene;
                }
            }
        }

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = _isInGameplayScene;
                }
            }
        }
    }
}
