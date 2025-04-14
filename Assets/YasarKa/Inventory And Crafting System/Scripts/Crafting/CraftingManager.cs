using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

namespace InventoryAndCrafting
{
[System.Serializable]
public class CraftingProgress
{
    public RecipeData recipe;
    public float progress;
    public int targetAmount;
}

public class CraftingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftingPanel craftingPanel;
    [SerializeField] private Transform requirementContainer;
    [SerializeField] private RequirementUI requirementPrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private List<RecipeData> availableRecipes = new List<RecipeData>();
    [SerializeField] private List<RecipeData> unlockedRecipes = new List<RecipeData>();
    [SerializeField] private List<CraftingProgress> activeRecipes = new List<CraftingProgress>();

    private RecipeData selectedRecipe;
    private bool isCrafting = false;
    private Coroutine craftingCoroutine;
    private List<RequirementUI> activeRequirements = new List<RequirementUI>();

    public UnityEvent<float> onCraftingProgressChanged = new UnityEvent<float>();
    public UnityEvent onCraftingComplete = new UnityEvent();

    public bool IsCrafting => isCrafting;

    private static CraftingManager instance;
    public static CraftingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CraftingManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged += UpdateCraftButtonState;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= UpdateCraftButtonState;
        }
    }

    private void UpdateCraftButtonState()
    {
        if (selectedRecipe != null && craftButton != null)
        {
            bool canCraft = CanCraftRecipe(selectedRecipe);
            craftButton.interactable = canCraft;
        }
    }

    public List<RecipeData> GetAvailableRecipes()
    {
        return availableRecipes;
    }

    public List<RecipeData> GetUnlockedRecipes()
    {
        return unlockedRecipes;
    }

    public List<CraftingProgress> GetActiveRecipes()
    {
        return activeRecipes;
    }

    public void SelectRecipe(RecipeData recipe)
    {
        // Eğer craft işlemi devam ediyorsa, yeni tarif seçimine izin verme
        if (isCrafting)
            return;

        selectedRecipe = recipe;
        craftingPanel.SelectRecipe(recipe);
        
        // Requirement UI'ları güncelle
        ClearRequirements();
        if (recipe != null)
        {
            foreach (var requirement in recipe.requirements)
            {
                CreateRequirementUI(requirement.item, requirement.amount);
            }
        }
        
        // Craft butonunu güncelle
        UpdateCraftButtonState();
    }

    private void ClearRequirements()
    {
        foreach (var req in activeRequirements)
        {
            if (req != null)
            {
                Destroy(req.gameObject);
            }
        }
        activeRequirements.Clear();
    }

    private void CreateRequirementUI(ItemData item, int amount)
    {
        if (requirementPrefab != null && requirementContainer != null)
        {
            RequirementUI reqUI = Instantiate(requirementPrefab, requirementContainer);
            reqUI.SetData(item, amount);
            activeRequirements.Add(reqUI);
        }
    }

    public bool CanCraftRecipe(RecipeData recipe, int amount = 1)
    {
        if (recipe == null) return false;

        foreach (var requirement in recipe.requirements)
        {
            int availableAmount = InventoryManager.Instance.GetItemCount(requirement.item);
            if (availableAmount < requirement.amount * amount)
                return false;
        }
        return true;
    }

    public void CraftItem(int amount = 1)
    {
        if (!CanCraftRecipe(selectedRecipe, amount) || isCrafting)
            return;

        // Craft işlemini başlat
        remainingCrafts = amount - 1; // İlk craft için -1
        isCrafting = true;
        currentProgress = 0f;
    }

    public void UnlockRecipe(RecipeData recipe)
    {
        if (!unlockedRecipes.Contains(recipe))
        {
            unlockedRecipes.Add(recipe);
        }
    }

    public void ResumeCrafting(RecipeData recipe, float progress, int targetAmount)
    {
        var craftingProgress = new CraftingProgress
        {
            recipe = recipe,
            progress = progress,
            targetAmount = targetAmount
        };
        activeRecipes.Add(craftingProgress);
    }

    private int remainingCrafts = 0;
    private float currentProgress = 0f;

    private void Update()
    {
        if (isCrafting && selectedRecipe != null)
        {
            currentProgress += Time.deltaTime;
            float progressPercent = currentProgress / selectedRecipe.craftingTime;
            onCraftingProgressChanged.Invoke(progressPercent);

            if (currentProgress >= selectedRecipe.craftingTime)
            {
                CompleteSingleCraft();
                
                // Eğer kalan craft varsa yeni bir craft başlat
                if (remainingCrafts > 0)
                {
                    currentProgress = 0f;
                    remainingCrafts--;
                }
                else
                {
                    // Tüm craftlar bitti
                    isCrafting = false;
                    currentProgress = 0f;
                    onCraftingComplete.Invoke();
                }
            }
        }
    }

    private void CompleteSingleCraft()
    {
        if (selectedRecipe == null) return;

        // Malzemeleri çıkar
        foreach (var requirement in selectedRecipe.requirements)
        {
            InventoryManager.Instance.RemoveItem(requirement.item, requirement.amount);
        }

        // Ürünü ekle
        InventoryManager.Instance.AddItem(selectedRecipe.resultItem, selectedRecipe.resultAmount);
    }

    public void FilterRecipes(CraftingCategory category)
    {
        // Önce mevcut recipe slot'ları temizle
        craftingPanel.ClearRecipeSlots();

        // Seçili kategori All ise veya recipe'nin kategorisi seçili kategori ile eşleşiyorsa göster
        foreach (var recipe in availableRecipes)
        {
            if (category == CraftingCategory.All || recipe.category == category)
            {
                craftingPanel.CreateRecipeSlot(recipe);
            }
        }
    }
}
}
