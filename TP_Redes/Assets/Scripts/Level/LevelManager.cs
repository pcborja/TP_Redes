using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class LevelManager : MonoBehaviourPun
{
    public GameObject[] characterObjects;
    public GameObject[] enemiesObjects;
    public GameObject[] healPowerUpObjects;
    public GameObject[] armorPowerUpObjects;
    public GameObject[] invulnerabilityPowerUpObjects;
    public GameObject winObject;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
    public static LevelManager Instance { get; private set; }
    
    private NetworkManager _networkManager;
    private PhotonView _view;
    private AudioSource _audioSource;
    private Dictionary<string, AudioClip> _loadedSounds = new Dictionary<string, AudioClip>();
    private Dictionary<string, GameObject> _loadedEffects = new Dictionary<string, GameObject>();
    
    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        if (!_view.IsMine) return;
        
        _networkManager = FindObjectOfType<NetworkManager>();
        _audioSource = GetComponent<AudioSource>();

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
        InstantiatePrefabs(armorPowerUpObjects, "ArmorPowerUp");
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
            players[p].TryToDie();
    }

    [PunRPC]
    private void RemoveCharacter(Player p)
    {
        var characters = FindObjectsOfType<Character>().Where(x => Equals(x.owner, p)).ToArray();
        
        if (characters.Any())
            characters[0].gameObject.SetActive(false);
    }

    public void OnClicked(Vector3 hitPoint, Player p, bool isMovement, bool isPowerUp)
    {
        _view.RPC("CheckActions", RpcTarget.MasterClient, hitPoint, p, isMovement, isPowerUp);
    }

    [PunRPC]
    private void CheckActions(Vector3 hitPoint, Player p, bool isMovement, bool isPowerUp)
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
                if (isPowerUp)
                    hitPoint = new Vector3(hitPoint.x, players[p].transform.position.y - 0.5f, hitPoint.z);
                
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
        _networkManager.OnPlayerDeadStatus(p);
        
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
        armorPowerUpObjects = _networkManager.armorPowerUpObjects;
        invulnerabilityPowerUpObjects = _networkManager.invulnerabilityPowerUpObjects;
        winObject = _networkManager.winObject;
    }

    public void HealPowerUp(Character character, float amount)
    {
        character.LifeChange(amount);
    }
    
    public void ArmorPowerUp(Character character, float amount)
    {
        character.ArmorChange(amount);
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

    public void TryToPlayEffect(string effectName, Vector3 location, Vector3 forward)
    {
        _view.RPC("PlayEffect", RpcTarget.MasterClient, effectName, location, forward);
    }
    
    [PunRPC]
    private void PlayEffect(string effectName, Vector3 location, Vector3 forward)
    {
        _view.RPC("PlayEffectAtLocation", RpcTarget.AllBuffered, effectName, location, forward);
    }
    
    [PunRPC]
    private void PlayEffectAtLocation(string effectName, Vector3 location, Vector3 forward)
    {
        if (!_loadedEffects.ContainsKey(effectName))
            _loadedEffects.Add(effectName, Instantiate(Resources.Load<GameObject>(effectName)));

        _loadedEffects[effectName].transform.position = location;
        _loadedEffects[effectName].transform.forward = forward;

        if (!_loadedEffects[effectName].GetComponent<ParticleSystem>())
        {
            var particleSystems = _loadedEffects[effectName].GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Play();
            }
        }
        else
            _loadedEffects[effectName].GetComponent<ParticleSystem>().Play();
    }
    
    public void TryToPlaySound(string soundName, Vector3 location)
    {
        _view.RPC("PlaySound", RpcTarget.MasterClient, soundName, location);
    }

    [PunRPC]
    private void PlaySound(string soundName, Vector3 location)
    {
        _view.RPC("PlaySoundAtLocation", RpcTarget.AllBuffered, soundName, location);
    }

    public void PlaySoundForPlayer(string soundName, Vector3 location, Character character)
    {
        if (players.ContainsValue(character))
        {
            var player = players.FirstOrDefault(x => x.Value == character).Key;
            _view.RPC("PlaySoundAtLocation", player, soundName, location);
        }
    }
    
    [PunRPC]
    private void PlaySoundAtLocation(string soundName, Vector3 location)
    {
        if (!_loadedSounds.ContainsKey(soundName))
            _loadedSounds.Add(soundName, Resources.Load<AudioClip>(soundName));
            
        AudioSource.PlayClipAtPoint(_loadedSounds[soundName], location, 1);
    }
}
