using UnityEngine;

public class Player : MonoBehaviour
{    
    private void OnEnable()
    {
        GameManager.Instance.RegisterPlayer(transform);
    }

    private void OnDisable(){
        GameManager.Instance.UnregisterPlayer(transform);
    }
}
