using UnityEngine;

namespace InventoryAndCrafting
{
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float edgeScrollThreshold = 20f;
    [SerializeField] private bool useEdgeScrolling = true;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float zoomSmoothness = 10f;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool allowCameraRotation = true;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private KeyCode rotateKey = KeyCode.Mouse2; // Orta mouse tuşu

    private float currentZoom;
    private float targetZoom;
    private Vector3 targetPosition;
    private float currentRotationAngle;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned to CameraController! Please assign a target in the inspector.");
            return;
        }

        currentZoom = offset.magnitude;
        targetZoom = currentZoom;
        currentRotationAngle = transform.eulerAngles.y;
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Inventory açıkken kamera kontrollerini devre dışı bırak
        if (UIManager.Instance != null && UIManager.Instance.IsInventoryOpen())
        {
            // Sadece karakteri takip et
            targetPosition = target.position;
            UpdateCameraPosition();
            return;
        }

        HandleZoom();
        HandleRotation();
        HandleMovement();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0)
        {
            targetZoom = Mathf.Clamp(targetZoom - scrollDelta * zoomSpeed, minZoom, maxZoom);
        }

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSmoothness);
    }

    private void HandleRotation()
    {
        if (!allowCameraRotation) return;

        if (Input.GetKey(rotateKey))
        {
            float rotationDelta = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentRotationAngle += rotationDelta;
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        // WASD veya ok tuşları ile hareket
        moveDirection.x = Input.GetAxisRaw("Horizontal");
        moveDirection.z = Input.GetAxisRaw("Vertical");

        // Ekran kenarlarında hareket
        if (useEdgeScrolling)
        {
            if (Input.mousePosition.x <= edgeScrollThreshold)
                moveDirection.x = -1;
            else if (Input.mousePosition.x >= Screen.width - edgeScrollThreshold)
                moveDirection.x = 1;

            if (Input.mousePosition.y <= edgeScrollThreshold)
                moveDirection.z = -1;
            else if (Input.mousePosition.y >= Screen.height - edgeScrollThreshold)
                moveDirection.z = 1;
        }

        if (moveDirection != Vector3.zero)
        {
            moveDirection = Quaternion.Euler(0, currentRotationAngle, 0) * moveDirection;
            targetPosition += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }
        else
        {
            targetPosition = Vector3.Lerp(targetPosition, target.position, Time.deltaTime * moveSpeed);
        }
    }

    private void UpdateCameraPosition()
    {
        // Offset'i zoom ve rotasyona göre güncelle
        Vector3 normalizedOffset = offset.normalized * currentZoom;
        normalizedOffset = Quaternion.Euler(0, currentRotationAngle, 0) * normalizedOffset;

        // Kamera pozisyonunu güncelle
        Vector3 newPosition = targetPosition + normalizedOffset;
        transform.position = newPosition;
        transform.LookAt(targetPosition);
    }

    // Hedefi dışarıdan ayarlamak için
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetPosition = target.position;
        }
    }
}
}
