using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class Enemy : MonoBehaviourPun
{
    public float damage;
    public float hp;
    public float timeToAttack = 1;
    [HideInInspector] public Transform currentTarget;
    public Character[] players;
    public float range;
    public float angle;
    public float waitTime;
    public float speed;
    public float attackRange;
    public float speedIncrementer;
    public int maxPatrolActions;
    public List<Transform> patrolNodes;
    
    public enum Input {Patrol, Wait, Attack, Search, Dead}
    
    [HideInInspector] public FSM<Input> fsm;
    
    private int _amountOfActions;
    private bool _wasSearching;
    private Animator _anim;
    private Rigidbody _rb;
    private bool _isAttacking;
    private PhotonView _view;
    private float _waitTimer;
    private Node[] _pathFindingNodes;
    private List<Transform> _currentNodePath;
    private int _currentWp;
    private int _direction = 1;
    private Vector3 _lastPlayerKnownPosition;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        _anim = GetComponent<Animator>(); 
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(FindPlayers());
            _pathFindingNodes = FindObjectsOfType<Node>().Where(x => !x.limitedNode).ToArray();
            GetPatrolNodes();
            CreateFSM();
        }
    }

    private void CreateFSM()
    {
        var patrol = new State<Input>("Patrol", Patrol, EnterPatrol);
        var wait = new State<Input>("Wait", Wait, EnterWait);
        var search = new State<Input>("Search", Search, EnterSearch, ExitSearch);
        var attack = new State<Input>("Attack", Attack, EnterAttack);
        var dead = new State<Input>("Dead", Dead, EnterDead);
        
        wait.AddTransition(Input.Patrol, patrol);
        wait.AddTransition(Input.Search, search);
        wait.AddTransition(Input.Attack, attack);
        wait.AddTransition(Input.Dead, dead);

        attack.AddTransition(Input.Patrol, patrol);
        attack.AddTransition(Input.Search, search);
        attack.AddTransition(Input.Dead, dead);
        attack.AddTransition(Input.Wait, wait);
        
        patrol.AddTransition(Input.Search, search);
        patrol.AddTransition(Input.Wait, wait);
        patrol.AddTransition(Input.Attack, attack);
        patrol.AddTransition(Input.Dead, dead);

        search.AddTransition(Input.Patrol, patrol);
        search.AddTransition(Input.Attack, attack);
        search.AddTransition(Input.Dead, dead);
        search.AddTransition(Input.Wait, wait);
        
        fsm = new FSM<Input>(patrol);
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (players.Length > 0)
                CheckForPlayers();
            
            fsm.Update();
            
            CheckVitals();
        }
    }

    private void EnterPatrol()
    {
        _amountOfActions = 0;
        _anim.SetBool("IsMoving", true);
    }

    private void Patrol()
    {
        if (_amountOfActions == maxPatrolActions)
            fsm.Feed(Input.Wait);
        else if (!PlayerSpoted())
            MoveBeetweenWaypoints(patrolNodes);
        else
            fsm.Feed(Input.Attack);
    }

    private void EnterWait()
    {
        _waitTimer = 0;
        _anim.SetBool("IsMoving", false);
    }
    
    private void Wait()
    {
        _waitTimer += Time.deltaTime;

        if (PlayerSpoted())
            fsm.Feed(Input.Attack);
        else if (_waitTimer > waitTime && !_wasSearching)
            fsm.Feed(Input.Patrol);
    }

    private void EnterSearch()
    {
        _currentWp = 0;
        speed += speedIncrementer; 
                
        _currentNodePath = new List<Transform>();
            
        var currentNode = GetClosestNodeTo(gameObject.transform.position);
        var finalNode = GetClosestNodeTo(_lastPlayerKnownPosition);
        
        var nodes = AStar.AStarNodes(currentNode,finalNode, Heuristic);

        foreach (var node in nodes)
        {
            _currentNodePath.Add(node.transform);
        }
        _anim.SetBool("IsMoving", true);
    }

    private void Search()
    {
        if (PlayerSpoted())
            fsm.Feed(Input.Attack);
        else
            MoveBeetweenWaypoints(_currentNodePath, true);
    }

    private void ExitSearch()
    {
        speed -= speedIncrementer;
        _direction *= -1;
    }
    
    private void EnterAttack(){}

    private void Attack()
    {
        if (!_isAttacking)
        {
            if (PlayerInRange())
                StartCoroutine(PerformAttack());
            else
            {
                _anim.SetBool("IsMoving", true);
                var step = speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, step);
                transform.LookAt(currentTarget.transform);
            }
        }
        
        if (!PlayerSpoted())
        {
            _lastPlayerKnownPosition = currentTarget.transform.position;
            fsm.Feed(Input.Search);
        }
    }
    
    private void EnterDead()
    {        
        DestroyImmediate(gameObject);
    }
    
    private void Dead(){}

    private IEnumerator PerformAttack()
    {
        _isAttacking = true;
        _anim.SetBool("IsAttacking", true);
        
        yield return new WaitForSeconds(timeToAttack);

        if (currentTarget && Vector3.Distance(transform.position, currentTarget.position) <= 2)
                currentTarget.gameObject.GetComponent<Character>().TakeDamage(-damage);
        
        _isAttacking = false;
        _anim.SetBool("IsAttacking", false);
        
        if (!PlayerSpoted())
            fsm.Feed(Input.Search);
    }

    private void CheckForPlayers()
    {
        if (!currentTarget)
            currentTarget = players.FirstOrDefault(x => x != null)?.transform;
        
        if (!currentTarget) return;
        
        foreach (var player in players)
        {
            if (!player) return;
            
            if (Vector3.Distance(transform.position, currentTarget.position) >
                Vector3.Distance(transform.position, player.transform.position))
            {
                currentTarget = player.transform;
            }
        }
    }

    private void GoToTarget(Vector3 position)
    {
        transform.LookAt(position);
        _rb.AddForce((position - transform.position) * speed);
    }

    private void CheckVitals()
    {
        if (hp <= 0)
            PhotonNetwork.Destroy(gameObject);
    }

    public void TakeDamage(float dmg, Character character)
    {
        hp -= dmg;
        transform.LookAt(character.transform.position);
    }
    
    private void SetIsMoving(bool v)
    {
        if (_anim)
            _anim.SetBool("IsMoving", v);
    }

    private void LookToTarget(Transform target)
    {
        var targetDir = target.position - transform.position;
        var step = speed * Time.deltaTime;
        var newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }

    private IEnumerator FindPlayers()
    {
        yield return new WaitForSeconds(2);
        players = FindObjectsOfType<Character>();
    }
    
    private void MoveBeetweenWaypoints(List<Transform> waypoints, bool searching = false, bool retreat = false)
    {
        if (waypoints.ElementAtOrDefault(_currentWp) == null)
            return;
            
        var distance = waypoints[_currentWp].position - transform.position;

        if (distance.magnitude > speed * Time.deltaTime)
        {
            transform.position += distance.normalized * speed * Time.deltaTime;
            transform.forward = Vector3.Lerp(transform.forward, distance.normalized, 0.5f);
        }
        else
        {
            transform.position = waypoints[_currentWp].position;
            _currentWp += _direction;
            _amountOfActions++;
            if (_currentWp >= waypoints.Count || _currentWp < 0)
            {
                _direction *= -1;
                
                if (searching && _direction < 0)
                {
                    _wasSearching = true;
                    fsm.Feed(Input.Wait);
                }
                else if (retreat)
                    fsm.Feed(Input.Patrol);
                else
                {
                    _currentWp += _direction;
                }
            }
        }
    }
    
    private Node GetClosestNodeTo(Vector3 position, List<Transform> filterList = null)
    {
        var closestNode = _pathFindingNodes[0];
        var nodesToUse = _pathFindingNodes;
        
        if (filterList != null)
            nodesToUse = _pathFindingNodes.Where(x => !filterList.Contains(x.transform)).ToArray();

        foreach (var node in nodesToUse)
        {
            if (Vector3.Distance(closestNode.transform.position, position) >
                Vector3.Distance(node.transform.position, position))
                closestNode = node;
        }

        return closestNode;
    }
    
    private float Heuristic(Node a, Node b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }
    
    private bool PlayerSpoted()
    {
        return currentTarget && BasicBehaviours.TargetIsInSight(gameObject.transform, currentTarget.transform, range, angle);
    }

    private bool PlayerInRange()
    {
        return Vector3.Distance(gameObject.transform.position, currentTarget.transform.position) <= attackRange;
    }

    private void GetPatrolNodes()
    {
        for (var i = 0; i < 3; i++)
        {
            patrolNodes.Add(GetClosestNodeTo(transform.position, patrolNodes).transform);  
        }
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
