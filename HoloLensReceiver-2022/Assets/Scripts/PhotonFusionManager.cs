using Fusion;
using Fusion.Sockets;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PhotonFusionManager : MonoBehaviour
{
    // Fusion Components
    private NetworkRunner _networkRunner;
    private PlayerRef currentPlayer;
    private List<PlayerRef> otherPlayers = new List<PlayerRef>();
    public bool currentPlayerConnected = false;

    // Unique key for image data
    private ReliableKey documentImageDataKey = ReliableKey.FromInts(43, 0, 0, 0);
    private ReliableKey receivedDocumentImageDataKey = ReliableKey.FromInts(44, 0, 0, 0);

    private byte[] ack = { 1 };

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

    public void SendAck()
    {
        if (otherPlayers.Count > 0)
        {
            foreach (PlayerRef player in otherPlayers)
            {
                if (_networkRunner.IsRunning && player.IsRealPlayer)
                {
                    _networkRunner.SendReliableDataToPlayer(player, receivedDocumentImageDataKey, ack);
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

            // Send acknowledgement to confirm reception
            SendAck();
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
