using UnityEngine;
using UnityEngine.UI;

namespace InventoryAndCrafting
{
    public class ScrollRectMouseScroll : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 10f;
        private ScrollRect scrollRect;

        private void Start()
        {
            // Disable drag functionality and use only scroll wheel
            scrollRect = GetComponent<ScrollRect>();
            scrollRect.vertical = false;   // Disable vertical drag
            scrollRect.horizontal = false; // Disable horizontal drag
        }

        private void Update()
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0 && RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)transform, Input.mousePosition))
            {
                Vector2 scrollPosition = scrollRect.normalizedPosition;
                scrollPosition.y += scrollDelta * scrollSpeed * Time.deltaTime;
                scrollPosition.y = Mathf.Clamp01(scrollPosition.y);
                scrollRect.normalizedPosition = scrollPosition;
            }
        }
    }
}
