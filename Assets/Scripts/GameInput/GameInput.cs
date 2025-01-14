using UnityEngine;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }
    private GameInputActionAsset gameInputScript; //generated script from inputasset
    private void Awake(){
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        gameInputScript = new GameInputActionAsset();
    }
    private void OnEnable(){
        gameInputScript.Enable();
    }
    private void OnDisable(){
        gameInputScript.Disable();
    }
    public Vector2 GetPlayerMovementVectorNormalized(){return gameInputScript.Player.Move.ReadValue<Vector2>().normalized;}

    public Vector2 GetPlayerLookVectorNormalized(){return gameInputScript.Player.Look.ReadValue<Vector2>().normalized;}

    public bool JumpPressed(){return gameInputScript.Player.Jump.triggered;}

    public bool SprintingPressed(){return gameInputScript.Player.Sprint.IsPressed();}

    public bool CrouchPressed(){return gameInputScript.Player.Crouch.IsPressed();}

    public bool RollPressed(){return gameInputScript.Player.Roll.IsPressed();}

    public bool RightClickPressed(){return gameInputScript.Player.RightClickAction.IsPressed();}

    public bool LeftClickPressed(){return gameInputScript.Player.LeftClickAction.IsPressed();}

}
