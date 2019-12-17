using Photon.Pun;
using UnityEngine;

public class PowerUp : MonoBehaviourPun
{
    public bool healPowerUp;
    public bool speedPowerUp;
    public bool invulnerabilityPowerUp;

    public float healAmount;
    public float speedTime;
    public float invulnerabilityTime;
    public float speedValue;
    
    private PhotonView _view;
    private bool _used;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }
    
    private void Start()
    {
        if (!_view.IsMine)
            DestroyImmediate(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Character>() && !_used)
        {
            _used = true;
        
            if (healPowerUp)
                LevelManager.Instance.HealPowerUp(other.gameObject.GetComponent<Character>(), healAmount);
            else if (speedPowerUp)
                LevelManager.Instance.SpeedPowerUp(other.gameObject.GetComponent<Character>(), speedValue, speedTime);
            else if (invulnerabilityPowerUp)
                LevelManager.Instance.InvulnerabilityPowerUp(other.gameObject.GetComponent<Character>(), invulnerabilityTime);
            
            Destroy(gameObject);
        }            
    }
}
