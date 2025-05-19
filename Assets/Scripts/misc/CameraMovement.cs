using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float verticalSpeed = 5f;
    public float sprintMultiplier = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isPaused = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the screen
    }

    void Update()
    {
        // Toggle pause state when Escape is pressed
        if (Input.GetKeyDown(KeyCode.L))
        {
            isPaused = !isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // Only move and look around if not paused
        Move();
        if (!isPaused)
        {
            LookAround();
        }
    }

    void Move()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        float moveX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        float moveY = 0f;
        if (Input.GetKey(KeyCode.LeftControl)) moveY -= verticalSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space)) moveY += verticalSpeed * Time.deltaTime;
        Vector3 move = transform.right * moveX + transform.up * moveY + transform.forward * moveZ;
        transform.position += move;
    }

    void LookAround()
    {
        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);
        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }
}