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
            _view.RPC("SetReference", RpcTarget.AllBuffered);
            StartCoroutine(SetEnemies());
        }
        else if (Instance)
            PhotonNetwork.Destroy(gameObject);
    }

    private IEnumerator SetEnemies()
    {
        yield return new WaitForSeconds(0.5f);
        StartEnemies();
    }

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
        
        if (!PhotonNetwork.IsMasterClient)
            _view.RPC("AddPlayer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }
    
    [PunRPC]
    private void AddPlayer(Player p)
    {
        if (!_view.IsMine) return;
        
        CreatePlayer(p);
    }

    private void CreatePlayer(Player p)
    {
        var characterObject = GetCharacterObject();
        var instantiatedChar = PhotonNetwork.Instantiate("Character", characterObject.transform.position, Quaternion.identity);
        var character = instantiatedChar.GetComponent<Character>();
       
        players.Add(p, character);
        
        instantiatedChar.transform.SetParent(characterObject.transform);
        instantiatedChar.gameObject.name = instantiatedChar.gameObject.name + " " + p.NickName;
        character.SetMyView();
        character.SetCamera(p);
        character.SetOwner(p);
    }

    private GameObject GetCharacterObject()
    {
        return characterObjects.FirstOrDefault(charObj => !charObj.GetComponentInChildren<Character>());
    }

    public void OnDisconnect(string sceneToLoad, Player p)
    {
        _view.RPC("Disconnect", RpcTarget.MasterClient, sceneToLoad, p);
    }

    [PunRPC]
    private void Disconnect(string sceneToLoad, Player p)
    {
        players[p].gameObject.SetActive(false);
        players.Remove(p);
        _networkManager.DisconnectPlayer(sceneToLoad, p);
    }

    public void OnClicked(Vector3 mousePosition, Player p)
    {
        _view.RPC("CheckActions", RpcTarget.MasterClient, mousePosition, p);
    }

    [PunRPC]
    private void CheckActions(Vector3 mousePosition, Player p)
    {
        if (Physics.Raycast(players[p].myCam.ScreenPointToRay(mousePosition), out var hit, 100))
        {
            bool hitIsEnemy = hit.transform.gameObject.GetComponent<Enemy>();
            bool hitIsCharacter = hit.transform.gameObject.GetComponent<Character>();

            if (players.ContainsKey(p))
            {
                if (hitIsEnemy || players[p].isHoldingPosition)
                {
                    if (players[p].shootTimer > players[p].timeToShoot)
                    {
                        players[p].shootTimer = 0;
                        players[p].Shoot(hit.point);
                    }
                }
                else if (!hitIsCharacter)
                {
                    players[p].SetCanMove(true, hit.point);
                }
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
