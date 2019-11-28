using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalSceneManger : MonoBehaviour
{
    public GameObject[] playerPositions;
    public GameObject[] enemiesPositions;
    public GameObject winObject;
    public GameObject winCanvas;
    public GameObject loseCanvas;
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();

        if (SceneManager.GetActiveScene().name == Constants.GAME_LEVEL)
            _networkManager.GameStarted(playerPositions, enemiesPositions, winObject);
    }

    private void Start()
    {
        _networkManager.ChatController();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var currentSceneName = SceneManager.GetActiveScene().name;
            
            if (currentSceneName == Constants.LOBBY_SCENE || currentSceneName == Constants.FINISH_GAME_SCENE || currentSceneName == Constants.GAME_LEVEL && PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                NetBackButton();    
            }
            else
            {
                SimpleBackButton();
            }
        }
    }

    public void NetExitGame()
    {
        _networkManager.ExitGame();
    }
    
    public void SimpleExitGame()
    {
        _networkManager.SimpleExit();
    }

    public void NetBackButton()
    {
        _networkManager.BackButton();
    }
    
    public void SimpleBackButton()
    {
        _networkManager.SimpleBack();
    }

    public void ActiveCanvas(bool winner)
    {
        if (winner)
            winCanvas.SetActive(true);
        else
            loseCanvas.SetActive(true);
    }
}
