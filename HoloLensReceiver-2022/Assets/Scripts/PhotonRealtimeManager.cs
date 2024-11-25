using ExitGames.Client.Photon;
using Photon.Realtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class PhotonRealtimeManager : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, IOnEventCallback
{
    PointCloudRenderer pointCloudRenderer;
    private LoadBalancingClient realtimeClient;

    // Event codes
    private const byte receiveVertexDataEventCode = 100;
    private const byte receiveColorDataEventCode = 101;
    private const byte receiveNumBytesEventCode = 102;
    private const byte sendFrameRequestEventCode = 103;

    private enum SendState { Idle, RequestedFrame, ReceivedNumBytes, ReceivedVertices, ReceivedColors }
    private SendState currentState = SendState.Idle;
    private int numBytes = 0;
    private float[] vertices;
    private byte[] colors;
    private bool isConnected = false;

    // Queue to hold received frames until they can be processed on the main thread
    ConcurrentQueue<(float[] vertices, byte[] colors)> frameQueue = new ConcurrentQueue<(float[], byte[])>();

    void Start()
    {
        // Initialize the Photon Realtime client
        realtimeClient = new LoadBalancingClient
        {
            AppId = "26afa8a3-5b4b-4c88-b92a-0b3432a1de9a", // Replace with your Photon Realtime App ID
            AppVersion = "1.0"    // Use your app's version
        };

        // Add callbacks
        realtimeClient.AddCallbackTarget(this);

        // Connect to the Photon server
        if (!realtimeClient.ConnectToRegionMaster("cae")) // Replace "cae" with your desired region
        {
            Debug.LogError("Failed to connect to Photon Realtime!");
        }

        pointCloudRenderer = GetComponent<PointCloudRenderer>();
    }

    void Update()
    {
        // Service the Photon Realtime client
        if (realtimeClient != null)
        {
            realtimeClient.Service();
        }

        if (isConnected)
        {
            if (currentState == SendState.Idle)
            {
                Debug.Log("Requesting frame");
                RequestFrame();
                currentState = SendState.RequestedFrame;
            }

            // Check if there is frame data in the queue, and render it on the main thread
            if (currentState == SendState.ReceivedColors)
            {
                //frameQueue.TryDequeue(out var frameData)
                pointCloudRenderer.Render(vertices, colors);
                currentState = SendState.Idle;
                Debug.Log("Frame rendered on main thread");
            }
        }
    }

    public void RequestFrame()
    {
        bool success = realtimeClient.OpRaiseEvent(sendFrameRequestEventCode, 0,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);

        currentState = SendState.RequestedFrame;

        Console.WriteLine((success ? "Successfully" : "NOT successfully") + " sent message: " + $"{0}");
    }

    // Connect to or create a room
    public void ConnectToRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 10, // Maximum players allowed
            IsVisible = true,
            IsOpen = true
        };

        if (!realtimeClient.OpJoinOrCreateRoom(new EnterRoomParams
        {
            RoomName = roomName,
            RoomOptions = roomOptions
        }))
        {
            Debug.LogError($"Failed to send join/create room request for {roomName}");
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        Debug.Log($"Event received: Code = {photonEvent.Code}, Data = {photonEvent.CustomData}");

        if (photonEvent.Code == receiveNumBytesEventCode)
        {
            currentState = SendState.ReceivedNumBytes;
            numBytes = (int)photonEvent.CustomData;
        }

        if (photonEvent.Code == receiveVertexDataEventCode)
        {
            if (currentState == SendState.ReceivedNumBytes)
            {
                float[] lVertices = new float[3 * numBytes];
                short[] lShortVertices = new short[3 * numBytes];

                int nBytesToRead = sizeof(short) * 3 * numBytes;
                byte[] vertexBuffer = new byte[nBytesToRead];

                // Read vertex data
                vertexBuffer = (byte[])photonEvent.CustomData;

                Buffer.BlockCopy(vertexBuffer, 0, lShortVertices, 0, nBytesToRead);

                for (int i = 0; i < lShortVertices.Length; i++)
                    lVertices[i] = lShortVertices[i] / 1000.0f;

                vertices = lVertices;
                currentState = SendState.ReceivedVertices;
            }
        }

        if (photonEvent.Code == receiveColorDataEventCode)
        {
            if (currentState == SendState.ReceivedVertices)
            {
                // Read color data
                int nBytesToRead = sizeof(byte) * 3 * numBytes;
                byte[] lColors = new byte[3 * numBytes];
                byte[] colorBuffer = new byte[nBytesToRead];

                colorBuffer = (byte[])photonEvent.CustomData;

                Buffer.BlockCopy(colorBuffer, 0, lColors, 0, nBytesToRead);

                colors = lColors;
                currentState = SendState.ReceivedColors;
            }
        }
    }

    // Callbacks
    public void OnConnected() => Debug.Log("Connected to Photon Realtime!");
    public void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server!");
        ConnectToRoom("TestRoom");
    }

    public void OnJoinedRoom()
    {
        Debug.Log($"Successfully joined room: {realtimeClient.CurrentRoom.Name}");
        isConnected = true;
    }

    public void OnCreateRoomFailed(short returnCode, string message) => Debug.LogError($"Room creation failed: {message}");
    public void OnJoinRoomFailed(short returnCode, string message) => Debug.LogError($"Failed to join room: {message}");
    public void OnDisconnected(DisconnectCause cause) => Debug.LogError($"Disconnected from Photon: {cause}");

    void OnDestroy()
    {
        // Clean up
        realtimeClient.RemoveCallbackTarget(this);
        realtimeClient.Disconnect();
        isConnected = false;
    }

    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    public void OnCreatedRoom() { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    public void OnLeftRoom() { isConnected = false; }
}
