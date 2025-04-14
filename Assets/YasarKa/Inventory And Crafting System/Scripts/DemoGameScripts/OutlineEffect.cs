using UnityEngine;

namespace InventoryAndCrafting
{
    [RequireComponent(typeof(Renderer))]
    public class OutlineEffect : MonoBehaviour
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private float outlineWidth = 0.02f;
        [SerializeField] private Color outlineColor = Color.yellow;

        private Renderer objectRenderer;
        private Material[] originalMaterials;
        private Material[] outlineMaterials;
        private bool isHighlighted = false;

        private void Awake()
        {
            objectRenderer = GetComponent<Renderer>();
            originalMaterials = objectRenderer.materials;
            
            // Outline materials dizisini hazırla
            outlineMaterials = new Material[originalMaterials.Length + 1];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                outlineMaterials[i] = originalMaterials[i];
            }
            
            // Outline material'ini oluştur
            outlineMaterials[originalMaterials.Length] = new Material(outlineMaterial);
            outlineMaterials[originalMaterials.Length].SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterials[originalMaterials.Length].SetColor("_OutlineColor", outlineColor);
        }

        public void ToggleOutline(bool enable)
        {
            if (enable == isHighlighted) return;
            
            isHighlighted = enable;
            
            if (enable)
            {
                objectRenderer.materials = outlineMaterials;
            }
            else
            {
                objectRenderer.materials = originalMaterials;
            }
        }

        private void OnDestroy()
        {
            // Oluşturulan outline material'ini temizle
            if (outlineMaterials != null && outlineMaterials.Length > originalMaterials.Length)
            {
                Destroy(outlineMaterials[originalMaterials.Length]);
            }
        }

        private void OnDisable()
        {
            // Object devre dışı kaldığında outline'ı kapat
            if (isHighlighted)
            {
                ToggleOutline(false);
            }
        }
    }
}
