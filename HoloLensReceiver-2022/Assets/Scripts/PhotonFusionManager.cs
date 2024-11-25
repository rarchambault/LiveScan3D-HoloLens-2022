using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;
using Fusion.Photon.Realtime;
using ExitGames.Client.Photon;

public class PhotonFusionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    private PhotonPeer photonPeer;

    // App ID and Room settings
    private string AppId = "26afa8a3-5b4b-4c88-b92a-0b3432a1de9a";
    private string RoomName = "TestRoom";
    private const byte TestMessageEventCode = 100; // Custom event code

    void Start()
    {
        // Initialize Fusion
        StartFusionClient();

        // Initialize PhotonPeer
        //photonPeer = new PhotonPeer(new CustomPhotonPeerListener(), ConnectionProtocol.Udp);
        //photonPeer.Connect("cae", AppId);
    }

    void Update()
    {
        // Service the PhotonPeer
        //photonPeer.Service();
    }

    void StartFusionClient()
    {
        // Create a NetworkRunner instance
        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        // Start the NetworkRunner with shared mode (Client-Server or Host)
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client, // Join as client
            SessionName = RoomName,    // Room name must match
            Scene = SceneManager.GetActiveScene().buildIndex, // Use current scene
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(), // Default scene manager
            CustomPhotonAppSettings = new AppSettings()
            {
                AppIdFusion = AppId,
                AppIdRealtime = AppId,
                FixedRegion = "cae",
                AppVersion = "1.0",
            }
        };

        networkRunner.StartGame(startGameArgs);
    }

    // Fusion Callbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined the room.");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left the room.");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Handle player input here
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.LogWarning($"Input missing for player {player.PlayerId}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogError($"NetworkRunner shut down due to: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to Photon Fusion server.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.LogError("Disconnected from server.");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Failed to connect: {reason}");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("Session list updated.");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.Log("Received custom authentication response.");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host migration occurred.");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        Debug.Log("Reliable data received!");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene loaded.");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene loading...");
    }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }
}
