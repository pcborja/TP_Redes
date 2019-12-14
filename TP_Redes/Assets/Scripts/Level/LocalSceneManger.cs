using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LocalSceneManger : MonoBehaviour
{
    public GameObject[] playerPositions;
    public GameObject[] enemiesPositions;
    public GameObject winObject;
    public GameObject winCanvas;
    public GameObject loseCanvas;
    private InputField _inputField;
    
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
 
        if (SceneManager.GetActiveScene().name == Constants.GAME_LEVEL)
            _networkManager.GameStarted(playerPositions, enemiesPositions, winObject);
    }

    private void Start()
    {
        _networkManager.ChatController();
        _inputField = _networkManager.chatObject.GetComponentInChildren<InputField>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var currentSceneName = SceneManager.GetActiveScene().name;
            
            if (currentSceneName == Constants.LOBBY_SCENE || currentSceneName == Constants.FINISH_GAME_SCENE || currentSceneName == Constants.GAME_LEVEL && PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                NetBackButton();    
            }
            else
            {
                SimpleBackButton();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Return) && _networkManager.chatObject.activeInHierarchy)
        {
            _networkManager.RequestSendMessage(PhotonNetwork.LocalPlayer, _inputField.text);

            UpdateChatText();
        }
    }

    private void UpdateChatText()
    {
        _inputField.text = "";

        EventSystem current;
        (current = EventSystem.current).SetSelectedGameObject(_inputField.gameObject, null);
        _inputField.OnPointerClick(new PointerEventData(current));
    }

    public void NetExitGame()
    {
        _networkManager.ExitGame();
    }
    
    public void SimpleExitGame()
    {
        _networkManager.SimpleExit();
    }

    public void NetBackButton()
    {
        _networkManager.BackButton();
    }
    
    public void SimpleBackButton()
    {
        _networkManager.SimpleBack();
    }

    public void ActiveCanvas(bool winner)
    {
        if (winner)
            winCanvas.SetActive(true);
        else
            loseCanvas.SetActive(true);
    }
}
