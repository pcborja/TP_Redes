using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform _target;
    public Vector3 offset;

    public void SetTarget(Transform t)
    {
        _target = t;
        //transform.SetParent(t);
        
        SetPosition();
    }

    private void Update()
    {
        if (!_target)
            return;
        var character = _target;
        var position = character.transform.position;
        var charPosX = position.x + offset.x;
        var charPosZ = position.z + offset.y;
        var charPosY = position.y + offset.z;

        transform.position = new Vector3(charPosX, charPosY, charPosZ);
    }

    private void SetPosition()
    {
        var character =_target;
        var position = character.transform.position;
        var charPosX = position.x + offset.x;
        var charPosZ = position.z + offset.y;
        var charPosY = position.y + offset.z;

        transform.position = new Vector3(charPosX, charPosY, charPosZ);
    }
}