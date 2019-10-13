using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviourPun
{
    public GameObject cameraPos;
    public float hp;
    public float speed;
    private PhotonView _view;
    public Rigidbody rb;
    public bool canMove;
    public float timeToShoot = 1;
    public Transform shootObject;
    public GameObject bulletPrefab;
    public bool isHoldingPosition;
    private Animator _anim;
    public Camera myCam;
    public float damage;
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

        CameraFollow();
        
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
        LookToPosition(position);
        rb.AddForce((position - transform.position) * speed);
    }

    public void Shoot(Vector3 position)
    {
        SetIsShooting(true);
        LookToPosition(position);
        StartCoroutine(Shooting());
    }
    
    public void SetIsMoving(bool v)
    {
        if (_anim)
            _anim.SetBool("IsMoving", v);
    }
    
    public void SetIsShooting(bool v)
    {
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
        var bullet = Instantiate(bulletPrefab, spawnPos, transform.rotation);
        bullet.GetComponent<Bullet>().shootBy = Bullet.ShootBy.Player;
        bullet.GetComponent<Bullet>().damage = damage;
        SetIsShooting(false);
    }

    private IEnumerator Dead()
    {
        yield return new WaitForSeconds(1);
        LevelManager.Instance.Disconnect("LoseScene");
    }
    
    private void CameraFollow()
    {
        var charPosX = transform.position.x;
        var charPosZ = transform.position.z + 3;
        var charPosY = transform.position.y + 10;
 
        myCam.transform.position = new Vector3(charPosX, charPosY, charPosZ);
    }

    public void TakeDamage(float dmg)
    {
        LevelManager.Instance.TakeDamage(dmg, PhotonNetwork.LocalPlayer);
    }
    
    private void LookToPosition(Vector3 position)
    {
        var localTarget = transform.InverseTransformPoint(position);
        var angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        var eulerAngleVelocity = new Vector3 (0, angle, 0);
        var deltaRotation = Quaternion.Euler(eulerAngleVelocity * Time.deltaTime * 10 );
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    public void UpdateHUD(float hpToUse)
    {
        _hpText.text = "HP: " + hpToUse;
    }
}
