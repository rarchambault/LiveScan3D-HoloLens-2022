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

    private byte[] receivedImageData;
    [Networked] private int receivedImageWidth { get; set; }
    [Networked] private int receivedImageHeight { get; set; }

    private bool isProcessingImage = false;

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
        // Process image only on the main thread
        if (receivedImageData != null && !isProcessingImage)
        {
            isProcessingImage = true;
        }

        // Hide renderer if no new image has been received in the timeout period
        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + " seconds, hiding display");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only apply the texture when width and height are updated (indicating image data is received)
        if (isProcessingImage && photonFusionManager.HasNewDocument())
        {
            StartCoroutine(ApplyTexture(receivedImageWidth, receivedImageHeight));
            isProcessingImage = false;
        }
    }

    IEnumerator ApplyTexture(int width, int height)
    {
        yield return null; // Ensure it runs on the main thread

        byte[] imageData = photonFusionManager.GetReceivedDocument(); // Get image data from PhotonFusionManager
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("Failed to retrieve image data from PhotonFusionManager.");
            yield break;
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
        }
        else
        {
            Debug.LogError("Failed to load image data into texture");
        }
    }

    private void AdjustRendererScale(int width, int height)
    {
        float aspectRatio = (float)width / height;
        Vector3 scale = targetRenderer.transform.localScale;

        // Adjust width and height while keeping depth unchanged
        scale.x = aspectRatio;  // Width
        scale.y = 1;            // Depth remains 1
        scale.z = 1;            // Height

        targetRenderer.transform.localScale = scale;

        Debug.Log($"Adjusted Renderer Scale to: {scale.x}, {scale.y}, {scale.z} (Aspect Ratio: {aspectRatio})");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
