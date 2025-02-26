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
    public string serverIP = "127.0.0.1";
    public int port = 48004;
    public Renderer targetRenderer;
    public PhotonFusionManager photonFusionManager;

    private TcpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private float lastImageTime = 0f;
    private const float IMAGE_TIMEOUT = 10f;
    [Networked] private int receivedImageWidth { get; set; }
    [Networked] private int receivedImageHeight { get; set; }

    private const float maxImageSize = 0.1f;
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
    }

    void Update()
    {
        // Hide renderer if no new image has been received in the timeout period
        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + " seconds, hiding display");
        }
    }

    public override void Render()
    {
        // Only apply the texture when width and height are updated (indicating image data is received)
        if (photonFusionManager.HasNewDocument())
        {
            Debug.Log("Found new image data on PhotonFusionManager, starting to display it");
            ApplyTexture(receivedImageWidth, receivedImageHeight);
        }
    }

    private void ApplyTexture(int width, int height)
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
            AdjustRendererScale(width, height);
            Debug.Log("Applied image to plane");
        }
        else
        {
            Debug.LogError("Failed to load image data into texture");
        }
    }

    private void AdjustRendererScale(int imageWidth, int imageHeight)
    {
        float realWidth = imageWidth * pixelToMeter;
        float realHeight = imageHeight * pixelToMeter;

        // Calculate scale factor to fit within maxSize while keeping aspect ratio
        float scaleFactor = Mathf.Min(maxImageSize / realWidth, maxImageSize / realHeight, 1.0f);

        Vector3 newScale = targetRenderer.transform.localScale;
        newScale.x = realWidth * scaleFactor;  // Width
        newScale.y = realHeight * scaleFactor; // Height

        targetRenderer.transform.localScale = newScale;

        Debug.Log($"Adjusted Renderer Scale to: {newScale.x}m x {newScale.y}m (Aspect Ratio: {(float)imageWidth / imageHeight})");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
