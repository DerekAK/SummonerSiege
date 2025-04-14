using UnityEngine;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NotificationUI notificationPrefab;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private int maxNotifications = 3;

    private Queue<NotificationUI> activeNotifications = new Queue<NotificationUI>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Eğer parent varsa, root'a taşı
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

    public void ShowItemNotification(ItemData item, int amount)
    {
        if (item == null) return;

        string message = amount > 1 
            ? $"Gained {amount}x {item.itemName}" 
            : $"Gained {item.itemName}";

        ShowNotification(item.itemIcon, message);
    }

    public void ShowNotification(Sprite icon, string message)
    {
        // Eğer container yoksa, ana canvas'ta ara
        if (notificationContainer == null)
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                notificationContainer = mainCanvas.transform;
            }
            else
            {
                Debug.LogError("No canvas found for notifications!");
                return;
            }
        }

        // Maksimum bildirim sayısını kontrol et
        while (activeNotifications.Count >= maxNotifications)
        {
            var oldNotification = activeNotifications.Dequeue();
            if (oldNotification != null)
            {
                Destroy(oldNotification.gameObject);
            }
        }

        // Yeni bildirimi oluştur
        NotificationUI notification = Instantiate(notificationPrefab, notificationContainer);
        activeNotifications.Enqueue(notification);
        
        // Bildirimi göster
        notification.ShowNotification(icon, message);
    }
}
}
