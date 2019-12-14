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
    public GameObject chatObject;
    public GameObject connectionObj;
    public int pps;
    public int maxMessages;
    public Text connectingText;
    public GameObject textObject;
    public GameObject chatScroll;
    
    [HideInInspector] public GameObject[] playerPositions;
    [HideInInspector] public GameObject[] enemiesPositions;
    [HideInInspector] public GameObject winObject;
    
    private PhotonView _view;
    private Dictionary<Player, PlayerMenuData> _playersData = new Dictionary<Player, PlayerMenuData>();
    private ChatController _chatController;
    private PlayerMenuData _localPlayerData;
    private bool _host;
    private List<Message> _messagesList = new List<Message>();
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _view = GetComponent<PhotonView>();
    }

    public void ConnectToServerButton(bool host)
    {
        if (playerNameInputfield.text != "" || host)
        {
            _host = host;
            PhotonNetwork.LocalPlayer.NickName = playerNameInputfield.text;
            PhotonNetwork.ConnectUsingSettings();
            connectingText.gameObject.SetActive(true);
        }
        else
        {
            ShowMessage(Constants.MessageTypes.Error, Constants.NAME_ERROR);
        }
    }

    private IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(2);
        messageObject.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        if (_host)
            PhotonNetwork.CreateRoom("MainRoom", new RoomOptions { MaxPlayers = 5 }, TypedLobby.Default);
        else if (PhotonNetwork.CountOfRooms > 0)
            PhotonNetwork.JoinRandomRoom();
        else
        {
            OnDisconnectPlayer();
            ShowMessage(Constants.MessageTypes.Error, "There are no rooms hosted");
        }
    }
    
    public override void OnJoinedRoom()
    {
        connectingText.gameObject.SetActive(false);
        PhotonNetwork.LoadLevel("Lobby");
        
        if (!PhotonNetwork.IsMasterClient)
        {
            _view.RPC("OnConnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            readyButtonObject.SetActive(true);
        }
        else
        {
            PhotonNetwork.LocalPlayer.NickName = "[SERVER]";
        }

        ActiveChat(true);
    }

    [PunRPC]
    private void ActiveChat(bool active)
    {
        chatObject.SetActive(active);
        
        if (!active && _chatController)
            _chatController.DisconnectFromChat();
    }

    public void ChatController()
    {
        if (PhotonNetwork.IsMasterClient)
            _chatController = CreateChatController();
    }
    
    private ChatController CreateChatController()
    {
        var chatController = PhotonNetwork.Instantiate("ChatController", Vector3.zero, Quaternion.identity).GetComponent<ChatController>();

        return chatController;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        ActiveChat(false);
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }

    public void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        _view.RPC("ActiveChat", RpcTarget.AllBuffered, false);
        _view.RPC("LoadGameScene", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void LoadGameScene()
    {
        if (_localPlayerData)
            _localPlayerData.gameObject.SetActive(false);

        ClearLobbyData();
        
        PhotonNetwork.LoadLevel(Constants.GAME_LEVEL);
    }

    private void ClearLobbyData()
    {
        readyButtonObject.SetActive(false);
        startButtonObject.SetActive(false);
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
        StartCoroutine(HideMessage());
    }

    public void BackButton()
    {
        DisconnectBehaviour(OnDisconnectPlayer);
    }

    private void DisconnectBehaviour(Action customAction = null)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            _view.RPC("DisconnectAll", RpcTarget.OthersBuffered);
            customAction?.Invoke();
        }
        else
        {
            _view.RPC("NotifyDisconnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            customAction?.Invoke();
        }
    }

    [PunRPC]
    private void DisconnectAll()
    {
        OnDisconnectPlayer();
    }
    
    public void DisconnectPlayer(Player p)
    {
        _view.RPC("OnDisconnectPlayer", p);
    }
    
    [PunRPC]
    private void OnDisconnectPlayer()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        
        PhotonNetwork.Disconnect();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }
    
    [PunRPC]
    private void NotifyDisconnection(Player p)
    {
        _view.RPC("ActivePlayerMenuObject", RpcTarget.AllBuffered, Array.IndexOf(_playersData.Keys.ToArray(), p), false, p);
        _playersData.Remove(p);
    }

    [PunRPC]
    private void OnConnection(Player p)
    {
        _playersData.Add(p, GetNotTakenMenuData());
        _chatController.SendConnectionMessage(p);
        _view.RPC("ActivePlayerMenuObject", RpcTarget.AllBuffered, Array.IndexOf(_playersData.Keys.ToArray(), p), true, p);
    }

    public string GetPlayerColor(Player p)
    {
        if (Equals(p, PhotonNetwork.MasterClient))
            return "#" + ColorUtility.ToHtmlStringRGB(Color.red);
        
        return "#" + ColorUtility.ToHtmlStringRGB(_playersData[p].colorImg.GetComponent<Image>().color);
    }
    
    [PunRPC]
    private void ActivePlayerMenuObject(int index, bool active, Player p)
    {
        if (Equals(PhotonNetwork.LocalPlayer, p))
            _localPlayerData = playersObjects[index].GetComponent<PlayerMenuData>();
        
        playersObjects[index].SetActive(active);
        playersObjects[index].GetComponent<PlayerMenuData>().isTaken = active;

        if (active)
            playersObjects[index].GetComponent<PlayerMenuData>().GetComponentInChildren<Text>().text = p.NickName;
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

    public void GameStarted(GameObject[] playerPos, GameObject[] enemiesPos, GameObject winObjectPos)
    {
        playerPositions = playerPos;
        enemiesPositions = enemiesPos;
        winObject = winObjectPos;
        
        if (PhotonNetwork.IsMasterClient)
            CreateLevelManager();
        else
            CreateController();
    }
    
    private void CreateLevelManager()
    {
        PhotonNetwork.Instantiate("LevelManager", Vector3.zero, Quaternion.identity);
    }
    
    private void CreateController()
    {
        PhotonNetwork.Instantiate("Controller", Vector3.zero, Quaternion.identity).GetComponent<Controller>().SetPPS(pps);
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
        _view.RPC("PlayerReadyVisuals", RpcTarget.AllBuffered, Array.IndexOf(_playersData.Keys.ToArray(), p), playerData.isReady);

        playerData.readyObj.SetActive(playerData.isReady);
        CheckStartButton();
    }

    [PunRPC]
    private void PlayerReadyVisuals(int index, bool active)
    {
        playersObjects[index].GetComponent<PlayerMenuData>().readyObj.SetActive(active);
    }

    private void CheckStartButton()
    { 
        startButtonObject.SetActive(_playersData.Count > 1 && AllPlayersReady());
    }

    private bool AllPlayersReady()
    {
        return _playersData.All(x => x.Value.GetComponent<PlayerMenuData>().isReady);
    }

    private IEnumerator FinishGameSceneLoaded(bool winner)
    {
        yield return new WaitForSeconds(0.5f);
        FindObjectOfType<LocalSceneManger>().ActiveCanvas(winner);
    }

    public void PlayerLose(Player p)
    {
        _view.RPC("LoadFinishGame", p, p, false);
    }
    
    public void FinishGame(Player p)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!Equals(player, PhotonNetwork.MasterClient))
                _view.RPC("LoadFinishGame", player, player, player.Equals(p));
        }

        StartCoroutine(MasterFinishGame());
    }

    private IEnumerator MasterFinishGame()
    {
        yield return new WaitForSeconds(0.5f);
        LoadFinishGame(PhotonNetwork.LocalPlayer, true);
    }

    [PunRPC]
    private void LoadFinishGame(Player p, bool winner)
    {
        FinishGameScene(p, winner);
    }
    
    private void FinishGameScene(Player p, bool winner)
    {
        PhotonNetwork.LoadLevel(Constants.FINISH_GAME_SCENE);
        
        StartCoroutine(FinishGameSceneLoaded(winner));

        if (!Equals(p, PhotonNetwork.MasterClient))
            _view.RPC("ActivateChat", RpcTarget.MasterClient, p);
        
        ActiveChat(true);
    }

    public void RequestSendMessage(Player p, string text)
    {
        _view.RPC("SendChatMessage", RpcTarget.MasterClient, p, text);
    }

    [PunRPC]
    public void SendChatMessage(Player p, string text)
    {
        if (text[0] == '@')
            _chatController.SendPrivateMessage(p, text);
        else
            _chatController.SendPublicMessage(p, text);
    }

    public void WriteChat(string text, Player p = null)
    {
        if (p != null)
            _view.RPC("UpdatePlayersChat", p, text);
        else
            _view.RPC("UpdatePlayersChat", RpcTarget.AllBuffered, text);
    }
    
    [PunRPC]
    private void UpdatePlayersChat(string text)
    {
        if (_messagesList.Count >= maxMessages)
            _messagesList.Remove(_messagesList[0]);

        var newMessage = new Message();

        newMessage.text = text;
        
        Instantiate(textObject, chatScroll.transform);
        
        _messagesList.Add(newMessage);
        
        //chatObject.GetComponentInChildren<Text>().text += text + "\n";
    }
}

[Serializable]
public class Message
{
    public string text;
}
