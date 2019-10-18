using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviourPun
{
    public float hp;
    public float speed;
    private PhotonView _view;
    public Rigidbody rb;
    public bool canMove;
    public float timeToShoot = 1;
    public Transform shootObject;
    public bool isHoldingPosition;
    private Animator _anim;
    public float damage;
    public bool isShooting;
    private Text _hpText; 
    
    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        _anim = GetComponent<Animator>();
        _hpText = GameObject.Find("HPText").GetComponent<Text>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {    
        if (!_view.IsMine) return;
        
        if (hp <= 0)
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
        
        LevelManager.Instance.RequestMove(PhotonNetwork.LocalPlayer);
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

    public void SetCanMove(bool v)
    {
        if (!_view.IsMine)
            return;
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
        LevelManager.Instance.Disconnect("LoseScene");
    }

    public void TakeDamage(float dmg)
    {
        LevelManager.Instance.TakeDamage(dmg, PhotonNetwork.LocalPlayer);
    }

    private void InstantRotation(Vector3 position)
    {
        var transform1 = transform;
        var position1 = transform1.position;
        var xzPos = new Vector3 (position.x, position1.y, position.z);
        transform1.forward = (xzPos - position1).normalized;
    }

    public void UpdateHUD(float hpToUse)
    {
        _hpText.text = "HP: " + hpToUse;
    }
}
