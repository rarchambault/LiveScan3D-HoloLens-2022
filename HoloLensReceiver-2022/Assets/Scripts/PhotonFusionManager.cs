using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PhotonFusionManager : MonoBehaviour
{
    // Fusion Components
    private NetworkRunner _networkRunner;

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

    public void ConnectedToServer()
    {
        Debug.Log("Connected to server!");
    }

    public void ReliableData()
    {
        Debug.Log("Received reliable data!");
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
}
