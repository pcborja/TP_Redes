using Photon.Pun;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void ConnectToServerButton()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinRoom("Lobby");
    }
    
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameLevel");
    }
    
    public override void OnCreatedRoom()
    {
        
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }
}
