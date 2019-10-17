using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public InputField playerNameInputfield;
    public GameObject messageObject;
    public GameObject startButtonObject;
    public GameObject[] playersObjects;
    private PhotonView _view;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _view = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == Constants.INTRO_SCENE && Input.GetKeyDown(KeyCode.Return))
        {
            ConnectToServerButton();
        }
    }

    public void ConnectToServerButton()
    {
        if (playerNameInputfield.text != "")
        {
            PhotonNetwork.LocalPlayer.NickName = playerNameInputfield.text;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            ShowMessage(Constants.MessageTypes.Error, Constants.NAME_ERROR);
            StartCoroutine(HideMessage());
        }
    }

    private IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(2);
        messageObject.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        startButtonObject.SetActive(true);
    }
    
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
        Debug.Log(PhotonNetwork.CurrentRoom);
        _view.RPC("NotifyConnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.Disconnect();
    }

    public void StartGame()
    {
        startButtonObject.SetActive(false);
        _view.RPC("StartScene", RpcTarget.All);
    }   

    [PunRPC]
    private void StartScene()
    {
        _view.RPC("ActivePlayerMenuObject", RpcTarget.All, PhotonNetwork.LocalPlayer, false);
        PhotonNetwork.LoadLevel("GameLevel");        
    }

    public void ExitGame()
    {
        DisconnectBehaviour(SimpleExit);
    }

    private void ShowMessage(Constants.MessageTypes messageType, string message)
    {
        messageObject.SetActive(true);
        messageObject.GetComponentInChildren<Text>().text = message;
    }

    [PunRPC]
    private void ActivePlayerMenuObject(Player p, bool active)
    {
        playersObjects[p.ActorNumber - 1].SetActive(active);
        playersObjects[p.ActorNumber - 1].GetComponent<PlayerMenuData>().isTaken = active;

        if (active)
            playersObjects[p.ActorNumber - 1].GetComponentInChildren<Text>().text = p.NickName;
    }

    public void BackButton()
    {
        DisconnectBehaviour(() =>
        {
            DisconnectPlayer(Constants.INTRO_SCENE);
            DestroyImmediate(gameObject);
        });
    }

    private void DisconnectBehaviour(Action customAction)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            _view.RPC("DisconnectAll", RpcTarget.OthersBuffered);
            customAction();
        }
        else
        {
            _view.RPC("NotifyDisconnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            customAction();
        }
    }

    [PunRPC]
    private void DisconnectAll()
    {
        DisconnectPlayer(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }

    public void DisconnectPlayer(string sceneToLoad)
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene(sceneToLoad);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }
    
    [PunRPC]
    private void NotifyDisconnection(Player p)
    {
        if (!_view.IsMine) return;
        _view.RPC("ActivePlayerMenuObject", RpcTarget.AllBuffered, p, false);
    }

    [PunRPC]
    private void NotifyConnection(Player p)
    {
        if (!_view.IsMine) return;
        CheckForAlreadyConnectedPlayers(p);
        _view.RPC("ActivePlayerMenuObject", RpcTarget.AllBuffered, p, true);
    }

    private void CheckForAlreadyConnectedPlayers(Player p)
    {
        for (var i = 0; i < playersObjects.Length; i++)
        {
            if (playersObjects[i].activeInHierarchy)
            {
                _view.RPC("ActivePlayerMenuObject", p, PhotonNetwork.PlayerList[i + 1], true);
            }
        }
    }

    public void HowToPlayButton()
    {
        SceneManager.LoadScene(Constants.HOW_TO_PLAY_SCENE);
    }

    public void SimpleExit()
    {
        Application.Quit();
    }

    public void SimpleBack()
    {
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }
}
