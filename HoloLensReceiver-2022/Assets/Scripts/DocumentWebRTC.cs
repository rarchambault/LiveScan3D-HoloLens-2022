using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

public class DocumentWebRTC : MonoBehaviour
{
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _dataChannel;

    private ElementRenderingWebRTC elementRenderingWebRTC;
    private DocumentWebRTC documentWebRTC;

    public ElemRenderer elemRenderer;
    public DocumentPictureReceiver documentPictureReceiver;

    void Start()
    {
    }

    private void SetupWebRTC()
    {
    }

    private void OnDataChannel(RTCDataChannel channel)
    {
    }

    // Event handlers
    private void OnMeshDataReceived(int nVertices, int nTriangles, Vector3[] vertices, List<int> triangles)
    {
        //elemRenderer.TriggerMeshUpdate(nVertices, nTriangles, vertices, triangles);
    }
}
