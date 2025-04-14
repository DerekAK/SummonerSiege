using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace InventoryAndCrafting
{
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("Slot Components")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private Image slotImage; // Slot'un kendisi için Image

    [Header("Visual Effects")]
    [SerializeField] private float hoverIntensity = 1.2f;
    [SerializeField] private float hoverTransitionSpeed = 5f;
    private Color originalSlotColor;
    private Color targetColor;

    [Header("Slot Data")]
    private ItemData currentItem;
    private int itemCount;
    private bool isDragging;
    
    public int ItemCount => itemCount; 
    public ItemData CurrentItem => currentItem; 
    
    private TooltipSystem tooltipSystem;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;
    private Vector2 originalPosition;
    private Transform originalParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        tooltipSystem = FindFirstObjectByType<TooltipSystem>();
        mainCanvas = GetComponentInParent<Canvas>();
        
        // Slot'un orijinal rengini kaydet
        if (slotImage != null)
        {
            originalSlotColor = slotImage.color;
            targetColor = originalSlotColor;
        }
        
        ClearSlot();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        // Renk geçişini yumuşat
        if (slotImage != null && slotImage.color != targetColor)
        {
            slotImage.color = Color.Lerp(slotImage.color, targetColor, Time.deltaTime * hoverTransitionSpeed);
        }
    }

    public void SetItem(ItemData item, int amount)
    {
        currentItem = item;
        itemCount = amount;
        UpdateUI();
        
        // Inventory Manager'ı bilgilendir
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged?.Invoke();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemCount = 0;
        UpdateUI();
        
        // Inventory Manager'ı bilgilendir
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged?.Invoke();
        }
    }

    public void SetAmount(int newAmount)
    {
        itemCount = newAmount;
        UpdateUI();
    }

    public int GetAmount()
    {
        return itemCount;
    }

    public ItemData GetItem()
    {
        return currentItem;
    }

    public bool IsEmpty() => currentItem == null;

    public bool CanAddToStack(ItemData item)
    {
        return (currentItem == null) || 
               (currentItem == item && 
                item.isStackable && 
                itemCount < item.maxStackSize);
    }

    public void AddToStack(int amount)
    {
        if (currentItem != null)
        {
            itemCount += amount;
            UpdateUI();
            
            // Inventory Manager'ı bilgilendir
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryChanged?.Invoke();
            }
        }
    }

    public void RemoveFromStack(int amount)
    {
        if (currentItem != null)
        {
            itemCount = Mathf.Max(0, itemCount - amount);
            if (itemCount == 0)
            {
                ClearSlot();
            }
            else
            {
                UpdateUI();
            }
            
            // Inventory Manager'ı bilgilendir
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryChanged?.Invoke();
            }
        }
    }

    public void UpdateUI()
    {
        if (currentItem != null && itemCount > 0)
        {
            if (itemIconImage != null)
            {
                itemIconImage.sprite = currentItem.itemIcon;
                itemIconImage.enabled = true;
                itemIconImage.gameObject.SetActive(true);
            }

            if (itemCountText != null)
            {
                itemCountText.text = itemCount > 1 ? itemCount.ToString() : "";
                itemCountText.enabled = itemCount > 1;
                itemCountText.gameObject.SetActive(true);
            }

            if (slotImage != null)
            {
                targetColor = GetSlotColorByRarity(currentItem.rarity);
            }
        }
        else
        {
            if (itemIconImage != null)
            {
                itemIconImage.sprite = null;
                itemIconImage.enabled = false;
                itemIconImage.gameObject.SetActive(false);
            }

            if (itemCountText != null)
            {
                itemCountText.text = "";
                itemCountText.enabled = false;
                itemCountText.gameObject.SetActive(false);
            }

            if (slotImage != null)
            {
                targetColor = originalSlotColor;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            string tooltipContent = GenerateTooltipContent(currentItem);
            tooltipSystem?.Show(tooltipContent);
            
            // Hover efekti
            if (slotImage != null)
            {
                Color hoverColor = GetSlotColorByRarity(currentItem.rarity);
                hoverColor.a = originalSlotColor.a; // Alpha değerini koru
                targetColor = hoverColor * hoverIntensity;
            }
        }
    }

    private string GenerateTooltipContent(ItemData item)
    {
        string rarityColor = item.GetRarityColorHex();
        string content = $"<color={rarityColor}><b>{item.itemName}</b></color>\n";
        
        // Item tipi ve nadirlik
        content += $"<color=#A0A0A0>{item.itemType} - {item.rarity}</color>\n\n";
        
        // Ana özellikler
        if (item.isEquippable)
        {
            if (item.itemType == ItemType.Weapon)
            {
                if (item.damage > 0) content += $"Damage: {item.damage}\n";
                if (item.attackSpeed > 0) content += $"Attack Speed: {item.attackSpeed}\n";
                if (item.criticalChance > 0) content += $"Critical Chance: {item.criticalChance}%\n";
                if (item.criticalMultiplier > 1) content += $"Critical Multiplier: x{item.criticalMultiplier}\n";
            }
            else if (item.itemType == ItemType.Armor)
            {
                if (item.defense > 0) content += $"Defense: {item.defense}\n";
                if (item.magicResistance > 0) content += $"Magic Resistance: {item.magicResistance}\n";
                if (item.weight > 0) content += $"Weight: {item.weight}\n";
            }
            
            // Durability
            if (item.durability > 0)
            {
                content += $"Durability: {item.durability}/{item.maxDurability}\n";
            }

            // Required Level
            if (item.requiredLevel > 0)
            {
                content += $"Required Level: {item.requiredLevel}\n";
            }
        }
        
        // Consumable özellikleri
        if (item.itemType == ItemType.Consumable)
        {
            if (item.healthRestore > 0) content += $"Restores {item.healthRestore} Health\n";
            if (item.manaRestore > 0) content += $"Restores {item.manaRestore} Mana\n";
            if (item.staminaRestore > 0) content += $"Restores {item.staminaRestore} Stamina\n";
            if (item.duration > 0) content += $"Duration: {item.duration} seconds\n";
        }

        // Stack bilgisi
        if (item.isStackable)
        {
            content += $"\nMax Stack: {item.maxStackSize}";
        }

        // Item açıklaması
        if (!string.IsNullOrEmpty(item.description))
        {
            content += $"\n\n<color=#A0A0A0>{item.description}</color>";
        }

        return content;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipSystem?.Hide();
        
        // Normal renge dön
        if (slotImage != null)
        {
            if (currentItem != null)
                targetColor = GetSlotColorByRarity(currentItem.rarity);
            else
                targetColor = originalSlotColor;
        }
    }

    private Color GetSlotColorByRarity(ItemRarity rarity)
    {
        Color color = originalSlotColor;
        
        switch (rarity)
        {
            case ItemRarity.Common:
                color = new Color(0.7f, 0.7f, 0.7f, originalSlotColor.a); // Beyaz
                break;
            case ItemRarity.Uncommon:
                color = new Color(0.12f, 1f, 0f, originalSlotColor.a); // Yeşil
                break;
            case ItemRarity.Rare:
                color = new Color(0f, 0.44f, 0.87f, originalSlotColor.a); // Mavi
                break;
            case ItemRarity.Epic:
                color = new Color(0.64f, 0.21f, 0.93f, originalSlotColor.a); // Mor
                break;
            case ItemRarity.Legendary:
                color = new Color(1f, 0.5f, 0f, originalSlotColor.a); // Turuncu
                break;
            case ItemRarity.Mythic:
                color = new Color(1f, 0f, 0.5f, originalSlotColor.a); // Pembe
                break;
        }
        
        return color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsEmpty())
        {
            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;

            // DraggableItem'ı ayarla
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.BeginDrag(this, currentItem, itemCount, eventData.position);
            }
            
            tooltipSystem?.Hide();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            InventoryManager.Instance.HandleDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;

            // DraggableItem'ı sonlandır
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.EndDrag();
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HandleDrop(this);
            InventoryManager.Instance.EndDrag(); // HandleDrop'tan sonra EndDrag'i çağır
        }
    }

    public bool HasItem()
    {
        return currentItem != null;
    }

    public void SetItemIconAlpha(float alpha)
    {
        if (itemIconImage != null)
        {
            Color color = itemIconImage.color;
            color.a = alpha;
            itemIconImage.color = color;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentItem != null)
        {
            ItemContextMenu.Instance.Show(this, eventData.position);
        }
    }
}
}
