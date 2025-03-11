using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.WebRTC;
using UnityEngine;

public class WebRTCManager : NetworkBehaviour
{
    private RTCPeerConnection peerConnection;
    private RTCDataChannel dataChannel;
    private RTCConfiguration rtcConfig;

    // Room management
    private NetworkRunner networkRunner;
    public bool isSender = true; // true if this object is the transferer (sending the file)
    private byte[] documentData;
    private bool hasNewDocument = false;

    private bool isWebRTCInitialized = false; // Flag to ensure WebRTC initialization
    private bool isFusionInitialized = false; // Flag to track Fusion network initialization

    private PlayerRef currentPlayer;
    public List<NetworkObject> networkObjects = new List<NetworkObject>();

    // Initialization of WebRTC
    void OnEnable()
    {
        StartCoroutine(WaitForFusionConnection());
    }

    private void Update()
    {
        if (dataChannel != null)
        {
            Debug.Log(dataChannel.ReadyState);
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

        dataChannel = peerConnection.CreateDataChannel("documentTransfer");
        dataChannel.OnOpen += () => Debug.Log("DataChannel Opened.");
        dataChannel.OnClose += () => Debug.Log("DataChannel Closed.");
        dataChannel.OnMessage += (message) => HandleDataChannelMessage(message);

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

    private void HandleDataChannelMessage(byte[] data)
    {
        // Handle the received file data
        Debug.Log($"Received file data of size {data.Length} bytes");
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

    // Method to send the document
    public void SendDocument(byte[] imageData)
    {
        if (dataChannel != null && dataChannel.ReadyState == RTCDataChannelState.Open)
        {
            Debug.Log("Sending document!");
            documentData = imageData;
            hasNewDocument = true;
            dataChannel.Send(imageData); ;
        }
        else
        {
            Debug.LogError("DataChannel not open or not initialized.");
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

    private void OnDestroy()
    {
        // Cleanup
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