using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
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
    public Text hpText;
    public float maxHp;
    public Player owner;
    public float range;
    public float angle;
    
    [HideInInspector] public bool isDead;
    
    private float _hp;
    private Animator _anim;
    private PhotonView _view;
    private float _speedTimer;
    private float _invulnerabilityTimer;
    private float _speedTime;
    private float _invulnerabilityTime;
    private float _speedValue;
    private bool _invulnerabilityActive;
    private List<Transform> _nodePath;
    private Node[] _pathFindingNodes;
    private int _currentWp;
    private GameObject _posToMove;
    
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        _view = GetComponent<PhotonView>();
        _hp = maxHp;
        _pathFindingNodes = FindObjectsOfType<Node>().ToArray();
        _posToMove = new GameObject();
    }

    private void Update()
    {    
        if (!_view.IsMine) return;
        
        Timers();
        TryToMove();
        
        if (!isDead && _hp <= 0)
        {
            isDead = true;
            
            if (_anim)
                _anim.SetBool("IsDead", true);
            
            StartCoroutine(Dead());
        }
    }

    private void TryToMove()
    {
        if (canMove)
            Move();

        SetIsMoving(Math.Abs(rb.velocity.magnitude) > 0.01f);
    }

    private void Move()
    {
       if (Vector3.Distance(transform.position, _posToMove.transform.position) < 0.3f || !canMove)
            SetCanMove(false, Vector3.zero);
            
       if (TargetSpoted())
           Arrive.D_Arrive(gameObject, _posToMove.transform.position, rb, speed, 0.2f);
       else
       {
           if (isShooting || _nodePath.ElementAtOrDefault(_currentWp) == null)
               return;
           
           var distance = _nodePath[_currentWp].position - transform.position;

           if (distance.magnitude > speed * Time.deltaTime)
           {
               Arrive.D_Arrive(gameObject, _nodePath[_currentWp].position, rb, speed, 0.2f);
               transform.forward = Vector3.Lerp(transform.forward, distance.normalized, 0.5f);
           }
           else
           {
               transform.position = _nodePath[_currentWp].position;
           }
       }
    }
    
    public void SetCanMove(bool v, Vector3 posToMove)
    {
        if (!_view.IsMine)
            return;
        
        canMove = v;
        
        if (v)
        {
            CalculePath(posToMove);
            _posToMove.transform.position = posToMove;
        }
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

    public IEnumerator Dead()
    {
        yield return new WaitForSeconds(1);
        gameObject.SetActive(false);
        LevelManager.Instance.PlayerDead(owner);
    }

    public void LifeChange(float amount)
    {
        if (amount < 0 && _invulnerabilityActive) return;
        
        _hp += amount;
        _view.RPC("OnLifeChange", owner, _hp);
    }
    
    [PunRPC] 
    private void OnLifeChange(float hp)
    {
        if (hpText)
            hpText.text = "HP: " + hp;
    }

    [PunRPC]
    private void OnInvulnerability(bool active)
    {
        if (hpText)
            hpText.color = active ? Color.magenta : Color.green;
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
        _speedTimer += Time.deltaTime;
        _invulnerabilityTimer += Time.deltaTime;
        
        if (_speedTimer >= _speedTime)
            ChangeSpeed(false, -_speedValue);
        
        if (_invulnerabilityTimer >= _invulnerabilityTime)
            ChangeInvulnerability(false);
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

    public void ChangeSpeed(bool active, float value, float time = 0)
    {
        speed += value;

        if (active)
        {
            _speedTimer = 0;
            _speedTime = time;
            _speedValue = value;
        }
    }

    public void ChangeInvulnerability(bool active, float time = 0)
    {
        _invulnerabilityActive = active;
        _view.RPC("OnInvulnerability", owner, active);

        if (active)
            _invulnerabilityTime = time;
    }

    private void CalculePath(Vector3 position)
    {
        _currentWp = 0;
        _nodePath = new List<Transform>();
            
        var currentNode = GetClosestNodeTo(gameObject.transform.position);
        var finalNode = GetClosestNodeTo(position);
        
        var nodes = AStar.AStarNodes(currentNode,finalNode, Heuristic);

        foreach (var node in nodes)
        {
            _nodePath.Add(node.transform); 
        }
        Debug.Log("Path Calculed: " + _nodePath.Count);
    }
    
    private float Heuristic(Node a, Node b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }
    
    private Node GetClosestNodeTo(Vector3 position)
    {
        var closestNode = _pathFindingNodes[0];
        foreach (var node in _pathFindingNodes)
        {
            if (Vector3.Distance(closestNode.transform.position, position) >
                Vector3.Distance(node.transform.position, position))
                closestNode = node;
        }

        return closestNode;
    }
    
    private bool TargetSpoted()
    {
        return BasicBehaviours.TargetIsInSight(gameObject.transform, _posToMove.transform, range, angle);
    }
    
    private void OnDrawGizmos()
    {
        var position = transform.position;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(position, range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, position + Quaternion.Euler(0, angle / 2, 0) * transform.forward * range);
        Gizmos.DrawLine(position, position + Quaternion.Euler(0, -angle / 2, 0) * transform.forward * range);
    }
}
