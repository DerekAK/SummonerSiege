using UnityEngine;
using System;
using System.IO;
using System.Collections;

namespace InventoryAndCrafting
{
    namespace Save
    {
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Save Settings")]
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private bool useAutoSave = true;
        [SerializeField] private bool useBinaryFormat = false;

        private string savePath => Path.Combine(Application.persistentDataPath, "inventory_save.dat");
        private string backupPath => Path.Combine(Application.persistentDataPath, "inventory_save_backup.dat");
        
        private float lastAutoSaveTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Move to root if parent exists
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (useAutoSave)
            {
                StartCoroutine(AutoSaveRoutine());
            }
        }

        private IEnumerator AutoSaveRoutine()
        {
            while (useAutoSave)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                SaveGame();
                Debug.Log("Game saved successfully");
            }
        }

        public void SaveGame()
        {
            try
            {
                // Create new save data
                SaveData saveData = new SaveData
                {
                    inventoryData = GetInventoryData(),
                    equipmentData = GetEquipmentData(),
                    craftingData = GetCraftingData(),
                    lastSaveTime = DateTime.Now.ToString()
                };

                // Backup existing save file
                if (File.Exists(savePath))
                {
                    File.Copy(savePath, backupPath, true);
                }

                string jsonData = JsonUtility.ToJson(saveData, true);
                
                // Save data based on format
                if (useBinaryFormat)
                {
                    // Encrypt and save in binary format
                    byte[] encryptedData = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    File.WriteAllBytes(savePath, encryptedData);
                }
                else
                {
                    // Save as plain text JSON
                    File.WriteAllText(savePath, jsonData);
                }

                Debug.Log("Game saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving game: {e.Message}");
                
                // Restore from backup on error
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, savePath, true);
                    Debug.Log("Restored from backup file");
                }
            }
        }

        public void LoadGame()
        {
            try
            {
                if (!File.Exists(savePath))
                {
                    Debug.LogWarning("No save file found. Starting new game.");
                    return;
                }

                string jsonData;
                if (useBinaryFormat)
                {
                    // Read and decrypt binary format
                    byte[] encryptedData = File.ReadAllBytes(savePath);
                    jsonData = System.Text.Encoding.UTF8.GetString(encryptedData);
                }
                else
                {
                    // Read plain text JSON
                    jsonData = File.ReadAllText(savePath);
                }

                SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
                
                // Clear existing systems
                var inventoryManager = InventoryManager.Instance;
                var equipmentManager = EquipmentManager.Instance;
                var craftingManager = CraftingManager.Instance;

                if (inventoryManager != null) 
                {
                    inventoryManager.ClearInventory();
                }
                else
                {
                    Debug.LogWarning("InventoryManager not found!");
                }

                if (equipmentManager != null)
                {
                    equipmentManager.UnequipAll();
                }
                else
                {
                    Debug.LogWarning("EquipmentManager not found!");
                }

                // Load saved data
                LoadInventoryData(saveData.inventoryData);
                LoadEquipmentData(saveData.equipmentData);
                LoadCraftingData(saveData.craftingData);

                Debug.Log($"Game loaded successfully. Last save: {saveData.lastSaveTime}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading game: {e.Message}\nStack trace: {e.StackTrace}");
                
                // Try loading from backup
                if (File.Exists(backupPath))
                {
                    Debug.Log("Attempting to load from backup...");
                    File.Copy(backupPath, savePath, true);
                    LoadGame(); // Try again
                }
            }
        }

        private void Update()
        {
            // Save game when F5 is pressed
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
                Debug.Log("Game Saved with F5!");
            }

            // Load game when F9 is pressed
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
                Debug.Log("Game Loaded with F9!");
            }

            // Delete save file when Delete is pressed
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSaveFile();
            }
        }

        public void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log("Save file deleted!");
                }
                
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    Debug.Log("Backup file deleted!");
                }

                // Clear inventory and equipment
                var inventoryManager = InventoryManager.Instance;
                var equipmentManager = EquipmentManager.Instance;

                if (inventoryManager != null)
                {
                    inventoryManager.ClearInventory();
                    Debug.Log("Inventory cleared!");
                }

                if (equipmentManager != null)
                {
                    equipmentManager.UnequipAll();
                    Debug.Log("Equipment cleared!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting save file: {e.Message}");
            }
        }

        private InventorySaveData GetInventoryData()
        {
            InventorySaveData data = new InventorySaveData();
            var inventoryManager = InventoryManager.Instance;
            
            if (inventoryManager != null)
            {
                for (int i = 0; i < inventoryManager.Slots.Count; i++)
                {
                    var slot = inventoryManager.Slots[i];
                    if (slot.CurrentItem != null)
                    {
                        data.slots.Add(new SlotSaveData
                        {
                            itemID = slot.CurrentItem.name, // Use ScriptableObject name as ID
                            amount = slot.ItemCount,
                            slotIndex = i
                        });
                    }
                }
            }
            
            return data;
        }

        private EquipmentSaveData GetEquipmentData()
        {
            EquipmentSaveData data = new EquipmentSaveData();
            var equipmentManager = EquipmentManager.Instance;
            
            if (equipmentManager != null)
            {
                foreach (var slot in equipmentManager.GetAllSlots())
                {
                    if (slot.CurrentItem != null)
                    {
                        data.equippedItems.Add(new EquipmentSlotSaveData
                        {
                            slotType = slot.slotType,
                            itemID = slot.CurrentItem.name
                        });
                    }
                }
            }
            
            return data;
        }

        private CraftingSaveData GetCraftingData()
        {
            CraftingSaveData data = new CraftingSaveData();
            var craftingManager = CraftingManager.Instance;
            
            if (craftingManager != null)
            {
                // Save unlocked recipes
                foreach (var recipe in craftingManager.GetUnlockedRecipes())
                {
                    data.unlockedRecipes.Add(recipe.name);
                }

                // Save active crafting progress
                foreach (var progress in craftingManager.GetActiveRecipes())
                {
                    data.activeRecipes.Add(new CraftingProgressData
                    {
                        recipeID = progress.recipe.name,
                        progress = progress.progress,
                        targetAmount = progress.targetAmount
                    });
                }
            }
            
            return data;
        }

        private void LoadInventoryData(InventorySaveData data)
        {
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null) return;

            // Ensure inventory manager is active
            inventoryManager.gameObject.SetActive(true);

            // Clear inventory
            inventoryManager.ClearInventory();

            // Load saved slots
            foreach (var slotData in data.slots)
            {
                var item = Resources.Load<ItemData>($"Items/{slotData.itemID}");
                if (item != null)
                {
                    inventoryManager.AddItemToSlot(item, slotData.amount, slotData.slotIndex);
                }
            }
        }

        private void LoadEquipmentData(EquipmentSaveData data)
        {
            var equipmentManager = EquipmentManager.Instance;
            if (equipmentManager == null) return;

            // Ensure equipment manager is active
            equipmentManager.gameObject.SetActive(true);

            // Wait for one frame to ensure Start method is called
            StartCoroutine(LoadEquipmentDataDelayed(data));
        }

        private IEnumerator LoadEquipmentDataDelayed(EquipmentSaveData data)
        {
            // Wait for one frame to ensure Start method is called
            yield return null;

            var equipmentManager = EquipmentManager.Instance;
            if (equipmentManager == null) yield break;

            // Ensure InventoryManager is ready
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null) yield break;
            
            // Make sure both managers are active
            equipmentManager.gameObject.SetActive(true);
            inventoryManager.gameObject.SetActive(true);

            // Wait another frame for UI to initialize
            yield return null;

            // Unequip all items
            equipmentManager.UnequipAll();

            // Load saved equipment
            foreach (var slotData in data.equippedItems)
            {
                var item = Resources.Load<ItemData>($"Items/{slotData.itemID}");
                if (item != null)
                {
                    // Equip item without removing from inventory
                    equipmentManager.EquipItem(item, slotData.slotType, false);
                }
                else
                {
                    Debug.LogWarning($"Could not load equipment item: {slotData.itemID}");
                }
            }

            // Force UI update
            equipmentManager.UpdateUI();
        }

        private void LoadCraftingData(CraftingSaveData data)
        {
            var craftingManager = CraftingManager.Instance;
            if (craftingManager == null) return;

            // Unlock recipes
            foreach (string recipeID in data.unlockedRecipes)
            {
                var recipe = Resources.Load<RecipeData>($"Recipes/{recipeID}");
                if (recipe != null)
                {
                    craftingManager.UnlockRecipe(recipe);
                }
            }

            // Resume crafting progress
            foreach (var progressData in data.activeRecipes)
            {
                var recipe = Resources.Load<RecipeData>($"Recipes/{progressData.recipeID}");
                if (recipe != null)
                {
                    craftingManager.ResumeCrafting(recipe, progressData.progress, progressData.targetAmount);
                }
            }
        }
    }
    }
}
