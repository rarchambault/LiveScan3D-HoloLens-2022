using Fusion;
using GK;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointCloudRenderer : MonoBehaviour
{
    public int maxChunkSize = 65535; // If you want to whole point cloud
    //public int maxChunkSize = 1361; // If you only want a small portion of the points
    public float pointSize = 0.005f;
    public GameObject pointCloudElem;
    public Material pointCloudMaterial;

    public int maxNumElems = 1;
    //public int maxNumElems = 4;

    //List<GameObject> elems;

    public PhotonFusionManager photonFusionManager;

    void Start()
    {
        //elems = new List<GameObject>();
        UpdatePointSize();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            UpdatePointSize();
            transform.hasChanged = false;
        }
    }

    void UpdatePointSize()
    {
        pointCloudMaterial.SetFloat("_PointSize", pointSize * transform.localScale.x);
    }

    public void Render(float[] arrVertices, byte[] arrColors)
    {
        int nPoints, nChunks;
        if (arrVertices == null || arrColors == null)
        {
            nPoints = 0;
            nChunks = 0;
        }
        else
        {
            nPoints = arrVertices.Length / 3;
            nChunks = 1 + nPoints / maxChunkSize;
        }

        nChunks = Mathf.Min(nChunks, maxNumElems);

        if (photonFusionManager.networkObjects.Count < nChunks)
            AddElems(nChunks - photonFusionManager.networkObjects.Count);
        if (photonFusionManager.networkObjects.Count > nChunks)
            RemoveElems(photonFusionManager.networkObjects.Count - nChunks);

        int offset = 0;
        for (int i = 0; i < nChunks; i++)
        {
            int nPointsToRender = System.Math.Min(maxChunkSize, nPoints - offset);

            Vector3[] points = new Vector3[nPoints];
            int[] indices = new int[nPoints];
            Color[] colors = new Color[nPoints];

            for (int j = 0; j < nPoints; j++)
            {
                int ptIdx = 3 * j;

                points[j] = new Vector3(arrVertices[ptIdx + 0], arrVertices[ptIdx + 1], -arrVertices[ptIdx + 2]);
                indices[j] = j;
                colors[j] = new Color((float)arrColors[ptIdx + 0] / 256.0f, (float)arrColors[ptIdx + 1] / 256.0f, (float)arrColors[ptIdx + 2] / 256.0f, 1.0f);
            }

            // Convert the point cloud into a mesh
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            ConvexHullCalculator convexHull = new ConvexHullCalculator();

            try
            {
                convexHull.GenerateHull(points.ToList(), false, ref vertices, ref triangles, ref normals);

                Debug.Log("Original points: " + points.Length + ", Vertices: " + vertices.Count + ", Triangles: " + triangles.Count + ", Normals: " + normals.Count);

                ElemRenderer renderer = photonFusionManager.networkObjects[i].GetComponent<ElemRenderer>();
                renderer.TriggerMeshUpdate(vertices.Count, triangles.Count, vertices, triangles);

                offset += nPointsToRender;
            }
            catch
            {
                Debug.Log("A problem occurred with QuickHull");
            }
        }
    }

    void AddElems(int nElems)
    {
        for (int i = 0; i < nElems; i++)
        {
            //GameObject newElem = GameObject.Instantiate(pointCloudElem);
            //newElem.transform.parent = transform;
            //newElem.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            //newElem.transform.localRotation = Quaternion.identity;
            //newElem.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            //elems.Add(newElem);

            photonFusionManager.SpawnNetworkObject(pointCloudElem, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
        }
    }

    void RemoveElems(int nElems)
    {
        //for (int i = 0; i < nElems; i++)
        //{
        //    Destroy(elems[0]);
        //    elems.Remove(elems[0]);
        //}

        photonFusionManager.DestroyNetworkObjects(nElems);
    }
}
