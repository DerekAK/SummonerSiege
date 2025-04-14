using System.Collections.Generic;
using UnityEngine;

namespace InventoryAndCrafting
{
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [SerializeField] private List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();
        [SerializeField] private DraggableItem draggableItemPrefab;
        private DraggableItem draggableItem;
        
        // Equipment değişikliklerini dinlemek için event
        public event System.Action onEquipmentChanged;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("InventoryManager instance not found!");
                return;
            }

            draggableItem = InventoryManager.Instance.GetDraggableItem();
            if (draggableItem == null)
            {
                Debug.LogError("DraggableItem could not be found in InventoryManager!");
            }
        }

        public DraggableItem GetDraggableItem()
        {
            if (draggableItem == null)
            {
                if (InventoryManager.Instance != null)
                {
                    draggableItem = InventoryManager.Instance.GetDraggableItem();
                }
                else
                {
                    Debug.LogError("InventoryManager instance not found in GetDraggableItem!");
                }
            }
            return draggableItem;
        }

        public void EquipItem(ItemData item, EquipmentSlotType slotType, bool removeFromInventory = true)
        {
            if (item == null) return;

            // Find the correct equipment slot
            EquipmentSlot targetSlot = null;
            foreach (var slot in equipmentSlots)
            {
                if (slot.slotType == slotType)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot != null)
            {
                // Eğer slot doluysa, önceki itemi envantere geri koy
                if (targetSlot.CurrentItem != null)
                {
                    InventoryManager.Instance.AddItem(targetSlot.CurrentItem);
                }

                // Yeni itemi ekipman slotuna yerleştir
                targetSlot.EquipItem(item);

                // Eğer removeFromInventory true ise inventory'den kaldır
                if (removeFromInventory)
                {
                    InventoryManager.Instance.RemoveItem(item);
                }

                // Event'i tetikle
                onEquipmentChanged?.Invoke();
            }
        }

        public void HandleDrop(EquipmentSlot targetSlot)
        {
            // DraggableItem'dan sürüklenen item'ı al
            DraggableItem dragItem = GetDraggableItem();
            if (dragItem == null) return;

            // Inventory'den sürüklenen slot'u al
            InventorySlot draggedFromSlot = InventoryManager.Instance.GetDraggedFromSlot();
            if (draggedFromSlot == null) return;

            ItemData draggedItem = draggedFromSlot.GetItem();
            if (draggedItem == null || !draggedItem.isEquippable) return;

            if (draggedItem.equipmentSlotType == targetSlot.GetSlotType())
            {
                // Eğer hedef slot doluysa, içindeki item'ı inventory'e geri koy
                if (!targetSlot.IsEmpty())
                {
                    ItemData equippedItem = targetSlot.UnequipItem();
                    draggedFromSlot.SetItem(equippedItem, 1);
                }
                else
                {
                    draggedFromSlot.ClearSlot();
                }
                
                // Yeni item'ı equip et
                targetSlot.EquipItem(draggedItem);
            }
            else
            {
                Debug.Log($"Slot types don't match! Item type: {draggedItem.equipmentSlotType}, Slot type: {targetSlot.GetSlotType()}");
            }
        }

        private EquipmentSlot GetSlotByType(EquipmentSlotType type)
        {
            return equipmentSlots.Find(slot => slot.GetSlotType() == type);
        }

        public void UnequipAll()
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot.CurrentItem != null)
                {
                    ItemData item = slot.UnequipItem();
                    if (item != null)
                    {
                        InventoryManager.Instance.AddItem(item, 1);
                    }
                }
            }
        }

        public void UpdateUI()
        {
            onEquipmentChanged?.Invoke();
        }

        public List<EquipmentSlot> GetAllSlots()
        {
            return equipmentSlots;
        }
    }
}
