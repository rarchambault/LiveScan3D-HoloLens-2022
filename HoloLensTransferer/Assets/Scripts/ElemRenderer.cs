using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Fusion.NetworkObjectBaker.TransformPath;

public class ElemRenderer : MonoBehaviour
{
    private Mesh mesh;
    private WebRTCManager webRTCManager;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        webRTCManager = GameObject.FindObjectOfType<WebRTCManager>();

        if (webRTCManager == null)
        {
            Debug.LogError("WebRTCManager is not assigned!");
            return;
        }
    }

    void Update()
    {
        if (webRTCManager.HasNewPointCloud())
        {
            Vector3[] vertices;
            Color[] colors;
            (vertices, colors) = webRTCManager.GetReceivedPointCloud();

            int[] indices = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                indices[i] = i;  // Fill the indices array with the index values
            }

            if (mesh != null)
                Destroy(mesh);

            mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}