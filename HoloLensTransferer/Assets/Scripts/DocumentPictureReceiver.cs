using Fusion;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;

public class DocumentPictureReceiver : NetworkBehaviour
{
    public string serverIP = "127.0.0.1";
    public int port = 48004;
    public Renderer targetRenderer;
    public WebRTCManager webRTCManager;

    private TcpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private float lastImageTime = 0f;
    private const float IMAGE_TIMEOUT = 10f;

    private byte[] receivedImageData;
    private int newImageWidth;
    private int newImageHeight;
    [Networked] private int receivedImageWidth { get; set; }
    [Networked] private int receivedImageHeight { get; set; }

    private const float maxImageSize = 0.1f;
    private const float pixelToMeter = 0.26f / 1000f; // Convert pixels to meters
    private bool isProcessingImage = false;

    private readonly object lockObject = new object(); // Ensure thread safety

    void Start()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer is not assigned!");
            return;
        }

        targetRenderer.enabled = false; // Hide initially

        webRTCManager = GameObject.FindObjectOfType<WebRTCManager>();

        if (webRTCManager == null)
        {
            Debug.LogError("WebRTCManager is not assigned!");
            return;
        }

        isRunning = true;
        listenerThread = new Thread(ListenForImages);
        listenerThread.IsBackground = true;
        listenerThread.Start();
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
                    int height = reader.ReadInt32();
                    int width = reader.ReadInt32();
                    int imageSize = reader.ReadInt32();
                    byte[] imageData = reader.ReadBytes(imageSize);

                    if (imageData.Length > 0)
                    {
                        lock (lockObject)
                        {
                            receivedImageData = imageData;
                            newImageWidth = width;
                            newImageHeight = height;
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
        if (receivedImageData != null && !isProcessingImage)
        {
            isProcessingImage = true;
            lock (lockObject)
            {
                SendImageData(receivedImageData, newImageWidth, newImageHeight);
                receivedImageData = null;
            }
        }

        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + " seconds, hiding display");
        }

        if (isProcessingImage)
        {
            StartCoroutine(ApplyTexture(receivedImageWidth, receivedImageHeight));
            isProcessingImage = false;
        }
    }

    private void SendImageData(byte[] imageData, int width, int height)
    {
        Debug.Log("Sending image data via WebRTCManager...");

        webRTCManager.SendDocument(imageData);
        receivedImageWidth = width;
        receivedImageHeight = height;

        Debug.Log($"Updated properties: Width={receivedImageWidth}, Height={receivedImageHeight}");
    }

    IEnumerator ApplyTexture(int width, int height)
    {
        yield return null;

        byte[] imageData = webRTCManager.GetReceivedDocument();
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("Failed to retrieve image data from WebRTCManager.");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            texture.Apply();
            targetRenderer.material.mainTexture = texture;
            targetRenderer.enabled = true;
            lastImageTime = Time.time;

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

        float scaleFactor = Mathf.Min(maxImageSize / realWidth, maxImageSize / realHeight, 1.0f);

        Vector3 newScale = targetRenderer.transform.localScale;
        newScale.x = realWidth * scaleFactor;
        newScale.y = realHeight * scaleFactor;

        targetRenderer.transform.localScale = newScale;

        Debug.Log($"Adjusted Renderer Scale to: {newScale.x}m x {newScale.y}m (Aspect Ratio: {(float)imageWidth / imageHeight})");
    }

    private void OnDestroy()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
