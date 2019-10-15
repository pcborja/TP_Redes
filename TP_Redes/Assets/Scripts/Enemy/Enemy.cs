using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Enemy : MonoBehaviourPun
{
    public float damage;
    public float hp;
    public float timeToShoot = 1;
    public Transform shootObject;
    [HideInInspector] public Transform currentTarget;
    public GameObject bulletPrefab;
    public Character[] players;
    public float visionRange;
    public bool shootEnemy;
    public bool meleeEnemy;
    public float speed;
    private Animator _anim;
    private float _shootTimer;
    private bool _targetSpoted;
    private Rigidbody _rb;

    private void Awake()
    {
        _anim = GetComponent<Animator>(); 
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(FindPlayers());
    }

    void Update()
    {
        Timers();
        CheckVitals();
        
        if (players.Length == 0) return; 
        CheckForPlayers();
        Attack();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponent<Character>() && meleeEnemy)
        {
            other.gameObject.GetComponent<Character>().TakeDamage(damage);
            _anim.SetBool("IsAttacking", true);
        }
    }

    private void Timers()
    {
        _shootTimer += Time.deltaTime;
    }

    private void Attack()
    {
        if (shootEnemy && _targetSpoted && _shootTimer > timeToShoot)
        {
            Shoot(currentTarget);
        }

        if (meleeEnemy && _targetSpoted)
        {
            GoToTarget(currentTarget.position);
            SetIsMoving(currentTarget.position != Vector3.zero);
        }
    }

    private void CheckForPlayers()
    {
        if (!currentTarget)
            currentTarget = players[0].transform;
        
        foreach (var player in players)
        {
            if (Vector3.Distance(transform.position, currentTarget.position) >
                Vector3.Distance(transform.position, player.transform.position))
            {
                currentTarget = player.transform;
            }
        }

        _targetSpoted = Vector3.Distance(transform.position, currentTarget.position) < visionRange;
    }

    private void GoToTarget(Vector3 position)
    {
        transform.LookAt(position);
        _rb.AddForce((position - transform.position) * speed);
    }

    private void CheckVitals()
    {
        if (hp <= 0)
        {
            if (_anim)
            {
                _anim.SetBool("IsDead", true);
                StartCoroutine(Dead());
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;
    }
    
    public void Shoot(Transform target)
    {
        SetIsShooting(true);
        LookToTarget(target);
        StartCoroutine(Shooting());
    }

    private IEnumerator Shooting()
    {
        yield return new WaitForSeconds(1);
        var spawnPos = new Vector3(shootObject.position.x, shootObject.position.y + 1, shootObject.position.z);
        var bullet = Instantiate(bulletPrefab, spawnPos, transform.rotation);
        bullet.GetComponent<Bullet>().shootBy = Bullet.ShootBy.Enemy;
        bullet.GetComponent<Bullet>().damage = damage;
        SetIsShooting(false);
    }
    
    private void SetIsShooting(bool v)
    {
        if (_anim)
            _anim.SetBool("IsShooting", v);
    }
    
    private IEnumerator Dead()
    {
        yield return new WaitForSeconds(1);
        DestroyImmediate(gameObject);
    }
    
    public void SetIsMoving(bool v)
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
}
