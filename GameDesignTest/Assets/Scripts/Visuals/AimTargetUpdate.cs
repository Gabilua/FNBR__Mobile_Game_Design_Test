using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimTargetUpdate : MonoBehaviour
{
    [SerializeField] private Transform _aimTargetRef;

    void Update()
    {
        transform.position = _aimTargetRef.position;
    }
}
