using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AuthenticationValues = Photon.Chat.AuthenticationValues;

public class PlayerChatController : MonoBehaviourPun, IChatClientListener
{
    private PhotonView _view;
    private ChatClient _chatClient;
    private Text _chatText;
    private InputField _inputField;
    private string _playerColorCode;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        
        if (!_view.IsMine) return;

        _chatClient = new ChatClient(this) {ChatRegion = "EU"};
        PhotonNetwork.AuthValues.UserId = PhotonNetwork.NickName;
        var newValues = new AuthenticationValues(PhotonNetwork.AuthValues.UserId);
        _chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.AppVersion, newValues);
    }

    private void Update()
    {
        if (!_view.IsMine)
            return;

        _chatClient.Service();
        
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (_inputField.text[0] == '@')
                _view.RPC("RequestSendPrivateMessage", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, _inputField.text);
            else
                _view.RPC("RequestSendPublicMessage", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, _inputField.text, _playerColorCode);
            
            UpdateChatText();
        }
    }

    public void DisconnectFromChat()
    {
        _chatClient.Disconnect();
        Destroy(gameObject);
    }
    
    public void SetChat(Text chatText, InputField inputField, string color)
    {
        _chatText = chatText;
        _inputField = inputField;
        _playerColorCode = color;
    }

    public void SendConnectionMessage(Player p)
    {
        _chatClient.PublishMessage("channelA", p.NickName + " has joined the game.");
    }

    [PunRPC]
    private void RequestSendPublicMessage(Player p, string text, string color)
    {
        var userColor = "<color=" + color + ">" + p.NickName + "</color>";
        var textToSend = userColor + " : " + text;
        
        _chatClient.PublishMessage("channelA", textToSend);
    }

    [PunRPC]
    private void RequestSendPrivateMessage(Player p, string text)
    {
        var user = text.Split('@')[1].Split(' ')[0];
        var msg = text.Split('@')[1].Split(' ')[1];
        
        var userColor = "<color=" + Color.magenta + ">" + p.NickName + "</color>";
        var textToSend = userColor + " : " + msg;
        
        _chatClient.SendPrivateMessage(user, textToSend);
    }
    
    private void UpdateChatText()
    {
        _inputField.text = "";

        EventSystem current;
        (current = EventSystem.current).SetSelectedGameObject(_inputField.gameObject, null);
        _inputField.OnPointerClick(new PointerEventData(current));
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        
    }

    public void OnDisconnected()
    {
        _chatText.text += "[DISCONNECTED]" +"\n";
    }

    public void OnConnected()
    {
        _chatClient.Subscribe(new[] {"channelA"});
    }

    public void OnChatStateChange(ChatState state)
    {
        
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (var i = 0; i < senders.Length; i++)
        {
            _chatText.text += messages[i] + "\n";
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        _chatText.text += "[WHISPER] " + message + "\n";
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        
    }

    public void OnUnsubscribed(string[] channels)
    {
        
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        
    }

    public void OnUserSubscribed(string channel, string user)
    {
        
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        
    }
}
