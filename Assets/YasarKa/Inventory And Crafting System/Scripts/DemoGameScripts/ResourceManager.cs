using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace InventoryAndCrafting
{
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResourceManager>();
                }
                return _instance;
            }
        }

        private Dictionary<ResourceNode, Coroutine> respawnTimers = new Dictionary<ResourceNode, Coroutine>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                
                // Eğer parent varsa, root'a taşı
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                
                DontDestroyOnLoad(gameObject);
                
                // Resource'ları bul ve kaydet
                FindAndRegisterResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartRespawnTimer(ResourceNode resource, float respawnTime)
        {
            if (!respawnTimers.ContainsKey(resource))
            {
                respawnTimers.Add(resource, StartCoroutine(RespawnTimer(resource, respawnTime)));
            }
        }

        private IEnumerator RespawnTimer(ResourceNode resource, float respawnTime)
        {
            yield return new WaitForSeconds(respawnTime);
            resource.gameObject.SetActive(true);
            respawnTimers.Remove(resource);
        }

        private void FindAndRegisterResources()
        {
            // Bu method henüz implement edilmemiş
        }
    }
}
