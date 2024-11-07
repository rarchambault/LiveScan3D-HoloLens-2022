using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 10.0f; // Speed of movement

    void Update()
    {
        // Get input from keyboard
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
    }
}
