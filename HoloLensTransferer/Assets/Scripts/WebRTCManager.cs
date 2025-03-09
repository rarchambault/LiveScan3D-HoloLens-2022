using Unity.WebRTC;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WebRTCManager : MonoBehaviour
{
    // WebRTC Components
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _dataChannel;
    private List<RTCPeerConnection> connectedPeers = new List<RTCPeerConnection>();
    public bool currentPlayerConnected = false;
    public List<GameObject> networkObjects = new List<GameObject>();

    // Unique key for image data
    private string documentImageDataKey = "doc_image_data";

    // Store received image data
    private byte[] _receivedDocumentImageData = null;
    private bool hasNewDocument = false;

    void Start()
    {
        WebRTC.Initialize();
        _peerConnection = new RTCPeerConnection();
        _dataChannel = _peerConnection.CreateDataChannel("dataChannel");
        _dataChannel.OnMessage = OnDataReceived;

        Debug.Log("WebRTC initialized and data channel created");
    }

    public void PlayerJoined(RTCPeerConnection peerConnection)
    {
        Debug.Log("New peer joined");

        connectedPeers.Add(peerConnection);
        if (!currentPlayerConnected)
        {
            _peerConnection = peerConnection;
            currentPlayerConnected = true;
        }
    }

    public void ConnectedToServer()
    {
        Debug.Log("Connected to WebRTC peer!");
    }

    public void SendDocument(byte[] imageData)
    {
        if (_dataChannel.ReadyState == RTCDataChannelState.Open)
        {
            _dataChannel.Send(imageData);
            Debug.Log("Sent document image data");
        }
    }

    private void OnDataReceived(byte[] data)
    {
        _receivedDocumentImageData = data;
        hasNewDocument = true;
        Debug.Log($"Received image data (Size: {_receivedDocumentImageData.Length} bytes)");
    }

    public bool HasNewDocument()
    {
        return hasNewDocument;
    }

    public byte[] GetReceivedDocument()
    {
        hasNewDocument = false;
        return _receivedDocumentImageData;
    }

    public void SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject networkedObject = Instantiate(prefab, position, rotation);
        networkObjects.Add(networkedObject);
    }

    public void DestroyNetworkObjects(int nObjects)
    {
        for (int i = 0; i < nObjects && networkObjects.Count > 0; i++)
        {
            GameObject toRemove = networkObjects[0];
            Destroy(toRemove);
            networkObjects.RemoveAt(0);
        }
    }

    private void OnDestroy()
    {
        _dataChannel.Close();
        _peerConnection.Close();
        WebRTC.Dispose();
    }
}
