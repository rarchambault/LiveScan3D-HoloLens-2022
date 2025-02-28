using Fusion;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class DocumentPictureReceiver : NetworkBehaviour
{
    public Renderer targetRenderer;
    public float maxImageSize = 3.0f;
    public float minImageSize = 1.0f;

    private PhotonFusionManager photonFusionManager;
    private TcpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private float lastImageTime = 0f;
    private const float IMAGE_TIMEOUT = 120f;
    [Networked] private int receivedImageWidth { get; set; }
    [Networked] private int receivedImageHeight { get; set; }

    private float xScaleUnitWidth;
    private float zScaleUnitHeight;
    private const float pixelToMeter = 0.26f / 1000f; // Convert pixels to meters

    private readonly object lockObject = new object(); // Ensure thread safety

    public override void Spawned()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer is not assigned!");
            return;
        }

        targetRenderer.enabled = false; // Hide initially

        photonFusionManager = GameObject.FindObjectOfType<PhotonFusionManager>();

        if (photonFusionManager == null)
        {
            Debug.LogError("PhotonFusionManager is not assigned!");
            return;
        }

        // Get the current world-space width and height of the plane
        float currentWorldWidth = targetRenderer.localBounds.size.x;
        float currentWorldHeight = targetRenderer.localBounds.size.z;

        // Get the current local scale
        Vector3 localScale = targetRenderer.transform.localScale;

        // Compute the unit local x-scale and y-scale
        xScaleUnitWidth = (1.0f * localScale.x) / currentWorldWidth;
        zScaleUnitHeight = (1.0f * localScale.z) / currentWorldHeight;
    }

    public override void Render()
    {
        // Only apply the texture when width and height are updated (indicating image data is received)
        if (photonFusionManager.HasNewDocument())
        {
            Debug.Log("Found new image data on PhotonFusionManager, starting to display it");
            ApplyTexture();
        }

        // Hide renderer if no new image has been received in the timeout period
        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + " seconds, hiding display");
        }
    }

    private void ApplyTexture()
    {
        byte[] imageData = photonFusionManager.GetReceivedDocument(); // Get image data from PhotonFusionManager
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("Failed to retrieve image data from PhotonFusionManager.");
            return;
        }

        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            texture.Apply();
            targetRenderer.material.mainTexture = texture;
            targetRenderer.enabled = true;
            lastImageTime = Time.time;

            // Scale the renderer plane to match the aspect ratio
            AdjustRendererScale();
            Debug.Log("Applied image to plane");
        }
        else
        {
            Debug.LogError("Failed to load image data into texture");
        }
    }

    private void AdjustRendererScale()
    {
        if (receivedImageWidth <= 0 || receivedImageHeight <= 0)
        {
            Debug.Log("Received image width or height was null");
            return;
        }

        float aspectRatio = (float)receivedImageWidth / (float)receivedImageHeight;
        float realWidth = (float)receivedImageWidth * pixelToMeter;
        float realHeight = (float)receivedImageHeight * pixelToMeter;

        realWidth = Mathf.Clamp(realWidth, minImageSize, maxImageSize);
        realHeight = realWidth / aspectRatio;

        realHeight = Mathf.Clamp(realHeight, minImageSize, maxImageSize);
        realWidth = realHeight * aspectRatio;

        Vector3 newScale = targetRenderer.transform.localScale;
        newScale.x = realWidth * xScaleUnitWidth;  // Width
        newScale.z = realHeight * zScaleUnitHeight; // Height

        targetRenderer.transform.localScale = newScale;

        Debug.Log($"Adjusted Renderer Scale to: {newScale.x}m x {newScale.z}m (Aspect Ratio: {(float)receivedImageWidth / receivedImageHeight})");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
