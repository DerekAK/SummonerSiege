using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace InventoryAndCrafting
{
public class DraggableItem : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemCount;
    private RectTransform rectTransform;
    private Canvas mainCanvas;
    private CanvasGroup canvasGroup;
    private Camera mainCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;

        // Component referanslarını kontrol et
        if (itemIcon == null)
        {
            Debug.LogError("Item Icon reference is missing on DraggableItem!");
        }
        if (itemCount == null)
        {
            Debug.LogError("Item Count reference is missing on DraggableItem!");
        }
        
        // Başlangıçta gizli
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Her aktif olduğunda en üstte olduğundan emin ol
        transform.SetAsLastSibling();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void InitializeDragData(Sprite icon, int count, Vector2 screenPosition)
    {
        // Icon'u ayarla
        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = true;
        }

        // Sayıyı ayarla
        if (itemCount != null)
        {
            itemCount.text = count > 1 ? count.ToString() : "";
            itemCount.enabled = count > 1;
        }

        // Pozisyonu ayarla
        UpdatePosition(screenPosition);
        
        // Görünür yap
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        if (rectTransform != null && mainCanvas != null)
        {
            // Screen pozisyonunu canvas pozisyonuna çevir
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform,
                screenPosition,
                mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out pos);

            rectTransform.localPosition = pos;
        }
    }

    public void OnDrag(Vector2 screenPosition)
    {
        UpdatePosition(screenPosition);
    }

    public void EndDrag()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        gameObject.SetActive(false);
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemCount != null)
        {
            itemCount.text = "";
        }
    }
}
}
