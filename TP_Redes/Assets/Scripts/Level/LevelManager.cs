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
    public GameObject winObject;
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
        winObject = _networkManager.winObject;
        
        if (!Instance && PhotonNetwork.IsMasterClient)
        {
            _view.RPC("SetReference", RpcTarget.AllBuffered);
            StartEnemies();
            StartWinObject();
        }
        else if (Instance)
            PhotonNetwork.Destroy(gameObject);
    }

    private void StartWinObject()
    {
        PhotonNetwork.Instantiate("WinObject", winObject.transform.position, Quaternion.identity);
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

    public void Disconnect(Player p)
    {
        _view.RPC("DisconnectRPC", RpcTarget.MasterClient, p);
    }

    [PunRPC]
    public void DisconnectRPC(Player p)
    {
        if (players.ContainsKey(p))
        {
            PhotonNetwork.Destroy(players[p].gameObject);
            players.Remove(p);
        }
        
        _networkManager.DisconnectPlayer(p);
    }

    [PunRPC]
    private void RemoveCharacter(Player p)
    {
        var characters = FindObjectsOfType<Character>().Where(x => Equals(x.owner, p)).ToArray();
        
        if (characters.Any())
            characters[0].gameObject.SetActive(false);
    }

    public void OnClicked(Vector3 hitPoint, Player p)
    {
        _view.RPC("Shoot", RpcTarget.MasterClient, hitPoint, p);
    }

    [PunRPC]
    private void Shoot(Vector3 hitPoint, Player p)
    {
        if (players.ContainsKey(p))
        {
            if (players[p].shootTimer > players[p].timeToShoot)
            {
                players[p].shootTimer = 0;
                players[p].Shoot(hitPoint);
            }
        }
    }

    public void NotifyWinner(Character c)
    {
        _networkManager.FinishGame(players.FirstOrDefault(x => x.Value == c).Key);
    }

    public void PlayerDead(Player p)
    {
        if (players.All(x => x.Value.isDead))
            _networkManager.FinishGame(null);
        else
            _networkManager.PlayerLose(p);
    }

    public void PlayerRequestMove(Vector3 dir, Player p)
    {
        _view.RPC("RequestMove", RpcTarget.MasterClient, dir, p);
    }

    public void PlayerRequestRotation(float dir, Player p)
    {
        _view.RPC("RequestRotation", RpcTarget.MasterClient, dir, p);
    }

    [PunRPC]
    private void RequestMove(Vector3 dir, Player p)
    {
        if (players.ContainsKey(p))
        {
            players[p].Move(dir);
            players[p].SetIsMoving(dir != Vector3.zero);
        }
    }
    
    [PunRPC]
    void RequestRotation(float dir, Player p)
    {
        if (players.ContainsKey(p))
            players[p].Rotate(new Vector3(0, dir * 45, 0));
    }
}
