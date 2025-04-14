using UnityEngine;
using UnityEngine.UI;

public class StartGameUI : MonoBehaviour
{

    [SerializeField] private Button startGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;

    private void Awake(){
        Hide(); //start as invisible
        startGameButton.onClick.AddListener(()=>{
            Show();}
        );
        
        newGameButton.onClick.AddListener(()=>{
            Hide();
        });
        loadGameButton.onClick.AddListener(()=>{
            Hide();
        });
    }


    private void Hide(){
        gameObject.SetActive(false);
    }

    private void Show(){
        gameObject.SetActive(true);
    }
}
