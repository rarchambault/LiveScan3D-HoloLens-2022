using Fusion;
using GK;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

public class ElemRenderer : NetworkBehaviour
{
    Mesh mesh;
    public const int maxChunkSize = 1000;
    public Material particleMaterial;
    public float particleSize = 0.1f;

    [Networked] private bool hasChanged { get; set; }
    //[Networked, Capacity(maxChunkSize)] private NetworkArray<float> vertices { get; }
    //[Networked, Capacity(maxChunkSize)] private NetworkArray<byte> colors { get; }
    //[Networked] private int nPointsToRender { get; set; }
    //[Networked] private int nPointsRendered { get; set; }

    [Networked] private int nVertices { get; set; }
    [Networked] private int nColors { get; set; }
    [Networked, Capacity(maxChunkSize)] private NetworkArray<Vector3> vertices { get; }
    [Networked, Capacity(maxChunkSize)] private NetworkArray<Color> colors { get; }

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
    }

    public override void Spawned()
    {
        UpdateShaderProperties();
    }

    public override void Render()
    {
        if (hasChanged)
        {
            UpdateMesh();

            if (!Object.HasStateAuthority)
            {
                hasChanged = false;
            }
        }
    }

    void UpdateShaderProperties()
    {
        if (particleMaterial != null)
        {
            particleMaterial.SetFloat("_Size", particleSize);
        }
    }

    public void TriggerMeshUpdate(int nVertices, int nColors, List<Vector3> newVertices, List<Color> newColors)
    {
        this.nVertices = Mathf.Min(nVertices, maxChunkSize - 1);
        this.nColors = Mathf.Min(nColors, maxChunkSize - 1);
        this.vertices.Clear();
        this.vertices.CopyFrom(newVertices, 0, this.nVertices);
        //this.vertices.AddRange(newVertices);

        this.colors.Clear();
        this.colors.CopyFrom(newColors, 0, this.nColors);

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
        List<Color> newColors = new List<Color>();

        for (int i = 0; i < nVertices; i++)
        {
            newVertices.Add(vertices[i]);
        }

        for (int i = 0; i < nColors; i++)
        {
            newColors.Add(colors[i]);
        }

        mesh.vertices = newVertices.ToArray();
        mesh.colors = newColors.ToArray();
        mesh.SetIndices(Enumerable.Range(0, nVertices).ToList(), MeshTopology.Points, 0);

        //mesh.SetVertices(newVertices);
        //mesh.SetTriangles(newTriangles, 0);
        //mesh.SetNormals(normals);
        //mesh.RecalculateNormals();

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
