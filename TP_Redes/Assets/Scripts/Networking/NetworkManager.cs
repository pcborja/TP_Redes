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
    [HideInInspector] public GameObject[] playerPositions;
    [HideInInspector] public GameObject[] enemiesPositions;
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
        PhotonNetwork.JoinOrCreateRoom("MainRoom", new RoomOptions { MaxPlayers = 5 }, TypedLobby.Default);
    }
    
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
        
        if (!PhotonNetwork.IsMasterClient)
            _view.RPC("NotifyConnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        _view.RPC("NotifyDisconnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    public void StartGame()
    {
        startButtonObject.SetActive(false);
        _view.RPC("StartScene", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void StartScene()
    {
        _view.RPC("ActivePlayerMenuObject", RpcTarget.All, PhotonNetwork.LocalPlayer, false);
        PhotonNetwork.LoadLevel(Constants.GAME_LEVEL);
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
        if (!_view.IsMine) return;
        
        playersObjects[p.ActorNumber].SetActive(active);
        playersObjects[p.ActorNumber].GetComponent<PlayerMenuData>().isTaken = active;

        if (active)
            playersObjects[p.ActorNumber].GetComponentInChildren<Text>().text = p.NickName;
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
        
        if (PhotonNetwork.PlayerList.Length > 2)
            startButtonObject.SetActive(true);
        
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

    public void SimpleExit()
    {
        Application.Quit();
    }

    public void SimpleBack()
    {
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }

    public void GameStarted(GameObject[] playerPos, GameObject[] enemiesPos)
    {
        playerPositions = playerPos;
        enemiesPositions = enemiesPos;

        PhotonNetwork.Instantiate(PhotonNetwork.LocalPlayer.IsMasterClient ? "LevelManager" : "Controller",
            Vector3.zero, Quaternion.identity);
    }
    
    public void HowToPlayButton()
    {
        SceneManager.LoadScene(Constants.HOW_TO_PLAY_SCENE);
    }

}
