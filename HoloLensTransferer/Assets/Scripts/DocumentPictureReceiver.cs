using Fusion;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class DocumentPictureReceiver : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int port = 48004;
    public Renderer targetRenderer;

    private TcpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private byte[] receivedImageData;
    private float lastImageTime = 0f;
    private const float IMAGE_TIMEOUT = 10f;
    private bool newImageReceived = false; // Flag to update time on main thread

    void Start()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer is not assigned!");
            return;
        }

        targetRenderer.enabled = false; // Hide initially

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
                    int imageSize = reader.ReadInt32();
                    byte[] imageData = reader.ReadBytes(imageSize);

                    // Store received data and request main thread to process it
                    lock (this)
                    {
                        receivedImageData = imageData;
                        newImageReceived = true; // Flag to update time on main thread
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
        if (newImageReceived)
        {
            lastImageTime = Time.time;
            newImageReceived = false;
        }

        if (receivedImageData != null)
        {
            StartCoroutine(ApplyTexture(receivedImageData));
            receivedImageData = null;
            Debug.Log("Received new image");
        }

        // Hide renderer if no new image has been received in the timeout period
        if (Time.time - lastImageTime > IMAGE_TIMEOUT && targetRenderer.enabled)
        {
            targetRenderer.enabled = false;
            Debug.Log("No new image in over " + IMAGE_TIMEOUT + "seconds, stopping display");
        }
    }

    IEnumerator ApplyTexture(byte[] imageData)
    {
        yield return null; // Ensure it runs on the main thread

        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            texture.Apply();
            targetRenderer.material.mainTexture = texture;
            targetRenderer.enabled = true; // Show renderer when a new image arrives
        }
        else
        {
            Debug.LogError("Failed to load image data into texture");
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        listener?.Stop();
        listenerThread?.Abort();
    }
}
