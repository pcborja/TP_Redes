using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;

public class Controller : MonoBehaviourPun
{
    private PhotonView _view;
    private int _packagePerSecond;
    
    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (_view.IsMine)
        {
            StartCoroutine(SendPackage());
        }
    }

    private IEnumerator SendPackage()
    {
        yield return new WaitForSeconds(0.5f);
        
        while (true)
        {
            yield return new WaitForSeconds(1 / _packagePerSecond);

            if (Input.GetMouseButtonDown(0))
                CheckClickActions(true);
            if (Input.GetMouseButtonDown(1))
                CheckClickActions(false);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LevelManager.Instance.Disconnect(PhotonNetwork.LocalPlayer);
            }
        }
    }

    private void CheckClickActions(bool isMovement)
    {
        //Andrea me dijo que haga aca el raycast aunque "no respete" FA porque no se pudo de otra forma
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100))
        {
            var isPowerUp = hit.transform.GetComponent<PowerUp>();
            LevelManager.Instance.OnClicked(hit.point, PhotonNetwork.LocalPlayer, isMovement, isPowerUp);
        }
    }
    
    public void SetPPS(int pps)
    {
        _packagePerSecond = pps;
    }
}
