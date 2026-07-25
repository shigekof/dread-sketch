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
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 6f, -12f);

    private Camera _mainCamera;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    private bool _isInGameplayScene;
    private bool _loggedInitialVisibilityState;
    private bool _loggedCameraMissing;
    private bool _loggedCameraRecovered;

    private void Update()
    {
        if (!IsOwner)
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

        TryResolveMainCamera();
    }

    private void LateUpdate()
    {
        if (!IsOwner || !_isInGameplayScene)
        {
            return;
        }

        if (_mainCamera == null)
        {
            TryResolveMainCamera();
            if (_mainCamera == null)
            {
                return;
            }
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

        if (!_loggedInitialVisibilityState && IsOwner)
        {
            _loggedInitialVisibilityState = true;
            int rendererCount = _renderers != null ? _renderers.Length : 0;
            Debug.Log($"NetworkPlayerController: Owner {OwnerClientId}, scene={SceneManager.GetActiveScene().name}, gameplay={_isInGameplayScene}, renderers={rendererCount}, cameraMain={(_mainCamera != null)}");
        }
    }

    private void TryResolveMainCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            if (!_loggedCameraMissing)
            {
                _loggedCameraMissing = true;
                Debug.LogWarning($"NetworkPlayerController: Owner {OwnerClientId} could not find Camera.main in scene {SceneManager.GetActiveScene().name}. Will retry.");
            }

            return;
        }

        if (!_loggedCameraRecovered)
        {
            _loggedCameraRecovered = true;
            Debug.Log($"NetworkPlayerController: Owner {OwnerClientId} attached to Camera.main '{_mainCamera.name}'.");
        }
    }
}
