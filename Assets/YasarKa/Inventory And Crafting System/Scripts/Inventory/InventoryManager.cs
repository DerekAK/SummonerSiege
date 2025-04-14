using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    // Envanter değişikliklerini dinlemek için event
    public System.Action onInventoryChanged;

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 24;
    [SerializeField] private int defaultMaxStackSize = 24; // Varsayılan maksimum stack boyutu
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private DraggableItem draggableItem;
    [SerializeField] private Transform slotContainer; // Mevcut SlotContainer referansı
    [Header("UI References")]
    [SerializeField] private ItemDeleteZone deleteZone;
    [SerializeField] private ItemContextMenu contextMenu;

    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private InventorySlot draggedFromSlot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Canvas referansını kaydet
            Canvas originalCanvas = GetComponentInParent<Canvas>();
            
            // Eğer root obje değilse, yeni bir root obje oluştur ve taşı
            if (transform.parent != null)
            {
                // Mevcut referansları kaydet
                Transform oldParent = transform.parent;
                Vector3 oldPosition = transform.position;
                Vector3 oldScale = transform.localScale;
                Quaternion oldRotation = transform.rotation;
                
                // Yeni bir root GameObject oluştur
                GameObject newRoot = new GameObject("InventorySystem");
                
                // Pozisyonu ve rotasyonu ayarla
                newRoot.transform.position = oldPosition;
                newRoot.transform.rotation = oldRotation;
                newRoot.transform.localScale = oldScale;
                
                // Bu objeyi yeni root'a taşı
                transform.SetParent(newRoot.transform);
                
                // Root objeyi DontDestroyOnLoad yap
                DontDestroyOnLoad(newRoot);
                
                // Canvas'ı tekrar bağla
                if (originalCanvas != null)
                {
                    transform.SetParent(originalCanvas.transform);
                }
            }
            else
            {
                // Zaten root objeyse direkt DontDestroyOnLoad yap
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeInventory();
    }

        [System.Obsolete]
        private void Start()
    {
        // DraggableItem'ı oluştur
        if (draggableItem == null)
        {
            Debug.LogError("DraggableItem prefab is not assigned!");
            return;
        }

        // Canvas'ı bul
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Sahnedeki tüm Canvas'ları ara
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases.Length > 0)
            {
                // Ana Canvas'ı bul (genellikle Screen Space - Overlay olan)
                foreach (Canvas c in canvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvas = c;
                        break;
                    }
                }
                
                // Eğer Screen Space - Overlay Canvas bulunamazsa, ilk Canvas'ı kullan
                if (canvas == null)
                {
                    canvas = canvases[0];
                }
                
                // InventoryManager'ı Canvas'a taşı
                transform.SetParent(canvas.transform, false);
            }
            else
            {
                Debug.LogError("No Canvas found in the scene!");
                return;
            }
        }

        // DraggableItem'ı instantiate et
        DraggableItem dragInstance = Instantiate(draggableItem, canvas.transform);
        dragInstance.gameObject.SetActive(true);
        dragInstance.transform.SetAsLastSibling();
        draggableItem = dragInstance;
        draggableItem.gameObject.SetActive(false);

        if (deleteZone != null)
        {
            deleteZone.gameObject.SetActive(false);
        }

        // UI'ı başlangıçta güncelle
        UpdateAllSlots();
    }

    private void InitializeInventory()
    {
        // Önce mevcut slotları temizle
        if (slotContainer != null)
        {
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
        }
        inventorySlots.Clear();

        // Grid Layout Group'u kontrol et
        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = slotContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(60, 60);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        // Slotları oluştur
        for (int i = 0; i < inventorySize; i++)
        {
            InventorySlot newSlot = Instantiate(slotPrefab, slotContainer);
            if (newSlot != null)
            {
                newSlot.GetComponent<RectTransform>().localScale = Vector3.one;
                inventorySlots.Add(newSlot);
            }
            else
            {
                Debug.LogError($"Failed to create slot {i + 1}");
            }
        }

        //Debug.Log($"Initialized inventory with {inventorySlots.Count} slots");
    }

    public int GetMaxStackSize(ItemData item)
    {
        if (item == null) return 1;
        if (!item.isStackable) return 1;
        return item.useCustomStackSize ? item.maxStackSize : defaultMaxStackSize;
    }

    public bool CanAddToStack(InventorySlot slot, ItemData item, int amount = 1)
    {
        if (slot == null || item == null) return false;
        if (!item.isStackable) return false;
        
        int maxStack = GetMaxStackSize(item);
        return slot.ItemCount + amount <= maxStack;
    }

    public void UpdateAllSlots()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
            {
                slot.UpdateUI();
            }
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        //Debug.Log($"Adding {amount} {item.itemName}(s) to inventory");

        // Önce mevcut stack'lere eklemeyi dene
        int remainingAmount = amount;
        foreach (var slot in inventorySlots)
        {
            if (slot.CurrentItem == item && CanAddToStack(slot, item, remainingAmount))
            {
                int canAdd = GetMaxStackSize(item) - slot.ItemCount;
                int amountToAdd = Mathf.Min(canAdd, remainingAmount);
                slot.AddToStack(amountToAdd);
                remainingAmount -= amountToAdd;

                //Debug.Log($"Added {amountToAdd} {item.itemName}(s) to existing stack");

                if (remainingAmount <= 0)
                {
                    onInventoryChanged?.Invoke();
                    UpdateAllSlots();
                    return true;
                }
            }
        }

        // Kalan itemlar için yeni slotlar kullan
        while (remainingAmount > 0)
        {
            InventorySlot emptySlot = FindEmptySlot();
            if (emptySlot == null) 
            {
                Debug.LogWarning($"Couldn't add {remainingAmount} {item.itemName}(s) to inventory. No space left!");
                onInventoryChanged?.Invoke();
                UpdateAllSlots();
                return false;
            }

            int maxStack = GetMaxStackSize(item);
            int amountToAdd = Mathf.Min(maxStack, remainingAmount);
            emptySlot.SetItem(item, amountToAdd);
            remainingAmount -= amountToAdd;

            //Debug.Log($"Added {amountToAdd} {item.itemName}(s) to new slot");
        }

        onInventoryChanged?.Invoke();
        UpdateAllSlots();
        return true;
    }

    public void HandleItemTransfer(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot == null || toSlot == null) return;

        // Kaynak slot boşsa işlem yapma
        if (fromSlot.IsEmpty()) return;

        // Hedef slot boşsa direkt transfer et
        if (toSlot.IsEmpty())
        {
            toSlot.SetItem(fromSlot.CurrentItem, fromSlot.ItemCount);
            fromSlot.ClearSlot();
            onInventoryChanged?.Invoke();
            return;
        }

        // Aynı item ise ve stacklenebilirse
        if (fromSlot.CurrentItem == toSlot.CurrentItem && toSlot.CurrentItem.isStackable)
        {
            int maxStack = GetMaxStackSize(toSlot.CurrentItem);
            int totalAmount = toSlot.ItemCount + fromSlot.ItemCount;

            if (totalAmount <= maxStack)
            {
                // Tümünü ekle
                toSlot.SetItem(toSlot.CurrentItem, totalAmount);
                fromSlot.ClearSlot();
            }
            else
            {
                // Maksimum stack kadar ekle, kalanı kaynak slotta bırak
                toSlot.SetItem(toSlot.CurrentItem, maxStack);
                fromSlot.SetItem(fromSlot.CurrentItem, totalAmount - maxStack);
            }
            onInventoryChanged?.Invoke();
        }
        // Farklı itemler ise yer değiştir
        else
        {
            ItemData tempItem = toSlot.CurrentItem;
            int tempCount = toSlot.ItemCount;
            
            toSlot.SetItem(fromSlot.CurrentItem, fromSlot.ItemCount);
            fromSlot.SetItem(tempItem, tempCount);
            onInventoryChanged?.Invoke();
        }
    }

    public void HandleDrop(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot == null || toSlot == null) return;

        ItemData fromItem = fromSlot.GetItem();
        ItemData toItem = toSlot.GetItem();
        
        if (fromItem != null)
        {
            if (toItem == null)
            {
                // Boş slot'a taşıma
                toSlot.SetItem(fromItem, fromSlot.GetAmount());
                fromSlot.ClearSlot();
                onInventoryChanged?.Invoke();
            }
            else if (fromItem == toItem && toItem.isStackable)
            {
                // Aynı item'ları birleştirme
                int totalAmount = fromSlot.GetAmount() + toSlot.GetAmount();
                int maxStack = toItem.maxStackSize;
                
                if (totalAmount <= maxStack)
                {
                    // Tümünü birleştir
                    toSlot.SetItem(toItem, totalAmount);
                    fromSlot.ClearSlot();
                }
                else
                {
                    // Maximum stack kadar birleştir, kalanı eski yerde bırak
                    toSlot.SetItem(toItem, maxStack);
                    fromSlot.SetItem(fromItem, totalAmount - maxStack);
                }
                onInventoryChanged?.Invoke();
            }
            else
            {
                // Farklı item'ları yer değiştir
                ItemData tempItem = toItem;
                int tempAmount = toSlot.GetAmount();
                
                toSlot.SetItem(fromItem, fromSlot.GetAmount());
                fromSlot.SetItem(tempItem, tempAmount);
                onInventoryChanged?.Invoke();
            }
        }
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;

        int remainingAmount = amount;
        bool removed = false;

        // Sondan başlayarak itemları kaldır (kısmi stack'ler önce)
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            var slot = inventorySlots[i];
            if (slot.GetItem() == item)
            {
                int slotAmount = slot.GetAmount();
                int toRemove = Mathf.Min(remainingAmount, slotAmount);
                
                if (slotAmount == toRemove)
                {
                    slot.ClearSlot();
                }
                else
                {
                    slot.RemoveFromStack(toRemove);
                }
                
                remainingAmount -= toRemove;
                removed = true;
                
                if (remainingAmount <= 0) break;
            }
        }

        if (removed)
        {
            // Envanter değiştiğinde event'i tetikle
            onInventoryChanged?.Invoke();
        }

        return removed;
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null) return;

        // Find the first slot with this item
        foreach (var slot in inventorySlots)
        {
            if (slot.CurrentItem == item)
            {
                slot.ClearSlot();
                onInventoryChanged?.Invoke();
                break;
            }
        }
    }

    public void BeginDrag(InventorySlot fromSlot, ItemData item, int count, Vector2 position)
    {
        draggedFromSlot = fromSlot; // fromSlot null olabilir (equipment slot'tan geliyorsa)
        if (draggableItem != null)
        {
            draggableItem.gameObject.SetActive(true);
            draggableItem.InitializeDragData(item.itemIcon, count, position);
            draggableItem.transform.SetAsLastSibling(); // En üstte göster
        }

        // Delete zone'u göster
        if (deleteZone != null)
        {
            deleteZone.gameObject.SetActive(true);
        }
    }

    public void OnDrag(Vector2 position)
    {
        if (draggableItem != null)
        {
            draggableItem.UpdatePosition(position);
        }
    }

    public void HandleDrag(Vector3 position)
    {
        if (draggableItem != null && draggedFromSlot != null)
        {
            draggableItem.UpdatePosition(position);
        }
    }

    public void HandleDrop(InventorySlot targetSlot)
    {
        if (draggedFromSlot == null || targetSlot == null) return;

        // Aynı slot ise işlem yapma
        if (draggedFromSlot == targetSlot) return;

        // Hedef slot boşsa direkt transfer et
        if (targetSlot.IsEmpty())
        {
            targetSlot.SetItem(draggedFromSlot.CurrentItem, draggedFromSlot.ItemCount);
            draggedFromSlot.ClearSlot();
            return;
        }

        // Aynı item ise ve stacklenebilirse
        if (draggedFromSlot.CurrentItem == targetSlot.CurrentItem && targetSlot.CurrentItem.isStackable)
        {
            int maxStack = GetMaxStackSize(targetSlot.CurrentItem);
            int totalAmount = targetSlot.ItemCount + draggedFromSlot.ItemCount;

            if (totalAmount <= maxStack)
            {
                // Tümünü ekle
                targetSlot.SetItem(targetSlot.CurrentItem, totalAmount);
                draggedFromSlot.ClearSlot();
            }
            else
            {
                // Maksimum stack kadar ekle, kalanı kaynak slotta bırak
                targetSlot.SetItem(targetSlot.CurrentItem, maxStack);
                draggedFromSlot.SetItem(draggedFromSlot.CurrentItem, totalAmount - maxStack);
            }
        }
        // Farklı itemler ise yer değiştir
        else
        {
            ItemData tempItem = targetSlot.CurrentItem;
            int tempCount = targetSlot.ItemCount;
            
            targetSlot.SetItem(draggedFromSlot.CurrentItem, draggedFromSlot.ItemCount);
            draggedFromSlot.SetItem(tempItem, tempCount);
        }

        onInventoryChanged?.Invoke();
    }

    public void EndDrag()
    {
        draggedFromSlot = null;
        if (draggableItem != null)
        {
            draggableItem.gameObject.SetActive(false);
            draggableItem.EndDrag();
        }

        // Delete zone'u gizle
        if (deleteZone != null)
        {
            deleteZone.gameObject.SetActive(false);
        }
    }

    public DraggableItem GetDraggableItem()
    {
        return draggableItem;
    }

    public InventorySlot GetDraggedFromSlot()
    {
        return draggedFromSlot;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;

        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot.GetItem() == item)
            {
                total += slot.GetAmount();
            }
        }
        return total;
    }

    public bool HasEmptySlot()
    {
        return FindEmptySlot() != null;
    }

    public InventorySlot FindEmptySlot()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.IsEmpty())
            {
                return slot;
            }
        }
        return null;
    }

    public void ShowContextMenu(InventorySlot slot, Vector2 position)
    {
        if (contextMenu != null)
        {
            contextMenu.Show(slot, position);
        }
    }

    public List<InventorySlot> Slots => inventorySlots;

    public void ClearInventory()
    {
        foreach (var slot in inventorySlots)
        {
            slot.ClearSlot();
        }
    }

    public void AddItemToSlot(ItemData item, int amount, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Count)
        {
            inventorySlots[slotIndex].SetItem(item, amount);
        }
    }
}
}
