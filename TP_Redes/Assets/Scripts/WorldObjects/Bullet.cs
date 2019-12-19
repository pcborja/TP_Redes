using Photon.Pun;
using UnityEngine;

public class Bullet : MonoBehaviourPun
{
    public enum ShootBy {Player, Enemy}
    [HideInInspector] public ShootBy shootBy;
    [HideInInspector] public float damage;
    [HideInInspector] public Vector3 startPos;
    public float bulletLife;
    public float bulletSpeed;
    
    private float _timer;
    private PhotonView _view;

    private void Awake()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        _view = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        transform.position += transform.forward * bulletSpeed * Time.deltaTime;
        CheckDeath();
    }

    private void CheckDeath()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= bulletLife)
            Destroy();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        
        if (other.gameObject.GetComponent<Character>() && shootBy == ShootBy.Enemy)
        {
            other.gameObject.GetComponent<Character>().TakeDamage(-damage);
            Destroy();
        }

        if (other.gameObject.GetComponent<Enemy>() && shootBy == ShootBy.Player)
            other.gameObject.GetComponent<Enemy>().TakeDamage(damage, startPos);
        
        if (!other.gameObject.GetComponent<Character>())
            Destroy();
    }

    
    private void Destroy()
    {
        PhotonNetwork.Destroy(gameObject);
    }
}
