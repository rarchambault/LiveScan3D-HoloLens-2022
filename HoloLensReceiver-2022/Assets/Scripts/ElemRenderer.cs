using UnityEngine;
using Unity.WebRTC;
using System;
using System.Collections.Generic;

public class ElemRenderer : MonoBehaviour
{
    private Mesh mesh;
    private const int maxChunkSize = 1000;

    private bool hasChanged = false;
    private int nVertices = 0;
    private int nTriangles = 0;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private WebRTCManager webRTCManager;

    void Start()
    {
        webRTCManager = GameObject.FindObjectOfType<WebRTCManager>();

        if (webRTCManager == null)
        {
            Debug.LogError("WebRTCManager is not assigned!");
            return;
        }

        // Register to listen for mesh data from WebRTCManager
        webRTCManager.OnMeshDataReceived += TriggerMeshUpdate;

        mesh = new Mesh();
    }

    void Update()
    {
        // Optionally handle continuous updates here if needed
        if (hasChanged)
        {
            UpdateMesh();
            hasChanged = false;
        }
    }

    public void TriggerMeshUpdate(int nVertices, int nTriangles, Vector3[] newVertices, List<int> newTriangles)
    {
        this.nVertices = nVertices;
        this.nTriangles = nTriangles;
        this.vertices.Clear();
        this.vertices.AddRange(newVertices);

        this.triangles.Clear();
        this.triangles.AddRange(newTriangles);

        hasChanged = true;
    }

    public void UpdateMesh()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }

        mesh = new Mesh();
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < nVertices; i++)
        {
            newVertices.Add(vertices[i]);
        }

        for (int i = 0; i < nTriangles; i++)
        {
            newTriangles.Add(triangles[i]);
        }

        mesh.SetVertices(newVertices);
        mesh.SetTriangles(newTriangles, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnDestroy()
    {
        // Unsubscribe from WebRTCManager event
        if (webRTCManager != null)
        {
            webRTCManager.OnMeshDataReceived -= TriggerMeshUpdate;
        }
    }
}
