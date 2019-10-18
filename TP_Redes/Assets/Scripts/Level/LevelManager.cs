using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LevelManager : MonoBehaviourPun
{
    public GameObject[] characterObjects;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
    public Dictionary<Player, Controller> playersController = new Dictionary<Player, Controller>();
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
        characterObjects = _networkManager.playerPositions;
        if (!Instance)
        {
            if (_view.IsMine)
                StartCoroutine(SetPlayers());
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
    
    private void Timers()
    {
        _shootTimer += Time.deltaTime;
    }

    private IEnumerator SetPlayers()
    {
        yield return new WaitForSeconds(0.5f);
        _view.RPC("SetReference", RpcTarget.AllBuffered);
    }
    
    [PunRPC]
    private void SetReference()
    {
        Instance = this;
        _view.RPC("AddPlayer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        CreateController(PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void AddPlayer(Player p)
    {
        var character = CreatePlayer(p);
        players.Add(p, character);
    }

    public void StartPlayerData(Player p)
    {
        _view.RPC("StartPlayer", RpcTarget.MasterClient, p);
    }

    [PunRPC]
    private void StartPlayer(Player p)
    {
        if (!players.ContainsKey(p)) return;
        
        var characterObject = characterObjects[p.ActorNumber - 1];
        players[p].transform.SetParent(characterObject.transform);
        players[p].transform.rotation = Quaternion.identity;
    }

    private Character CreatePlayer(Player p)
    {
        var characterObject = characterObjects[p.ActorNumber - 1];
        var instantiatedChar = PhotonNetwork.Instantiate("Character", characterObject.transform.position, Quaternion.identity);
        instantiatedChar.gameObject.name = instantiatedChar.gameObject.name + " " + (p.ActorNumber - 1);
        return instantiatedChar.GetComponent<Character>();
    }

    private void CreateController(Player p)
    {
        var controller = PhotonNetwork.Instantiate("Controller", Vector3.zero, Quaternion.identity).GetComponent<Controller>();
        controller.SetPPS(20);
        controller.myCam.tag = "MainCamera";
        playersController.Add(p, controller);
        _view.RPC("GetPlayerHP", RpcTarget.MasterClient, p);
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
        if (Physics.Raycast(playersController[p].myCam.ScreenPointToRay(mousePosition), out var hit, 100))
        {
            bool hitIsEnemy = hit.transform.gameObject.GetComponent<Enemy>();
            bool hitIsCharacter = hit.transform.gameObject.GetComponent<Character>();
            _view.RPC("CheckActions", RpcTarget.MasterClient, hit.point, p, hitIsEnemy, hitIsCharacter);
        }
    }

    public void MoveCamera(Player p)
    {
        _view.RPC("MoveCharacterCamera", RpcTarget.MasterClient, p);
    }

    [PunRPC]
    private void MoveCharacterCamera(Player p)
    {   
        var character = players[p];
        var position = character.transform.position;
        var charPosX = position.x;
        var charPosZ = position.z + 2;
        var charPosY = position.y + 10;

        _view.RPC("MoveCamera", p, p, new Vector3(charPosX, charPosY, charPosZ));
        
    }

    [PunRPC]
    private void MoveCamera(Player p, Vector3 position)
    {
        playersController[p].myCam.transform.position = position;
    }

    [PunRPC]
    private void CheckActions(Vector3 hitPoint, Player p, bool hitIsEnemy, bool hitIsCharacter)
    {
        if (players.ContainsKey(p))
        {
            if (hitIsEnemy || players[p].isHoldingPosition)
            {
                if (_shootTimer > players[p].timeToShoot)
                {
                    _shootTimer = 0;
                    players[p].Shoot(hitPoint);
                }  
            }
            else if (!hitIsCharacter)
            {
                players[p].SetCanMove(true);
                _positionToMove = hitPoint;
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

            players[p].SetIsMoving(Math.Abs(players[p].rb.velocity.magnitude) > 0.01f);
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

    public void NotifyWinner()
    {
        _view.RPC("FinishGame", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    public void FinishGame(Player p)
    {
        var masterWon = p.IsMasterClient;

        foreach (var player in players)
        {
            if (player.Key.IsMasterClient)
                continue;
            
            _view.RPC(player.Key.Equals(p) ? "WinGame" : "LoseGame", player.Key);
        }
        
        _view.RPC(masterWon ? "WinGame" : "LoseGame", PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void WinGame()
    {
        Disconnect("WinScene");
    }
    
    [PunRPC]
    private void LoseGame()
    {
        Disconnect("LoseScene");
    }

    public void TakeDamage(float damage, Player p)
    {
        _view.RPC("TakeDmg", RpcTarget.MasterClient, damage, p);
        _view.RPC("GetPlayerHP", RpcTarget.MasterClient, p);
    }
    
    [PunRPC]
    public void TakeDmg(float damage, Player p)
    {
        if (players.ContainsKey(p))
        {
            players[p].hp -= damage;
        }
    }
    
    [PunRPC]
    public void GetPlayerHP(Player p)
    {
        if (players.ContainsKey(p))
        {
            _view.RPC("UpdatePlayerHUD", p,p, players[p].hp);
        }
    }
    
    [PunRPC]
    private void UpdatePlayerHUD(Player p, float hp)
    {
        playersController[p].UpdateHUD(hp);
    }

    public void InstantiateBullet(Player p, Vector3 spawnPos)
    {
        _view.RPC("CreateBullet", RpcTarget.MasterClient, p, spawnPos);
    }

    [PunRPC]
    private void CreateBullet(Player p, Vector3 spawnPos)
    {
        if (!players.ContainsKey(p)) return;
        
        var bullet = PhotonNetwork.Instantiate("Bullet", spawnPos, players[p].transform.rotation);
        bullet.GetComponent<Bullet>().shootBy = Bullet.ShootBy.Player;
        bullet.GetComponent<Bullet>().damage = players[p].damage;
    }
}
