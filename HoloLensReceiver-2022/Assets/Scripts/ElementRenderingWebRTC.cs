using Unity.WebRTC;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ElementRenderingWebRTC
{
    private RTCDataChannel _dataChannel;
    private int nVertices;
    private int nTriangles;
    private Vector3[] newVertices;
    private List<int> newTriangles;

    public delegate void MeshDataReceived(int nVertices, int nTriangles, Vector3[] vertices, List<int> triangles);
    public event MeshDataReceived OnMeshDataReceived;

    public ElementRenderingWebRTC(RTCDataChannel dataChannel)
    {
        _dataChannel = dataChannel;
        _dataChannel.OnMessage = OnDataChannelMessage;
    }

    private void OnDataChannelMessage(byte[] data)
    {
        if (data.Length > 0)
        {
            DeserializeMeshData(data);
            OnMeshDataReceived?.Invoke(nVertices, nTriangles, newVertices, newTriangles);
        }
    }

    private void DeserializeMeshData(byte[] data)
    {
        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesList = new List<int>();

        int idx = 0;

        // Deserialize vertices
        for (int i = 0; i < nVertices; i++)
        {
            float x = BitConverter.ToSingle(data, idx);
            idx += sizeof(float);
            float y = BitConverter.ToSingle(data, idx);
            idx += sizeof(float);
            float z = BitConverter.ToSingle(data, idx);
            idx += sizeof(float);

            verticesList.Add(new Vector3(x, y, z));
        }

        // Deserialize triangles
        for (int i = 0; i < nTriangles; i++)
        {
            int triangle = BitConverter.ToInt32(data, idx);
            idx += sizeof(int);
            trianglesList.Add(triangle);
        }

        newVertices = verticesList.ToArray();
        newTriangles = trianglesList;
    }

    public void SendMeshData(int nVertices, int nTriangles, Vector3[] vertices, List<int> triangles)
    {
        byte[] serializedData = SerializeMeshData(nVertices, nTriangles, vertices, triangles);
        if (_dataChannel != null)
        {
            _dataChannel.Send(serializedData);
        }
    }

    private byte[] SerializeMeshData(int nVertices, int nTriangles, Vector3[] vertices, List<int> triangles)
    {
        List<byte> dataList = new List<byte>();

        // Serialize vertices
        foreach (var vertex in vertices)
        {
            dataList.AddRange(BitConverter.GetBytes(vertex.x));
            dataList.AddRange(BitConverter.GetBytes(vertex.y));
            dataList.AddRange(BitConverter.GetBytes(vertex.z));
        }

        // Serialize triangles
        foreach (var triangle in triangles)
        {
            dataList.AddRange(BitConverter.GetBytes(triangle));
        }

        return dataList.ToArray();
    }
}
