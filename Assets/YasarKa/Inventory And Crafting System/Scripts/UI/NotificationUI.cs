using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace InventoryAndCrafting
{
public class NotificationUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float slideDistance = 50f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowNotification(Sprite icon, string message)
    {
        // Setup UI components
        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }
        
        if (messageText != null)
        {
            messageText.text = message;
        }

        // Set initial position
        targetPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = targetPosition + Vector2.left * slideDistance;
        
        // Start animation
        StartCoroutine(AnimateNotification());
    }

    private IEnumerator AnimateNotification()
    {
        // Fade in
        float elapsed = 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            float alpha = fadeCurve.Evaluate(progress);
            
            canvasGroup.alpha = alpha;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, alpha);
            
            yield return null;
        }

        // Display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = targetPosition + Vector2.right * slideDistance;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            float alpha = 1f - fadeCurve.Evaluate(progress);
            
            canvasGroup.alpha = alpha;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, fadeCurve.Evaluate(progress));
            
            yield return null;
        }

        // Destroy notification
        Destroy(gameObject);
    }
}
}
