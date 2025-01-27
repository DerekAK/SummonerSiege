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
        RightMouse
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
        playerMap.LeftClickPressed.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.LeftMouse);
        playerMap.LeftClickPressed.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.LeftMouse);
        playerMap.RightClickPressed.started += ctx => OnAttackButtonStarted?.Invoke(AttackInput.RightMouse);
        playerMap.RightClickPressed.canceled += ctx => OnAttackButtonCanceled?.Invoke(AttackInput.RightMouse);
    }
    private void OnDisable(){
        gameInputScript.Disable();
    }
    
    public bool IsButtonPressed(AttackInput input){
        bool retVal;
        switch(input){
            case AttackInput.LeftMouse:
                retVal = playerMap.LeftClickPressed.IsPressed();
                break;
            case AttackInput.RightMouse:
                retVal = playerMap.RightClickPressed.IsPressed();
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

    public bool ScrolledDown(){return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().y < 0 || gameInputScript.Player.MouseScroll.ReadValue<Vector2>().x > 0;}

    public bool JumpPressed(){return gameInputScript.Player.Jump.triggered;}

    public bool SprintingPressed(){return gameInputScript.Player.Sprint.IsPressed();}

    public bool CrouchPressed(){return gameInputScript.Player.Crouch.IsPressed();}

    public bool MouseMiddleTriggered(){return gameInputScript.Player.MouseMiddle.triggered;}    
}
