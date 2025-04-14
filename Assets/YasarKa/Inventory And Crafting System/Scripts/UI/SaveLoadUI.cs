using UnityEngine;
using UnityEngine.UI;
using InventoryAndCrafting.Save;

namespace InventoryAndCrafting
{
    public class SaveLoadUI : MonoBehaviour
    {
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;

        private void Start()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);
                
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClicked);
        }

        public void OnSaveButtonClicked()
        {
            SaveManager.Instance.SaveGame();
        }

        public void OnLoadButtonClicked()
        {
            SaveManager.Instance.LoadGame();
        }
    }
}
