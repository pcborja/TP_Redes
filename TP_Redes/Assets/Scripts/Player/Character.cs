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
    public float timeToShoot;
    public Transform shootObject;
    public GameObject bulletPrefab;
    private int _packagePerSecond = 20;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        isHost = PhotonNetwork.LocalPlayer.IsMasterClient;
        
        if (_view.IsMine)
            StartCoroutine(SendPackageMovement());
    }

    private IEnumerator SendPackageMovement()
    {
        while(true)
        {
            yield return new WaitForSeconds( 1 / _packagePerSecond);
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100))
            {
                if (hit.transform.gameObject.GetComponent<Enemy>())
                {
                    LevelManager.Instance.RequestShoot(hit.point, PhotonNetwork.LocalPlayer);
                }
                else
                {
                    LevelManager.Instance.RequestMovement(hit.point, PhotonNetwork.LocalPlayer);        
                }
            }
            
        }
    }

    private void Update()
    {    
        if (!_view.IsMine) return;
        
        if (hp <= 0)
            LevelManager.Instance.Disconnect("LoseScene");
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
}
