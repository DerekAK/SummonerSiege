using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace InventoryAndCrafting
{
public class EquipmentSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Slot Components")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image slotImage; // HeadItem'ın Image componenti
    [SerializeField] private Image backgroundImage;
    public EquipmentSlotType slotType;

    [Header("Visual Effects")]
    [SerializeField] private float hoverIntensity = 1.2f;
    [SerializeField] private float hoverTransitionSpeed = 5f;
    [SerializeField] private Color emptySlotHoverColor = new Color(0.2f, 1f, 0.8f, 1f);
    private Color originalSlotColor;
    private Color targetColor;

    private ItemData currentItem;
    public ItemData CurrentItem { get => currentItem; private set => currentItem = value; }
    private TooltipSystem tooltipSystem;
    private bool isDragging = false;

    private void Awake()
    {
        if (slotImage != null)
        {
            originalSlotColor = slotImage.color;
            targetColor = originalSlotColor;
        }
    }

    private void Start()
    {
        tooltipSystem = FindObjectOfType<TooltipSystem>();
        
        // Only clear slot if there's no item
        if (CurrentItem == null)
        {
            ClearSlot();
        }
        else
        {
            // Re-equip the current item to ensure proper visualization
            EquipItem(CurrentItem);
        }
    }

    private void Update()
    {
        // Renk geçişini yumuşat
        if (slotImage != null && slotImage.color != targetColor)
        {
            slotImage.color = Color.Lerp(slotImage.color, targetColor, Time.deltaTime * hoverTransitionSpeed);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentItem != null)
        {
            isDragging = true;
            
            // InventoryManager üzerinden drag işlemini başlat
            InventoryManager.Instance.BeginDrag(null, CurrentItem, 1, eventData.position);
            
            // Item'ı yarı saydam yap
            var tempColor = itemIconImage.color;
            tempColor.a = 0.5f;
            itemIconImage.color = tempColor;
            
            tooltipSystem?.Hide();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            InventoryManager.Instance.OnDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            
            // Item'ın rengini normale döndür
            var tempColor = itemIconImage.color;
            tempColor.a = 1f;
            itemIconImage.color = tempColor;

            // InventoryManager üzerinden drag işlemini sonlandır
            InventoryManager.Instance.EndDrag();

            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                InventorySlot targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<InventorySlot>();
                EquipmentSlot targetEquipSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<EquipmentSlot>();

                if (targetSlot != null) // Inventory slot'una bırakıldıysa
                {
                    ItemData itemToMove = UnequipItem();
                    if (targetSlot.IsEmpty())
                    {
                        targetSlot.SetItem(itemToMove, 1);
                    }
                    else
                    {
                        InventoryManager.Instance.AddItem(itemToMove, 1);
                    }
                }
                else if (targetEquipSlot != null && targetEquipSlot != this) // Başka bir equipment slot'una bırakıldıysa
                {
                    if (targetEquipSlot.GetSlotType() == CurrentItem.equipmentSlotType)
                    {
                        // Hedef slot'ta item varsa swap yapalım
                        ItemData targetItem = targetEquipSlot.GetEquippedItem();
                        if (targetItem != null)
                        {
                            ItemData currentItemTemp = CurrentItem;
                            UnequipItem();
                            targetEquipSlot.UnequipItem();
                            EquipItem(targetItem);
                            targetEquipSlot.EquipItem(currentItemTemp);
                        }
                    }
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Tooltip göster
        if (CurrentItem != null)
        {
            tooltipSystem?.Show(GetSlotTooltip());
            
            // Hover efekti (item varsa rarity'ye göre)
            if (slotImage != null)
            {
                Color hoverColor = GetSlotColorByRarity(CurrentItem.rarity);
                hoverColor.a = originalSlotColor.a;
                targetColor = hoverColor * hoverIntensity;
            }
        }
        else
        {
            // Boş slot için hover efekti
            tooltipSystem?.Show(GetEmptySlotTooltip());
            if (slotImage != null)
            {
                targetColor = emptySlotHoverColor * hoverIntensity;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipSystem?.Hide();
        
        // Normal renge dön
        if (slotImage != null)
        {
            if (CurrentItem != null)
                targetColor = GetSlotColorByRarity(CurrentItem.rarity);
            else
                targetColor = originalSlotColor;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.HandleDrop(this);
        }
    }

    private string GetEmptySlotTooltip()
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                return "Head Slot\nEquip helmets and other headgear";
            case EquipmentSlotType.Chest:
                return "Chest Slot\nEquip armor and chest pieces";
            case EquipmentSlotType.Hands:
                return "Hands Slot\nEquip gloves and hand armor";
            case EquipmentSlotType.Feet:
                return "Feet Slot\nEquip boots and foot armor";
            case EquipmentSlotType.MainHand:
                return "Main Hand Slot\nEquip weapons and tools";
            case EquipmentSlotType.OffHand:
                return "Off Hand Slot\nEquip shields and off-hand items";
            default:
                return "Equipment Slot";
        }
    }

    private string GetSlotTooltip()
    {
        if (CurrentItem == null) return "";
        
        string tooltip = $"<color=#{ColorUtility.ToHtmlStringRGB(GetSlotColorByRarity(CurrentItem.rarity))}>{CurrentItem.itemName}</color>";
        tooltip += $"\n{CurrentItem.tooltipText}";
        
        // Ekstra equipment bilgileri
        if (CurrentItem.isEquippable)
        {
            tooltip += $"\n\nRequired Level: {CurrentItem.requiredLevel}";
            if (CurrentItem.damage > 0) tooltip += $"\nDamage: {CurrentItem.damage}";
            if (CurrentItem.defense > 0) tooltip += $"\nDefense: {CurrentItem.defense}";
            if (CurrentItem.durability > 0) tooltip += $"\nDurability: {CurrentItem.durability}/{CurrentItem.maxDurability}";
        }
        
        return tooltip;
    }

    private Color GetSlotColorByRarity(ItemRarity rarity)
    {
        Color color = originalSlotColor;
        
        switch (rarity)
        {
            case ItemRarity.Common:
                color = new Color(0.7f, 0.7f, 0.7f, originalSlotColor.a);
                break;
            case ItemRarity.Uncommon:
                color = new Color(0.12f, 1f, 0f, originalSlotColor.a);
                break;
            case ItemRarity.Rare:
                color = new Color(0f, 0.44f, 0.87f, originalSlotColor.a);
                break;
            case ItemRarity.Epic:
                color = new Color(0.64f, 0.21f, 0.93f, originalSlotColor.a);
                break;
            case ItemRarity.Legendary:
                color = new Color(1f, 0.5f, 0f, originalSlotColor.a);
                break;
            case ItemRarity.Mythic:
                color = new Color(1f, 0f, 0.5f, originalSlotColor.a);
                break;
        }
        
        return color;
    }

    public void EquipItem(ItemData item)
    {
        if (item != null && item.equipmentSlotType == slotType)
        {
            CurrentItem = item;
            itemIconImage.sprite = item.itemIcon;
            itemIconImage.enabled = true; // Enable the image component
            itemIconImage.color = new Color(1f, 1f, 1f, 1f); // Set full opacity

            // Hide background
            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
            }

            // Update slot color based on item rarity
            if (slotImage != null)
            {
                targetColor = GetSlotColorByRarity(item.rarity);
                slotImage.color = targetColor;
            }
        }
    }

    public ItemData UnequipItem()
    {
        var item = CurrentItem;
        ClearSlot(); // Önce slot'u temizle
        return item;
    }

    public void ClearSlot()
    {
        CurrentItem = null;
        itemIconImage.sprite = null;
        itemIconImage.enabled = false;
        
        // Background'ı göster
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
        }
        
        // Slot rengini sıfırla
        if (slotImage != null)
        {
            targetColor = originalSlotColor;
        }
    }

    public bool IsEmpty()
    {
        return CurrentItem == null;
    }

    public ItemData GetEquippedItem()
    {
        return CurrentItem;
    }

    public EquipmentSlotType GetSlotType()
    {
        return slotType;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Sağ tık kontrolü
        if (eventData.button == PointerEventData.InputButton.Right && CurrentItem != null)
        {
            ItemContextMenu.Instance.ShowForEquipment(this, eventData.position);
        }
    }
}
}
