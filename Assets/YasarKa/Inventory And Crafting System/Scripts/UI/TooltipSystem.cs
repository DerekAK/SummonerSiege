using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventoryAndCrafting
{
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [SerializeField] private GameObject tooltipContainer;
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        private RectTransform rectTransform;
        private Canvas canvas;
        private float padding = 5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            rectTransform = tooltipContainer.GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        private void Update()
        {
            if (tooltipContainer.activeSelf)
            {
                Vector2 mousePosition = Input.mousePosition;
                Vector2 tooltipPosition = mousePosition;

                // Get tooltip dimensions
                float tooltipWidth = rectTransform.rect.width;
                float tooltipHeight = rectTransform.rect.height;

                // Show tooltip above if mouse is in lower half of screen
                if (mousePosition.y < Screen.height / 2)
                {
                    tooltipPosition.y += tooltipHeight + padding;
                }
                else
                {
                    tooltipPosition.y -= padding;
                }

                // Move tooltip to left if it doesn't fit on right side
                if (mousePosition.x + tooltipWidth + padding > Screen.width)
                {
                    tooltipPosition.x = mousePosition.x - tooltipWidth - padding;
                }
                else
                {
                    tooltipPosition.x += padding;
                }

                // Ensure tooltip stays within screen bounds
                tooltipPosition.x = Mathf.Clamp(tooltipPosition.x, padding, Screen.width - tooltipWidth - padding);
                tooltipPosition.y = Mathf.Clamp(tooltipPosition.y, tooltipHeight + padding, Screen.height - padding);

                tooltipContainer.transform.position = tooltipPosition;
            }
        }

        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            tooltipText.text = text;
            tooltipContainer.SetActive(true);
            
            // Adjust tooltip size based on content
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        public void Hide()
        {
            tooltipContainer.SetActive(false);
        }
    }
}
