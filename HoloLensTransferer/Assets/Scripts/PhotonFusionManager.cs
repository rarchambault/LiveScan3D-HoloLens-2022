using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PhotonFusionManager : MonoBehaviour
{
    // Fusion Components
    private NetworkRunner _networkRunner;
    private PlayerRef currentPlayer;
    private List<PlayerRef> connectedPlayers = new List<PlayerRef>();
    public bool currentPlayerConnected = false;
    public List<NetworkObject> networkObjects = new List<NetworkObject>();

    // Unique key for image data
    private ReliableKey documentImageDataKey = ReliableKey.FromInts(43, 0, 0, 0);

    // Store received image data
    private byte[] _receivedDocumentImageData = null;
    private bool hasNewDocument = false;

    void Start()
    {
        _networkRunner = FindObjectOfType<NetworkRunner>();

        if (_networkRunner != null)
        {
            Debug.Log("Found network runner");
        }
    }

    public void PlayerJoined(NetworkRunner runner, PlayerRef playerRef)
    {
        Debug.Log("Player " + playerRef.ToString() + " joined");

        _networkRunner = runner;
        connectedPlayers.Add(playerRef);

        if (!currentPlayerConnected)
        {
            currentPlayer = playerRef;
            currentPlayerConnected = true;
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

    public void SendDocument(byte[] imageData)
    {
        if (connectedPlayers.Count > 0)
        {
            foreach (PlayerRef player in connectedPlayers)
            {
                if (_networkRunner.IsRunning && player.IsRealPlayer)
                {
                    _networkRunner.SendReliableDataToPlayer(player, documentImageDataKey, imageData);
                }
            }
        }
    }

    // Handle incoming reliable data
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (key.Equals(documentImageDataKey))
        {
            _receivedDocumentImageData = data.ToArray();
            hasNewDocument = true;
            Debug.Log($"Received image data from player {player} (Size: {_receivedDocumentImageData.Length} bytes)");
        }
    }

    public bool HasNewDocument()
    {
        return hasNewDocument;
    }

    // Function to retrieve the received image data
    public byte[] GetReceivedDocument()
    {
        hasNewDocument = false;
        return _receivedDocumentImageData;
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
