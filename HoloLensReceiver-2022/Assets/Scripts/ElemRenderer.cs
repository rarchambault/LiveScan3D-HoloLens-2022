using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;

public class ElemRenderer : MonoBehaviour
{
    private Mesh mesh;
    public const int maxChunkSize = 1000;

    private bool hasChanged;
    private int nVertices;
    private int nTriangles;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
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
        vertices.Clear();
        vertices.AddRange(newVertices);

        triangles.Clear();
        triangles.AddRange(newTriangles);
        hasChanged = true;
    }

    private void UpdateMesh()
    {
        if (mesh != null)
            Destroy(mesh);

        mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
