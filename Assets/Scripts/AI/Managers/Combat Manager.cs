using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

public abstract class CombatManager: NetworkBehaviour
{   
    [HideInInspector] public BaseAttackSO ChosenAttack { get; protected set; }
    protected Dictionary<BaseAttackSO.eBodyPart, DamageCollider> damageColliderDict = new();
    protected Animator _anim;
    protected AnimatorOverrideController _animOverrideController;
    private NetworkVariable<int> nvChosenAttackId = new NetworkVariable<int>(0);
    
    // --- FIX: Updated Caching System ---
    
    // Cache for clips that are fully loaded
    protected Dictionary<int, AnimationClip> loadedClips = new Dictionary<int, AnimationClip>();
    
    // Tracks the *task* of clips currently being loaded to prevent duplicate loads
    private Dictionary<int, Task<AnimationClip>> loadingTasks = new Dictionary<int, Task<AnimationClip>>();
    
    // Used to manage cache eviction (FIFO)
    private Queue<int> clipLoadOrder = new Queue<int>();
    [SerializeField] private int cacheCapacity = 10;
    
    // --- End Fix ---


    protected virtual void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim.runtimeAnimatorController != null)
        {
            _animOverrideController = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _animOverrideController;
        }
        else
        {
            Debug.LogError($"Animator on {gameObject.name} does not have a Runtime Animator Controller!");
        }
    }

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. Load ALL data first.
        await AttackDatabase.Initialize();
        await LoadDefaultAttackAnimations();

        // 2. NOW, sync the *current* state from the network.
        // This must run BEFORE subscribing to OnValueChanged.
        if (nvChosenAttackId.Value != 0)
        {
            // Use the new robust loading function
            await LoadAndApplyClip(nvChosenAttackId.Value);
        }

        // 3. NOW that we are fully synced, subscribe to *future* changes.
        nvChosenAttackId.OnValueChanged += OnAttackIDChanged;
    }
    
    private async void OnAttackIDChanged(int previousID, int newID)
    {
        // This just wraps our new safe loading function
        await LoadAndApplyClip(newID);
    }

    /// <summary>
    /// Safely loads a clip (from cache or Addressables) and applies it to the animator.
    /// This function is now fully protected from race conditions.
    /// </summary>
    private async Task LoadAndApplyClip(int attackID)
    {
        if (attackID == 0) return;

        // LoadClipFromReference now returns the clip and handles all
        // concurrent loading logic internally.
        AnimationClip clip = await LoadClipFromReference(attackID);
            
        if (clip != null)
        {
            ApplyClipToAnimator(clip);
        }
        else
        {
            Debug.LogWarning($"Failed to load or apply clip for attack ID {attackID}");
        }
    }

    // --- FIX: This is the new, robust loading function ---
    /// <summary>
    /// Gets an AnimationClip, either from the cache or by loading it from Addressables.
    /// This is safe to call multiple times for the same ID.
    /// </summary>
    /// <returns>The loaded AnimationClip, or null if loading failed.</returns>
    protected Task<AnimationClip> LoadClipFromReference(int attackID)
    {
        // 1. Check if it's already loaded
        if (loadedClips.TryGetValue(attackID, out AnimationClip clip))
        {
            return Task.FromResult(clip);
        }

        // 2. Check if it's *already being loaded*
        if (loadingTasks.TryGetValue(attackID, out Task<AnimationClip> existingTask))
        {
            // It is! Just return that existing task.
            return existingTask; 
        }

        // 3. It's not loaded and not loading. We must load it.
        // We call a new helper function and store the Task
        Task<AnimationClip> loadTask = DoLoadClip(attackID);
        loadingTasks.Add(attackID, loadTask);

        return loadTask;
    }

    /// <summary>
    /// The internal helper that *actually* loads from Addressables.
    /// This is only called once per clip by LoadClipFromReference.
    /// </summary>
    private async Task<AnimationClip> DoLoadClip(int attackID)
    {
        BaseAttackSO attackData = AttackDatabase.GetAttack(attackID);
        Debug.Log($"attackData is null: {attackData == null}");
        Debug.Log($"attackData.AnimationClipRef is valid: {attackData.AnimationClipRef.IsValid()}");
        if (attackData == null || !attackData.AnimationClipRef.IsValid())
        {
            Debug.LogWarning($"No attack data or invalid AssetRef for ID {attackID}");
            loadingTasks.Remove(attackID); // Remove from loading list
            return null;
        }

        AsyncOperationHandle<AnimationClip> handle = attackData.AnimationClipRef.LoadAssetAsync<AnimationClip>();
        AnimationClip clip = null;
        try
        {
            clip = await handle.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception awaiting handle for {attackID}: {e.Message}");
            // Ensure we release the handle if awaiting it fails
            if(handle.IsValid()) Addressables.Release(handle);
            loadingTasks.Remove(attackID);
            return null;
        }

        // Task is complete, remove it from the loading dictionary
        loadingTasks.Remove(attackID);

        if (handle.Status == AsyncOperationStatus.Succeeded && clip != null)
        {
            // Success! Manage cache size *before* adding.
            if (loadedClips.Count >= cacheCapacity)
            {
                EvictOldestClip(); 
            }

            if (!loadedClips.ContainsKey(attackID)) // Check again just in case
            {
                 loadedClips.Add(attackID, clip);
                 clipLoadOrder.Enqueue(attackID);
            }
            
            return clip;
        }
        else
        {
            // Load failed
            Debug.LogError($"Failed to load AnimationClip for attack ID {attackID}.");
            if(handle.IsValid()) Addressables.Release(handle);
            return null;
        }
    }
    
    // --- FIX: Updated eviction logic ---
    // Uses a loop instead of recursion to prevent StackOverflow
    // and correctly checks the new 'loadingTasks' dictionary.
    private void EvictOldestClip()
    {
        if (clipLoadOrder.Count == 0) return;

        int originalCount = clipLoadOrder.Count;
        for (int i = 0; i < originalCount; i++)
        {
            int idToEvict = clipLoadOrder.Dequeue();

            // Check if this clip is in the middle of being loaded.
            if (loadingTasks.ContainsKey(idToEvict))
            {
                // If so, don't evict it. Put it back at the end of the line
                // and try the next one.
                clipLoadOrder.Enqueue(idToEvict); 
            }
            else
            {
                // Found one we can evict.
                if (loadedClips.TryGetValue(idToEvict, out AnimationClip clipToRelease))
                {
                    Addressables.Release(clipToRelease);
                    loadedClips.Remove(idToEvict);
                }
                return; // Eviction complete.
            }
        }
        
        // If we get here, all clips in the queue are currently loading.
        Debug.LogWarning("Cache is full, but all items are being loaded. Cannot evict.");
    }
    
    public void SetChosenAttack(BaseAttackSO newAttack)
    {
        int newId = (newAttack != null) ? newAttack.UniqueID : 0;
        ChosenAttack = newAttack;

        if (IsServer)
        {
            nvChosenAttackId.Value = newId;
        }
        else if (IsOwner)
        {
            ChangeChosenAttackIdServerRpc(newId);
        }
    }

    [ServerRpc]
    private void ChangeChosenAttackIdServerRpc(int newId)
    {
        nvChosenAttackId.Value = newId;
    }

    // --- No changes to the functions below this line ---

    protected abstract Task LoadDefaultAttackAnimations();

    protected abstract void ApplyClipToAnimator(AnimationClip clip);
   
    private int currentHitboxGroupIndex = 0;

    public void RegisterDamageCollider(DamageCollider damageCollider)
    {
        if (!damageColliderDict.ContainsKey(damageCollider.BodyPart))
        {
            damageColliderDict[damageCollider.BodyPart] = damageCollider;
        }
        else
        {
            Debug.LogError($"{damageCollider.transform.root.name} contains multiple damage colliders with {damageCollider.BodyPart} bodypart!");
        }
    }

    protected void ResetHitboxIndex()
    {
        currentHitboxGroupIndex = 0;
    }

    protected void AnimationEvent_EnableHitBoxes()
    {
        if (ChosenAttack == null || (!IsServer && !IsOwner)) return;
 
        ChosenAttack.EnableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
    }

    protected void AnimationEvent_DisableHitBoxes()
    {
        if (ChosenAttack == null || (!IsServer && !IsOwner)) return;
        
        ChosenAttack.DisableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
        
        if (ChosenAttack.HitboxGroups.Count > 0)
        {
            currentHitboxGroupIndex = (currentHitboxGroupIndex + 1) % ChosenAttack.HitboxGroups.Count;
        }
    }

    protected virtual void AnimationEvent_Trigger(int numEvent)
    {
        ChosenAttack?.OnAnimationEvent(numEvent, this);
    }

    protected virtual void AnimationEvent_ComboTransfer()
    {
        // no implementation, keep it this way for ai.
    }

    protected abstract void AnimationEvent_AttackFinished();
}