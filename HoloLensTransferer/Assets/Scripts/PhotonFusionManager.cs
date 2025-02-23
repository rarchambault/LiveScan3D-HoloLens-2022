using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PhotonFusionManager : MonoBehaviour
{
    // Fusion Components
    private NetworkRunner _networkRunner;
    private PlayerRef currentPlayer;
    private List<PlayerRef> otherPlayers = new List<PlayerRef>();
    public bool currentPlayerConnected = false;
    public List<NetworkObject> networkObjects = new List<NetworkObject>();

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
        //timeSinceLastSend += Time.deltaTime;

        //if (timeSinceLastSend >= sendTimer)
        //{
        //    // Large data that needs to be sent
        //    byte[] largeData = new byte[10000];

        //    // Initialize the array with random values
        //    System.Random random = new System.Random();
        //    random.NextBytes(largeData);

        //    // Provide 4 numbers as a unique key for the data
        //    var key = ReliableKey.FromInts(42, 0, 0, 0);


        //    if (otherPlayers.Count > 0)
        //    {
        //        foreach (PlayerRef player in otherPlayers)
        //        {
        //            // Use as a client to send data to the server/host
        //            if (_networkRunner == null) Debug.Log("Network runner is null");
        //            if (player == null) Debug.Log("Player is null");
        //            if (key.Equals(null)) Debug.Log("Key is null");
        //            if (largeData == null) Debug.Log("Data is null");

        //            if (_networkRunner.IsRunning)
        //            {
        //                if (player.IsRealPlayer)
        //                {
        //                    Debug.Log("Network is running and player is a real player");
        //                    _networkRunner.SendReliableDataToPlayer(player, key, largeData);
        //                    timeSinceLastSend = 0.0f;
        //                }
        //                else
        //                {
        //                    Debug.LogError("Player is not a real player");
        //                }
        //            }
        //            else
        //            {
        //                Debug.LogError("Network is not running");
        //            }
        //        }
        //    }
        //}
    }

    private void OnDestroy()
    {
    }

    public void PlayerJoined(NetworkRunner runner, PlayerRef playerRef)
    {
        Debug.Log("Player " + playerRef.ToString() + " joined");

        _networkRunner = runner;

        if (!currentPlayerConnected)
        {
            currentPlayer = playerRef;
            currentPlayerConnected = true;
        }
        else
        {
            otherPlayers.Add(playerRef);
        }
    }

    public void ConnectedToServer()
    {
        Debug.Log("Connected to server!");
    }

    public void ReliableData()
    {
        Debug.Log("Received reliable data!");
    }

    public void SendFrame(byte[] vertices, byte[] colors)
    {
        // Provide 4 numbers as a unique key for the data
        var verticesKey = ReliableKey.FromInts(42, 0, 0, 0);
        var colorsKey = ReliableKey.FromInts(43, 0, 0, 0);

        if (otherPlayers.Count > 0)
        {
            foreach (PlayerRef player in otherPlayers)
            {
                if (_networkRunner.IsRunning && player.IsRealPlayer)
                {
                    _networkRunner.SendReliableDataToPlayer(player, verticesKey, vertices);
                    Debug.Log("Sent vertex data to all players");

                    _networkRunner.SendReliableDataToPlayer(player, colorsKey, colors);
                    Debug.Log("Sent color data to all players");
                }
            }
        }
    }

    public void SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        NetworkObject networkedObject = _networkRunner.Spawn(prefab.GetComponent<NetworkObject>(), position, rotation, currentPlayer);
        networkObjects.Add(networkedObject);
    }

    public void DestroyNetworkObjects(int nObjects)
    {
        for (int i = 0; i < nObjects; i++)
        {
            NetworkObject toRemove = networkObjects[0];
            _networkRunner.Despawn(toRemove);
            networkObjects.Remove(networkObjects[0]);
        }
    }
}
