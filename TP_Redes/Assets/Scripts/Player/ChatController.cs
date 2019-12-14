using System.Linq;
using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using AuthenticationValues = Photon.Chat.AuthenticationValues;

public class ChatController : MonoBehaviourPun, IChatClientListener
{
    private PhotonView _view;
    private ChatClient _chatClient;
    private NetworkManager _networkManager;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        _networkManager = FindObjectOfType<NetworkManager>();
        
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
    }

    public void DisconnectFromChat()
    {
        _chatClient.Disconnect();
        Destroy(gameObject);
    }

    public void SendConnectionMessage(Player p)
    {
        _chatClient.PublishMessage("channelA", p.NickName + " has joined the game.");
    }

    public void SendPublicMessage(Player p, string text)
    {
        var userColor = "<color=" + _networkManager.GetPlayerColor(p) + ">" + p.NickName + "</color>";
        var textToSend = userColor + " : " + text;
        
        _chatClient.PublishMessage("channelA", textToSend);
    }

    public void SendPrivateMessage(Player p, string text)
    {
        var user = text.Split('@')[1].Split(' ')[0];
        var msg = text.Split('@')[1].Split(' ')[1];
        
        var userColor = "<color=" + _networkManager.GetPlayerColor(p) + ">" + p.NickName + "</color>";
        var textToSend = user + '@' + userColor + " : " + msg;
        
        _chatClient.SendPrivateMessage(user, textToSend);
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        
    }

    public void OnDisconnected()
    {
        _networkManager.WriteChat("[DISCONNECTED]");
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
            _networkManager.WriteChat(messages[i] as string);
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        var userName = message.ToString().Split('@')[0];
        var msg = message.ToString().Split('@')[1];
        
        var playersWithName = PhotonNetwork.PlayerList.Where(x => x.NickName == userName).ToArray();

        foreach (var player in playersWithName)
        {
            var whisper = "<color=" + "#" + ColorUtility.ToHtmlStringRGB(Color.magenta) + ">" + "[WHISPER] " + "</color>";
            var textToSend = whisper + msg;
            
            _networkManager.WriteChat(textToSend, player);
        }
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
