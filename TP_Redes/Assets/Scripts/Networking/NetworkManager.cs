using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject messageObject;
    public GameObject startButtonObject;
    public GameObject readyButtonObject;
    public GameObject[] playersObjects;
    public GameObject chatObject;
    public int pps;
    public int maxMessages;
    public Text connectingText;
    public GameObject textObject;
    public GameObject chatScroll;
    public GameObject playerStatusPanel;
    public Text[] playerStatusNames;
    public Text[] playerStatusStatus;
    
    [HideInInspector] public GameObject[] playerPositions;
    [HideInInspector] public GameObject[] enemiesPositions;
    [HideInInspector] public GameObject[] healPowerUpObjects;
    [HideInInspector] public GameObject[] armorPowerUpObjects;
    [HideInInspector] public GameObject[] invulnerabilityPowerUpObjects;
    [HideInInspector] public GameObject winObject;
    
    private PhotonView _view;
    private Dictionary<Player, PlayerMenuData> _playersData = new Dictionary<Player, PlayerMenuData>();
    private ChatController _chatController;
    private PlayerMenuData _localPlayerData;
    private PlayFabController _playFabController;
    private bool _host;
    private List<Message> _messagesList = new List<Message>();
    private float _timer;
    private AudioSource _audioSource;
    private Dictionary<string, AudioClip> _musicsLoaded = new Dictionary<string, AudioClip>();
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _view = GetComponent<PhotonView>();
        _playFabController = FindObjectOfType<PlayFabController>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            _timer += Time.deltaTime;

            if (_timer >= 300)
            {
                _timer = 0;
                _view.RPC("RefreshFriendsPanel", RpcTarget.AllBuffered);
            }
        }
    }

    public void ConnectToServerButton(string nickname, bool host)
    {
        _host = host;
        PhotonNetwork.LocalPlayer.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    private IEnumerator HideMessage(int time)
    {
        yield return new WaitForSeconds(time);
        messageObject.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        FindObjectOfType<PlayFabLogin>().IsConnecting();
        
        if (_host)
            PhotonNetwork.CreateRoom("MainRoom", new RoomOptions { MaxPlayers = 5 }, TypedLobby.Default);
        else if (PhotonNetwork.CountOfRooms > 0)
            PhotonNetwork.JoinRandomRoom();
        else
        {
            ShowMessage(Constants.MessageTypes.Error, "There are no rooms hosted", 5);
            OnDisconnectPlayer();
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
            TryActivateFriendsPanel(true);
            TryPlayMusic(Constants.LOBBY_MUSIC);
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
        if (SceneManager.GetActiveScene().name == "IntroMenu") return;
        
        ActiveChat(false);
        SceneManager.LoadScene(Constants.INTRO_SCENE);
        DestroyImmediate(gameObject);
    }

    public void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        _view.RPC("ActivateFriendsPanel", RpcTarget.AllBuffered, false);
        _view.RPC("ActiveChat", RpcTarget.AllBuffered, false);
        _view.RPC("LoadGameScene", RpcTarget.AllBuffered);

        StartPlayerStatus();
    }

    private void StartPlayerStatus()
    {
        _view.RPC("ActivePlayerStatus", RpcTarget.AllBuffered, true);

        var players = PhotonNetwork.PlayerListOthers;
        for (var i = 0; i < players.Length; i++)
        {
            _view.RPC("UpdatePlayerStatusInfo", RpcTarget.AllBuffered, i, players[i].NickName, GetPlayerColor(players[i]), Constants.ALIVE);
        }
    }

    public void OnPlayerDeadStatus(Player p)
    {
        _view.RPC("UpdatePlayerStatusInfo", RpcTarget.AllBuffered, GetPlayerIndex(p), p.NickName, "", Constants.DEAD);
    }

    [PunRPC]
    private void ActivePlayerStatus(bool active)
    {
        playerStatusPanel.SetActive(active);

        foreach (var statusName in playerStatusNames)
        {
            statusName.text = "";
        }
        
        foreach (var status in playerStatusStatus)
        {
            status.text = "";
        }
    }

    [PunRPC]
    private void UpdatePlayerStatusInfo(int index, string playerName, string playerColor, string playerStatus)
    {
        playerStatusNames[index].text = playerName;
        playerStatusStatus[index].text = playerStatus;
        playerStatusStatus[index].color = playerStatusStatus[index].text == Constants.ALIVE ? Color.green : Color.red;
        if (ColorUtility.TryParseHtmlString(playerColor, out var color))
            playerStatusNames[index].color = color;
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

    public void ShowMessage(Constants.MessageTypes messageType, string message, int time)
    {
        switch (messageType)
        {
            case Constants.MessageTypes.Success:
                messageObject.GetComponentsInChildren<Image>()[1].sprite = Resources.Load<Sprite>("SuccessIcon");
                break;
            case Constants.MessageTypes.Error:
                messageObject.GetComponentsInChildren<Image>()[1].sprite = Resources.Load<Sprite>("ErrorIcon");
                break;
        }
        
        messageObject.SetActive(true);
        messageObject.GetComponentInChildren<Text>().text = message;
        StartCoroutine(HideMessage(time));
    }

    public void BackButton()
    {
        DisconnectBehaviour(OnDisconnectPlayer);
    }

    public void DisconnectBehaviour(Action customAction = null)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            _view.RPC("DisconnectAll", RpcTarget.OthersBuffered);
            StartCoroutine(DisconnectAction(customAction));
        }
        else
        {
            _view.RPC("NotifyDisconnection", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            StartCoroutine(DisconnectAction(customAction));
        }
    }

    private IEnumerator DisconnectAction(Action customAction)
    {
        yield return new WaitForSeconds(1);
        customAction?.Invoke();
    }

    [PunRPC]
    private void DisconnectAll()
    {
        OnDisconnectPlayer();
    }
    
    public void OnDisconnectPlayer()
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
        _chatController.SendDisconnectionMessage(p);
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
        {
            _localPlayerData = playersObjects[index].GetComponent<PlayerMenuData>();
            _localPlayerData.currentPlayerImg.SetActive(active);
            
            var imageColor = _localPlayerData.colorImg.GetComponent<Image>().color;
            _localPlayerData.currentPlayerImg.GetComponent<Image>().color = new Color(imageColor.r, imageColor.g, imageColor.b, 141);
        }
        
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

    public void GameStarted(GameObject[] playerPos, GameObject[] enemiesPos, GameObject winObjectPos, 
        GameObject[] healPowerUps, GameObject[] armorPowerUps, GameObject[] invulnerabilityPowerUps)
    {
        playerPositions = playerPos;
        enemiesPositions = enemiesPos;
        winObject = winObjectPos;
        healPowerUpObjects = healPowerUps;
        armorPowerUpObjects = armorPowerUps;
        invulnerabilityPowerUpObjects = invulnerabilityPowerUps;

        PlayMusicForAll(Constants.GAME_MUSIC);
        
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
        EventSystem.current.SetSelectedGameObject(null, null);
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
        _view.RPC("LoadFinishGame", p, false);
    }
    
    public void FinishGame(Player p)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!Equals(player, PhotonNetwork.MasterClient))
                _view.RPC("LoadFinishGame", player, player.Equals(p));
        }

        StartCoroutine(MasterFinishGame());
    }

    private IEnumerator MasterFinishGame()
    {
        yield return new WaitForSeconds(0.5f);
        LoadFinishGame(false);
    }

    [PunRPC]
    private void LoadFinishGame(bool winner)
    {
        PhotonNetwork.LoadLevel(Constants.FINISH_GAME_SCENE);
        
        StartCoroutine(FinishGameSceneLoaded(winner));

        ActivateFriendsPanel(true);
        ActiveChat(true);
        ActivePlayerStatus(false);
        TryPlayMusic(winner ? Constants.WIN_GAME_MUSIC : Constants.LOSE_GAME_MUSIC);
    }

    public void RequestSendMessage(Player p, string text)
    {
        _view.RPC("SendChatMessage", RpcTarget.MasterClient, p, text);
    }

    [PunRPC]
    public void SendChatMessage(Player p, string text)
    {
        if (text == "") return;
        
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
        {
            Destroy(_messagesList[0].textObject.gameObject);
            _messagesList.Remove(_messagesList[0]);
        }

        var newMessage = new Message {text = text};
        var newText = Instantiate(textObject, chatScroll.transform);
        
        newMessage.textObject = newText.GetComponent<Text>();
        newMessage.textObject.text = newMessage.text;
        
        _messagesList.Add(newMessage);
    }
    
    public void UpdateInputField(InputField inputField)
    {
        inputField.text = "";

        EventSystem current;
        (current = EventSystem.current).SetSelectedGameObject(inputField.gameObject, null);
        inputField.OnPointerClick(new PointerEventData(current));
    }

    public void TryAddFriend()
    {
        _view.RPC("RequestAddFriend", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer);
    }
    
    [PunRPC]
    public void RequestAddFriend(Player p)
    {
        _view.RPC("AddFriend", p);
    }
    
    [PunRPC]
    public void AddFriend()
    {
        if (!PhotonNetwork.IsMasterClient)
            _playFabController.SubmitFriendRequest();
    }

    private void TryActivateFriendsPanel(bool active)
    {
        _view.RPC("RequestActivateFriendsPanel", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer, active);
    }
    
    [PunRPC]
    public void RequestActivateFriendsPanel(Player p, bool active)
    {
        _view.RPC("ActivateFriendsPanel", p, active);
    }
    
    [PunRPC]
    private void ActivateFriendsPanel(bool active)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            _playFabController.OpenCloseFriends(active);
            
            if (active)
                _playFabController.GetFriends();
        }
    }

    [PunRPC]
    private void RefreshFriendsPanel()
    {
        if (!PhotonNetwork.IsMasterClient)
            _playFabController.GetFriends();
    }
    
    private int GetPlayerIndex(Player player)
    {
        var desiredIndex = 0;
        
        for (var i = 0; i < PhotonNetwork.PlayerListOthers.Length; i++)
        {
            if (Equals(PhotonNetwork.PlayerListOthers[i], player))
            {
                desiredIndex = i;
                break;
            }
        }
        return desiredIndex;
    }
    
    private void PlayMusicForAll(string musicName)
    {
        _view.RPC("PlayRequestedMusic", RpcTarget.AllBuffered, musicName);
    }

    private void TryPlayMusic(string musicName)
    {
        _view.RPC("PlayMusic", PhotonNetwork.MasterClient,PhotonNetwork.LocalPlayer, musicName);
    }

    [PunRPC]
    private void PlayMusic(Player p, string musicName)
    {
        _view.RPC("PlayRequestedMusic", p, musicName);
    }
    
    [PunRPC]
    private void PlayRequestedMusic(string musicName)
    {
        if (!_musicsLoaded.ContainsKey(musicName))
            _musicsLoaded.Add(musicName, Resources.Load<AudioClip>(musicName));

        _audioSource.Stop();
        _audioSource.clip = _musicsLoaded[musicName];
        _audioSource.Play();
    }
}

[Serializable]
public class Message
{
    public string text;
    public Text textObject;
}
