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

    public WebRTCManager webRTCManager;

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

        if (webRTCManager.networkObjects.Count < nChunks)
            AddElems(nChunks - webRTCManager.networkObjects.Count);
        if (webRTCManager.networkObjects.Count > nChunks)
            RemoveElems(webRTCManager.networkObjects.Count - nChunks);

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

            // Thin the point cloud
            //List<Vector3> newPoints = new List<Vector3>();
            //List<Color> newColors = new List<Color>();

            //float voxelSize = 0.02f;

            //VoxelDownsample(points.ToList(), colors.ToList(), ref newPoints, ref newColors, voxelSize);
            //Debug.Log("Original points: " + points.Length + ", new points: " + newPoints.Count);

            //ElemRenderer renderer = webRTCManager.networkObjects[i].GetComponent<ElemRenderer>();
            //renderer.TriggerMeshUpdate(points.Length, colors.Length, points, colors);

            webRTCManager.SendPointCloud(points, colors);

            offset += nPointsToRender;
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

            webRTCManager.SpawnNetworkObject(pointCloudElem, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
        }
    }

    void RemoveElems(int nElems)
    {
        //for (int i = 0; i < nElems; i++)
        //{
        //    Destroy(elems[0]);
        //    elems.Remove(elems[0]);
        //}

        webRTCManager.DestroyNetworkObjects(nElems);
    }

    public void VoxelDownsample(List<Vector3> originalPoints, List<Color> originalColors, ref List<Vector3> newPoints, ref List<Color> newColors, float voxelSize)
    {
        Dictionary<Vector3Int, List<(Vector3, Color)>> voxelMap = new Dictionary<Vector3Int, List<(Vector3, Color)>>();

        for (int i = 0; i < originalPoints.Count; i++)
        {
            Vector3 point = originalPoints[i];
            Color color = originalColors[i];

            Vector3Int voxelKey = new Vector3Int(
                Mathf.FloorToInt(point.x / voxelSize),
                Mathf.FloorToInt(point.y / voxelSize),
                Mathf.FloorToInt(point.z / voxelSize)
            );

            if (!voxelMap.ContainsKey(voxelKey))
                voxelMap[voxelKey] = new List<(Vector3, Color)>();

            voxelMap[voxelKey].Add((point, color));
        }

        // Compute the average position and color for each voxel
        foreach (var voxel in voxelMap)
        {
            Vector3 avgPosition = Vector3.zero;
            Color avgColor = Color.black;
            int count = voxel.Value.Count;

            foreach (var (pos, col) in voxel.Value)
            {
                avgPosition += pos;
                avgColor += col; // Convert Color to Vector4 for addition
            }

            avgPosition /= count;
            avgColor /= count; // Averaging the color

            newPoints.Add(avgPosition);
            newColors.Add(avgColor);
        }
    }
}