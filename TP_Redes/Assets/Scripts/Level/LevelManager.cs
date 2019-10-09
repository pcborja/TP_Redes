using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public Character[] characters;
    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        _networkManager.StartPlayer(characters);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
