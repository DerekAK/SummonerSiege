using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MultiplayerUI : MonoBehaviour
{
    private string loadSceneStr;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private GameObject canvasBackground;

    private void Awake()
    {
        gameObject.SetActive(false);
        createGameButton.onClick.AddListener(CreateGame);
        joinGameButton.onClick.AddListener(JoinGame);
    }

    private void CreateGame()
    {
        SceneManager.sceneLoaded += SceneLoadedHost;
        SceneManager.LoadScene(loadSceneStr);
        canvasBackground.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void JoinGame()
    {
        SceneManager.sceneLoaded += SceneLoadedClient;
        SceneManager.LoadScene(loadSceneStr);
        canvasBackground.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void SceneLoadedHost(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= SceneLoadedHost;
        NetworkManager.Singleton.StartHost();
    }

    private void SceneLoadedClient(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= SceneLoadedClient;
        NetworkManager.Singleton.StartClient();
    }

    public void SetSceneString(string val)
    {
        loadSceneStr = val;
    }
}
