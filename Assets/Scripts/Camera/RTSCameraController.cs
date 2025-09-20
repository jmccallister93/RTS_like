using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    public static RTSCameraController instance;

    // If we want to select an item to follow, inside the item script add:
    // public void OnMouseDown(){
    //   CameraController.instance.followTransform = transform;
    // }

    [Header("General")]
    [SerializeField] Transform cameraTransform;
    public Transform followTransform;
    Vector3 newPosition;
    Vector3 dragStartPosition;
    Vector3 dragCurrentPosition;

    [Header("Optional Functionality")]
    [SerializeField] bool moveWithKeyboad;
    [SerializeField] bool moveWithEdgeScrolling;
    [SerializeField] bool moveWithMouseDrag;
    [SerializeField] bool zoomEnabled = true;
    [SerializeField] bool rotationEnabled = true;

    [Header("Keyboard Movement")]
    [SerializeField] float fastSpeed = 0.5f;
    [SerializeField] float normalSpeed = 0.05f;
   
    float movementSpeed;

    [Header("Zoom Settings")]
    [SerializeField] float mouseWheelZoomSpeed = 2f;  // Per scroll step
    [SerializeField] float zoomSmoothSpeed = 5f;      // How fast it smooths to target zoom
    [SerializeField] float minZoomY = 5f;  // Minimum height/zoom level
    [SerializeField] float maxZoomY = 100f; // Maximum height/zoom level
    [SerializeField] bool useMouseWheelZoom = true;

    private float targetZoomY;      // Target zoom level to smoothly move towards
    private float targetFollowDistance;  // Target distance when following

    [Header("Rotation Settings")]
    [SerializeField] float rotationSpeed = 50f; // Degrees per second
    Vector3 newRotation;

    [Header("Edge Scrolling Movement")]
    [SerializeField] float edgeSize = 50f;
    
    public Texture2D cursorArrowUp;
    public Texture2D cursorArrowDown;
    public Texture2D cursorArrowLeft;
    public Texture2D cursorArrowRight;

    CursorArrow currentCursor = CursorArrow.DEFAULT;
    enum CursorArrow
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        DEFAULT
    }

    private Mouse mouse;
    private Keyboard keyboard;

    private void Start()
    {
        instance = this;
        mouse = Mouse.current;
        keyboard = Keyboard.current;

        newPosition = transform.position;
        newRotation = transform.eulerAngles;
        targetZoomY = transform.position.y;  // Initialize target zoom to current height
        targetFollowDistance = 10f;  // Default follow distance

        movementSpeed = normalSpeed;
    }

    private void Update()
    {
        // Allow Camera to follow Target
        if (followTransform != null)
        {
            // Initialize target follow distance if we just started following
            if (targetFollowDistance == 0f || targetFollowDistance == 10f)
            {
                targetFollowDistance = Vector3.Distance(transform.position, followTransform.position);
                targetFollowDistance = Mathf.Clamp(targetFollowDistance, minZoomY, maxZoomY);
            }

            transform.position = followTransform.position;
        }
        // Let us control Camera
        else
        {
            HandleCameraMovement();
        }

        // Handle zoom regardless of follow state
        if (zoomEnabled)
        {
            HandleZoomInput();
        }

        // Handle rotation regardless of follow state
        if (rotationEnabled)
        {
            HandleRotationInput();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            followTransform = null;
        }
    }

    void HandleCameraMovement()
    {
        // Mouse Drag
        if (moveWithMouseDrag)
        {
            HandleMouseDragInput();
        }

        // Keyboard Control
        if (moveWithKeyboad)
        {
            if (keyboard.leftMetaKey.isPressed)
            {
                movementSpeed = fastSpeed;
            }
            else
            {
                movementSpeed = normalSpeed;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                newPosition += (transform.forward * movementSpeed);
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                newPosition += (transform.forward * -movementSpeed);
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                newPosition += (transform.right * movementSpeed);
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                newPosition += (transform.right * -movementSpeed);
            }
        }

        // Edge Scrolling
        //if (moveWithEdgeScrolling)
        //{
        //    Vector2 mousePos = mouse.position.ReadValue();

        //    // Move Right
        //    if (mousePos.x > Screen.width - edgeSize)
        //    {
        //        newPosition += (transform.right * movementSpeed);
        //        ChangeCursor(CursorArrow.RIGHT);
        //        isCursorSet = true;
        //    }

        //    // Move Left
        //    else if (mousePos.x < edgeSize)
        //    {
        //        newPosition += (transform.right * -movementSpeed);
        //        ChangeCursor(CursorArrow.LEFT);
        //        isCursorSet = true;
        //    }

        //    // Move Up
        //    else if (mousePos.y > Screen.height - edgeSize)
        //    {
        //        newPosition += (transform.forward * movementSpeed);
        //        ChangeCursor(CursorArrow.UP);
        //        isCursorSet = true;
        //    }

        //    // Move Down
        //    else if (mousePos.y < edgeSize)
        //    {
        //        newPosition += (transform.forward * -movementSpeed);
        //        ChangeCursor(CursorArrow.DOWN);
        //        isCursorSet = true;
        //    }
        //    else
        //    {
        //        if (isCursorSet)
        //        {
        //            ChangeCursor(CursorArrow.DEFAULT);
        //            isCursorSet = false;
        //        }
        //    }
        //}

        transform.position = newPosition;
        transform.rotation = Quaternion.Euler(newRotation);

        Cursor.lockState = CursorLockMode.Confined; // If we have an extra monitor we don't want to exit screen bounds
    }

    private void HandleZoomInput()
    {
        // Handle mouse wheel input - updates target zoom levels
        if (useMouseWheelZoom)
        {
            float scrollInput = mouse.scroll.ReadValue().y;
            if (scrollInput != 0)
            {
                float zoomInput = -scrollInput * mouseWheelZoomSpeed; // Negative so scroll up = zoom in

                if (followTransform != null)
                {
                    // Update target follow distance
                    targetFollowDistance += zoomInput;
                    targetFollowDistance = Mathf.Clamp(targetFollowDistance, minZoomY, maxZoomY);
                }
                else
                {
                    // Update target zoom height
                    targetZoomY += zoomInput;
                    targetZoomY = Mathf.Clamp(targetZoomY, minZoomY, maxZoomY);
                }
            }
        }

        // Smooth interpolation towards target zoom levels
        if (followTransform != null)
        {
            // When following, smoothly adjust distance to target
            float currentDistance = Vector3.Distance(transform.position, followTransform.position);
            if (Mathf.Abs(currentDistance - targetFollowDistance) > 0.1f)
            {
                Vector3 direction = (transform.position - followTransform.position).normalized;
                float newDistance = Mathf.Lerp(currentDistance, targetFollowDistance, zoomSmoothSpeed * Time.deltaTime);
                transform.position = followTransform.position + direction * newDistance;
            }
        }
        else
        {
            // When free roaming, smoothly move Y towards target
            if (Mathf.Abs(newPosition.y - targetZoomY) > 0.1f)
            {
                newPosition.y = Mathf.Lerp(newPosition.y, targetZoomY, zoomSmoothSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleRotationInput()
    {
        // Q/E rotation input
        if (keyboard.qKey.isPressed)
        {
            newRotation.y -= rotationSpeed * Time.deltaTime; // Rotate left
        }
        if (keyboard.eKey.isPressed)
        {
            newRotation.y += rotationSpeed * Time.deltaTime; // Rotate right
        }

        // Apply rotation
        transform.rotation = Quaternion.Euler(newRotation);
    }

    private void ChangeCursor(CursorArrow newCursor)
    {
        // Only change cursor if its not the same cursor
        if (currentCursor != newCursor)
        {
            switch (newCursor)
            {
                case CursorArrow.UP:
                    Cursor.SetCursor(cursorArrowUp, Vector2.zero, CursorMode.Auto);
                    break;
                case CursorArrow.DOWN:
                    Cursor.SetCursor(cursorArrowDown, new Vector2(cursorArrowDown.width, cursorArrowDown.height), CursorMode.Auto); // So the Cursor will stay inside view
                    break;
                case CursorArrow.LEFT:
                    Cursor.SetCursor(cursorArrowLeft, Vector2.zero, CursorMode.Auto);
                    break;
                case CursorArrow.RIGHT:
                    Cursor.SetCursor(cursorArrowRight, new Vector2(cursorArrowRight.width, cursorArrowRight.height), CursorMode.Auto); // So the Cursor will stay inside view
                    break;
                case CursorArrow.DEFAULT:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
            }

            currentCursor = newCursor;
        }
    }

    private void HandleMouseDragInput()
    {
        if (mouse.middleButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject() == false)
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (mouse.middleButton.isPressed && EventSystem.current.IsPointerOverGameObject() == false)
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);

                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }
    }


}