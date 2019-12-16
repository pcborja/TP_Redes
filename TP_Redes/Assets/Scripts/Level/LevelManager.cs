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
    public GameObject[] healPowerUpObjects;
    public GameObject[] speedPowerUpObjects;
    public GameObject[] invulnerabilityPowerUpObjects;
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

        GetObjectPositions();
        
        if (!Instance && PhotonNetwork.IsMasterClient)
        {
            _view.RPC("SetReference", RpcTarget.AllBuffered);
            StartEnemies();
            StartWinObject();
            StartPowerUps();
        }
        else if (Instance)
            PhotonNetwork.Destroy(gameObject);
    }

    private void StartPowerUps()
    {
        InstantiatePrefabs(healPowerUpObjects, "HealPowerUp");
        InstantiatePrefabs(speedPowerUpObjects, "SpeedPowerUp");
        InstantiatePrefabs(invulnerabilityPowerUpObjects, "InvulnerabilityPowerUp");
    }

    private void StartWinObject()
    {
        PhotonNetwork.Instantiate("WinObject", winObject.transform.position, Quaternion.identity);
    }

    private void StartEnemies()
    {
        InstantiatePrefabs(enemiesObjects, "Enemy");
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

    public void OnClicked(Vector3 hitPoint, Player p, bool isMovement)
    {
        _view.RPC("CheckActions", RpcTarget.MasterClient, hitPoint, p, isMovement);
    }

    [PunRPC]
    private void CheckActions(Vector3 hitPoint, Player p, bool isMovement)
    {
        if (players.ContainsKey(p))
        {
            if (players[p].shootTimer > players[p].timeToShoot && !isMovement)
            {
                players[p].shootTimer = 0;
                players[p].Shoot(hitPoint);
            }
            else if (isMovement)
            {
                players[p].SetCanMove(true, hitPoint);
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
    
    private void GetObjectPositions()
    {
        characterObjects = _networkManager.playerPositions;
        enemiesObjects = _networkManager.enemiesPositions;
        healPowerUpObjects = _networkManager.healPowerUpObjects;
        speedPowerUpObjects = _networkManager.speedPowerUpObjects;
        invulnerabilityPowerUpObjects = _networkManager.invulnerabilityPowerUpObjects;
        winObject = _networkManager.winObject;
    }

    public void HealPowerUp(Character character, float amount)
    {
        character.LifeChange(amount);
    }
    
    public void SpeedPowerUp(Character character, float value, float time)
    {
        character.ChangeSpeed(true, value, time);
    }
    
    public void InvulnerabilityPowerUp(Character character, float time)
    {
        character.ChangeInvulnerability(true, time);
    }

    private void InstantiatePrefabs(IEnumerable<GameObject> places, string prefabName)
    {
        foreach (var place in places)
        {
            PhotonNetwork.Instantiate(prefabName, place.transform.position, Quaternion.identity);
        }
    }
}
