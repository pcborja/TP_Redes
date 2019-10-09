using UnityEngine;

public class LocalSceneManger : MonoBehaviour
{
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
    }

    public void ExitGame()
    {
        _networkManager.ExitGame();
    }

    public void BackButton()
    {
        _networkManager.BackButton();
    }
}
