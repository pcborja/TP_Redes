using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LevelManager : MonoBehaviourPun
{
    public GameObject[] characterObjects;
    public List<Character> characters;
    public Dictionary<Player, Character> players = new Dictionary<Player, Character>();
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
        if (!Instance)
        {
            if (_view.IsMine)
            {
                _view.RPC("SetReference", RpcTarget.AllBuffered);
                CreateController();
            }
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
    }
    
    private void Timers()
    {
        _shootTimer += Time.deltaTime;
    }

    [PunRPC]
    private void SetReference()
    {
        Instance = this;
        _view.RPC("AddPlayer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        //_view.RPC("CreateController", RpcTarget.All);
    }

    [PunRPC]
    private void AddPlayer(Player p)
    {
        var currPlayerObj = characterObjects[PhotonNetwork.LocalPlayer.ActorNumber - 1];
        var instantiatedChar = Instantiate(Resources.Load<Character>("Character"));
        instantiatedChar.transform.SetParent(currPlayerObj.transform);
        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(instantiatedChar.cameraPos.transform);
            Camera.main.transform.position = Vector3.zero;
        }
            
        players.Add(p, instantiatedChar);
    }

    //[PunRPC]
    void CreateController()
    {
        if (!_view.IsMine) return;
        Instantiate(Resources.Load<GameObject>("Controller"));
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

    [PunRPC]
    private void DrawRaycast(Vector3 mousePos, Player p)
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out var hit, 100))
        {
            if (hit.transform.gameObject.GetComponent<Enemy>())
            {
                if (players.ContainsKey(p))
                {
                    if (_shootTimer > players[p].timeToShoot)
                    {
                        _shootTimer = 0;
                        players[p].Shoot(mousePos);
                    }                        
                }
            }
            else
            {
                players[p].SetCanMove(true);
                _positionToMove = hit.point;
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

            if (Vector3.Distance(players[p].transform.position, _positionToMove) < 0.01f)
                players[p].SetCanMove(false);

            players[p].SetIsMoving(_positionToMove != Vector3.zero);
        }
    }
}
