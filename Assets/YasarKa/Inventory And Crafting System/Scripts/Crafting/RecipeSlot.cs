using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventoryAndCrafting
{
public class RecipeSlot : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;

    private RecipeData recipeData;
    private bool isSelected;

    private void Awake()
    {
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
        button.onClick.AddListener(OnClick);
    }

    public void SetData(RecipeData data)
    {
        recipeData = data;
        itemIcon.sprite = data.resultItem.itemIcon;
        itemNameText.text = data.recipeName;
        descriptionText.text = data.description;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        backgroundImage.color = isSelected ? 
            new Color(0.29f, 0.56f, 0.89f, 1f) : // #4A90E2
            new Color(0.2f, 0.2f, 0.2f, 1f);     // #333333
    }

    private void OnClick()
    {
        CraftingManager.Instance.SelectRecipe(recipeData);
    }

    public RecipeData Recipe => recipeData;
}
}
