using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public InputField playerNameInputfield;
    public GameObject messageObject;
    public GameObject startButtonObject;
    public GameObject[] _playersObjects;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void ConnectToServerButton()
    {
        if (playerNameInputfield.text != "")
        {
            PhotonNetwork.LocalPlayer.NickName = playerNameInputfield.text;
            PhotonNetwork.ConnectUsingSettings();
        }            
        else
            ShowMessage(Constants.MessageTypes.Error, Constants.NAME_ERROR);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
        ActivePlayerMenuObject(PhotonNetwork.LocalPlayer.ActorNumber - 1, true);
    }

    public override void OnCreatedRoom()
    {
        startButtonObject.SetActive(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
    }

    public void StartGame()
    {
        startButtonObject.SetActive(false);
        ActivePlayerMenuObject(PhotonNetwork.LocalPlayer.ActorNumber - 1, false);
        PhotonNetwork.LoadLevel("GameLevel");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void ShowMessage(Constants.MessageTypes messageType, string message)
    {
        messageObject.SetActive(true);
        messageObject.GetComponentInChildren<Text>().text = message;
    }

    private void ActivePlayerMenuObject(int position, bool active)
    {
        _playersObjects[position].SetActive(active);
        _playersObjects[position].GetComponent<PlayerMenuData>().isTaken = active;

        if (active)
            _playersObjects[position].GetComponentInChildren<Text>().text = PhotonNetwork.LocalPlayer.NickName;
    }

    public void StartPlayer(Character[] players)
    {
        var currPlayer = players[PhotonNetwork.LocalPlayer.ActorNumber - 1];
        currPlayer.gameObject.SetActive(true);
        if (Camera.main != null) 
            Camera.main.transform.SetParent(currPlayer.cameraPos.transform);
    }

    public void BackButton()
    {
        startButtonObject.SetActive(false);
        ActivePlayerMenuObject(PhotonNetwork.LocalPlayer.ActorNumber - 1, false);
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("IntroMenu");
        playerNameInputfield.text = PhotonNetwork.LocalPlayer.NickName;
    }
}
