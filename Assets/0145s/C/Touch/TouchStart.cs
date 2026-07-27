using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchStart : MonoBehaviour, CanTouch
{
    public GameObject[] StartObjects;

    public void TouchObject()
    {
        foreach(GameObject obj in StartObjects)
        {
            if(obj != null)
            {
                obj.SetActive(true);
            }
        }
    }
}
