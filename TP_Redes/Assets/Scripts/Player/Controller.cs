using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Controller : MonoBehaviourPun
{
    private PhotonView _view;
    private int _packagePerSecond;


    bool canClick;
    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (_view.IsMine)
            StartCoroutine(SendPackageMovement());
    }

    void Update()
    {
    }

    private IEnumerator SendPackageMovement()
    {
        while (true)
        {
            yield return new WaitForSeconds(1 / 20);
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
}
