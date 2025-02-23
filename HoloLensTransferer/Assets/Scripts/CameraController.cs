using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 10.0f; // Speed of movement
    public float sensitivity = 1.0f; // Sensitivity of mouse movement

    private float rotationX = 0.0f; // Rotation around X (up/down)
    private float rotationY = 0.0f; // Rotation around Y (left/right)

    void Update()
    {
        // Get input from keyboard for movement
        float horizontal = Input.GetAxis("Horizontal"); // Left/Right (A/D)
        float vertical = Input.GetAxis("Vertical");     // Forward/Backward (W/S)

        // Move the camera forward/backward and left/right
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Optional: Move the camera up/down with Q/E keys
        if (Input.GetKey(KeyCode.Q)) // Move up
        {
            transform.Translate(Vector3.up * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E)) // Move down
        {
            transform.Translate(Vector3.down * speed * Time.deltaTime);
        }

        // Mouse-based camera rotation (rotation around X and Y axes)
        // Get the mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Adjust rotation based on mouse movement and sensitivity
        rotationX -= mouseY * sensitivity; // Up/Down rotation (X axis)
        rotationY += mouseX * sensitivity; // Left/Right rotation (Y axis)

        // Clamp rotation to avoid camera flipping
        rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f);

        // Apply the rotation to the camera
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
    }
}
