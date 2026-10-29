using UnityEngine;

/// <summary>
/// RTS/tower-defense-style camera controller.
/// Attach this to your Main Camera (or an empty "CameraRig" parent with the camera as a child).
/// Controls:
///  - WASD / Arrow keys: pan
///  - Space / Left Ctrl: move up / down
///  - Mouse scroll wheel: zoom in/out
///  - Right mouse button + move mouse: free look (yaw + pitch)
///  - Move mouse to screen edge: pan (toggle with edgeScrollEnabled)
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 20f;
    public float edgeScrollSpeed = 15f;
    public bool edgeScrollEnabled = true;
    [Tooltip("How many pixels from the screen edge triggers edge scrolling")]
    public float edgeSize = 20f;

    [Header("Vertical Movement")]
    public float verticalSpeed = 15f;
    public bool useHeightLimits = true;
    public float minHeight = 5f;
    public float maxHeight = 60f;

    [Header("Zoom")]
    public float zoomSpeed = 15f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    [Header("Look")]
    [Tooltip("Hold right mouse button and move the mouse to look around")]
    public float lookSensitivity = 3f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Map Boundaries")]
    public bool useBoundaries = true;
    public Vector2 xLimits = new Vector2(-50f, 50f);
    public Vector2 zLimits = new Vector2(-50f, 50f);

    private Camera cam;
    private float yaw;
    private float pitch;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = GetComponentInChildren<Camera>();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandlePan();
        HandleEdgeScroll();
        HandleVerticalMove();
        HandleZoom();
        HandleLook();

        if (useBoundaries)
            ClampPosition();
    }

    private void HandlePan()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move -= transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move += transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move -= transform.right;

        // Keep movement flat (ignore camera tilt on Y)
        move.y = 0f;
        move.Normalize();

        transform.position += move * panSpeed * Time.deltaTime;
    }

    private void HandleEdgeScroll()
    {
        if (!edgeScrollEnabled) return;

        Vector3 move = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;

        if (mousePos.x <= edgeSize)
            move -= transform.right;
        else if (mousePos.x >= Screen.width - edgeSize)
            move += transform.right;

        if (mousePos.y <= edgeSize)
            move -= transform.forward;
        else if (mousePos.y >= Screen.height - edgeSize)
            move += transform.forward;

        move.y = 0f;
        move.Normalize();

        transform.position += move * edgeScrollSpeed * Time.deltaTime;
    }

    private void HandleVerticalMove()
    {
        float vertical = 0f;

        if (Input.GetKey(KeyCode.Space))
            vertical += 1f;
        if (Input.GetKey(KeyCode.LeftControl))
            vertical -= 1f;

        if (Mathf.Abs(vertical) < 0.001f) return;

        Vector3 pos = transform.position;
        pos.y += vertical * verticalSpeed * Time.deltaTime;

        if (useHeightLimits)
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);

        transform.position = pos;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f || cam == null) return;

        if (cam.orthographic)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            // Move the camera forward/back along its own view direction for perspective zoom
            transform.position += transform.forward * scroll * zoomSpeed;
        }
    }

    private void HandleLook()
    {
        // Hold right mouse button to free-look. No cursor lock, so it still works
        // fine alongside UI clicks for placing towers etc.
        if (!Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, xLimits.x, xLimits.y);
        pos.z = Mathf.Clamp(pos.z, zLimits.x, zLimits.y);
        transform.position = pos;
    }
}
