using Fusion;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private PeerConnection peerConnection;
    private DataChannel documentChannel;
    private DataChannel pointCloudChannel;

    private NetworkRunner networkRunner;
    public bool isSender = true; // This is the sender

    private byte[] documentData;
    private bool hasNewDocument = false;

    private Vector3[] receivedVertices;
    private Color[] receivedColors;
    private bool hasNewPointCloud = false;

    private bool isWebRTCInitialized = false;
    private bool isFusionInitialized = false;

    private const float POSITION_SCALE = 1000f;

    private PlayerRef currentPlayer;
    public List<NetworkObject> networkObjects = new List<NetworkObject>();

    void OnEnable()
    {
        StartCoroutine(WaitForFusionConnection());
    }

    public void PlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        // Once a player joins, we can initialize Fusion and WebRTC
        if (!isFusionInitialized)
        {
            isFusionInitialized = true;
            currentPlayer = player;
        }

        this.networkRunner = networkRunner;
        Debug.Log("PlayerJoined called. Fusion is now initialized.");
    }

    public void SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (isFusionInitialized)
        {
            NetworkObject networkedObject = networkRunner.Spawn(prefab.GetComponent<NetworkObject>(), position, rotation, currentPlayer);
            networkObjects.Add(networkedObject);
        }
    }

    public void DestroyNetworkObjects(int nObjects)
    {
        for (int i = 0; i < nObjects; i++)
        {
            NetworkObject toRemove = networkObjects[0];
            networkRunner.Despawn(toRemove);
            networkObjects.Remove(networkObjects[0]);
        }
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
        Debug.Log("Initializing WebRTC for sender...");

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

        // Create Data Channels (Explicitly on the Sender Side)
        documentChannel = await peerConnection.AddDataChannelAsync("documentTransfer", ordered: true, reliable: true);
        pointCloudChannel = await peerConnection.AddDataChannelAsync("pointCloudTransfer", ordered: true, reliable: true);

        // Register Event Handlers for the Data Channels
        documentChannel.StateChanged += OnDocumentChannelStateChanged;
        documentChannel.MessageReceived += HandleDocumentMessage;

        pointCloudChannel.StateChanged += OnPointCloudChannelStateChanged;
        pointCloudChannel.MessageReceived += HandlePointCloudMessage;

        isWebRTCInitialized = true;
        Debug.Log("WebRTC initialized successfully. Creating SDP offer...");

        // Start the offer process
        peerConnection.CreateOffer();
    }

    private void OnDataChannelAdded(DataChannel channel)
    {
        Debug.Log($"Data channel added: {channel.Label}");
    }

    private void OnDocumentChannelStateChanged()
    {
        Debug.Log($"Document DataChannel state changed: {documentChannel.State}");
    }

    private void OnPointCloudChannelStateChanged()
    {
        Debug.Log($"Point Cloud DataChannel state changed: {pointCloudChannel.State}");
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
            var offer = new SdpMessage
            {
                Type = SdpMessageType.Offer,
                Content = sdp
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

    // Sending Document Data
    public void SendDocument(byte[] documentData)
    {
        if (documentChannel != null && documentChannel.State == DataChannel.ChannelState.Open)
        {
            Debug.Log($"Sending document of size {documentData.Length} bytes...");
            documentChannel.SendMessage(documentData);
            this.documentData = documentData;
            hasNewDocument = true;
        }
    }

    // Sending Point Cloud Data
    public void SendPointCloud(Vector3[] vertices, Color[] colors)
    {
        if (pointCloudChannel != null && pointCloudChannel.State == DataChannel.ChannelState.Open)
        {
            byte[] data = SerializePointCloud(vertices, colors);
            Debug.Log($"Sending point cloud with {vertices.Length} points. (Data size: {data.Length} bytes)");
            pointCloudChannel.SendMessage(data);
            receivedVertices = vertices;
            receivedColors = colors;
            hasNewPointCloud = true;
        }
    }

    private byte[] SerializePointCloud(Vector3[] vertices, Color[] colors)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(vertices.Length);

            foreach (var vertex in vertices)
            {
                writer.Write((short)(vertex.x * POSITION_SCALE));
                writer.Write((short)(vertex.y * POSITION_SCALE));
                writer.Write((short)(vertex.z * POSITION_SCALE));
            }

            foreach (var color in colors)
            {
                writer.Write((byte)(color.r * 255));
                writer.Write((byte)(color.g * 255));
                writer.Write((byte)(color.b * 255));
            }

            return stream.ToArray();
        }
    }

    // Handling Received Data (For Debugging and Validation)
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