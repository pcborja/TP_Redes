using Photon.Pun;
using UnityEngine;

public class Bullet : MonoBehaviourPun
{
    public enum ShootBy {Player, Enemy}
    [HideInInspector] public ShootBy shootBy;
    [HideInInspector] public float damage;
    public float bulletLife;
    public float bulletSpeed;
    private float _timer;

    void Update()
    {
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
        if (other.gameObject.GetComponent<Character>() && shootBy == ShootBy.Enemy)
        {
            other.gameObject.GetComponent<Character>().TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.gameObject.GetComponent<Enemy>() && shootBy == ShootBy.Player)
        {
            other.gameObject.GetComponent<Enemy>().TakeDamage(damage);
        }
        
        if (!other.gameObject.GetComponent<Character>())
            Destroy(gameObject);
    }

    private void Destroy()
    {
        DestroyImmediate(gameObject);
    }
}
