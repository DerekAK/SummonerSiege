using System;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }
    private GameInputActionAsset gameInputScript; //generated script from inputasset

    //combat events to subscribe to
    public event Action OnLeftClickStarted, OnLeftClickCanceled, OnRightClickTriggered;

    private void Awake(){
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        gameInputScript = new GameInputActionAsset();
    }
    private void OnEnable(){
        gameInputScript.Enable();
        
        var playerMap = gameInputScript.Player;
        playerMap.LeftClickPressed.started += ctx => OnLeftClickStarted?.Invoke();
        playerMap.LeftClickPressed.canceled += ctx => OnLeftClickCanceled?.Invoke();
        playerMap.RightClickPressed.performed += ctx => OnRightClickTriggered?.Invoke();
    }
    private void OnDisable(){
        gameInputScript.Disable();
    }
    
    public bool LeftClickPressed(){return gameInputScript.Player.LeftClickPressed.IsPressed();}

    public Vector2 GetPlayerMovementVectorNormalized(){return gameInputScript.Player.Move.ReadValue<Vector2>().normalized;}

    public Vector2 GetPlayerLookVectorNormalized(){return gameInputScript.Player.Look.ReadValue<Vector2>().normalized;}

    public bool ScrolledUp(){return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().y > 0 || gameInputScript.Player.MouseScroll.ReadValue<Vector2>().x < 0;}

    public bool ScrolledDown(){return gameInputScript.Player.MouseScroll.ReadValue<Vector2>().y < 0 || gameInputScript.Player.MouseScroll.ReadValue<Vector2>().x > 0;}

    public bool JumpPressed(){return gameInputScript.Player.Jump.triggered;}

    public bool SprintingPressed(){return gameInputScript.Player.Sprint.IsPressed();}

    public bool CrouchPressed(){return gameInputScript.Player.Crouch.IsPressed();}

    public bool RollPressed(){return gameInputScript.Player.Roll.IsPressed();}

    public bool RightClickPressed(){return gameInputScript.Player.RightClickPressed.IsPressed();}

    public bool MouseMiddleTriggered(){return gameInputScript.Player.MouseMiddle.triggered;}    
}
