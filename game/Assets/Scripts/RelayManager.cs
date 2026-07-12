using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages Unity Relay connection logic for multiplayer sessions.
/// Handles both host (server) and client join flows.
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [SerializeField] private int maxPlayers = 5;
    private UnityTransport _unityTransport;
    private NetworkManager _networkManager;

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

        Debug.Log($"RelayManager initialized successfully. MaxPlayers={maxPlayers}");
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
}
