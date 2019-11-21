using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

public class Character : MonoBehaviourPun
{
    public float speed;
    public Rigidbody rb;
    public bool canMove;
    public float timeToShoot = 1;
    public Transform shootObject;
    public bool isHoldingPosition;
    public float damage;
    public bool isShooting;
    public float shootTimer;
    public Vector3 positionToMove;
    public Text hpText;
    public float maxHp;
    public Camera myCam;
    
    private float _hp;
    private Animator _anim;
    private PhotonView _view;
    private Player _owner;
    
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        _view = GetComponent<PhotonView>();
        _hp = maxHp;
    }

    private void Update()
    {    
        if (!_view.IsMine) return;
        
        Timers();
        
        if (_hp <= 0)
        {
            if (_anim)
                _anim.SetBool("IsDead", true);
            StartCoroutine(Dead());
        }
    }

    private void FixedUpdate()
    {
        if (!_view.IsMine)
            return;

        TryToMove(positionToMove);
        //LevelManager.Instance.RequestMove(PhotonNetwork.LocalPlayer, positionToMove);
    }

    private void TryToMove(Vector3 posToMove)
    {
        if (canMove)
            Move(posToMove);

        if (Vector3.Distance(transform.position, posToMove) < 0.1f)
        {
            SetCanMove(false, Vector3.zero);
        }

        SetIsMoving(Math.Abs(rb.velocity.magnitude) > 0.01f);
    }

    public void Move(Vector3 position)
    {
        if (isShooting) return;
        Arrive.D_Arrive(gameObject, position, rb, 10, 0.5f);
    }

    public void Shoot(Vector3 position)
    {
        SetIsShooting(true);
        InstantRotation(position);
        StartCoroutine(Shooting());
    }
    
    public void SetIsMoving(bool v)
    {
        if (_anim)
            _anim.SetBool("IsMoving", v);
    }
    
    public void SetIsShooting(bool v)
    {
        isShooting = v;
        if (_anim)
            _anim.SetBool("IsShooting", v);
    }

    public void SetCanMove(bool v, Vector3 posToMove)
    {
        if (!_view.IsMine)
            return;

        positionToMove = posToMove;
        canMove = v;
    }

    public void SetHoldingPos(bool holdingPos)
    {
        if (!_view.IsMine) return;
        isHoldingPosition = holdingPos;
    }
    
    private IEnumerator Shooting()
    {
        yield return new WaitForSeconds(1);
        var spawnPos = new Vector3(shootObject.position.x, shootObject.position.y + 1, shootObject.position.z);
        LevelManager.Instance.InstantiateBullet(PhotonNetwork.LocalPlayer, spawnPos);

        SetIsShooting(false);
    }

    private IEnumerator Dead()
    {
        yield return new WaitForSeconds(1);
        LevelManager.Instance.PlayerDead(PhotonNetwork.LocalPlayer);
    }

    public void TakeDamage(float dmg)
    {
        _hp -= dmg;
        _view.RPC("OnLifeChange", _owner, _hp);
    }
    
    [PunRPC]
    void OnLifeChange(float hp)
    {
        hpText.text = hp.ToString();
    }

    private void InstantRotation(Vector3 position)
    {
        var transform1 = transform;
        var position1 = transform1.position;
        var xzPos = new Vector3 (position.x, position1.y, position.z);
        transform1.forward = (xzPos - position1).normalized;
    }
    
    private void Timers()
    {
        shootTimer += Time.deltaTime;
    }

    public void SetView()
    {
        _view = GetComponent<PhotonView>();
    }
    
    public void SetCamera(Player p)
    {
        _view.RPC("SetMyCamera", p);
    }
    
    [PunRPC]
    void SetMyCamera()
    {
        myCam = Camera.main;
        myCam.GetComponent<CameraFollow>().SetTarget(transform);
    }
    
    public void SetOwner(Player p)
    {
        _owner = p;
        photonView.RPC("SetLocalSettings", _owner);
    }
    
    [PunRPC]
    void SetLocalSettings()
    {
        hpText = FindObjectOfType<Text>();
        hpText.text = _hp.ToString();
    }
}
