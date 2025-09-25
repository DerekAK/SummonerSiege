using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenUI : MonoBehaviour
{
    [SerializeField] private string loadSceneString = "World Building";
    [SerializeField] private Button singleplayerButton;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private Image canvasBackground;

    private void Awake(){
        singleplayerButton.onClick.AddListener(StartSinglePlayerWorld);
        multiplayerButton.onClick.AddListener(ShowMultiplayerCanvas);
    }

    private void StartSinglePlayerWorld(){
        gameObject.SetActive(false);
        canvasBackground.gameObject.SetActive(false);
        SceneManager.sceneLoaded += SceneLoaded;
        SceneManager.LoadScene(loadSceneString);   
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode){
        SceneManager.sceneLoaded -= SceneLoaded;
    }


    private void ShowMultiplayerCanvas()
    {
        gameObject.SetActive(false);
        multiplayerPanel.SetActive(true);
        multiplayerPanel.GetComponent<MultiplayerUI>().SetSceneString(loadSceneString);
    }
}
