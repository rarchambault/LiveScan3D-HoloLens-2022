using Unity.WebRTC;
using System;

public class WebRTCManager : MonoBehaviour
{
    private RTCDataChannel _dataChannel;
    private byte[] _receivedDocumentData = null;
    private int _documentWidth;
    private int _documentHeight;

    public event Action<byte[], int, int> OnDocumentImageReceived;

    public void Start()
    {
        WebRTC.Initialize();
        CreatePeerConnection();
    }

    void OnDestroy()
    {
        WebRTC.Dispose();
    }

    private void CreatePeerConnection()
    {
        RTCConfiguration config = new RTCConfiguration();
        RTCPeerConnection peerConnection = new RTCPeerConnection(config);
        _dataChannel = peerConnection.CreateDataChannel("documentDataChannel", new RTCDataChannelInit());
        _dataChannel.OnMessage = OnDataChannelMessage;
    }

    private void OnDataChannelMessage(byte[] data)
    {
        // Assuming the first part of the message is a key and then the image data follows
        string key = System.Text.Encoding.UTF8.GetString(data, 0, 18);
        if (key == "documentImageData")
        {
            _receivedDocumentData = new byte[data.Length - 18];
            Array.Copy(data, 18, _receivedDocumentData, 0, _receivedDocumentData.Length);

            // Send the received data to the receiver
            OnDocumentImageReceived?.Invoke(_receivedDocumentData, _documentWidth, _documentHeight);
        }
    }

    public void SendDocumentImage(byte[] imageData, int width, int height)
    {
        if (_dataChannel.ReadyState == RTCDataChannelState.Open)
        {
            byte[] key = System.Text.Encoding.UTF8.GetBytes("documentImageData");
            byte[] message = new byte[key.Length + imageData.Length];
            Array.Copy(key, message, key.Length);
            Array.Copy(imageData, 0, message, key.Length, imageData.Length);
            _dataChannel.Send(message);

            // Store width and height for future use
            _documentWidth = width;
            _documentHeight = height;
        }
    }

    public byte[] GetReceivedDocument()
    {
        return _receivedDocumentData;
    }

    public bool HasNewDocument()
    {
        return _receivedDocumentData != null;
    }

    public void SetReceivedDocument(byte[] imageData)
    {
        _receivedDocumentData = imageData;
    }
}
