using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabLogin : MonoBehaviour
{
    public Text userEmail;
    public Text userPassword;
    public Text userName;

    [HideInInspector] public bool isConnecting;

    private bool _host;
    private NetworkManager _networkManager;
    
    public void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            PlayFabSettings.TitleId = "5824A"; 
        
        userEmail.text = PlayerPrefs.GetString("EMAIL");
        userPassword.text = PlayerPrefs.GetString("PASSWORD");
    }

    private void OnLoginSuccess(LoginResult result)
    {
        PlayerPrefs.SetString("EMAIL", userEmail.text);
        PlayerPrefs.SetString("PASSWORD", userPassword.text);
        
        _networkManager.ConnectToServerButton(_host);
    } 
    
    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        PlayerPrefs.SetString("EMAIL", userEmail.text);
        PlayerPrefs.SetString("PASSWORD", userPassword.text);
        
        _networkManager.ConnectToServerButton(_host);
    }
    
    private void OnRegisterFailure(PlayFabError error)
    {
        var messageWithoutPath = error.GenerateErrorReport().Substring(error.GenerateErrorReport().IndexOf(' '));
        messageWithoutPath = messageWithoutPath.Replace("\n", "").Replace(" Invalid input parameters: ", "");
        _networkManager.ShowMessage(Constants.MessageTypes.Error, messageWithoutPath, 5);
        IsConnecting();
    }

    private void OnLoginFailure(PlayFabError error)
    {
        var registerRequest = new RegisterPlayFabUserRequest{Email = userEmail.text, Password = userPassword.text, Username = userName.text};
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);
    }
 
    public void OnStartLogin(bool host)
    {
        if (isConnecting) return;

        IsConnecting();
        _host = host;
        
        if (!_host)
        {
            var request = new LoginWithEmailAddressRequest{Email = userEmail.text, Password = userPassword.text};
            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }
        else if (_host)
        {
            _networkManager.ConnectToServerButton(_host);
        }
    }

    public void IsConnecting()
    {
        isConnecting = !isConnecting;
        _networkManager.connectingText.gameObject.SetActive(isConnecting);
    }
}