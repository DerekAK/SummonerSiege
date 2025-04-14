using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InventoryAndCrafting
{
    public class UIThemeController : MonoBehaviour
    {
        public static UIThemeController Instance { get; private set; }

        [System.Serializable]
        public class ThemeColors
        {
            public Color mainColor = Color.white;
            public float colorIntensity = 1f;

            public void OnValidate(UIThemeController controller, System.Action updateAction)
            {
                if (controller != null && updateAction != null)
                {
                    updateAction.Invoke();
                }
            }
        }

        [Header("Inventory Panel Theme")]
        [SerializeField] private Sprite selectedInventoryPanelTheme;
        public Sprite[] inventoryPanelThemes;
        [SerializeField] private ThemeColors _inventoryPanelColors = new ThemeColors();
        public ThemeColors inventoryPanelColors
        {
            get => _inventoryPanelColors;
            set
            {
                _inventoryPanelColors = value;
                UpdateInventoryPanels();
            }
        }

        [Header("Equipment Panel Theme")]
        [SerializeField] private Sprite selectedEquipmentPanelTheme;
        public Sprite[] equipmentPanelThemes;
        [SerializeField] private ThemeColors _equipmentPanelColors = new ThemeColors();
        public ThemeColors equipmentPanelColors
        {
            get => _equipmentPanelColors;
            set
            {
                _equipmentPanelColors = value;
                UpdateEquipmentPanels();
            }
        }

        [Header("Crafting Panel Theme")]
        [SerializeField] private Sprite selectedCraftingPanelTheme;
        public Sprite[] craftingPanelThemes;
        [SerializeField] private ThemeColors _craftingPanelColors = new ThemeColors();
        public ThemeColors craftingPanelColors
        {
            get => _craftingPanelColors;
            set
            {
                _craftingPanelColors = value;
                UpdateCraftingPanels();
            }
        }

        [Header("Other Panel Theme")]
        [SerializeField] private Sprite selectedOtherPanelTheme;
        public Sprite[] otherPanelThemes;
        [SerializeField] private ThemeColors _otherPanelColors = new ThemeColors();
        public ThemeColors otherPanelColors
        {
            get => _otherPanelColors;
            set
            {
                _otherPanelColors = value;
                UpdateOtherPanels();
            }
        }

        [Header("Slot Theme")]
        [SerializeField] private Sprite selectedSlotTheme;
        public Sprite[] slotThemes;
        [SerializeField] private ThemeColors _slotColors = new ThemeColors();
        public ThemeColors slotColors
        {
            get => _slotColors;
            set
            {
                _slotColors = value;
                UpdateAllSlots();
            }
        }

        [Header("Header Theme")]
        [SerializeField] private Sprite selectedHeaderTheme;
        public Sprite[] headerThemes;
        [SerializeField] private ThemeColors _headerColors = new ThemeColors();
        public ThemeColors headerColors
        {
            get => _headerColors;
            set
            {
                _headerColors = value;
                UpdateAllHeaders();
            }
        }

        [Header("Button Theme")]
        [SerializeField] private Sprite selectedButtonTheme;
        public Sprite[] buttonThemes;
        [SerializeField] private ThemeColors _buttonColors = new ThemeColors();
        public ThemeColors buttonColors
        {
            get => _buttonColors;
            set
            {
                _buttonColors = value;
                UpdateAllButtons();
            }
        }

        [Header("References")]
        [Header("Inventory Panels")]
        public Image[] inventoryPanels;
        [Header("Equipment Panels")]
        public Image[] equipmentPanels;
        [Header("Crafting Panels")]
        public Image[] craftingPanels;
        [Header("Other Panels")]
        public Image[] otherPanels;
        public Image[] allSlots;
        public Image[] allHeaders;
        public Image[] allButtons;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            UpdateAllThemes();
        }

        private void OnValidate()
        {
            UpdateAllThemes();
        }

        public void UpdateAllThemes()
        {
            UpdateInventoryPanels();
            UpdateEquipmentPanels();
            UpdateCraftingPanels();
            UpdateOtherPanels();
            UpdateAllSlots();
            UpdateAllHeaders();
            UpdateAllButtons();
        }

        private void UpdateInventoryPanels()
        {
            if (inventoryPanels != null && selectedInventoryPanelTheme != null)
            {
                foreach (var panel in inventoryPanels)
                {
                    if (panel != null)
                    {
                        panel.sprite = selectedInventoryPanelTheme;
                        panel.color = new Color(inventoryPanelColors.mainColor.r, inventoryPanelColors.mainColor.g, inventoryPanelColors.mainColor.b, inventoryPanelColors.mainColor.a * inventoryPanelColors.colorIntensity);
                    }
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
                #endif
            }
        }

        private void UpdateEquipmentPanels()
        {
            if (equipmentPanels != null && selectedEquipmentPanelTheme != null)
            {
                foreach (var panel in equipmentPanels)
                {
                    if (panel != null)
                    {
                        panel.sprite = selectedEquipmentPanelTheme;
                        panel.color = new Color(equipmentPanelColors.mainColor.r, equipmentPanelColors.mainColor.g, equipmentPanelColors.mainColor.b, equipmentPanelColors.mainColor.a * equipmentPanelColors.colorIntensity);
                    }
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
                #endif
            }
        }

        private void UpdateCraftingPanels()
        {
            if (craftingPanels != null && selectedCraftingPanelTheme != null)
            {
                foreach (var panel in craftingPanels)
                {
                    if (panel != null)
                    {
                        panel.sprite = selectedCraftingPanelTheme;
                        panel.color = new Color(craftingPanelColors.mainColor.r, craftingPanelColors.mainColor.g, craftingPanelColors.mainColor.b, craftingPanelColors.mainColor.a * craftingPanelColors.colorIntensity);
                    }
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
                #endif
            }
        }

        private void UpdateOtherPanels()
        {
            if (otherPanels != null && selectedOtherPanelTheme != null)
            {
                foreach (var panel in otherPanels)
                {
                    if (panel != null)
                    {
                        panel.sprite = selectedOtherPanelTheme;
                        panel.color = new Color(otherPanelColors.mainColor.r, otherPanelColors.mainColor.g, otherPanelColors.mainColor.b, otherPanelColors.mainColor.a * otherPanelColors.colorIntensity);
                    }
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
                #endif
            }
        }

        private void UpdateAllSlots()
        {
            if (allSlots != null && selectedSlotTheme != null)
            {
                foreach (Image slot in allSlots)
                {
                    if (slot != null)
                    {
                        slot.sprite = selectedSlotTheme;
                        slot.color = _slotColors.mainColor * _slotColors.colorIntensity;
                    }
                }
            }
        }

        private void UpdateAllHeaders()
        {
            if (allHeaders != null && selectedHeaderTheme != null)
            {
                foreach (Image header in allHeaders)
                {
                    if (header != null)
                    {
                        header.sprite = selectedHeaderTheme;
                        header.color = _headerColors.mainColor * _headerColors.colorIntensity;
                    }
                }
            }
        }

        private void UpdateAllButtons()
        {
            if (allButtons != null && selectedButtonTheme != null)
            {
                foreach (Image button in allButtons)
                {
                    if (button != null)
                    {
                        button.sprite = selectedButtonTheme;
                        button.color = _buttonColors.mainColor * _buttonColors.colorIntensity;
                    }
                }
            }
        }

        public void SetInventoryPanelTheme(Sprite theme)
        {
            if (selectedInventoryPanelTheme != theme)
            {
                selectedInventoryPanelTheme = theme;
                UpdateInventoryPanels();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        public void SetEquipmentPanelTheme(Sprite theme)
        {
            if (selectedEquipmentPanelTheme != theme)
            {
                selectedEquipmentPanelTheme = theme;
                UpdateEquipmentPanels();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        public void SetCraftingPanelTheme(Sprite theme)
        {
            if (selectedCraftingPanelTheme != theme)
            {
                selectedCraftingPanelTheme = theme;
                UpdateCraftingPanels();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        public void SetOtherPanelTheme(Sprite theme)
        {
            if (selectedOtherPanelTheme != theme)
            {
                selectedOtherPanelTheme = theme;
                UpdateOtherPanels();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        public void SetSlotTheme(Sprite newTheme)
        {
            selectedSlotTheme = newTheme;
            UpdateAllSlots();
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        public void SetHeaderTheme(Sprite newTheme)
        {
            selectedHeaderTheme = newTheme;
            UpdateAllHeaders();
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        public void SetButtonTheme(Sprite newTheme)
        {
            selectedButtonTheme = newTheme;
            UpdateAllButtons();
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        #if UNITY_EDITOR
        public void AddTheme(string arrayName, Sprite newTheme)
        {
            if (newTheme == null) return;
            
            Sprite[] themeArray = GetThemeArray(arrayName);
            if (themeArray == null) return;

            // Check if theme already exists
            for (int i = 0; i < themeArray.Length; i++)
            {
                if (themeArray[i] == newTheme) return;
            }
            
            // Create new array with increased size
            Sprite[] newArray = new Sprite[themeArray.Length + 1];
            themeArray.CopyTo(newArray, 0);
            newArray[themeArray.Length] = newTheme;
            
            SetThemeArray(arrayName, newArray);
            
            EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public void RemoveTheme(string arrayName, Sprite themeToRemove)
        {
            if (themeToRemove == null) return;
            
            Sprite[] themeArray = GetThemeArray(arrayName);
            if (themeArray == null) return;

            // Find the index of the theme to remove
            int removeIndex = -1;
            for (int i = 0; i < themeArray.Length; i++)
            {
                if (themeArray[i] == themeToRemove)
                {
                    removeIndex = i;
                    break;
                }
            }
            
            if (removeIndex == -1) return;
            
            // Create new array with decreased size
            Sprite[] newArray = new Sprite[themeArray.Length - 1];
            for (int i = 0, j = 0; i < themeArray.Length; i++)
            {
                if (i != removeIndex)
                {
                    newArray[j] = themeArray[i];
                    j++;
                }
            }
            
            SetThemeArray(arrayName, newArray);
            
            EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private Sprite[] GetThemeArray(string arrayName)
        {
            switch (arrayName)
            {
                case "inventoryPanelThemes":
                    return inventoryPanelThemes;
                case "equipmentPanelThemes":
                    return equipmentPanelThemes;
                case "craftingPanelThemes":
                    return craftingPanelThemes;
                case "otherPanelThemes":
                    return otherPanelThemes;
                case "slotThemes":
                    return slotThemes;
                case "headerThemes":
                    return headerThemes;
                case "buttonThemes":
                    return buttonThemes;
                default:
                    return null;
            }
        }

        private void SetThemeArray(string arrayName, Sprite[] newArray)
        {
            switch (arrayName)
            {
                case "inventoryPanelThemes":
                    inventoryPanelThemes = newArray;
                    break;
                case "equipmentPanelThemes":
                    equipmentPanelThemes = newArray;
                    break;
                case "craftingPanelThemes":
                    craftingPanelThemes = newArray;
                    break;
                case "otherPanelThemes":
                    otherPanelThemes = newArray;
                    break;
                case "slotThemes":
                    slotThemes = newArray;
                    break;
                case "headerThemes":
                    headerThemes = newArray;
                    break;
                case "buttonThemes":
                    buttonThemes = newArray;
                    break;
            }
        }
        #endif
    }
}
