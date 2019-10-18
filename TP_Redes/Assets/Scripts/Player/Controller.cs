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
    public Text hpText; 
    public Camera myCam;
    
    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (_view.IsMine)
        {
            LevelManager.Instance.StartPlayerData(PhotonNetwork.LocalPlayer);
            StartCoroutine(SendPackage());
        }
        else
        {
            myCam.enabled = false;
        }
    }

    private IEnumerator SendPackage()
    {
        while (true)
        {
            yield return new WaitForSeconds(1 / _packagePerSecond);
            
            LevelManager.Instance.MoveCamera(PhotonNetwork.LocalPlayer);
            
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
        }
    }
    
    public void SetPPS(int pps)
    {
        _packagePerSecond = pps;
    }

    public void UpdateHUD(float hpToUse)
    {
        hpText.text = "HP: " + hpToUse;
    }
}
