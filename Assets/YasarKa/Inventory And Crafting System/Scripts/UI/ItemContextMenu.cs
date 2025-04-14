using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
    public class ItemContextMenu : MonoBehaviour
    {
        public static ItemContextMenu Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button splitButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;  
        [SerializeField] private GameObject amountPanel;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private TextMeshProUGUI amountText;
        
        private InventorySlot currentInventorySlot;
        private EquipmentSlot currentEquipmentSlot;
        private bool isSelectingAmount = false;
        private MenuAction currentAction;

        private enum MenuAction
        {
            None,
            Delete,
            Split
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            // Button listeners
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (splitButton != null) splitButton.onClick.AddListener(OnSplitClicked);
            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
            if (unequipButton != null) unequipButton.onClick.AddListener(OnUnequipClicked);
            
            // Amount slider
            if (amountSlider != null)
            {
                amountSlider.onValueChanged.AddListener(OnAmountValueChanged);
                amountSlider.wholeNumbers = true;
            }

            // Panel settings
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.pivot = new Vector2(0, 1);
            }

            // Initially hide
            gameObject.SetActive(false);
            if (amountPanel != null) amountPanel.SetActive(false);
        }

        public void Show(InventorySlot slot, Vector2 position)
        {
            currentInventorySlot = slot;
            currentEquipmentSlot = null;
            currentAction = MenuAction.None;
            
            SetupMenuPosition(position);
            ConfigureInventoryButtons();
        }

        public void ShowForEquipment(EquipmentSlot slot, Vector2 position)
        {
            currentEquipmentSlot = slot;
            currentInventorySlot = null;
            currentAction = MenuAction.None;

            SetupMenuPosition(position);
            ConfigureEquipmentButtons();
        }

        private void SetupMenuPosition(Vector2 position)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float panelWidth = rectTransform.rect.width;
                float panelHeight = rectTransform.rect.height;
                Vector2 mousePos = position;

                if (mousePos.x + panelWidth > Screen.width)
                {
                    mousePos.x = Screen.width - panelWidth;
                }

                if (mousePos.y - panelHeight < 0)
                {
                    mousePos.y = panelHeight;
                }

                rectTransform.position = mousePos;
            }

            gameObject.SetActive(true);
            isSelectingAmount = false;
            if (amountPanel != null) amountPanel.SetActive(false);
        }

        private void ConfigureInventoryButtons()
        {
            if (currentInventorySlot != null && currentInventorySlot.CurrentItem != null)
            {
                // Delete button is always visible
                if (deleteButton != null) deleteButton.gameObject.SetActive(true);

                // Split button only for stackable items with more than one item
                if (splitButton != null)
                {
                    bool canSplit = currentInventorySlot.CurrentItem.isStackable && currentInventorySlot.ItemCount > 1;
                    splitButton.gameObject.SetActive(canSplit);
                }

                // Equip button only for non-stackable equipment items
                if (equipButton != null)
                {
                    bool canEquip = !currentInventorySlot.CurrentItem.isStackable && 
                                   currentInventorySlot.CurrentItem.equipmentSlotType != EquipmentSlotType.None;
                    equipButton.gameObject.SetActive(canEquip);
                }

                // Unequip button is hidden
                if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            }
        }

        private void ConfigureEquipmentButtons()
        {
            if (currentEquipmentSlot != null && currentEquipmentSlot.CurrentItem != null)
            {
                // Only unequip button is visible for equipment slots
                if (deleteButton != null) deleteButton.gameObject.SetActive(false);
                if (splitButton != null) splitButton.gameObject.SetActive(false);
                if (equipButton != null) equipButton.gameObject.SetActive(false);
                if (unequipButton != null) unequipButton.gameObject.SetActive(true);
            }
        }

        private void OnDeleteClicked()
        {
            if (currentInventorySlot != null)
            {
                if (!isSelectingAmount && currentInventorySlot.ItemCount > 1)
                {
                    ShowAmountSelector(MenuAction.Delete);
                }
                else if (isSelectingAmount)
                {
                    // Delete selected amount
                    int amountToDelete = Mathf.RoundToInt(amountSlider.value);
                    int remainingAmount = currentInventorySlot.ItemCount - amountToDelete;
                    
                    if (remainingAmount > 0)
                    {
                        currentInventorySlot.SetItem(currentInventorySlot.CurrentItem, remainingAmount);
                    }
                    else
                    {
                        currentInventorySlot.ClearSlot();
                    }
                    
                    Hide();
                }
                else
                {
                    // If only 1 item, delete directly
                    currentInventorySlot.ClearSlot();
                    Hide();
                }
            }
        }

        private void OnSplitClicked()
        {
            if (currentInventorySlot != null)
            {
                if (!isSelectingAmount)
                {
                    ShowAmountSelector(MenuAction.Split);
                }
                else
                {
                    // Execute split
                    int splitAmount = Mathf.RoundToInt(amountSlider.value);
                    if (splitAmount > 0)
                    {
                        InventorySlot emptySlot = InventoryManager.Instance.FindEmptySlot();
                        if (emptySlot != null)
                        {
                            // Split stack
                            int remainingAmount = currentInventorySlot.ItemCount - splitAmount;
                            emptySlot.SetItem(currentInventorySlot.CurrentItem, splitAmount);
                            currentInventorySlot.SetItem(currentInventorySlot.CurrentItem, remainingAmount);
                        }
                    }
                    Hide();
                }
            }
        }

        private void OnEquipClicked()
        {
            if (currentInventorySlot == null || !currentInventorySlot.CurrentItem.isEquippable) return;

            if (EquipmentManager.Instance == null)
            {
                Debug.LogError("EquipmentManager.Instance is null! Make sure EquipmentManager is in the scene.");
                return;
            }

            // Check if item can be equipped
            if (currentInventorySlot.CurrentItem.equipmentSlotType != EquipmentSlotType.None)
            {
                EquipmentManager.Instance.EquipItem(currentInventorySlot.CurrentItem, currentInventorySlot.CurrentItem.equipmentSlotType);
                Hide();
            }
            else
            {
                Debug.LogWarning($"Item {currentInventorySlot.CurrentItem.itemName} cannot be equipped (SlotType is None)");
            }
        }

        private void OnUnequipClicked()
        {
            if (currentEquipmentSlot != null && currentEquipmentSlot.CurrentItem != null)
            {
                // Check if there's space in inventory
                if (InventoryManager.Instance.HasEmptySlot())
                {
                    ItemData item = currentEquipmentSlot.UnequipItem();
                    InventoryManager.Instance.AddItem(item);
                    Hide();
                }
            }
        }

        private void ShowAmountSelector(MenuAction action)
        {
            currentAction = action;
            isSelectingAmount = true;
            
            if (amountPanel != null)
            {
                amountPanel.SetActive(true);
                
                // Configure slider
                if (amountSlider != null)
                {
                    amountSlider.minValue = 1;
                    amountSlider.maxValue = currentInventorySlot.ItemCount - (action == MenuAction.Split ? 1 : 0);
                    amountSlider.value = 1;
                    UpdateAmountText(1);
                }

                // Hide other buttons while selecting amount
                if (deleteButton != null) deleteButton.gameObject.SetActive(action == MenuAction.Delete);
                if (splitButton != null) splitButton.gameObject.SetActive(action == MenuAction.Split);
                if (equipButton != null) equipButton.gameObject.SetActive(false);
                if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            }
        }

        private void OnAmountValueChanged(float value)
        {
            UpdateAmountText(Mathf.RoundToInt(value));
        }

        private void UpdateAmountText(int amount)
        {
            if (amountText != null)
            {
                amountText.text = amount.ToString();
            }
        }

        private void Hide()
        {
            gameObject.SetActive(false);
            if (amountPanel != null) amountPanel.SetActive(false);
            isSelectingAmount = false;
            currentAction = MenuAction.None;
        }

        private void Update()
        {
            // Right click anywhere to close
            if (Input.GetMouseButtonDown(1))
            {
                Hide();
            }

            // Left click outside menu to close
            if (Input.GetMouseButtonDown(0) && !IsMouseOverMenu())
            {
                Hide();
            }

            // Escape key to exit amount selection
            if (Input.GetKeyDown(KeyCode.Escape) && isSelectingAmount)
            {
                Hide();
            }
        }

        private bool IsMouseOverMenu()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Check if any of the hit objects is this menu or its children
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
