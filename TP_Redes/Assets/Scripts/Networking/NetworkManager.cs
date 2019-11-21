using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject readyButtonObject;
    public GameObject[] playersObjects;
    [HideInInspector] public GameObject[] playerPositions;
    [HideInInspector] public GameObject[] enemiesPositions;
    private PhotonView _view;
    private Dictionary<Player, PlayerMenuData> _playersData = new Dictionary<Player, PlayerMenuData>();
    
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
        {
            _view.RPC("NotifyConnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            readyButtonObject.SetActive(true);
        }
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
        PhotonNetwork.LoadLevel(Constants.GAME_LEVEL);
    }

    public void ClearLobbyData()
    {
        startButtonObject.SetActive(false);
        _playersData.Clear();
        foreach (var playersObject in playersObjects)
        {
            playersObject.SetActive(false);
        }
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
        ActivePlayerMenuObject(p, false);
        _playersData.Remove(p);
    }

    [PunRPC]
    private void NotifyConnection(Player p)
    {
        _playersData.Add(p, GetNotTakenMenuData());
        ActivePlayerMenuObject(p, true);
    }
    
    private void ActivePlayerMenuObject(Player p, bool active)
    {
        if (!_playersData[p]) return;
        
        _playersData[p].gameObject.SetActive(active);
        _playersData[p].isTaken = active;

        if (active)
            _playersData[p].GetComponentInChildren<Text>().text = p.NickName;
    }
    
    private PlayerMenuData GetNotTakenMenuData()
    {
        return playersObjects.Select(playerObj => playerObj.GetComponent<PlayerMenuData>()).FirstOrDefault(dataObj => !dataObj.isTaken);
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
    
    private void CreateController(Player p)
    {
        var controller = PhotonNetwork.Instantiate("Controller", Vector3.zero, Quaternion.identity).GetComponent<Controller>();
        controller.SetPPS(20);
    }
    
    public void HowToPlayButton()
    {
        SceneManager.LoadScene(Constants.HOW_TO_PLAY_SCENE);
    }

    public void OnReady()
    {
        _view.RPC("SetPlayerIsReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void SetPlayerIsReady(Player p)
    {
        var playerData = _playersData[p];
        playerData.isReady = !playerData.isReady;
        playerData.readyObj.SetActive(playerData.isReady);
        CheckStartButton();
    }

    private void CheckStartButton()
    { 
        startButtonObject.SetActive(_playersData.Count > 1 && AllPlayersReady());
    }

    private bool AllPlayersReady()
    {
        return _playersData.All(x => x.Value.GetComponent<PlayerMenuData>().isReady);
    }
}
