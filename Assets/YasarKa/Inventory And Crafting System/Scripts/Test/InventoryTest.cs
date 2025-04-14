using UnityEngine;

namespace InventoryAndCrafting
{
    public class InventoryTest : MonoBehaviour
    {
        [SerializeField] private ItemData testItem;
        [SerializeField] private KeyCode addItemKey = KeyCode.Space;
        [SerializeField] private int amountToAdd = 1;

        private void Update()
        {
            if (Input.GetKeyDown(addItemKey))
            {
                bool added = InventoryManager.Instance.AddItem(testItem, amountToAdd);
                if (added)
                {
                    Debug.Log($"Added {amountToAdd}x {testItem.itemName} to inventory");
                }
                else
                {
                    Debug.Log("Inventory is full!");
                }
            }
        }
    }
}
