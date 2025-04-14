using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace InventoryAndCrafting
{
public class ResourceNode : MonoBehaviour
{
    [Header("Resource Settings")]
    [SerializeField] private ItemData resourceItem;
    [SerializeField] private int minItemAmount = 1;
    [SerializeField] private int maxItemAmount = 3;
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private float respawnTime = 60f;
    [SerializeField] private float harvestTime = 3f;

    [Header("Animation Settings")]
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float floatHeight = 0.5f;

    private bool isCollectable = true;
    private bool isSelected = false;
    private bool isCollecting = false;

    // Herhangi bir resource toplanıyor mu kontrolü için static değişken
    private static bool isAnyResourceCollecting = false;

    private GameObject playerObject;
    private NavMeshAgent playerAgent;
    private OutlineEffect outlineEffect;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float resourceRadius;

    private void Awake()
    {
        outlineEffect = GetComponent<OutlineEffect>();
        if (outlineEffect == null)
        {
            outlineEffect = GetComponentInChildren<OutlineEffect>();
        }

        originalScale = transform.localScale;
        originalPosition = transform.position;

        Collider resourceCollider = GetComponent<Collider>();
        if (resourceCollider != null)
        {
            resourceRadius = Mathf.Max(resourceCollider.bounds.extents.x, resourceCollider.bounds.extents.z);
        }
        else
        {
            resourceRadius = 1f;
        }
    }

    private void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerAgent = playerObject.GetComponent<NavMeshAgent>();
            if (playerAgent != null)
            {
                playerAgent.stoppingDistance = resourceRadius + 0.5f;
            }
        }

        if (outlineEffect != null)
        {
            outlineEffect.ToggleOutline(false);
        }
    }

    private void OnEnable()
    {
        isCollectable = true;
        transform.position = originalPosition;
        transform.localScale = originalScale;
        
        if (outlineEffect != null)
        {
            outlineEffect.ToggleOutline(false);
        }
    }

    private void OnDisable()
    {
        if (outlineEffect != null)
        {
            outlineEffect.ToggleOutline(false);
        }

        // Eğer bu resource toplanıyorken devre dışı kalırsa, static flag'i sıfırla
        if (isCollecting)
        {
            isAnyResourceCollecting = false;
            isCollecting = false;
        }
    }

    private void Update()
    {
        if (!isCollectable) return;

        // UI açıkken veya UI elemanlarının üzerindeyken etkileşimi engelle
        if (UIManager.Instance != null && (UIManager.Instance.IsInventoryOpen() || UIManager.Instance.IsPointerOverUI()))
        {
            if (outlineEffect != null)
            {
                outlineEffect.ToggleOutline(false);
            }
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool isMouseOver = Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject;
        
        if (outlineEffect != null)
        {
            if (isMouseOver && !isSelected)
            {
                outlineEffect.ToggleOutline(true);
            }
            else if (!isMouseOver && !isSelected)
            {
                outlineEffect.ToggleOutline(false);
            }
        }

        if (isMouseOver && Input.GetMouseButtonDown(0) && !isSelected && !isCollecting && !isAnyResourceCollecting)
        {
            isSelected = true;
            if (outlineEffect != null)
            {
                outlineEffect.ToggleOutline(true);
            }
            
            if (playerAgent != null)
            {
                Vector3 directionToPlayer = (playerObject.transform.position - transform.position).normalized;
                Vector3 targetPosition = transform.position + directionToPlayer * (resourceRadius + 0.5f);
                playerAgent.SetDestination(targetPosition);
            }
        }

        if (playerObject != null && isSelected)
        {
            float distance = Vector3.Distance(transform.position, playerObject.transform.position);
            if (distance <= interactionDistance)
            {
                if (!isCollecting && !isAnyResourceCollecting)
                {
                    StartCoroutine(CollectResourceWithAnimation());
                }
            }
            else if (isCollecting)
            {
                StopCollecting();
            }
        }
    }

    private IEnumerator CollectResourceWithAnimation()
    {
        isCollecting = true;
        isAnyResourceCollecting = true;

        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < harvestTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / harvestTime;

            float yOffset = Mathf.Sin(progress * Mathf.PI) * floatHeight;
            transform.position = startPosition + Vector3.up * yOffset;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            transform.localScale = originalScale * Mathf.Lerp(1f, 0.1f, progress);

            yield return null;
        }

        AddToInventory();
    }

    private void StopCollecting()
    {
        if (isCollecting)
        {
            isCollecting = false;
            isAnyResourceCollecting = false;
            isSelected = false;
            transform.position = originalPosition;
            transform.localScale = originalScale;
            
            if (outlineEffect != null)
            {
                outlineEffect.ToggleOutline(false);
            }
            
            StopAllCoroutines();
        }
    }

    private void AddToInventory()
    {
        if (!isCollectable) return;

        int amount = Random.Range(minItemAmount, maxItemAmount + 1);

        if (InventoryManager.Instance != null && resourceItem != null)
        {
            InventoryManager.Instance.AddItem(resourceItem, amount);
            
            // Bildirim göster
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowItemNotification(resourceItem, amount);
            }
            
            isCollectable = false;
            isSelected = false;
            isCollecting = false;
            isAnyResourceCollecting = false;
            
            if (outlineEffect != null)
            {
                outlineEffect.ToggleOutline(false);
            }

            gameObject.SetActive(false);
            
            // ResourceManager'ı kullanarak respawn timer'ı başlat
            ResourceManager.Instance.StartRespawnTimer(this, respawnTime);
        }
    }

    public void ResetResource()
    {
        transform.position = originalPosition;
        transform.localScale = originalScale;
        isCollectable = true;
        isSelected = false;
        isCollecting = false;
        
        if (outlineEffect != null)
        {
            outlineEffect.ToggleOutline(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
}
