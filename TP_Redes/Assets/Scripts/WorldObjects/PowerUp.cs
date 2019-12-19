using Photon.Pun;
using UnityEngine;

public class PowerUp : MonoBehaviourPun
{
    public bool healPowerUp;
    public bool armorPowerUp;
    public bool invulnerabilityPowerUp;

    public float healAmount;
    public float armorAmount;
    public float invulnerabilityTime;
    
    private PhotonView _view;
    private bool _used;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        
        if (other.gameObject.GetComponent<Character>() && !_used)
        {
            _used = true;
        
            if (healPowerUp)
                LevelManager.Instance.HealPowerUp(other.gameObject.GetComponent<Character>(), healAmount);
            else if (armorPowerUp)
                LevelManager.Instance.ArmorPowerUp(other.gameObject.GetComponent<Character>(), armorAmount);
            else if (invulnerabilityPowerUp)
                LevelManager.Instance.InvulnerabilityPowerUp(other.gameObject.GetComponent<Character>(), invulnerabilityTime);
            
            LevelManager.Instance.PlaySoundForPlayer(Constants.PICKUP_SOUND, transform.position, other.gameObject.GetComponent<Character>());
            
            PhotonNetwork.Destroy(gameObject);
        }            
    }
}
