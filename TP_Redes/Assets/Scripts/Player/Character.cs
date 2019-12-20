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
    public bool canMove;
    public float timeToShoot = 1;
    public Transform shootObject;
    public float damage;
    public bool isShooting;
    public float shootTimer;
    public float maxHp;
    public float maxArmor;
    public Player owner;
    public float range;
    public float angle;
    public Image healthBar;
    public Image armorBar;
    public int pathfindingMaxNodes;
    
    [HideInInspector] public bool isDead;
    
    private float _hp;
    private float _armor;
    private Animator _anim;
    private PhotonView _view;
    private float _invulnerabilityTimer;
    private float _invulnerabilityTime;
    private bool _invulnerabilityActive;
    private List<Transform> _nodePath;
    private Node[] _pathFindingNodes;
    private int _currentWp;
    private bool _alreadySpotted;
    private GameObject _posToMove;
    private Vector3 UIPos = new Vector3(0f, 0.424f, 0.86f);
    
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }
    
    private void Start()
    {
        _view = GetComponent<PhotonView>();
        _hp = maxHp;
        _pathFindingNodes = FindObjectsOfType<Node>().ToArray();
        _posToMove = new GameObject();
        _posToMove.AddComponent<SphereCollider>().isTrigger = true;
    }

    private void Update()
    {    
        if (!_view.IsMine || isDead || !PhotonNetwork.IsMasterClient) return;
        
        Timers();
        TryToMove();
    }

    private void TryToMove()
    {
        if (canMove)
            Move();
    }

    private void Move()
    {
       if (Vector3.Distance(transform.position, _posToMove.transform.position) < 1f || !canMove)
            SetCanMove(false, Vector3.zero);

       if (TargetSpoted())
       {
           _alreadySpotted = true;
           var step = speed * Time.deltaTime;
           transform.parent.transform.position = Vector3.MoveTowards(transform.position, _posToMove.transform.position, step);
           transform.LookAt(_posToMove.transform);
       }
       else if (!_alreadySpotted)
       {
           if (isShooting || _nodePath.ElementAtOrDefault(_currentWp) == null)
               return;
           
           var distance = _nodePath[_currentWp].position - transform.position;

           if (distance.magnitude > speed * Time.deltaTime)
           {
               transform.parent.transform.position += distance.normalized * speed * Time.deltaTime;
               transform.forward = Vector3.Lerp(transform.forward, distance.normalized, 0.5f);
           }
           else
           {
               transform.parent.transform.position = _nodePath[_currentWp].position;
               _currentWp++;
           }
       }
    }
    
    public void SetCanMove(bool v, Vector3 posToMove)
    {
        if (!_view.IsMine || posToMove.y >= transform.parent.transform.position.y)
            return;
        
        SetIsMoving(v);
        
        canMove = v;
        
        if (v)
        {
            CalculePath(posToMove);
            _posToMove.transform.parent.transform.position = posToMove;
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
        yield return new WaitForSeconds(0.5f);
        
        var position = shootObject.position;
        var bullet = PhotonNetwork.Instantiate("Bullet", position, transform.rotation).GetComponent<Bullet>();
        LevelManager.Instance.TryToPlaySound(Constants.SHOOT_SOUND, position);
        LevelManager.Instance.TryToPlayEffect(Constants.SHOOT_EFFECT, position, shootObject.forward);
        
        bullet.shootBy = Bullet.ShootBy.Player;
        bullet.startPos = transform.position;
        bullet.damage = damage;
        
        SetIsShooting(false);
    }

    public void TryToDie()
    {
        if (!isDead)
            StartCoroutine(Dead());
    }

    private IEnumerator Dead()
    {
        isDead = true;
            
        if (_anim)
            _anim.SetBool("IsDead", true);
        
        yield return new WaitForSeconds(1);

        _view.RPC("TurnColliderOff", RpcTarget.AllBuffered);
        LevelManager.Instance.PlayerDead(owner);
    }

    [PunRPC]
    public void TurnColliderOff()
    {
        GetComponent<CapsuleCollider>().isTrigger = true;
    }

    public void TakeDamage(float amount)
    {
        if (!_invulnerabilityActive)
            ArmorChange(amount);
    }
    
    public void ArmorChange(float amount)
    {
        _armor += amount;
        
        if (_armor >= maxArmor)
            _armor = maxArmor;
        else if (_armor < 0)
        {
            LifeChange(_armor);
            _armor = 0;
        }
        
        _view.RPC("SetArmorImage", owner, _armor);
    }
    
    public void LifeChange(float amount)
    {
        _hp += amount;

        if (_hp >= maxHp)
            _hp = maxHp;

        _view.RPC("SetLifeImage", owner, _hp);

        if (_hp <= 0)
            TryToDie();
    }
    
    [PunRPC] 
    private void SetArmorImage(float armor)
    {
        if (armorBar)
            armorBar.fillAmount = armor / 100;
    }
    
    [PunRPC] 
    private void SetLifeImage(float hp)
    {
        if (healthBar)
            healthBar.fillAmount = hp / 100;
    }

    [PunRPC]
    private void OnInvulnerability(bool active)
    {
        if (armorBar)
            armorBar.color = active ? Color.magenta : Color.green;
        
        if (healthBar)
            healthBar.color = active ? Color.magenta : Color.red;
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
        _invulnerabilityTimer += Time.deltaTime;
        
        if (_invulnerabilityTimer >= _invulnerabilityTime && _invulnerabilityActive)
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
        photonView.RPC("SetLocalData", owner);
    }
    
    [PunRPC]
    private void SetLocalData()
    {
        SetCanvasPos();
        
        if (armorBar)
            armorBar.fillAmount = 0;
        
        if (healthBar)
            healthBar.fillAmount = maxHp;
    }

    public void ChangeInvulnerability(bool active, float time = 0)
    {
        _invulnerabilityActive = active;
        _invulnerabilityTimer = 0;
        _view.RPC("OnInvulnerability", owner, active);

        if (active)
            _invulnerabilityTime = time;
    }

    private void CalculePath(Vector3 position)
    {
        _currentWp = 0;
        _alreadySpotted = false;
        _nodePath = new List<Transform>();
            
        var currentNode = GetClosestNodeTo(transform.parent.transform.position);
        var finalNode = GetClosestNodeTo(position);
        
        var nodes = AStar.AStarNodes(currentNode,finalNode, Heuristic);

        if (nodes.Count > pathfindingMaxNodes)
            nodes = nodes.Take(pathfindingMaxNodes).ToList();
        
        foreach (var node in nodes)
        {
            _nodePath.Add(node.transform); 
        }
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
    
    private void SetCanvasPos()
    {
        var canvas = FindObjectOfType<LocalSceneManger>().localCanvas;
        healthBar = canvas.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        armorBar = canvas.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        var rect = canvas.GetComponent<RectTransform>();
        StartCoroutine(SetUIPos(rect));
    }

    private IEnumerator SetUIPos(RectTransform canvasRect)
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            canvasRect.localPosition = transform.position + UIPos;
        }
    }
}
