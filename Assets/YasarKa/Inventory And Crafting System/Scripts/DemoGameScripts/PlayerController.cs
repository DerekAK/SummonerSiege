using UnityEngine;
using UnityEngine.AI;

namespace InventoryAndCrafting
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float stoppingDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;

        private NavMeshAgent agent;
        private Camera mainCamera;
        private Vector3 targetPosition;
        private bool isMoving;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            mainCamera = Camera.main;
        }

        private void Start()
        {
            targetPosition = transform.position;
            agent.stoppingDistance = stoppingDistance;
        }

        private void Update()
        {
            // Inventory açıkken tüm kontrolleri devre dışı bırak
            if (UIManager.Instance != null && UIManager.Instance.IsInventoryOpen())
                return;

            HandleMovement();
            UpdateRotation();
        }

        private void HandleMovement()
        {
            // Sağ tık kontrolü
            if (Input.GetMouseButton(1))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    SetDestination(hit.point);
                }
            }

            // Hareket durumunu güncelle
            isMoving = agent.remainingDistance > agent.stoppingDistance;
        }

        private void SetDestination(Vector3 destination)
        {
            targetPosition = destination;
            agent.SetDestination(destination);
        }

        private void UpdateRotation()
        {
            if (isMoving)
            {
                // Hareket yönüne doğru yumuşak dönüş
                Vector3 lookDirection = agent.velocity.normalized;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }

        // Hareket durumunu dışarıdan kontrol etmek için
        public bool IsMoving => isMoving;
    }
}
