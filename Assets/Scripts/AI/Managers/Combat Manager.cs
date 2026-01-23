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
    
    // UNIFIED CACHE - keyed by AssetReference RuntimeKey
    private Dictionary<string, AnimationClip> clipCache = new Dictionary<string, AnimationClip>();
    private Dictionary<string, Task<AnimationClip>> loadingTasks = new Dictionary<string, Task<AnimationClip>>();
    private Queue<string> clipLoadOrder = new Queue<string>();
    [SerializeField] private int clipCacheCapacity = 50; // Increased for basic attacks (3 categories × 8 = 24 + specials)

    [SerializeField] private BaseWeapon weaponEquipped;
    [SerializeField] private BaseWeapon shieldEquipped;

    protected bool inAttack = false;
    public bool InAttack => inAttack;

    // Animator parameters shared between players and enemies
    // 1H
    private const string anim_1H_Up_Placeholder = "anim_1H_Up_Placeholder";
    private const string anim_1H_Down_Placeholder = "anim_1H_Down_Placeholder";
    private const string anim_1H_Left_Placeholder = "anim_1H_Left_Placeholder";
    private const string anim_1H_Right_Placeholder = "anim_1H_Right_Placeholder";
    
    // 2H
    private const string anim_2H_Up_Placeholder = "anim_2H_Up_Placeholder";
    private const string anim_2H_Down_Placeholder = "anim_2H_Down_Placeholder";
    private const string anim_2H_Left_Placeholder = "anim_2H_Left_Placeholder";
    private const string anim_2H_Right_Placeholder = "anim_2H_Right_Placeholder";

    // Parry
    private const string anim_Parry_Up_Placeholder = "anim_Parry_Up_Placeholder";
    private const string anim_Parry_Down_Placeholder = "anim_Parry_Down_Placeholder";
    private const string anim_Parry_Left_Placeholder = "anim_Parry_Left_Placeholder";
    private const string anim_Parry_Right_Placeholder = "anim_Parry_Right_Placeholder";


    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        await AttackDatabase.Initialize();
        await LoadDefaultAttackAnimations();

        // Sync current state before subscribing
        if (nvChosenAttackId.Value != 0)
        {
            await LoadAndApplyAttack(nvChosenAttackId.Value);
        }

        nvChosenAttackId.OnValueChanged += OnAttackIDChanged;
    }
    
    /*
        This ensures that anytime an attack is changed, its animation clips will all be loaded into the cache
        which will allow the animations to be synced across the network. To make this possible, both the 
        AttackSO and the aniamtion clip need to be marked as addressable, and the AttackSO has to have the label
        "Attack Data" because that is the label the AttackDatabase uses to initialize itself
    */
    private async void OnAttackIDChanged(int previousID, int newID)
    {
        if (newID == 0) return;

        ChosenAttack = AttackDatabase.GetAttack(newID);
        await LoadAndApplyAttack(newID);
    }

    /// <summary>
    /// Unified loading function - handles both special and basic attacks
    /// </summary>
    protected async Task LoadAndApplyAttack(int attackID)
    {
        BaseAttackSO attack = AttackDatabase.GetAttack(attackID);
        if (attack == null)
        {
            Debug.LogError($"Attack {attackID} not found in database!");
            return;
        }

        // Route to appropriate loading method based on attack type
        if (attack is SpecialPlayerAttackSO || attack is SpecialEnemyAttackSO)
        {
            await LoadAndApplySpecialAttack(attack);
        }
        else if (attack is BasicPlayerAttackSO || attack is BasicEnemyAttackSO)
        {
            await LoadAndApplyBasicAttack(attack);
        }
        else
        {
            Debug.LogError($"Unknown attack type: {attack.GetType()}");
        }
    }

    /// <summary>
    /// Load single clip for special attacks
    /// </summary>
    private async Task LoadAndApplySpecialAttack(BaseAttackSO attack)
    {
        AssetReference clipRef = GetAssetReferenceFromAttack(attack);
        if (clipRef == null || !clipRef.RuntimeKeyIsValid())
        {
            Debug.LogError($"Invalid animation reference for attack {attack.UniqueID}");
            return;
        }

        AnimationClip clip = await LoadClip(clipRef);
        if (clip != null)
        {
            ApplySpecialAttackClipToAnimator(clip);
        }
    }

    /// <summary>
    /// Load 8 clips for basic attacks
    /// </summary>
    private async Task LoadAndApplyBasicAttack(BaseAttackSO attack)
    {
        WeaponCategorySO category = GetCategoryFromAttack(attack);
        if (category == null)
        {
            Debug.LogError($"No weapon category for basic attack {attack.UniqueID}");
            return;
        }

        // Load all 8 clips in parallel
        var loadTasks = new List<Task<AnimationClip>>
        {
            LoadClip(category.anim_1H_E),
            LoadClip(category.anim_1H_NE),
            LoadClip(category.anim_1H_NW),
            LoadClip(category.anim_1H_SE),
            LoadClip(category.anim_1H_SW),
            LoadClip(category.anim_1H_W),
            LoadClip(category.anim_2H_E),
            LoadClip(category.anim_2H_NE),
            LoadClip(category.anim_2H_NW),
            LoadClip(category.anim_2H_SE),
            LoadClip(category.anim_2H_SW),
            LoadClip(category.anim_2H_W),
            LoadClip(category.anim_Parry_E),
            LoadClip(category.anim_Parry_NE),
            LoadClip(category.anim_Parry_NW),
            LoadClip(category.anim_Parry_SE),
            LoadClip(category.anim_Parry_SW),
            LoadClip(category.anim_Parry_W),
        };
        
        AnimationClip[] clips = await Task.WhenAll(loadTasks);
        
        // Validate all clips loaded
        if (!ValidateClipArray(clips))
        {
            Debug.LogError("Failed to load all basic attack clips!");
            return;
        }
        
        ApplyBasicAttackClipsToAnimator(clips);
    }

    /// <summary>
    /// UNIFIED CLIP LOADING - handles caching, concurrent loading, eviction
    /// Works for both special and basic attacks
    /// </summary>
    private async Task<AnimationClip> LoadClip(AssetReference assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            return null;
        }

        string key = assetRef.RuntimeKey.ToString();

        // 1. Check cache
        if (clipCache.TryGetValue(key, out AnimationClip cachedClip))
        {
            return cachedClip;
        }

        // 2. Check if already loading
        if (loadingTasks.TryGetValue(key, out Task<AnimationClip> existingTask))
        {
            return await existingTask;
        }

        // 3. Start new load
        Task<AnimationClip> loadTask = DoLoadClip(assetRef, key);
        loadingTasks[key] = loadTask;

        return await loadTask;
    }

    /// <summary>
    /// Actually loads from Addressables and manages cache
    /// </summary>
    private async Task<AnimationClip> DoLoadClip(AssetReference assetRef, string key)
    {
        AsyncOperationHandle<AnimationClip> handle = Addressables.LoadAssetAsync<AnimationClip>(assetRef.RuntimeKey);
        
        AnimationClip clip;
        try
        {
            clip = await handle.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception loading clip {key}: {e.Message}");
            if (handle.IsValid()) Addressables.Release(handle);
            loadingTasks.Remove(key);
            return null;
        }

        loadingTasks.Remove(key);

        if (handle.Status == AsyncOperationStatus.Succeeded && clip != null)
        {
            // Evict if cache full
            if (clipCache.Count >= clipCacheCapacity)
            {
                EvictOldestClip();
            }

            // Add to cache
            if (!clipCache.ContainsKey(key))
            {
                clipCache[key] = clip;
                clipLoadOrder.Enqueue(key);
            }
            
            return clip;
        }
        else
        {
            Debug.LogError($"Failed to load clip {key}");
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
    }

    /// <summary>
    /// Evict oldest clip from unified cache
    /// </summary>
    private void EvictOldestClip()
    {
        if (clipLoadOrder.Count == 0) return;

        int originalCount = clipLoadOrder.Count;
        for (int i = 0; i < originalCount; i++)
        {
            string keyToEvict = clipLoadOrder.Dequeue();

            // Don't evict if currently loading
            if (loadingTasks.ContainsKey(keyToEvict))
            {
                clipLoadOrder.Enqueue(keyToEvict);
            }
            else
            {
                // Evict
                if (clipCache.TryGetValue(keyToEvict, out AnimationClip clipToRelease))
                {
                    Addressables.Release(clipToRelease);
                    clipCache.Remove(keyToEvict);
                }
                return;
            }
        }
        
        Debug.LogWarning("Cache full but all clips are loading!");
    }

    // ========== HELPER METHODS ==========

    private AssetReference GetAssetReferenceFromAttack(BaseAttackSO attack)
    {
        if (attack is SpecialEnemyAttackSO enemyAttack)
            return enemyAttack.AnimationClipRef;
        if (attack is SpecialPlayerAttackSO playerAttack)
            return playerAttack.AnimationClipRef;
        return null;
    }

    private WeaponCategorySO GetCategoryFromAttack(BaseAttackSO attack)
    {
        if (attack is BasicEnemyAttackSO enemyBasic)
            return enemyBasic.WeaponCategorySO;
        if (attack is BasicPlayerAttackSO playerBasic)
            return playerBasic.WeaponCategorySO;
        return null;
    }

    private bool ValidateClipArray(AnimationClip[] clips)
    {
        if (clips == null || clips.Length != 8)
            return false;
        
        foreach (var clip in clips)
        {
            if (clip == null)
                return false;
        }
        
        return true;
    }

    private void ApplyBasicAttackClipsToAnimator(AnimationClip[] clips)
    {
        _animOverrideController[anim_1H_Up_Placeholder] = clips[0];
        _animOverrideController[anim_1H_Down_Placeholder] = clips[1];
        _animOverrideController[anim_1H_Left_Placeholder] = clips[2];
        _animOverrideController[anim_1H_Right_Placeholder] = clips[3];
        _animOverrideController[anim_2H_Up_Placeholder] = clips[4];
        _animOverrideController[anim_2H_Down_Placeholder] = clips[5];
        _animOverrideController[anim_2H_Left_Placeholder] = clips[6];
        _animOverrideController[anim_2H_Right_Placeholder] = clips[7];
        _animOverrideController[anim_Parry_Up_Placeholder] = clips[8];
        _animOverrideController[anim_Parry_Down_Placeholder] = clips[9];
        _animOverrideController[anim_Parry_Left_Placeholder] = clips[10];
        _animOverrideController[anim_Parry_Right_Placeholder] = clips[11];
    }

    // ========== NETWORK & STATE ==========
    
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

    // ========== CLIP LOAD CHECK ==========
    // In CombatManager.cs
    protected bool IsAttackLoaded(BaseAttackSO attack)
    {
        if (attack == null) return false;
        
        if (attack is SpecialPlayerAttackSO || attack is SpecialEnemyAttackSO)
        {
            AssetReference clipRef = GetAssetReferenceFromAttack(attack);
            if (clipRef == null || !clipRef.RuntimeKeyIsValid()) return false;
            
            string key = clipRef.RuntimeKey.ToString();
            return clipCache.ContainsKey(key);
        }
        else if (attack is BasicPlayerAttackSO || attack is BasicEnemyAttackSO)
        {
            WeaponCategorySO category = GetCategoryFromAttack(attack);
            if (category == null) return false;
            
            // Just check first clip - if it's loaded, they all are (loaded together)
            string key = category.anim_1H_E.RuntimeKey.ToString();
            return clipCache.ContainsKey(key);
        }
        
        return false;
    }

    // ========== HITBOXES ==========
   
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

    public void AnimationEvent_EnableHitBoxes()
    {
        if (ChosenAttack == null || !IsServer) return;
        ChosenAttack.EnableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
    }

    public void AnimationEvent_DisableHitBoxes()
    {
        if (ChosenAttack == null || !IsServer) return;
        
        ChosenAttack.DisableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
        
        if (ChosenAttack.HitboxGroups.Count > 0)
        {
            currentHitboxGroupIndex = (currentHitboxGroupIndex + 1) % ChosenAttack.HitboxGroups.Count;
        }
    }

    public virtual void AnimationEvent_Trigger(int numEvent)
    {
        ChosenAttack?.OnAnimationEvent(numEvent, this);
    }

    public virtual void AnimationEvent_ComboTransfer()
    {
        // No implementation for enemies
    }

    public abstract void AnimationEvent_AttackFinished();

    // ========== ABSTRACT METHODS ==========
    
    protected abstract Task LoadDefaultAttackAnimations();
    protected abstract void ApplySpecialAttackClipToAnimator(AnimationClip clip);
}