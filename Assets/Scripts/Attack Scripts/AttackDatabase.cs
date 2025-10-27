using System.Collections.Generic;
using System.Threading.Tasks; // <-- Added
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AttackDatabase
{
    // The master lookup table: maps a UniqueID to its Attack Data
    public static Dictionary<int, BaseAttackSO> AllAttacks { get; private set; }
    
    private static bool isInitialized = false;
    private static Task initializationTask; // <-- Added to track the task

    public static async Task Initialize()
    {
        if (isInitialized) return;

        // If a task is already running, just wait for it to finish
        if (initializationTask != null)
        {
            await initializationTask;
            return;
        }

        // If no task is running, start one and store it
        initializationTask = DoInitialize();
        await initializationTask;
    }

    private static async Task DoInitialize()
    {
        AllAttacks = new Dictionary<int, BaseAttackSO>();

        // Load all ScriptableObjects labeled "AttackData"
        var handle = Addressables.LoadAssetsAsync<BaseAttackSO>("AttackData", (attackSO) =>
        {
            if (!AllAttacks.ContainsKey(attackSO.UniqueID))
            {
                AllAttacks.Add(attackSO.UniqueID, attackSO);
            }
            else
            {
                Debug.LogError($"Duplicate Attack ID: {attackSO.UniqueID}!");
            }
        });

        await handle.Task; // Wait for it to finish
        isInitialized = true;
        Debug.Log($"Attack Database initialized with {AllAttacks.Count} attacks.");
    }

    public static BaseAttackSO GetAttack(int id)
    {
        // Add a null check for safety, which fixes one of your errors
        if (AllAttacks == null)
        {
            Debug.LogError("AttackDatabase.GetAttack called before initialization!");
            return null;
        }
        
        AllAttacks.TryGetValue(id, out BaseAttackSO attack);
        return attack;
    }
}