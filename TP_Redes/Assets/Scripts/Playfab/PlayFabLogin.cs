using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayFabLogin : MonoBehaviour
{
    public Text userEmail;
    public Text userPassword;
    public Text userName;

    [HideInInspector] public bool isConnecting;

    private bool _host;
    private NetworkManager _networkManager;
    private Text[] _texts;
    
    public void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        _texts = new[] { userName, userEmail, userPassword };
        
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            PlayFabSettings.TitleId = "5824A"; 
        
        userEmail.text = PlayerPrefs.GetString("EMAIL");
        userPassword.text = PlayerPrefs.GetString("PASSWORD");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && SceneManager.GetActiveScene().name.Equals("IntroMenu"))
            SwitchSelection();
        if (Input.GetKeyDown(KeyCode.Return) && SceneManager.GetActiveScene().name.Equals("IntroMenu"))
            OnStartLogin(false);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        PlayerPrefs.SetString("EMAIL", userEmail.text);
        PlayerPrefs.SetString("PASSWORD", userPassword.text);
        
        _networkManager.ConnectToServerButton(userName.text, _host);
    } 
    
    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        PlayerPrefs.SetString("EMAIL", userEmail.text);
        PlayerPrefs.SetString("PASSWORD", userPassword.text);
        
        _networkManager.ConnectToServerButton(userName.text, _host);
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
            _networkManager.ConnectToServerButton("[SERVER]", _host);
        }
    }

    public void IsConnecting()
    {
        isConnecting = !isConnecting;
        _networkManager.connectingText.gameObject.SetActive(isConnecting);
    }
    
    private void SwitchSelection()
    {
        for (var i = 0; i < _texts.Length; i++)
        {
            if (_texts[i].transform.parent.GetComponent<InputField>().isFocused)
            {
                if (_texts.ElementAtOrDefault(i + 1) != null)
                {
                    EventSystem current;
                    (current = EventSystem.current).SetSelectedGameObject(_texts[i + 1].transform.parent.GetComponent<InputField>().gameObject, null);
                    _texts[i + 1].transform.parent.GetComponent<InputField>().OnPointerClick(new PointerEventData(current));
                }
                else
                {
                    EventSystem current;
                    (current = EventSystem.current).SetSelectedGameObject(_texts[0].transform.parent.GetComponent<InputField>().gameObject, null);
                    _texts[0].transform.parent.GetComponent<InputField>().OnPointerClick(new PointerEventData(current));
                }
                break;
            }
        }
    }
}