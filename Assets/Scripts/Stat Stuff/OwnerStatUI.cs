using UnityEngine;
using Unity.Netcode; // Make sure this is included
using System.Collections.Generic;

// --- CHANGE: Inherit from NetworkBehaviour instead of MonoBehaviour ---
public class OwnerStatUI : NetworkBehaviour
{
    [Header("Target Entity")]
    [Tooltip("The Entity whose stats we want to display. This is typically on the parent object.")]
    private EntityStats _targetStats;

    [Header("UI Setup")]
    [SerializeField] private List<StatUIPair> statUIPairs;
    [SerializeField] private GameObject container;

    private void Awake()
    {
        container.SetActive(false);
        _targetStats = GetComponent<EntityStats>();
    }

    // --- CHANGE: Use OnNetworkSpawn() for initialization ---
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            return; 
        }

        container.SetActive(true);

        _targetStats.OnStatValueChanged += OnStatChanged;

        InitializeBars(); // Set initial values
    }

    private void InitializeBars()
    {
        foreach (StatUIPair pair in statUIPairs)
        {

            if (_targetStats.TryGetStat(pair.statToDisplay, out NetStat stat))
            {
                float fillAmount = stat.CurrentValue / stat.MaxValue;
                pair.uiFillBar.fillAmount = fillAmount;

                Vector3 fillVector3 = pair.uiFillBar.rectTransform.localScale;
                fillVector3.x = fillAmount;
                pair.uiFillBar.rectTransform.localScale = fillVector3;
            }
        }
    }

    private void OnStatChanged(StatType type, float newValue)
    {
        foreach (StatUIPair pair in statUIPairs)
        {
            if (_targetStats.TryGetStat(type, out NetStat stat) && pair.statToDisplay == type)
            {
                if (stat.MaxValue > 0)
                {
                    float fillAmount = stat.CurrentValue / stat.MaxValue;
                    pair.uiFillBar.fillAmount = fillAmount;

                    Vector3 fillVector3 = pair.uiFillBar.rectTransform.localScale;
                    fillVector3.x = fillAmount;
                    pair.uiFillBar.rectTransform.localScale = fillVector3;
                }
                else
                {
                    pair.uiFillBar.fillAmount = 0;

                    Vector3 fillVector3 = pair.uiFillBar.rectTransform.localScale;
                    fillVector3.x = 0;
                    pair.uiFillBar.rectTransform.localScale = fillVector3;
                }
            }
        }

    }
}