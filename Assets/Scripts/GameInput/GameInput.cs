using System;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }
    private GameInputActionAsset gameInputScript; //generated script from inputasset

    //combat events to subscribe to
    public event Action<AttackInput> OnAttackButtonStarted, OnAttackButtonCanceled;

    private GameInputActionAsset.PlayerActions playerMap;

    public enum AttackInput{
        LeftMouse,
        RightMouse,
        E_Key,
        R_Key,
        None
    }

    private void Awake(){
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        gameInputScript = new GameInputActionAsset();
    }
    private void OnEnable(){
        gameInputScript.Enable();
        playerMap = gameInputScript.Player;
        playerMap.LeftClick.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.LeftMouse);
        playerMap.LeftClick.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.LeftMouse);
        playerMap.RightClick.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.RightMouse);
        playerMap.RightClick.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.RightMouse);
        playerMap.E.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.E_Key);
        playerMap.E.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.E_Key);
        playerMap.R.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.R_Key);
        playerMap.R.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.R_Key);
    }
    private void OnDisable(){
        gameInputScript.Disable();
    }
    
    public bool IsAttackButtonPressed(AttackInput input){
        bool retVal;
        switch(input){
            case AttackInput.LeftMouse:
                retVal = playerMap.LeftClick.IsPressed();
                break;
            case AttackInput.RightMouse:
                retVal = playerMap.RightClick.IsPressed();
                break;
            case AttackInput.E_Key:
                retVal = playerMap.E.IsPressed();
                break;
            case AttackInput.R_Key:
                retVal = playerMap.R.IsPressed();
                break;
            default:
                retVal = false;
                break;
        }
        return retVal;
    }

    public Vector2 GetPlayerMovementVectorNormalized(){return gameInputScript.Player.Move.ReadValue<Vector2>().normalized;}

    public Vector2 GetPlayerLookVectorNormalized(){return gameInputScript.Player.Look.ReadValue<Vector2>().normalized;}

    public bool ScrolledUp(){return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().y > 0 || gameInputScript.Player.MouseScroll.ReadValue<Vector2>().x < 0;}

    public bool ScrolledDown() { return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().y < 0 || gameInputScript.Player.MouseScroll.ReadValue<Vector2>().x > 0; }

    public float GetMouseScrollValue() { return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().normalized.y; }

    public bool JumpPressed() { return gameInputScript.Player.Jump.triggered; }

    public bool SprintingPressed(){return gameInputScript.Player.Sprint.IsPressed();}

    public bool CrouchPressed(){return gameInputScript.Player.Crouch.IsPressed();}

    public bool MouseMiddleTriggered(){return gameInputScript.Player.MouseMiddle.triggered;}    
}
