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
        players[p].UpdateHUD(players[p].hp);
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
        playersController.Add(p, controller);
        controller.myCam = Instantiate(Resources.Load<Camera>("CameraPos"), characterObjects[p.ActorNumber - 1].transform, true);
        controller.myCam.transform.eulerAngles = new Vector3(65,180,0);
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

    public void MoveCamera(Player p)
    {
        _view.RPC("MoveCharacterCamera", RpcTarget.MasterClient, p);
    }

    [PunRPC]
    private void MoveCharacterCamera(Player p)
    {
        if (!playersController.ContainsKey(p)) return;
        
        var character = players[p];
        var position = character.transform.position;
        var charPosX = position.x;
        var charPosZ = position.z + 3;
        var charPosY = position.y + 10;

        playersController[p].myCam.transform.position = new Vector3(charPosX, charPosY, charPosZ);
    }

    [PunRPC]
    private void DrawRaycast(Vector3 mousePos, Player p)
    {
        if (players.ContainsKey(p) && playersController.ContainsKey(p))
        {
            if (Physics.Raycast(playersController[p].myCam.ScreenPointToRay(mousePos), out var hit, 100))
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

    public void NotifyWinner(Character character)
    {
        var player = players.FirstOrDefault(x => x.Value == character).Key;
        _view.RPC("FinishGame", RpcTarget.All, player);
    }

    [PunRPC]
    public void FinishGame(Player player)
    {
        Disconnect(Equals(PhotonNetwork.LocalPlayer, player) ? "WinScene" : "LoseScene");
    }

    public void TakeDamage(float damage, Player p)
    {
        _view.RPC("TakeDmg", RpcTarget.MasterClient, damage, p);
    }
    
    [PunRPC]
    public void TakeDmg(float damage, Player p)
    {
        if (players.ContainsKey(p))
        {
            players[p].hp -= damage;
            players[p].UpdateHUD(players[p].hp);
        }
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
