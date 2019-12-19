using Photon.Pun;
using UnityEngine;

public class WinObject : MonoBehaviourPun
{
    private bool _canBeTriggered;
    private PhotonView _view;

    private void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
            _view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            _canBeTriggered = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (other.gameObject.GetComponent<Character>() && _canBeTriggered)
        {
            _canBeTriggered = false;
            LevelManager.Instance.NotifyWinner(other.gameObject.GetComponent<Character>());
        }            
    }
}
