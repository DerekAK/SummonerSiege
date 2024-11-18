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
    public Vector2 GetPlayerMovementVectorNormalized(){
        Debug.Log(gameInputScript.Player.Move.ReadValue<Vector2>().normalized);
        return gameInputScript.Player.Move.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetPlayerLookVectorNormalized(){
        return gameInputScript.Player.Look.ReadValue<Vector2>().normalized;
    }

    public bool JumpTriggered(){
        return gameInputScript.Player.Jump.triggered;
    }

    public bool SprintingPressed(){
        return gameInputScript.Player.Sprint.IsPressed();
    }

}
