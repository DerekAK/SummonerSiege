using UnityEngine;
using System;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
    [Serializable]
    public class SaveData
    {
        public InventorySaveData inventoryData;
        public EquipmentSaveData equipmentData;
        public CraftingSaveData craftingData;
        public string lastSaveTime;

        public SaveData()
        {
            inventoryData = new InventorySaveData();
            equipmentData = new EquipmentSaveData();
            craftingData = new CraftingSaveData();
            lastSaveTime = DateTime.Now.ToString();
        }
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<SlotSaveData> slots = new List<SlotSaveData>();
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public List<EquipmentSlotSaveData> equippedItems = new List<EquipmentSlotSaveData>();
    }

    [Serializable]
    public class EquipmentSlotSaveData
    {
        public EquipmentSlotType slotType;
        public string itemID;
    }

    [Serializable]
    public class CraftingSaveData
    {
        public List<string> unlockedRecipes = new List<string>();
        public List<CraftingProgressData> activeRecipes = new List<CraftingProgressData>();
    }

    [Serializable]
    public class SlotSaveData
    {
        public string itemID;
        public int amount;
        public int slotIndex;
    }

    [Serializable]
    public class CraftingProgressData
    {
        public string recipeID;
        public float progress;
        public int targetAmount;
    }
}
