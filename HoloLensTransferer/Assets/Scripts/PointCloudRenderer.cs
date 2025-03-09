using Unity.WebRTC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointCloudRenderer : MonoBehaviour
{
    public int maxChunkSize = 65535;
    public float pointSize = 0.005f;
    public GameObject pointCloudElem;
    public Material pointCloudMaterial;
    public int maxNumElems = 1;

    public WebRTCManager webRTCManager;
    private List<GameObject> pointCloudObjects = new List<GameObject>();

    void Start()
    {
        UpdatePointSize();

        if (webRTCManager == null)
        {
            webRTCManager = FindObjectOfType<WebRTCManager>();
            if (webRTCManager == null)
            {
                Debug.LogError("WebRTCManager not found!");
                return;
            }
        }
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
        if (arrVertices == null || arrColors == null) return;

        int nPoints = arrVertices.Length / 3;
        int nChunks = Mathf.Min(1 + nPoints / maxChunkSize, maxNumElems);

        while (pointCloudObjects.Count < nChunks)
            AddElem();
        while (pointCloudObjects.Count > nChunks)
            RemoveElem();

        int offset = 0;
        for (int i = 0; i < nChunks; i++)
        {
            int nPointsToRender = Mathf.Min(maxChunkSize, nPoints - offset);
            Vector3[] points = new Vector3[nPointsToRender];
            Color[] colors = new Color[nPointsToRender];

            for (int j = 0; j < nPointsToRender; j++)
            {
                int ptIdx = 3 * (offset + j);
                points[j] = new Vector3(arrVertices[ptIdx], arrVertices[ptIdx + 1], -arrVertices[ptIdx + 2]);
                colors[j] = new Color(arrColors[ptIdx] / 256f, arrColors[ptIdx + 1] / 256f, arrColors[ptIdx + 2] / 256f, 1.0f);
            }

            Mesh mesh = new Mesh { vertices = points, colors = colors };
            mesh.SetIndices(Enumerable.Range(0, points.Length).ToArray(), MeshTopology.Points, 0);

            pointCloudObjects[i].GetComponent<MeshFilter>().mesh = mesh;
            offset += nPointsToRender;
        }
    }

    void AddElem()
    {
        GameObject newElem = Instantiate(pointCloudElem, transform);
        newElem.transform.localPosition = Vector3.zero;
        newElem.transform.localRotation = Quaternion.identity;
        pointCloudObjects.Add(newElem);
    }

    void RemoveElem()
    {
        if (pointCloudObjects.Count > 0)
        {
            Destroy(pointCloudObjects[0]);
            pointCloudObjects.RemoveAt(0);
        }
    }
}
