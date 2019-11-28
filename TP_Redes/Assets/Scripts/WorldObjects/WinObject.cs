using Photon.Pun;
using UnityEngine;

public class WinObject : MonoBehaviourPun
{
    private bool _canBeTriggered;
    private PhotonView _view;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (!_view.IsMine)
            DestroyImmediate(gameObject);
        else
            _canBeTriggered = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Character>() && _canBeTriggered)
        {
            _canBeTriggered = false;
            LevelManager.Instance.NotifyWinner(other.gameObject.GetComponent<Character>());
        }            
    }
}
