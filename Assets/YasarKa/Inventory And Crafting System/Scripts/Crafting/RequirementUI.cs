using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventoryAndCrafting
{
public class RequirementUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI amountText;
    private ItemData itemData;
    private int requiredAmount;
    private float updateInterval = 0.1f; // Her 100ms'de bir güncelle
    private float nextUpdateTime;

    private void Start()
    {
        nextUpdateTime = Time.time + updateInterval;
    }

    private void Update()
    {
        // Belirli aralıklarla güncelle
        if (Time.time >= nextUpdateTime)
        {
            UpdateAmount();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void OnEnable()
    {
        // Event'e abone ol
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged += UpdateAmount;
        }
        UpdateAmount(); // Enable olduğunda hemen güncelle
    }

    private void OnDisable()
    {
        // Event aboneliğini kaldır
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= UpdateAmount;
        }
    }

    public void SetData(ItemData item, int amount)
    {
        itemData = item;
        requiredAmount = amount;

        if (itemIcon != null && item != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.enabled = true;
        }

        UpdateAmount();
    }

    public void UpdateAmount()
    {
        if (itemData != null && amountText != null && InventoryManager.Instance != null)
        {
            int currentAmount = InventoryManager.Instance.GetItemCount(itemData);
            string colorTag = currentAmount >= requiredAmount ? "<color=green>" : "<color=red>";
            amountText.text = $"{colorTag}{currentAmount}</color>/{requiredAmount}";
        }
    }
}
}
