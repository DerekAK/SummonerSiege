using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
public class CraftingPanel : MonoBehaviour
{
    [Header("Recipe List")]
    [SerializeField] private Transform recipeListContent;
    [SerializeField] private RecipeSlot recipeSlotPrefab;
    
    [Header("Search")]
    [SerializeField] private TMP_InputField searchInputField;
    private List<RecipeSlot> allRecipeSlots = new List<RecipeSlot>();
    private string currentSearchText = "";

    [Header("Details Panel")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemName;
    [SerializeField] private Transform requirementsContent;
    [SerializeField] private RequirementUI requirementUIPrefab;
    
    [Header("Crafting")]
    [SerializeField] private Button craftButton;
    [SerializeField] private Image progressBar;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private TextMeshProUGUI craftAmountText;
    
    [Header("Category Buttons")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button weaponsButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button materialsButton;

    private List<RecipeSlot> recipeSlots = new List<RecipeSlot>();
    private List<RequirementUI> requirementUIs = new List<RequirementUI>();
    private RecipeData selectedRecipe;

    private int currentCraftAmount = 1;

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged += OnInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= OnInventoryChanged;
        }
    }

    private void Start()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
        
        InitializeCategoryButtons();
        
        // Craft butonunun keyboard trigger'ını devre dışı bırak
        if (craftButton != null)
        {
            Navigation nav = craftButton.navigation;
            nav.mode = Navigation.Mode.None;
            craftButton.navigation = nav;
        }

        // Craft amount butonlarını başlat
        if (increaseButton != null)
            increaseButton.onClick.AddListener(IncreaseCraftAmount);
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(DecreaseCraftAmount);
        
        craftButton.onClick.AddListener(OnCraftButtonClick);
        
        // Crafting event'lerini dinle
        CraftingManager.Instance.onCraftingProgressChanged.AddListener(UpdateProgressBar);
        CraftingManager.Instance.onCraftingComplete.AddListener(OnCraftingComplete);
        
        // Başlangıçta detaylar panelini gizle
        detailsPanel.SetActive(false);
        
        // Progress bar'ı sıfırla
        if (progressBar != null)
            progressBar.fillAmount = 0f;
        
        // Başlangıç değerini ayarla
        UpdateCraftAmountText();
        
