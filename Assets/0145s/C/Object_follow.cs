using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_follow : MonoBehaviour
{
	public bool followRotation = false;
	
	public Transform target;
    private Transform self;
	
    void Awake()
    {
        self = transform;
    }

    void LateUpdate()
    {
        self.position = target.position;
		
		if (followRotation)
        {
            self.rotation = target.rotation;
        }
    }
}
