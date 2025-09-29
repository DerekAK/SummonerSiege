using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private GameObject pfTmpSword;
    private Animator _anim;
    private PlayerState _playerState;
    private PlayerClipsHandler _playerClipsHandler;
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private int abortAttack = Animator.StringToHash("AbortAttack");
    private float inputHoldTime = 0f;
    private float longAttackThreshold = 0.2f;
    private Coroutine inputHoldCoroutine;
    private bool isAttacking = false;
    private bool checkingForInputRelease = false;
    private ComboSystem.Combo currentCombo;
    private BaseAttackSO currentAttackSO;
    private Coroutine comboCoroutine;
    private Coroutine attackCoroutine;
    private bool comboTransition = false;
    private bool inCombo = false;
    private bool inHoldCombo = false;
    private int currAnimAttackState = 0;
    [HideInInspector] public List<BaseWeapon> EquippedWeapons = new List<BaseWeapon>();
    public static string HitboxTag = "Hitbox";
    public static string AttachPointTag = "AttachPoint";

    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    [Header("Attack Data")]
    public BaseAttackSO[] attackSOList;
    [HideInInspector] public NetworkVariable<int> nvAttackSOIndex = new NetworkVariable<int>();
    [SerializeField] private List<ComboSystem.Combo> defaultCombos = new List<ComboSystem.Combo>();
    private List<ComboSystem.Combo> possibleCombos = new List<ComboSystem.Combo>();

    private bool isInitialized = false;

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerState = GetComponent<PlayerState>();
        _playerClipsHandler = GetComponent<PlayerClipsHandler>();
    }
    
    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            Initialize();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Initialize();
        }
    }
    
    private void Initialize()
    {
        GameInput.Instance.OnAttackButtonStarted += AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled += AttackButtonCanceled;

        foreach (ComboSystem.Combo combo in defaultCombos){
            possibleCombos.Add(combo);
            combo.Initialize();
        }
        isInitialized = true;
    }

    private void Shutdown()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnAttackButtonStarted -= AttackButtonStart;
            GameInput.Instance.OnAttackButtonCanceled -= AttackButtonCanceled;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Shutdown();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient))
        {
            Shutdown();
        }
    }
    
    private void Update(){
        if(!isInitialized) return;
        
        if(Input.GetKeyDown(KeyCode.P))
        {
            // This is debug code, but now it's safe for single-player.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                WeaponSpawnServerRpc();
            }
            else
            {
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                {
                    NonNetworkedWeaponSpawn();
                }
            }
        }
    }

    private void PerformAttack(){
        isAttacking = true;
        _playerState.currentAttack = currentAttackSO;
        _playerState.ChangeAttackStatus(true);
        
        int attackIndex = GetIndexByAttackSO(currentAttackSO);
        
        // Safely update network variables and call RPCs
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            ChangeAttackSOIndexServerRpc(attackIndex);
            _playerClipsHandler.ChangeOverriderClipsServerRpc(attackIndex);
        }
        else
        {
            // In single-player, we just update the local value and animator directly
            nvAttackSOIndex.Value = attackIndex;
            // The ClientRpc in PlayerClipsHandler needs to be called directly
             _playerClipsHandler.ExecuteClipChange(attackIndex);
        }

        if (currAnimAttackState == 0) _anim.SetTrigger(attackParamA);
        else _anim.SetTrigger(attackParamB);
        currAnimAttackState = (currAnimAttackState + 1) % 2;
    }

    private void NonNetworkedWeaponSpawn()
    {
        
    }

    [ServerRpc]
    private void WeaponSpawnServerRpc()
    {
        //Debug.Log(NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(pfTmpSword));
        NetworkObject netWeaponObj = ObjectPoolManager.Singleton.GetObject(pfTmpSword, transform.position, transform.rotation).GetComponent<NetworkObject>();
        netWeaponObj.Spawn(true);
        EquippedWeapons.Add(netWeaponObj.GetComponent<BaseWeapon>());
        FollowTargetClientRpc(netWeaponObj);
        netWeaponObj.Despawn();
        
    }

    [ClientRpc]
    private void FollowTargetClientRpc(NetworkObjectReference networkObjectReference){
        networkObjectReference.TryGet(out NetworkObject networkObjectWeapon);
        
        Transform attachPoint = null;
        foreach(Transform child in _anim.GetBoneTransform(networkObjectWeapon.GetComponent<BaseWeapon>().AttachedBone)){
            if(child.CompareTag(AttachPointTag)){
                attachPoint = child;
            }
        }
        if(attachPoint == null){Debug.LogError("Attachpoint is NULL!");}

        StartCoroutine(networkObjectWeapon.GetComponent<FollowTarget>().
            FollowTargetCoroutine(attachPoint));
    }
    
    private void AttackButtonStart(GameInput.AttackInput inputButton){        
        if (inHoldCombo) return;

        if (!isAttacking || inCombo){
            checkingForInputRelease = true;
            if(inputHoldCoroutine != null) StopCoroutine(inputHoldCoroutine);
            inputHoldCoroutine = StartCoroutine(HandleInputHeldTime(inputButton));
        }
    }

    private IEnumerator HandleInputHeldTime(GameInput.AttackInput inputButton){
        currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Hold);
        if (currentAttackSO){
            inHoldCombo = true;
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(HandlePerformAttack());
            yield break;
        }

        inputHoldTime = 0;

        while (GameInput.Instance.IsAttackButtonPressed(inputButton)){
            inputHoldTime += Time.deltaTime;
            if (inputHoldTime > longAttackThreshold){
                currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Long);
                if (currentAttackSO){
                    if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                    checkingForInputRelease = false;
                }
                yield break;
            }
            yield return null;
        }
    }

    private void AttackButtonCanceled(GameInput.AttackInput inputButton){
        if (!checkingForInputRelease) return;
        checkingForInputRelease = false;
        if (inputHoldCoroutine != null) StopCoroutine(inputHoldCoroutine);

        if (inHoldCombo){
            if (currentAttackSO?.Holdable == true){
                currentAttackSO = DecideCurrentAttackSO(GameInput.AttackInput.None, ComboSystem.AttackPressType.Hold);
                if (currentAttackSO){
                    if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                }
            }
            else{
                _anim.SetTrigger(abortAttack);
                AttackFinish();

            }
            inHoldCombo = false;
            currentCombo?.ResetComboStep();
            return;
        }

        if (inputHoldTime < longAttackThreshold){
            currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Quick);
            if (currentAttackSO){ 
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(HandlePerformAttack());
            }
        }
    }
    
    private BaseAttackSO DecideCurrentAttackSO(GameInput.AttackInput inputButton, ComboSystem.AttackPressType pressType){
        if (inCombo && currentCombo.currComboStep.userPressType == pressType && currentCombo.currComboStep.userInput == inputButton){
            return currentCombo.currComboStep.attack;
        }
        else if (inCombo){
            return null;
        }
        else{
            if (!isAttacking){
                foreach (ComboSystem.Combo combo in possibleCombos){
                    ComboSystem.ComboStep firstComboStep = combo.comboSteps[0];
                    if (firstComboStep.userInput == inputButton && firstComboStep.userPressType == pressType){
                        currentCombo = combo;
                        return firstComboStep.attack;
                    }
                }
                return null;
            }
            else{
                return null;
            }
        }
    }

    private IEnumerator HandlePerformAttack(){
        if (((currentAttackSO.AirAttack && _playerState.InAir) || (!currentAttackSO.AirAttack && !_playerState.InAir)) && !_playerState.Rolling){
            if (inCombo && !inHoldCombo){
                while (!comboTransition){
                    yield return null;
                }
            }
            PerformAttack();
            comboTransition = false;
            currentCombo.UpdateComboStep();
            inCombo = currentCombo.currIndex > 0;
            if (inCombo){
                if (comboCoroutine != null) StopCoroutine(comboCoroutine);
                comboCoroutine = StartCoroutine(TrackComboWindow());
            }
        }
    }

    [ServerRpc]
    private void ChangeAttackSOIndexServerRpc(int newIndex){
        nvAttackSOIndex.Value = newIndex;
    }

    private IEnumerator TrackComboWindow(){
        if(inHoldCombo) yield break;
        float elapsedTime = 0f;
        inCombo = false;
        while (elapsedTime < comboTimeBeforeThreshold){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = true;
        while (elapsedTime < comboTimeEndThreshold){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = false;
        currentCombo.ResetComboStep();
    }

    private void AttackFinish(){
        isAttacking = false;
        _playerState.ChangeAttackStatus(false);
        inCombo = false;
    }

    private void ComboTransfer(){
        comboTransition = true;
    }

    private void HoldAttackTransfer(){
        if (!inHoldCombo) return;
        else if (currentAttackSO.Holdable){
            PerformAttack();
        }
        else{
            currentAttackSO = DecideCurrentAttackSO(GameInput.AttackInput.None, ComboSystem.AttackPressType.Hold);
            if (currentAttackSO){
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(HandlePerformAttack());
            }
        }
    }

    private void EnableHitBoxes(){
        if (!IsServer && NetworkManager.Singleton != null) return;
        GetAttackSOByIndex(nvAttackSOIndex.Value).Enable(this, _anim);
    }

    private void DisableHitBoxes(){
        if (!IsServer && NetworkManager.Singleton != null) return;
        GetAttackSOByIndex(nvAttackSOIndex.Value).Disable(this, _anim);
    }

    public int GetIndexByAttackSO(BaseAttackSO attackSO){
        return Array.IndexOf(attackSOList, attackSO);
    }

    public BaseAttackSO GetAttackSOByIndex(int attackSOIndex){
        return attackSOList[attackSOIndex];
    }
}