        LoadRecipes();
    }

    private void InitializeCategoryButtons()
    {
        if (allButton != null)
            allButton.onClick.AddListener(() => FilterByCategory(CraftingCategory.All));
        if (weaponsButton != null)
            weaponsButton.onClick.AddListener(() => FilterByCategory(CraftingCategory.Weapons));
        if (armorButton != null)
            armorButton.onClick.AddListener(() => FilterByCategory(CraftingCategory.Armor));
        if (materialsButton != null)
            materialsButton.onClick.AddListener(() => FilterByCategory(CraftingCategory.Materials));
    }

    private void LoadRecipes()
    {
        // CraftingManager'dan recipe'leri al ve listele
        var recipes = CraftingManager.Instance.GetAvailableRecipes();
        foreach (var recipe in recipes)
        {
            CreateRecipeSlot(recipe);
        }
    }

    public void CreateRecipeSlot(RecipeData recipe)
    {
        if (recipeListContent == null)
        {
            Debug.LogError("Recipe List Content is null! Make sure it's assigned in the inspector.");
            return;
        }

        RecipeSlot slot = Instantiate(recipeSlotPrefab, recipeListContent);
        slot.SetData(recipe);
        recipeSlots.Add(slot);
        allRecipeSlots.Add(slot);
    }

    public void ClearRecipeSlots()
    {
        foreach (var slot in recipeSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        recipeSlots.Clear();
        allRecipeSlots.Clear();
    }

    public void SelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        
        if (recipe != null)
        {
            selectedItemIcon.sprite = recipe.resultItem.itemIcon;
            selectedItemName.text = recipe.resultItem.itemName;
            
            // Requirement UI'ları güncelle
            ClearRequirements();
            foreach (var requirement in recipe.requirements)
            {
                CreateRequirementUI(requirement.item, requirement.amount);
            }
            
            detailsPanel.SetActive(true);
            
            // Craft miktarını sıfırla ve maksimumu hesapla
            currentCraftAmount = 1;
            UpdateCraftAmountText();
            UpdateCraftButtonState();
        }
        else
        {
            detailsPanel.SetActive(false);
        }
    }

    private void UpdateCraftButtonState()
    {
        if (selectedRecipe != null && craftButton != null)
        {
            bool canCraft = CraftingManager.Instance.CanCraftRecipe(selectedRecipe, currentCraftAmount);
            craftButton.interactable = canCraft;
        }
    }

    private void OnCraftButtonClick()
    {
        if (selectedRecipe != null)
        {
            CraftingManager.Instance.CraftItem(currentCraftAmount);
        }
    }

    private void CreateRequirementUI(ItemData item, int amount)
    {
        RequirementUI ui = Instantiate(requirementUIPrefab, requirementsContent);
        ui.SetData(item, amount);
        requirementUIs.Add(ui);
    }

    public void ClearRequirements()
    {
        foreach (var ui in requirementUIs)
        {
            Destroy(ui.gameObject);
        }
        requirementUIs.Clear();
    }

    private void UpdateProgressBar(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
        }
    }

    private void OnCraftingComplete()
    {
        // Progress bar'ı sıfırla
        if (progressBar != null)
            progressBar.fillAmount = 0f;
        
        UpdateCraftButtonState();
        
        // Requirement'ları güncelle
        if (selectedRecipe != null)
        {
            foreach (var requirementUI in requirementUIs)
            {
                requirementUI.UpdateAmount();
            }
        }
    }

    public void UpdateProgress(float progress)
    {
        progressBar.fillAmount = progress;
    }

    private void FilterByCategory(CraftingCategory category)
    {
        CraftingManager.Instance.FilterRecipes(category);
    }

    private void OnInventoryChanged()
    {
        if (selectedRecipe != null)
        {
            // Requirement UI'ları güncelle
            foreach (var ui in requirementUIs)
            {
                ui.UpdateAmount();
            }
            
            // Craft butonunu güncelle
            UpdateCraftButtonState();
        }
    }

    private void IncreaseCraftAmount()
    {
        // Shift tuşuna basılıysa 10 artır, değilse 1 artır
        int increment = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 10 : 1;
        
        if (selectedRecipe != null)
        {
            // Maksimum craft miktarını hesapla
            int maxAmount = CalculateMaxCraftAmount();
            currentCraftAmount = Mathf.Min(currentCraftAmount + increment, maxAmount);
            UpdateCraftAmountText();
            UpdateCraftButtonState();
        }
    }

    private void DecreaseCraftAmount()
    {
        // Shift tuşuna basılıysa 10 azalt, değilse 1 azalt
        int decrement = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 10 : 1;
        
        currentCraftAmount = Mathf.Max(1, currentCraftAmount - decrement);
        UpdateCraftAmountText();
        UpdateCraftButtonState();
    }

    private void UpdateCraftAmountText()
    {
        if (craftAmountText != null)
        {
            craftAmountText.text = currentCraftAmount.ToString();
        }
    }

    private int CalculateMaxCraftAmount()
    {
        if (selectedRecipe == null) return 1;

        int maxAmount = int.MaxValue;
        foreach (var requirement in selectedRecipe.requirements)
        {
            int availableAmount = InventoryManager.Instance.GetItemCount(requirement.item);
            int possibleCrafts = availableAmount / requirement.amount;
            maxAmount = Mathf.Min(maxAmount, possibleCrafts);
        }
        
        return Mathf.Max(1, maxAmount);
    }

    private void OnSearchTextChanged(string searchText)
    {
        currentSearchText = searchText.ToLower();
        FilterRecipes();
    }

    private void FilterRecipes()
    {
        if (string.IsNullOrEmpty(currentSearchText))
        {
            // Arama metni boşsa tüm recipe'leri göster
            foreach (var recipeSlot in allRecipeSlots)
            {
                recipeSlot.gameObject.SetActive(true);
            }
        }
        else
        {
            // Arama metnine göre filtrele
            foreach (var recipeSlot in allRecipeSlots)
            {
                bool matchesSearch = recipeSlot.Recipe.resultItem.itemName.ToLower().Contains(currentSearchText) ||
                                   recipeSlot.Recipe.recipeName.ToLower().Contains(currentSearchText);
                recipeSlot.gameObject.SetActive(matchesSearch);
            }
        }
    }
}
}