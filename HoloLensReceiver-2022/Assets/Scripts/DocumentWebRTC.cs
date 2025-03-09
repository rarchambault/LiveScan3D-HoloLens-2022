using Unity.WebRTC;
using UnityEngine;

public class WebRTCManager : MonoBehaviour
{
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _dataChannel;

    private ElementRenderingWebRTC elementRenderingWebRTC;
    private DocumentWebRTC documentWebRTC;

    public ElemRenderer elemRenderer;
    public DocumentPictureReceiver documentPictureReceiver;

    void Start()
    {
        WebRTC.Initialize();
        SetupWebRTC();

        // Initialize separate handlers for element rendering and document image data
        elementRenderingWebRTC = new ElementRenderingWebRTC(_dataChannel);
        documentWebRTC = new DocumentWebRTC(_dataChannel);

        // Subscribe to events
        elementRenderingWebRTC.OnMeshDataReceived += OnMeshDataReceived;
        documentWebRTC.OnDocumentDataReceived += OnDocumentDataReceived;
    }

    private void SetupWebRTC()
    {
        RTCConfiguration config = new RTCConfiguration
        {
            iceServers = new List<RTCIceServer>
            {
                new RTCIceServer { urls = new List<string> { "stun:stun.l.google.com:19302" } }
            }
        };

        _peerConnection = new RTCPeerConnection(config);
        _peerConnection.OnDataChannel = OnDataChannel;
    }

    private void OnDataChannel(RTCDataChannel channel)
    {
        _dataChannel = channel;

        // Initialize the handlers for element rendering and document data
        elementRenderingWebRTC = new ElementRenderingWebRTC(_dataChannel);
        documentWebRTC = new DocumentWebRTC(_dataChannel);
    }

    // Event handlers
    private void OnMeshDataReceived(int nVertices, int nTriangles, Vector3[] vertices, List<int> triangles)
    {
        elemRenderer.TriggerMeshUpdate(nVertices, nTriangles, vertices, triangles);
    }

    private void OnDocumentDataReceived(byte[] imageData)
    {
        documentPictureReceiver.ApplyTexture(imageData);
    }

    // Cleanup
    void OnDestroy()
    {
        _peerConnection.Close();
        WebRTC.Dispose();
    }
}
