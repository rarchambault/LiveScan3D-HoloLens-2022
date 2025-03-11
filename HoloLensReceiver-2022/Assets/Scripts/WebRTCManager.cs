using Fusion;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private RTCPeerConnection peerConnection;
    private RTCDataChannel dataChannel;
    private RTCConfiguration rtcConfig;

    // Room management
    private NetworkRunner networkRunner;
    public bool isSender = false; // Set to false for the receiver
    private byte[] documentData;
    private bool hasNewDocument = false;

    private bool isWebRTCInitialized = false;
    private bool isFusionInitialized = false;

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
            dataChannel = channel;
            dataChannel.OnOpen += () => Debug.Log("DataChannel Opened.");
            dataChannel.OnClose += () => Debug.Log("DataChannel Closed.");
            dataChannel.OnMessage += (message) => HandleDataChannelMessage(message);
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

    private void HandleDataChannelMessage(byte[] data)
    {
        Debug.Log($"Received file data of size {data.Length} bytes");
        documentData = data;
        hasNewDocument = true;
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

    private void OnDestroy()
    {
        if (dataChannel != null)
        {
            dataChannel.Close();
        }

        if (peerConnection != null)
        {
            peerConnection.Close();
        }
    }
}