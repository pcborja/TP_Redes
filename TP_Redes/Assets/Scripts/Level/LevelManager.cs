using Photon.Pun;
using UnityEngine;

public class LevelManager : MonoBehaviourPun
{
    public Character[] characters;
    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        StartPlayer();
    }

    void Update()
    {
        CheckPlayersVitals();
    }

    private void StartPlayer()
    {
        var currPlayer = characters[PhotonNetwork.LocalPlayer.ActorNumber - 1];
        currPlayer.gameObject.SetActive(true);
        if (Camera.main != null) 
            Camera.main.transform.SetParent(currPlayer.cameraPos.transform);
    }
    
    private void CheckPlayersVitals()
    {
        foreach (var character in characters) //TODO - Do this only in master client
        {
            if (character.hp <= 0)
                character.DestroyPlayer();
        }
    }

    public void RequestMove()
    {
        
    }

    public void Disconnect(string sceneToLoad)
    {
        _networkManager.DisconnectPlayer(sceneToLoad);
    }
}
