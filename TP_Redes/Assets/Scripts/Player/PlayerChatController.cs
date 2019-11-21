using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
                SendPrivateMessage();
            else
                SendPublicMessage();
            
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

    private void SendPublicMessage()
    {
        _chatClient.PublishMessage("channelA", _inputField.text);
    }

    private void SendPrivateMessage()
    {
        var user = _inputField.text.Split('@')[1].Split(' ')[0];
        var msg = _inputField.text.Split('@')[1].Split(' ')[1];
        _chatClient.SendPrivateMessage(user, msg);
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
            var userColor = "<color=" + _playerColorCode + ">" + senders[i] + "</color>";
            _chatText.text += userColor + " : " + messages[i] + "\n";
        }
            
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        _chatText.text += "[WHISPER] " + sender + " : " + message + "\n";
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
