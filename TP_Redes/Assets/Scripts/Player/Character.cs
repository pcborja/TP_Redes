using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Character : MonoBehaviourPun
{
    public GameObject cameraPos;
    public bool isHost;
    public float hp;
    public float speed;
    private PhotonView _view;
    public Rigidbody rb;
    public bool canMove;
    public float timeToShoot = 1;
    public Transform shootObject;
    public GameObject bulletPrefab;
    private int _packagePerSecond = 20;

    bool _canMove;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        isHost = PhotonNetwork.LocalPlayer.IsMasterClient;
    }

    private void Update()
    {    
        if (!_view.IsMine) return;
        
        if (hp <= 0)
            LevelManager.Instance.Disconnect("LoseScene");
    }

    private void FixedUpdate()
    {
        if (!_view.IsMine)
            return;

        LevelManager.Instance.RequestMove(PhotonNetwork.LocalPlayer);
    }

    public void Move(Vector3 position)
    {
        rb.AddForce((position - transform.position) * speed);
    }

    public void Shoot(Vector3 position)
    {
        transform.LookAt(position);
        var spawnPos = new Vector3(shootObject.position.x, shootObject.position.y + 1, shootObject.position.z);
        Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
    }
    
    public void SetPackagesPerSecond(int pps)
    {
        _packagePerSecond = pps;
    }
    
    public void SetIsMoving(bool v)
    {
        //Animations and stuff
            /*if (_anim)
                _anim.SetBool("IsMoving", v);*/
    }
    
    public void SetIsShooting(bool v)
    {
        //Animations and stuff
        /*if (_anim)
            _anim.SetBool("IsMoving", v);*/
    }

    public void SetCanMove(bool v)
    {
        if (!_view.IsMine)
            return;
        _canMove = v;
    }
}
