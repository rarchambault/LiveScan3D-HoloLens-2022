using Fusion;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Unity.WebRTC;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private RTCPeerConnection peerConnection;
    private RTCDataChannel documentChannel;
    private RTCDataChannel pointCloudChannel;
    private RTCConfiguration rtcConfig;

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

    // Initialization
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
                if (networkRunner.IsSceneAuthority)
                {
                    SendIceCandidate(candidate);
                }
            }
        };

        peerConnection.OnDataChannel = channel =>
        {
            if (channel.Label == "documentTransfer")
            {
                documentChannel = channel;
                documentChannel.OnOpen += () => Debug.Log("Document DataChannel Opened.");
                documentChannel.OnClose += () => Debug.Log("Document DataChannel Closed.");
                documentChannel.OnMessage += HandleDocumentMessage;
            }
            else if (channel.Label == "pointCloudTransfer")
            {
                pointCloudChannel = channel;
                pointCloudChannel.OnOpen += () => Debug.Log("Point Cloud DataChannel Opened.");
                pointCloudChannel.OnClose += () => Debug.Log("Point Cloud DataChannel Closed.");
                pointCloudChannel.OnMessage += HandlePointCloudMessage;
            }
        };

        isWebRTCInitialized = true;
        Debug.Log("WebRTC initialized for receiver.");
    }

    // Handle incoming SDP offer and create an SDP answer
    public void OnReceivedSdpOffer(string offerSdp)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot process offer.");
            return;
        }

        Debug.Log("Received SDP offer. Creating answer...");
        RTCSessionDescription offerDesc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
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

        Debug.Log("Sending SDP answer...");
        SendSdpAnswer(answerDesc);
    }

    private void SendSdpAnswer(RTCSessionDescription answer)
    {
        if (!isWebRTCInitialized || !isFusionInitialized)
        {
            Debug.LogError("WebRTC or Fusion not initialized. Cannot send SDP answer.");
            return;
        }

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

    // Fusion RPCs to receive signaling data
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
                type = RTCSdpType.Answer,
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

    // Handle incoming document data
    private void HandleDocumentMessage(byte[] data)
    {
        Debug.Log($"Received document data of size {data.Length} bytes");
        documentData = data;
        hasNewDocument = true;
    }

    // Handle incoming point cloud data
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

    // Method to check for new documents
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