using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Character>())
        {
            LevelManager.Instance.NotifyWinner(other.gameObject.GetComponent<Character>());
            DestroyImmediate(gameObject);
        }
            
    }
}
