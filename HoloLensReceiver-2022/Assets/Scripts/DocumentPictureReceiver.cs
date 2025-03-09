using UnityEngine;
using Unity.WebRTC;
using System;

public class DocumentPictureReceiver : MonoBehaviour
{
    public Renderer targetRenderer;
    public WebRTCManager webRTCManager;

    private float lastImageTime = 0f;
    private const float IMAGE_TIMEOUT = 10f;

    [Networked] private int receivedImageWidth { get; set; }
    [Networked] private int receivedImageHeight { get; set; }

    private const float maxImageSize = 0.1f;
    private const float pixelToMeter = 0.26f / 1000f; // Convert pixels to meters

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

        // Register to listen for messages from WebRTC data channel
        webRTCManager.OnDocumentImageReceived += OnDocumentImageReceived;
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

    public void Render()
    {
        // Only apply the texture when width and height are updated (indicating image data is received)
        if (webRTCManager.HasNewDocument())
        {
            Debug.Log("Found new image data on WebRTCManager, starting to display it");
            ApplyTexture(receivedImageWidth, receivedImageHeight);
        }
    }

    private void ApplyTexture(int width, int height)
    {
        byte[] imageData = webRTCManager.GetReceivedDocument(); // Get image data from WebRTCManager
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("Failed to retrieve image data from WebRTCManager.");
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

    private void OnDocumentImageReceived(byte[] imageData, int width, int height)
    {
        // Called when WebRTCManager receives image data
        receivedImageWidth = width;
        receivedImageHeight = height;

        // Save the data for rendering
        webRTCManager.SetReceivedDocument(imageData);
    }

    void OnDestroy()
    {
        // Unsubscribe from WebRTCManager event
        if (webRTCManager != null)
        {
            webRTCManager.OnDocumentImageReceived -= OnDocumentImageReceived;
        }
    }
}
