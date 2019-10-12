using System;
using UnityEngine;

public class LocalSceneManger : MonoBehaviour
{
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackButton();
        }
    }

    public void ExitGame()
    {
        _networkManager.ExitGame();
    }

    public void BackButton()
    {
        _networkManager.BackButton();
    }

    public void MainMenu()
    {
        _networkManager.MainMenu();
    }
}
