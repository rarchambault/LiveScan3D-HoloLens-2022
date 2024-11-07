using UnityEngine;
using System;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class PointCloudReceiver : MonoBehaviour
{
    TcpClient socket;
    public int port = 48002;

    PointCloudRenderer pointCloudRenderer;
    bool bReadyForNextFrame = true;
    bool bConnected = false;

    // Queue to hold received frames until they can be processed on the main thread
    ConcurrentQueue<(float[] vertices, byte[] colors)> frameQueue = new ConcurrentQueue<(float[], byte[])>();

    void Start()
    {
        pointCloudRenderer = GetComponent<PointCloudRenderer>();
    }

    void Update()
    {
        if (!bConnected)
            return;

        if (bReadyForNextFrame)
        {
            Debug.Log("Requesting frame");
            RequestFrameAsync();
            bReadyForNextFrame = false;
        }

        // Check if there is frame data in the queue, and render it on the main thread
        if (frameQueue.TryDequeue(out var frameData))
        {
            pointCloudRenderer.Render(frameData.vertices, frameData.colors);
            bReadyForNextFrame = true;
            Debug.Log("Frame rendered on main thread");
        }
    }

    public async void Connect(string IP)
    {
        Debug.Log("Attempting to connect to IP address " + IP + " on port " + port);

        socket = new TcpClient();
        try
        {
            await socket.ConnectAsync(IP, port);
            bConnected = true;
            Debug.Log("Successfully connected to IP address " + IP + " on port " + port);
        }
        catch (Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
        }
    }

    async void RequestFrameAsync()
    {
        if (socket == null || !socket.Connected) return;

        try
        {
            byte[] byteToSend = new byte[1] { 0 };
            await socket.GetStream().WriteAsync(byteToSend, 0, byteToSend.Length);

            // Asynchronously receive the frame data after requesting
            await ReceiveFrameAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending frame request: " + e.Message);
        }
    }

    async Task<int> ReadIntAsync()
    {
        byte[] buffer = new byte[4];
        int bytesRead = 0;

        while (bytesRead < 4)
        {
            bytesRead += await socket.GetStream().ReadAsync(buffer, bytesRead, 4 - bytesRead);
        }

        return BitConverter.ToInt32(buffer, 0);
    }

    async Task ReceiveFrameAsync()
    {
        if (socket == null || !socket.Connected) return;

        try
        {
            int nPointsToRead = await ReadIntAsync();

            float[] lVertices = new float[3 * nPointsToRead];
            short[] lShortVertices = new short[3 * nPointsToRead];
            byte[] lColors = new byte[3 * nPointsToRead];

            int nBytesToRead = sizeof(short) * 3 * nPointsToRead;
            byte[] vertexBuffer = new byte[nBytesToRead];

            // Read vertex data asynchronously
            int bytesRead = 0;
            while (bytesRead < nBytesToRead)
            {
                bytesRead += await socket.GetStream().ReadAsync(vertexBuffer, bytesRead, Math.Min(nBytesToRead - bytesRead, 64000));
            }

            Buffer.BlockCopy(vertexBuffer, 0, lShortVertices, 0, nBytesToRead);

            for (int i = 0; i < lShortVertices.Length; i++)
                lVertices[i] = lShortVertices[i] / 1000.0f;

            // Read color data asynchronously
            nBytesToRead = sizeof(byte) * 3 * nPointsToRead;
            byte[] colorBuffer = new byte[nBytesToRead];
            bytesRead = 0;

            while (bytesRead < nBytesToRead)
            {
                bytesRead += await socket.GetStream().ReadAsync(colorBuffer, bytesRead, Math.Min(nBytesToRead - bytesRead, 64000));
            }

            Buffer.BlockCopy(colorBuffer, 0, lColors, 0, nBytesToRead);

            // Enqueue the frame data to be processed on the main thread
            frameQueue.Enqueue((lVertices, lColors));
            Debug.Log("Frame data enqueued for main thread processing");
        }
        catch (Exception e)
        {
            Debug.LogError("Error receiving frame: " + e.Message);
        }
    }
}
