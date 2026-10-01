using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraOrtho : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;     // Speed of keyboard movement
    public float dragSpeed = 0.05f;   // Speed of mouse drag movement
    public float zoomSpeed = 0.1f;   // Speed of mouse zoom

    private Vector3 lastMousePosition;
    private bool isDragging = false;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        // Enable input callbacks (optional if using Input Actions asset)
        Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleMouseDrag();
        HandleMouseZoom();
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }

    void HandleKeyboardInput()
    {
        Vector2 move = Vector2.zero;

        if (Keyboard.current.leftArrowKey.isPressed) move.x -= 1;
        if (Keyboard.current.rightArrowKey.isPressed) move.x += 1;
        if (Keyboard.current.upArrowKey.isPressed) move.y += 1;
        if (Keyboard.current.downArrowKey.isPressed) move.y -= 1;

        Vector3 moveVec = new Vector3(move.x, move.y, 0f) * moveSpeed * Time.deltaTime;
        transform.position += moveVec;
    }

    void HandleMouseDrag()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
            isDragging = true;
        }
        else if (Mouse.current.rightButton.isPressed && isDragging)
        {
            Vector3 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            float deltaX = -delta.x * dragSpeed;
            float deltaY = -delta.y * dragSpeed;

            transform.position += new Vector3(deltaX, deltaY, 0f);
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }

    void HandleMouseZoom()
    {
        float deltaZ = Mouse.current.scroll.value.y * zoomSpeed;

        transform.position += new Vector3(0.0f, 0.0f, deltaZ);
    }

    void OnTextInput(char character)
    {
        // Optional: handle text input if needed
    }
}
