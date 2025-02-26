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

    private const float maxImageSize = 0.5f;
    private const float pixelToMeter = 0.26f / 1000f; // Convert pixels to meters
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

        if (Object.HasStateAuthority) // Only the host should receive images
        {
            isRunning = true;
            listenerThread = new Thread(ListenForImages);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
    }

    void ListenForImages()
    {
        try
        {
            listener = new TcpListener(IPAddress.Parse(serverIP), port);
            listener.Start();
            Debug.Log("Listening for images on " + serverIP + ":" + port);

            while (isRunning)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // Read image dimensions first
                    int height = reader.ReadInt32();
                    int width = reader.ReadInt32();

                    // Read image size
                    int imageSize = reader.ReadInt32();
                    byte[] imageData = reader.ReadBytes(imageSize);

                    if (imageData.Length > 0)
                    {
                        // Store data safely to be processed in the main thread
                        lock (lockObject)
                        {
                            receivedImageData = imageData;
                            receivedImageWidth = width;
                            receivedImageHeight = height;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("TCP Listener Error: " + e.Message);
        }
    }

    void Update()
    {
        // Process image only on the main thread
        if (receivedImageData != null && !isProcessingImage)
        {
            isProcessingImage = true;

            if (Object.HasStateAuthority)
            {
                lock (lockObject) // Ensure thread safety
                {
                    SendImageData(receivedImageData, receivedImageWidth, receivedImageHeight);
                    receivedImageData = null;
                }
            }
        }

        // Hide renderer if no new image has been received in the timeout period
        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + " seconds, hiding display");
        }
    }

    private void SendImageData(byte[] imageData, int width, int height)
    {
        Debug.Log("Sending image data via PhotonFusionManager...");

        // Send the image using your PhotonFusionManager's SendDocument function
        photonFusionManager.SendDocument(imageData);

        // Update networked properties for width and height (ensuring they are synchronized **after** data transmission)
        receivedImageWidth = width;
        receivedImageHeight = height;

        Debug.Log($"Updated network properties: Width={receivedImageWidth}, Height={receivedImageHeight}");
    }

    public override void Render()
    {
        // Check that the image was received here and fully sent through Photon
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

    private void AdjustRendererScale(int imageWidth, int imageHeight)
    {
        float realWidth = imageWidth * pixelToMeter;
        float realHeight = imageHeight * pixelToMeter;

        // Calculate scale factor to fit within maxSize while keeping aspect ratio
        float scaleFactor = Mathf.Min(maxImageSize / realWidth, maxImageSize / realHeight, 1.0f);

        Vector3 newScale = targetRenderer.transform.localScale;
        newScale.x = realWidth * scaleFactor;  // Width
        newScale.z = realHeight * scaleFactor; // Height (assuming Z is height)

        targetRenderer.transform.localScale = newScale;

        Debug.Log($"Adjusted Renderer Scale to: {newScale.x}m x {newScale.z}m (Aspect Ratio: {(float)imageWidth / imageHeight})");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
