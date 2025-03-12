using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.WebRTC;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private RTCPeerConnection peerConnection;
    private RTCDataChannel documentChannel;
    private RTCDataChannel pointCloudChannel;
    private RTCConfiguration rtcConfig;

    private NetworkRunner networkRunner;
    public bool isSender = true;

    private byte[] documentData;
    private bool hasNewDocument = false;

    private Vector3[] receivedVertices;
    private Color[] receivedColors;
    private bool hasNewPointCloud = false;

    private bool isWebRTCInitialized = false;
    private bool isFusionInitialized = false;

    private PlayerRef currentPlayer;
    public List<NetworkObject> networkObjects = new List<NetworkObject>();

    private const float POSITION_SCALE = 1000f; // Scale for converting float to short

    // Initialization of WebRTC
    void OnEnable()
    {
        StartCoroutine(WaitForFusionConnection());
    }

    private void Update()
    {
        if (documentChannel != null)
        {
            Debug.Log($"DocumentChannel State: {documentChannel.ReadyState}");
        }
        if (pointCloudChannel != null)
        {
            Debug.Log($"PointCloudChannel State: {pointCloudChannel.ReadyState}");
        }
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
        // Wait until Fusion is connected and ready
        while (!isFusionInitialized)
        {
            yield return null;  // Keep waiting until Fusion is initialized
        }

        // Now initialize WebRTC
        rtcConfig = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        InitializeWebRTC();
    }

    private void InitializeWebRTC()
    {
        peerConnection = new RTCPeerConnection(ref rtcConfig);

        peerConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                Debug.Log($"Sending ICE Candidate: {candidate.Candidate}");
                // Send ICE candidate to the other peer via Photon
                if (networkRunner.IsSceneAuthority) // Check for network ownership before sending the candidate
                {
                    SendIceCandidate(candidate);
                }
            }
        };

        // Document Transfer Channel
        documentChannel = peerConnection.CreateDataChannel("documentTransfer");
        documentChannel.OnOpen += () => Debug.Log("Document Channel Opened.");
        documentChannel.OnClose += () => Debug.Log("Document Channel Closed.");
        documentChannel.OnMessage += (message) => HandleDocumentMessage(message);

        // Point Cloud Transfer Channel
        pointCloudChannel = peerConnection.CreateDataChannel("pointCloudTransfer");
        pointCloudChannel.OnOpen += () => Debug.Log("Point Cloud Channel Opened.");
        pointCloudChannel.OnClose += () => Debug.Log("Point Cloud Channel Closed.");
        pointCloudChannel.OnMessage += (message) => HandlePointCloudMessage(message);

        isWebRTCInitialized = true;

        // Start the signaling process for sender
        if (isSender)
        {
            StartCoroutine(CreateOffer());
        }
        else
        {
            Debug.Log("Receiver waiting for an offer.");
        }
    }

    private IEnumerator CreateOffer()
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Waiting...");
            yield break;
        }

        Debug.Log("Creating SDP offer...");
        var offerOp = peerConnection.CreateOffer();
        yield return offerOp;

        var offerDesc = offerOp.Desc;
        yield return peerConnection.SetLocalDescription(ref offerDesc);

        // Send the SDP offer to the other peer using Photon
        SendSdpOffer(offerDesc);
    }

    // Function to handle the offer received (called by receiver)
    public void OnReceivedSdpOffer(string offerSdp)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot process offer.");
            return;
        }

        RTCSessionDescription offerDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,  // Directly set the type as a string for Unity WebRTC
            sdp = offerSdp
        };

        peerConnection.SetRemoteDescription(ref offerDesc);
        StartCoroutine(CreateAnswer());
    }

    private IEnumerator CreateAnswer()
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot create answer.");
            yield break;
        }

        var answerOp = peerConnection.CreateAnswer();
        yield return answerOp;

        var answerDesc = answerOp.Desc;
        yield return peerConnection.SetLocalDescription(ref answerDesc);

        // Send the SDP answer back to the sender
        SendSdpAnswer(answerDesc);
    }

    private void SendSdpOffer(RTCSessionDescription offer)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot send SDP offer.");
            return;
        }

        Debug.Log("Sending SDP offer...");
        RpcSendSdpOffer(offer.sdp);
    }

    private void SendSdpAnswer(RTCSessionDescription answer)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot send SDP answer.");
            return;
        }

        Debug.Log("Sending SDP answer...");
        RpcSendSdpAnswer(answer.sdp);
    }

    private void SendIceCandidate(RTCIceCandidate candidate)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot send ICE candidate.");
            return;
        }

        Debug.Log($"Sending ICE candidate: {candidate.Candidate}");

        // Send full ICE candidate details
        RpcSendIceCandidate(candidate.Candidate, candidate.SdpMid, (int)candidate.SdpMLineIndex);
    }

    // Fusion RPCs to send signaling data
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSendSdpOffer(string offerSdp)
    {
        Debug.Log("Received SDP Offer RPC.");
        if (!isSender)
        {
            OnReceivedSdpOffer(offerSdp);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSendSdpAnswer(string answerSdp)
    {
        Debug.Log("Received SDP Answer RPC.");
        if (isSender)
        {
            RTCSessionDescription answerDesc = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,  // Directly set the type as a string for Unity WebRTC
                sdp = answerSdp
            };
            peerConnection.SetRemoteDescription(ref answerDesc);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSendIceCandidate(string candidate, string sdpMid, int sdpMLineIndex)
    {
        Debug.Log("Received ICE Candidate RPC.");

        RTCIceCandidateInit iceCandidateInit = new RTCIceCandidateInit
        {
            candidate = candidate,
            sdpMid = sdpMid,
            sdpMLineIndex = sdpMLineIndex
        };

        RTCIceCandidate iceCandidate = new RTCIceCandidate(iceCandidateInit);
        peerConnection.AddIceCandidate(iceCandidate);
    }

    private void HandleDocumentMessage(byte[] data)
    {
        Debug.Log($"Received document of size {data.Length} bytes");
        //documentData = data;
        //hasNewDocument = true;
    }

    private void HandlePointCloudMessage(byte[] data)
    {
        Debug.Log("Received point cloud data.");
        DeserializePointCloud(data, out receivedVertices, out receivedColors);
        //hasNewPointCloud = true;
    }

    public void SendDocument(byte[] imageData)
    {
        if (documentChannel != null && documentChannel.ReadyState == RTCDataChannelState.Open)
        {
            Debug.Log("Sending document...");
            documentChannel.Send(imageData);
            documentData = imageData;
            hasNewDocument = true;
        }
    }

    public void SendPointCloud(Vector3[] vertices, Color[] colors)
    {
        if (pointCloudChannel != null && pointCloudChannel.ReadyState == RTCDataChannelState.Open)
        {
            byte[] data = SerializePointCloud(vertices, colors);
            Debug.Log($"Sending point cloud with {vertices.Length} points. (Data size: " + data.Length + ")");
            pointCloudChannel.Send(data);
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
            writer.Write(vertices.Length); // Write number of points

            // Write positions as shorts (scaled)
            foreach (var vertex in vertices)
            {
                writer.Write((short)(vertex.x * POSITION_SCALE));
                writer.Write((short)(vertex.y * POSITION_SCALE));
                writer.Write((short)(vertex.z * POSITION_SCALE));
            }

            // Write colors as RGB bytes (removing alpha)
            foreach (var color in colors)
            {
                writer.Write((byte)(color.r * 255));
                writer.Write((byte)(color.g * 255));
                writer.Write((byte)(color.b * 255));
            }

            return stream.ToArray();
        }
    }

    private void DeserializePointCloud(byte[] data, out Vector3[] vertices, out Color[] colors)
    {
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            int length = reader.ReadInt32();
            vertices = new Vector3[length];
            colors = new Color[length];

            // Read positions as shorts (scaled back to floats)
            for (int i = 0; i < length; i++)
            {
                float x = reader.ReadInt16() / POSITION_SCALE;
                float y = reader.ReadInt16() / POSITION_SCALE;
                float z = reader.ReadInt16() / POSITION_SCALE;
                vertices[i] = new Vector3(x, y, z);
            }

            // Read colors as RGB bytes (normalized to float)
            for (int i = 0; i < length; i++)
            {
                float r = reader.ReadByte() / 255f;
                float g = reader.ReadByte() / 255f;
                float b = reader.ReadByte() / 255f;
                colors[i] = new Color(r, g, b, 1f); // Default alpha to 1
            }
        }
    }

    public bool HasNewDocument()
    {
        return hasNewDocument;
    }

    public byte[] GetReceivedDocument()
    {
        hasNewDocument = false;
        return documentData;
    }

    public bool HasNewPointCloud()
    {
        return hasNewPointCloud;
    }

    public (Vector3[], Color[]) GetReceivedPointCloud()
    {
        hasNewPointCloud = false;
        return (receivedVertices, receivedColors);
    }

    private void OnDestroy()
    {
        // Cleanup
        if (documentChannel != null)
        {
            documentChannel.Close();
        }

        if (pointCloudChannel != null)
        {
            pointCloudChannel.Close();
        }

        if (peerConnection != null)
        {
            peerConnection.Close();
        }
    }
}