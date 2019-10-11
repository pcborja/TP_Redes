using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LevelManager : MonoBehaviourPun
{
    public Character[] characters;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
    public static LevelManager Instance { get; private set; }
    private NetworkManager _networkManager;
    private PhotonView _view;
    private bool _canMove;
    private float _shootTimer;
    private Player _currentPlayer;
    private Vector3 _positionToMove;

    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        _view = GetComponent<PhotonView>();
        if (!Instance)
        {
            if (_view.IsMine)
                _view.RPC("SetReference", RpcTarget.AllBuffered);
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
        
        _view.RPC("ProcedureMoveRequest", RpcTarget.MasterClient, _positionToMove, _currentPlayer);
        _view.RPC("SetIsMoving", RpcTarget.MasterClient, _positionToMove != Vector3.zero, _currentPlayer);
    }
    
    private void Timers()
    {
        _shootTimer += Time.deltaTime;
    }

    [PunRPC]
    private void SetReference()
    {
        Instance = this;
        _view.RPC("AddPlayer", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void AddPlayer(Player p)
    {
        var currPlayer = characters[PhotonNetwork.LocalPlayer.ActorNumber - 1];
        currPlayer.gameObject.SetActive(true);
        if (Camera.main != null) 
            Camera.main.transform.SetParent(currPlayer.cameraPos.transform);
        players.Add(p, currPlayer);
    }
    
    private void CheckInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Disconnect(Constants.INTRO_SCENE);
    }

    public void RequestMovement(Vector3 position, Player player)
    {
        _canMove = true;
        _positionToMove = position;
        _currentPlayer = player;
    }

    public void RequestShoot(Vector3 position, Player player)
    {
        _shootTimer = 0;
        _view.RPC("ProcedureShootRequest", RpcTarget.MasterClient, position, player);
        _view.RPC("SetIsShooting", RpcTarget.MasterClient, position != Vector3.zero, player);
    }

    public void Disconnect(string sceneToLoad)
    {
        _networkManager.DisconnectPlayer(sceneToLoad);
    }
    
    [PunRPC]
    private void ProcedureMoveRequest(Vector3 position, Player p)
    {
        if (players.ContainsKey(p))
        {
            if (_canMove)
                players[p].Move(position);
            
            if (Vector3.Distance(players[p].transform.position, position) < 0.01f)
                _canMove = false;
        }
    }
    
    [PunRPC]
    private void ProcedureShootRequest(Vector3 position, Player p)
    {
        if (players.ContainsKey(p))
        {
            if (_shootTimer > players[p].timeToShoot)
                players[p].Shoot(position);
        }
    }
    
    [PunRPC]
    void SetIsShooting(bool v, Player p)
    {
        if (players.ContainsKey(p))
            players[p].SetIsShooting(v);
    }

    [PunRPC]
    void SetIsMoving(bool v, Player p)
    {
        if (players.ContainsKey(p))
            players[p].SetIsMoving(v);
    }
}
