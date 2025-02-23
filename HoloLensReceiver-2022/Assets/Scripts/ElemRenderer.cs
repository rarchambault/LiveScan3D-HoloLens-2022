using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElemRenderer : NetworkBehaviour
{
    Mesh mesh;
    public const int maxChunkSize = 1000;

    [Networked] private bool hasChanged { get; set; }
    [Networked] private int nVertices { get; set; }
    [Networked] private int nTriangles { get; set; }
    [Networked, Capacity(maxChunkSize)] private NetworkArray<Vector3> vertices { get; }
    [Networked, Capacity(maxChunkSize)] private NetworkArray<int> triangles { get; }

    private bool hasRenderedNewFrame = false;
    private float timeSinceLastRender = 0.0f;

    private void Awake()
    {
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLastRender += Time.deltaTime;

        if (hasRenderedNewFrame)
        {
            Debug.Log("CurrentFPS = " + 1 / timeSinceLastRender);
            timeSinceLastRender = 0.0f;
            hasRenderedNewFrame = false;
        }
    }

    public override void Render()
    {
        if (hasChanged)
        {
            UpdateMesh();
            hasRenderedNewFrame = true;

            if (!Object.HasStateAuthority)
            {
                hasChanged = false;
            }
        }
    }

    public void TriggerMeshUpdate(int nVertices, int nTriangles, List<Vector3> newVertices, List<int> newTriangles)
    {
        this.nVertices = nVertices;
        this.nTriangles = nTriangles;
        this.vertices.Clear();
        this.vertices.CopyFrom(newVertices, 0, nVertices);

        this.triangles.Clear();
        this.triangles.CopyFrom(newTriangles, 0, nTriangles);
        hasChanged = true;
    }

    //public void TriggerMeshUpdateOld(float[] arrVertices, byte[] arrColors, int nPointsToRender, int nPointsRendered)
    //{
    //    vertices.Clear();
    //    //vertices.CopyFrom(arrVertices, nPointsRendered * 3, nPointsToRender * 3);
    //    vertices.AddRange(arrVertices);

    //    colors.Clear();
    //    //colors.CopyFrom(arrColors, nPointsRendered * 3, nPointsToRender * 3);
    //    colors.AddRange(arrColors);
    //    this.nPointsToRender = nPointsToRender;
    //    this.nPointsRendered = nPointsRendered;
    //    hasChanged = true;
    //}

    public void UpdateMesh()
    {
        if (mesh != null)
            Destroy(mesh);

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
        //mesh.SetNormals(normals);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void UpdateMeshOld(NetworkArray<float> arrVertices, NetworkArray<byte> arrColors, int nPointsToRender, int nPointsRendered)
    {
        int nPoints;

        if (arrVertices.Length <= 0 || arrColors.Length <= 0)
            nPoints = 0;
        else
            //nPoints = System.Math.Min(nPointsToRender, (arrVertices.Length / 3) - nPointsRendered);
            nPoints = nPointsToRender;
        nPoints = System.Math.Min(nPoints, maxChunkSize);

        Vector3[] points = new Vector3[nPoints];
        int[] indices = new int[nPoints];
        Color[] colors = new Color[nPoints];

        for (int i = 0; i < nPoints; i++)
        {
            //int ptIdx = 3 * (nPointsRendered + i);
            int ptIdx = 3 * i;

            points[i] = new Vector3(arrVertices[ptIdx + 0], arrVertices[ptIdx + 1], -arrVertices[ptIdx + 2]);
            indices[i] = i;
            colors[i] = new Color((float)arrColors[ptIdx + 0] / 256.0f, (float)arrColors[ptIdx + 1] / 256.0f, (float)arrColors[ptIdx + 2] / 256.0f, 1.0f);
        }

        if (mesh != null)
            Destroy(mesh);
        mesh = new Mesh();
        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
