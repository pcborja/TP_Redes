using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalSceneManger : MonoBehaviour
{
    public GameObject[] playerPositions;
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();

        if (SceneManager.GetActiveScene().name == Constants.GAME_LEVEL)
            _networkManager.GameStarted(playerPositions);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SceneManager.GetActiveScene().name == Constants.LOBBY_SCENE || SceneManager.GetActiveScene().name == Constants.GAME_LEVEL)
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
}
