using Fusion;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private PeerConnection peerConnection;
    private DataChannel documentChannel;
    private DataChannel pointCloudChannel;

    // Room management
    private NetworkRunner networkRunner;
    public bool isSender = false; // Set to false for the receiver
    private byte[] documentData;
    private bool hasNewDocument = false;

    private Vector3[] receivedVertices;
    private Color[] receivedColors;
    private bool hasNewPointCloud = false;

    private bool isWebRTCInitialized = false;
    private bool isFusionInitialized = false;

    private const float POSITION_SCALE = 1000f;

    void OnEnable()
    {
        StartCoroutine(WaitForFusionConnection());
    }

    public void PlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        this.networkRunner = networkRunner;
        isFusionInitialized = true;
        Debug.Log("PlayerJoined called. Fusion is now initialized.");
    }

    private IEnumerator WaitForFusionConnection()
    {
        while (!isFusionInitialized)
        {
            yield return null;
        }

        InitializeWebRTC();
    }

    private async void InitializeWebRTC()
    {
        Debug.Log("Initializing WebRTC...");

        var config = new PeerConnectionConfiguration
        {
            IceServers = new List<IceServer>
            {
                new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
            }
        };

        peerConnection = new PeerConnection();

        // Initialize WebRTC with the provided config
        await peerConnection.InitializeAsync(config);

        // Register event handlers
        peerConnection.LocalSdpReadytoSend += OnLocalSdpReadyToSend;
        peerConnection.IceCandidateReadytoSend += OnIceCandidateReadyToSend;
        peerConnection.DataChannelAdded += OnDataChannelAdded;

        isWebRTCInitialized = true;
        Debug.Log("WebRTC initialized successfully.");
    }

    private void OnDataChannelAdded(DataChannel channel)
    {
        Debug.Log($"Data channel added: {channel.Label}");

        if (channel.Label == "documentTransfer")
        {
            documentChannel = channel;
            documentChannel.StateChanged += OnDocumentChannelStateChanged;
            documentChannel.MessageReceived += HandleDocumentMessage;
        }
        else if (channel.Label == "pointCloudTransfer")
        {
            pointCloudChannel = channel;
            pointCloudChannel.StateChanged += OnPointCloudChannelStateChanged;
            pointCloudChannel.MessageReceived += HandlePointCloudMessage;
        }
    }

    private void OnDocumentChannelStateChanged()
    {
        Debug.Log($"Document DataChannel state changed: {documentChannel.State}");
    }

    private void OnPointCloudChannelStateChanged()
    {
        Debug.Log($"Point Cloud DataChannel state changed: {pointCloudChannel.State}");
    }

    public void OnReceivedSdpOffer(string offerSdp)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot process offer.");
            return;
        }

        Debug.Log("Received SDP offer. Setting remote description...");

        var offer = new SdpMessage
        {
            Type = SdpMessageType.Offer,
            Content = offerSdp
        };

        peerConnection.SetRemoteDescriptionAsync(offer).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Remote description set. Creating answer...");
                peerConnection.CreateAnswer();
            }
            else
            {
                Debug.LogError("Failed to set remote description: " + task.Exception);
            }
        });
    }

    private void OnLocalSdpReadyToSend(SdpMessage message)
    {
        Debug.Log("Sending SDP message: " + message.Type);
        RpcSendSdpMessage(message.Type == SdpMessageType.Offer ? "offer" : "answer", message.Content);
    }

    private void OnIceCandidateReadyToSend(IceCandidate candidate)
    {
        Debug.Log("Sending ICE Candidate...");
        RpcSendIceCandidate(candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSendSdpMessage(string type, string sdp)
    {
        Debug.Log($"Received SDP {type} RPC.");

        if (type == "offer" && !isSender)
        {
            OnReceivedSdpOffer(sdp);
        }
        else if (type == "answer" && isSender)
        {
            var answer = new SdpMessage
            {
                Type = SdpMessageType.Answer,
                Content = sdp
            };
            peerConnection.SetRemoteDescriptionAsync(answer);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSendIceCandidate(string candidateContent, string sdpMid, int sdpMlineIndex)
    {
        Debug.Log("Received ICE Candidate RPC.");

        IceCandidate candidate = new IceCandidate
        {
            Content = candidateContent,
            SdpMid = sdpMid,
            SdpMlineIndex = sdpMlineIndex
        };

        peerConnection.AddIceCandidate(candidate);
    }

    private void HandleDocumentMessage(byte[] data)
    {
        Debug.Log($"Received document data of size {data.Length} bytes");
        documentData = data;
        hasNewDocument = true;
    }

    private void HandlePointCloudMessage(byte[] data)
    {
        Debug.Log($"Received point cloud data of size {data.Length} bytes");
        DeserializePointCloud(data, out receivedVertices, out receivedColors);
        hasNewPointCloud = true;
    }

    private void DeserializePointCloud(byte[] data, out Vector3[] vertices, out Color[] colors)
    {
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            int length = reader.ReadInt32();
            vertices = new Vector3[length];
            colors = new Color[length];

            for (int i = 0; i < length; i++)
            {
                float x = reader.ReadInt16() / POSITION_SCALE;
                float y = reader.ReadInt16() / POSITION_SCALE;
                float z = reader.ReadInt16() / POSITION_SCALE;
                vertices[i] = new Vector3(x, y, z);
            }

            for (int i = 0; i < length; i++)
            {
                float r = reader.ReadByte() / 255f;
                float g = reader.ReadByte() / 255f;
                float b = reader.ReadByte() / 255f;
                colors[i] = new Color(r, g, b, 1f);
            }
        }
    }

    public bool HasNewDocument() => hasNewDocument;
    public byte[] GetReceivedDocument() { hasNewDocument = false; return documentData; }

    public bool HasNewPointCloud() => hasNewPointCloud;
    public (Vector3[], Color[]) GetReceivedPointCloud() { hasNewPointCloud = false; return (receivedVertices, receivedColors); }

    private void OnDestroy()
    {
        peerConnection?.Close();
        peerConnection?.Dispose();
    }
}