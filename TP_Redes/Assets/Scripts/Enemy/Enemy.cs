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
    public Transform shootObject;
    [HideInInspector] public Transform currentTarget;
    public GameObject bulletPrefab;
    public Character[] players;
    public float visionRange;
    public bool shootEnemy;
    public bool meleeEnemy;
    public float speed;
    private Animator _anim;
    private float _attackTimer;
    private bool _targetSpoted;
    private Rigidbody _rb;
    private bool _isAttacking;

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
        TryToAttack();
    }

    private void Timers()
    {
        _attackTimer += Time.deltaTime;
    }

    private void TryToAttack()
    {
        if (!currentTarget) return;
        
        if (shootEnemy && _targetSpoted && _attackTimer > timeToAttack)
        {
            Shoot(currentTarget);
        }

        if (meleeEnemy && _targetSpoted && !_isAttacking)
        {
            Arrive.D_Arrive(gameObject, currentTarget.position, _rb, speed, 1);
            SetIsMoving(Math.Abs(_rb.velocity.magnitude) > 0.1f);

            if (Vector3.Distance(transform.position, currentTarget.position) <= 2)
                Attack();
        }
    }

    private void Attack()
    {
        _isAttacking = true;
        _anim.SetBool("IsAttacking", true);
        StartCoroutine(ResetAttacking());
    }

    private IEnumerator ResetAttacking()
    {
        yield return new WaitForSeconds(timeToAttack);
        
        if (currentTarget && Vector3.Distance(transform.position, currentTarget.position) <= 2)
            currentTarget.gameObject.GetComponent<Character>().TakeDamage(damage);
        
        _isAttacking = false;
    }

    private void CheckForPlayers()
    {
        if (!currentTarget)
            currentTarget = players.FirstOrDefault(x => x != null)?.transform;
        
        if (!currentTarget) return;
        
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
            /*if (_anim)
            {
                _anim.SetBool("IsDead", true);
                StartCoroutine(Dead());
            }
            else
            {*/
                DestroyImmediate(gameObject);
            //}
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
