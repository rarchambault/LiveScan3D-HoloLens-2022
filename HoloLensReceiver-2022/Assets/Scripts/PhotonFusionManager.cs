using ExitGames.Client.Photon;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PhotonFusionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Fusion Components
    private NetworkRunner _networkRunner;

    // Event Codes
    private const byte ChatMessageEventCode = 1;

    void Start()
    {
        // Initialize Fusion
        _networkRunner = FindObjectOfType<NetworkRunner>();

        if (_networkRunner != null)
        {
            Debug.Log("Found network runner");
        }
    }

    void Update()
    {
    }

    private void OnDestroy()
    {
    }

    public void ConnectedToServer()
    {
        Debug.Log("Connected to server!");
    }

    public void PlayerJoined()
    {
        Debug.Log("Player joined!");
    }

    public void ReliableData(NetworkRunner networkRunner, PlayerRef playerRef, ReliableKey key, ArraySegment<byte> data)
    {
        if (key.Equals(ReliableKey.FromInts(42, 0, 0, 0)))
        {
            Debug.Log("Received vertex data!");
        }
        else if (key.Equals(ReliableKey.FromInts(43, 0, 0, 0)))
        {
            Debug.Log("Received color data!");
        }

        _networkRunner = networkRunner;
    }

    public void ReliableProgress(NetworkRunner networkRunner, PlayerRef playerRef, ReliableKey key, float progress)
    {
        Debug.Log("Currently receiving reliable data!");
        _networkRunner = networkRunner;
    }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log("Fusion object exit AOI");
    }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log("Fusion object enter AOI");
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Fusion player joined!");
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Fusion player left!");
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        Debug.Log("Fusion Input received!");
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Fusion OnShutdown called!");
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Fusion Connected to server!");
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        Debug.Log("Fusion Reliable data received!");
    }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}
