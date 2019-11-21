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
        while (true)
        {
            yield return new WaitForSeconds(1 / _packagePerSecond);
            
            if (Input.GetMouseButtonDown(0))
            {
                LevelManager.Instance.OnClicked(Input.mousePosition, PhotonNetwork.LocalPlayer);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                LevelManager.Instance.OnStartHoldingPosition(PhotonNetwork.LocalPlayer);
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                LevelManager.Instance.OnEndHoldingPosition(PhotonNetwork.LocalPlayer);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LevelManager.Instance.OnDisconnect(Constants.INTRO_SCENE, PhotonNetwork.LocalPlayer);
            }
        }
    }
    
    public void SetPPS(int pps)
    {
        _packagePerSecond = pps;
    }
}
