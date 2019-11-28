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
        
        if (!Instance && PhotonNetwork.IsMasterClient)
        {
            _view.RPC("SetReference", RpcTarget.AllBuffered);
            StartEnemies();
        }
        else if (Instance)
            PhotonNetwork.Destroy(gameObject);
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
        character.SetView();
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
        if (players.ContainsKey(p))
        {
            PhotonNetwork.Destroy(players[p].gameObject);
            players.Remove(p);
        }
        
        _networkManager.DisconnectPlayer(sceneToLoad, p);
    }

    [PunRPC]
    private void RemoveCharacter(Player p)
    {
        var characters = FindObjectsOfType<Character>().Where(x => Equals(x.owner, p)).ToArray();
        
        if (characters.Any())
            characters[0].gameObject.SetActive(false);
    }

    public void OnClicked(Vector3 hitPoint, Player p, bool hitIsEnemy, bool hitIsCharacter)
    {
        _view.RPC("CheckActions", RpcTarget.MasterClient, hitPoint, p, hitIsEnemy, hitIsCharacter);
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
        _view.RPC("Disconnect", RpcTarget.MasterClient, "LoseScene", p);
    }
    
    [PunRPC]
    public void FinishGame(Player p)
    {
        if (PhotonNetwork.IsMasterClient) return;
        
        foreach (var player in players)
        {
            _view.RPC("Disconnect", RpcTarget.MasterClient, player.Key.Equals(p) ? "WinScene" : "LoseScene", p);
        }
    }
}
