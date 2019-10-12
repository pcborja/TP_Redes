using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LevelManager : MonoBehaviourPun
{
    public GameObject[] characterObjects;
    public List<Character> characters;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
    public static LevelManager Instance { get; private set; }
    private NetworkManager _networkManager;
    private PhotonView _view;
    private bool _canMove;
    private float _shootTimer;
    private Vector3 _positionToMove;

    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        _view = GetComponent<PhotonView>();
        if (!Instance)
        {
            if (_view.IsMine)
            {
                _view.RPC("SetReference", RpcTarget.AllBuffered);
                CreateController();
            }
        }
        else
            PhotonNetwork.Destroy(gameObject);
    }

    private void Update()
    {
        if (!_view.IsMine) return;
        
        CheckInputs();
        Timers();
    }

    private void FixedUpdate()
    {
        if (!_view.IsMine) return;
    }
    
    private void Timers()
    {
        _shootTimer += Time.deltaTime;
    }

    [PunRPC]
    private void SetReference()
    {
        Instance = this;
        _view.RPC("AddPlayer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        //_view.RPC("CreateController", RpcTarget.All);
    }

    [PunRPC]
    private void AddPlayer(Player p)
    {
        var currPlayerObj = characterObjects[PhotonNetwork.LocalPlayer.ActorNumber - 1];
        var instantiatedChar = Instantiate(Resources.Load<Character>("Character"), currPlayerObj.transform.position, Quaternion.identity);
        instantiatedChar.transform.SetParent(currPlayerObj.transform);
        instantiatedChar.transform.rotation = Quaternion.identity;
        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(currPlayerObj.transform);
        }
        instantiatedChar.myCam = Camera.main;
        players.Add(p, instantiatedChar);
    }

    //[PunRPC]
    void CreateController()
    {
        if (!_view.IsMine) return;
        Instantiate(Resources.Load<GameObject>("Controller"));
    }
    
    private void CheckInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Disconnect(Constants.INTRO_SCENE);
    }

    public void Disconnect(string sceneToLoad)
    {
        _networkManager.DisconnectPlayer(sceneToLoad);
    }

    public void OnClicked(Vector3 mousePosition, Player p)
    {
        _view.RPC("DrawRaycast", RpcTarget.MasterClient, mousePosition, p);
    }

    [PunRPC]
    private void DrawRaycast(Vector3 mousePos, Player p)
    {
        if (players.ContainsKey(p))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out var hit, 100))
            {
                if (hit.transform.gameObject.GetComponent<Enemy>() || players[p].isHoldingPosition)
                {
                    if (_shootTimer > players[p].timeToShoot)
                    {
                        _shootTimer = 0;
                        players[p].Shoot(hit.point);
                    }  
                }
                else if (!hit.transform.gameObject.GetComponent<Character>())
                {
                    players[p].SetCanMove(true);
                    _positionToMove = hit.point;
                }
            }
        }
    }

    public void RequestMove(Player p)
    {
        if (!_view.IsMine) return;
        _view.RPC("Move", RpcTarget.MasterClient, p);
    }

    [PunRPC]
    private void Move(Player p)
    {
        if (players.ContainsKey(p))
        {
            if (players[p].canMove)
                players[p].Move(_positionToMove);

            if (Vector3.Distance(players[p].transform.position, _positionToMove) < 0.1f)
            {
                _positionToMove = Vector3.zero;
                players[p].SetCanMove(false);
            }

            players[p].SetIsMoving(_positionToMove != Vector3.zero);
        }
    }

    public void OnStartHoldingPosition(Player p)
    {
        _view.RPC("HoldPosKey", RpcTarget.MasterClient, true, p);
    }
    
    public void OnEndHoldingPosition(Player p)
    {
        _view.RPC("HoldPosKey", RpcTarget.MasterClient, false, p);
    }
    
    [PunRPC]
    private void HoldPosKey(bool hold, Player p)
    {
        if (players.ContainsKey(p))
        {
            players[p].SetHoldingPos(hold);
        }
    }

    public void NotifyWinner(Character character)
    {
        var player = players.FirstOrDefault(x => x.Value == character).Key;
        _view.RPC("Win", RpcTarget.MasterClient, player);
    }

    [PunRPC]
    public void Win(Player player)
    {
        Disconnect(Equals(PhotonNetwork.LocalPlayer, player) ? "WinScene" : "LoseScene");
    }
}
