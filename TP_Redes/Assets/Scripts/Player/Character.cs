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
    public PhotonView view;
    public Rigidbody rb;
    private LevelManager _levelManager;
    private bool canMove;
    private Vector3 positionToMove;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        _levelManager = FindObjectOfType<LevelManager>();
    }
    
    private void Start()
    {
        isHost = PhotonNetwork.LocalPlayer.IsMasterClient;
    }

    private void Update()
    {    
        Inputs();
        Movement();
    }
    
    private void FixedUpdate()
    {
        
    }

    private void Inputs()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100))
            {
                canMove = true;
                positionToMove = hit.point;
            }
        }
    }

    private void Movement()
    {
        if (canMove)
        {
            rb.AddForce((positionToMove - transform.position) * speed);
        }
        if (Vector3.Distance(transform.position, positionToMove) < 0.01f)
        {
            canMove = false;
        }
    }
    
    public void DestroyPlayer()
    {
        _levelManager.Disconnect("LoseScene");
    }
}
