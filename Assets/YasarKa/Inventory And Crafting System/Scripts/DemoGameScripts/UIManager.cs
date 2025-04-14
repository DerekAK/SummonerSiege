using UnityEngine;
using UnityEngine.EventSystems;

namespace InventoryAndCrafting
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private KeyCode toggleInventoryKey = KeyCode.Tab;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Başlangıçta inventory'i gizle
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Tab tuşuna basıldığında inventory'i aç/kapat
            if (Input.GetKeyDown(toggleInventoryKey))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            if (inventoryPanel != null)
            {
                bool newState = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(newState);
            }
        }

        // Inventory'nin açık olup olmadığını kontrol et
        public bool IsInventoryOpen()
        {
            return inventoryPanel != null && inventoryPanel.activeSelf;
        }

        // Herhangi bir UI elemanının üzerinde olup olmadığımızı kontrol et
        public bool IsPointerOverUI()
        {
            // Eğer envanter açıksa ve mouse UI üzerindeyse true döndür
            if (IsInventoryOpen() && EventSystem.current != null)
            {
                return EventSystem.current.IsPointerOverGameObject();
            }
            return false;
        }

        // Inventory'i programatik olarak aç
        public void OpenInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
            }
        }

        // Inventory'i programatik olarak kapat
        public void CloseInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }
    }
}
