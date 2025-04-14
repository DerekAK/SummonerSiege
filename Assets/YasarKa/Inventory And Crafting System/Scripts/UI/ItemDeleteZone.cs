using UnityEngine;
using UnityEngine.EventSystems;

namespace InventoryAndCrafting
{
    public class ItemDeleteZone : MonoBehaviour, IDropHandler
    {
        private void Start()
        {
            // Başlangıçta gizle
            gameObject.SetActive(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Inventory slot'undan düşürülen item'ı kontrol et
            InventorySlot inventorySlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
            if (inventorySlot != null && !inventorySlot.IsEmpty())
            {
                inventorySlot.ClearSlot();
                InventoryManager.Instance.EndDrag();
                return;
            }

            // Equipment slot'undan düşürülen item'ı kontrol et
            EquipmentSlot equipmentSlot = eventData.pointerDrag?.GetComponent<EquipmentSlot>();
            if (equipmentSlot != null && !equipmentSlot.IsEmpty())
            {
                equipmentSlot.UnequipItem();
                equipmentSlot.ClearSlot(); // Slot'u temizle
                InventoryManager.Instance.EndDrag();
                return;
            }
        }

        // Item sürüklemeye başladığında çağrılacak
        public void Show()
        {
            gameObject.SetActive(true);
        }

        // Item sürükleme bittiğinde çağrılacak
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
