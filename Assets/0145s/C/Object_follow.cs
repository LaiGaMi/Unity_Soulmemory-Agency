using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_follow : MonoBehaviour
{
	public Transform target;
    private Transform self;
	
    void Awake()
    {
        self = transform;
    }

    void LateUpdate()
    {
        self.position = target.position;
    }
}
