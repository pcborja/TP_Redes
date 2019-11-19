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
    public GameObject[] enemiesObjects;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
    public Dictionary<Player, Controller> playersController = new Dictionary<Player, Controller>();
    public static LevelManager Instance { get; private set; }
    private NetworkManager _networkManager;
    private PhotonView _view;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        if (!_view.IsMine) return;
        
        _networkManager = FindObjectOfType<NetworkManager>();
        characterObjects = _networkManager.playerPositions;
        enemiesObjects = _networkManager.enemiesPositions;
        _networkManager.ClearLobbyData();
        
        if (!Instance && PhotonNetwork.IsMasterClient)
        {
            SetPlayers();
            StartCoroutine(SetEnemies());
        }
        else if (Instance)
            PhotonNetwork.Destroy(gameObject);
    }

    private IEnumerator SetEnemies()
    {
        yield return new WaitForSeconds(0.5f);
        _view.RPC("StartEnemies", RpcTarget.MasterClient);
    }
    
    private void SetPlayers()
    {
        _view.RPC("SetReference", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void StartEnemies()
    {
        foreach (var enemyObj in enemiesObjects)
        {
            PhotonNetwork.Instantiate("Enemy", enemyObj.transform.position, Quaternion.identity);
        }
    }

    [PunRPC]
    private void SetReference()
    {
        Instance = this;
        if (PhotonNetwork.IsMasterClient) return;
        
        CreateController(PhotonNetwork.LocalPlayer);
    }

    public void StartPlayer(Player p)
    {
        _view.RPC("AddPlayer", RpcTarget.MasterClient, p);
    }
    
    [PunRPC]
    private void AddPlayer(Player p)
    {
        var character = CreatePlayer(p);
        players.Add(p, character);
    }

    private Character CreatePlayer(Player p)
    {
        var characterObject = GetCharacterObject();
        var instantiatedChar = PhotonNetwork.Instantiate("Character", characterObject.transform.position, Quaternion.identity);
        instantiatedChar.transform.SetParent(characterObject.transform);
        instantiatedChar.gameObject.name = instantiatedChar.gameObject.name + " " + p.NickName;
        return instantiatedChar.GetComponent<Character>();
    }

    private GameObject GetCharacterObject()
    {
        return characterObjects.FirstOrDefault(charObj => !charObj.GetComponentInChildren<Character>());
    }

    private void CreateController(Player p)
    {
        var controller = PhotonNetwork.Instantiate("Controller", Vector3.zero, Quaternion.identity).GetComponent<Controller>();
        controller.SetPPS(20);
        controller.myCam.tag = "MainCamera";
        playersController.Add(p, controller);
        _view.RPC("GetPlayerHP", RpcTarget.MasterClient, p);
    }

    public void Disconnect(string sceneToLoad, Player p)
    {
        players[p].gameObject.SetActive(false);
        players.Remove(p);
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
                if (players[p].shootTimer > players[p].timeToShoot)
                {
                    players[p].shootTimer = 0;
                    players[p].Shoot(hitPoint);
                }  
            }
            else if (!hitIsCharacter)
            {
                players[p].SetCanMove(true, hitPoint);
            }
        }
    }

    public void RequestMove(Player p, Vector3 posToMove)
    {
        _view.RPC("Move", RpcTarget.MasterClient, p, posToMove);
    }

    [PunRPC]
    private void Move(Player p, Vector3 posToMove)
    {
        if (players.ContainsKey(p))
        {
            if (players[p].canMove)
                players[p].Move(posToMove);

            if (Vector3.Distance(players[p].transform.position, posToMove) < 0.1f)
            {
                players[p].SetCanMove(false, Vector3.zero);
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

    public void NotifyWinner(Player p)
    {
        _view.RPC("FinishGame", RpcTarget.MasterClient, p);
    }

    public void PlayerDead(Player p)
    {
        _view.RPC("DisconnectPlayer", RpcTarget.MasterClient, "LoseScene", p);
    }

    [PunRPC]
    private void DisconnectPlayer(string sceneToLoad, Player p)
    {
        Disconnect(sceneToLoad, p);    
    }
    
    [PunRPC]
    public void FinishGame(Player p)
    {
        var masterWon = p.IsMasterClient;

        foreach (var player in players)
        {
            if (player.Key.IsMasterClient)
                continue;

            Disconnect(player.Key.Equals(p) ? "WinScene" : "LoseScene", p);
        }
        
        Disconnect(masterWon ? "WinScene" : "LoseScene", p);
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
