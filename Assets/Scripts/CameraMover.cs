using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1f;
    [SerializeField] GameObject pivot;
    
    void Start()
    {
        gameObject.transform.position = gameObject.transform.position - Vector3.forward*10;
    }
    void Update()
    {
        transform.RotateAround(pivot.transform.position,Vector3.up, moveSpeed);
    }
}
