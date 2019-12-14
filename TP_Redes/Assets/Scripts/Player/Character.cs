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
    public float damage;
    public bool isShooting;
    public float shootTimer;
    public Vector3 positionToMove;
    public Text hpText;
    public float maxHp;
    public Player owner;
    
    [HideInInspector] public bool isDead;
    
    private float _hp;
    private Animator _anim;
    private PhotonView _view;
    private float _direction;
    
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
        
        if (!isDead && _hp <= 0)
        {
            isDead = true;
            
            if (_anim)
                _anim.SetBool("IsDead", true);
            
            StartCoroutine(Dead());
        }
        
        transform.position += transform.forward * _direction * Time.deltaTime * speed;
    }

    public void Move(Vector3 dir)
    {
        if (isShooting) return;
        _direction = dir.z;
    }
    
    public void Rotate(Vector3 rot)
    {
        transform.Rotate(rot * Time.deltaTime);
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
    
    private void SetIsShooting(bool v)
    {
        isShooting = v;
        if (_anim)
            _anim.SetBool("IsShooting", v);
    }
    
    private IEnumerator Shooting()
    {
        var position = shootObject.position;
        var spawnPos = new Vector3(position.x, position.y + 1, position.z);
        var bullet = PhotonNetwork.Instantiate("Bullet", spawnPos, transform.rotation).GetComponent<Bullet>();
        
        bullet.shootBy = Bullet.ShootBy.Player;
        bullet.damage = damage;
        
        yield return new WaitForSeconds(1);
        
        SetIsShooting(false);
    }

    private IEnumerator Dead()
    {
        yield return new WaitForSeconds(1);
        LevelManager.Instance.PlayerDead(owner);
        gameObject.SetActive(false);
    }

    public void TakeDamage(float dmg)
    {
        _hp -= dmg;
        _view.RPC("OnLifeChange", owner, _hp);
    }
    
    [PunRPC] 
    void OnLifeChange(float hp)
    {
        if (hpText)
            hpText.text = "HP: " + hp;
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
    private void SetMyCamera()
    {
        Camera.main.GetComponent<CameraFollow>().SetTarget(transform);
    }
    
    public void SetOwner(Player p)
    {
        owner = p;
        photonView.RPC("SetLocalSettings", owner);
    }
    
    [PunRPC]
    private void SetLocalSettings()
    {
        hpText = FindObjectOfType<Text>();
        hpText.text = "HP: " + maxHp;
    }
}
