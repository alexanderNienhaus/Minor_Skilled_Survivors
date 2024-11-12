using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float slowSpeed = 0.125f;
    [SerializeField] private float normalSpeed = 0.5f;
    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float movementTime = 5;
    [SerializeField] private Vector2 moveLimit = new Vector2(100, 100);

    [Header("Rotation")]
    [SerializeField] private float slowRotationAmount = 0.125f;
    [SerializeField] private float normalRotationAmount = 0.5f;
    [SerializeField] private float fastRotationAmount = 2f;
    [SerializeField] private float rotaionSpeedMouse = 0.1f;

    [Header("Zoom")]
    [SerializeField] private Vector3 slowZoomAmount = new Vector3(0, -0.125f, 0.125f);
    [SerializeField] private Vector3 normalZoomAmount = new Vector3(0, -0.5f, 0.5f);
    [SerializeField] private Vector3 fastZoomAmount = new Vector3(0, -2f, 2f);
    [SerializeField] private Vector2 zoomLimit = new Vector2(-45, 45);
    [SerializeField] private float mouseZoomSpeedUp = 6f;

    private float movementSpeed;
    private float rotationAmount;
    private Vector3 zoomAmount;
    private Vector3 newPosition;
    private Quaternion newRotation;
    private Quaternion initialRotation;
    private Vector3 newZoom;
    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;

    private void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
        initialRotation = newRotation;
    }

    private void Update()
    {
        HandleSpeed();
        HandleMousePosition();
        HandleMovementInput();
    }

    private void HandleSpeed()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementSpeed = fastSpeed;
            rotationAmount = fastRotationAmount;
            zoomAmount = fastZoomAmount;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            movementSpeed = slowSpeed;
            rotationAmount = slowRotationAmount;
            zoomAmount = slowZoomAmount;
        }
        else
        {
            movementSpeed = normalSpeed;
            rotationAmount = normalRotationAmount;
            zoomAmount = normalZoomAmount;
        }
    }

    private void HandleMousePosition()
    {
        if (Input. mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomAmount * mouseZoomSpeedUp;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if(plane.Raycast(ray, out entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }

        if (Input.GetMouseButton (1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }

        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(transform.up * (-difference.x * rotaionSpeedMouse));
        }
    }

    private void HandleMovementInput()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += transform.forward * movementSpeed;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition += transform.forward * -movementSpeed;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += transform.right * movementSpeed;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition += transform.right * -movementSpeed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(transform.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(transform.up * -rotationAmount);
        }
        if (Input.GetKey(KeyCode.R))
        {
            newZoom += zoomAmount;
        }
        if (Input.GetKey(KeyCode.F))
        {
            newZoom -= zoomAmount;
        }
        if (Input.GetKey(KeyCode.T))
        {
            newRotation = initialRotation;
            newZoom = Vector3.zero;
        }

        float displacement = cameraTransform.position.y + cameraTransform.localPosition.z;
        float rad = Mathf.Deg2Rad * newRotation.eulerAngles.y;
        float sinRot = Mathf.Sin(rad);
        float cosRot = Mathf.Cos(rad);
        float sinDisplacement = sinRot * displacement;
        float cosDisplacement = cosRot * displacement;

        newPosition.x = Mathf.Clamp(newPosition.x, -moveLimit.x - sinDisplacement, moveLimit.x - sinDisplacement);
        newPosition.z = Mathf.Clamp(newPosition.z, -moveLimit.y - cosDisplacement, moveLimit.y - cosDisplacement);

        newZoom.y = Mathf.Clamp(newZoom.y, zoomLimit.x, zoomLimit.y);
        newZoom.z = Mathf.Clamp(newZoom.z, zoomLimit.x, zoomLimit.y);

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }
}
