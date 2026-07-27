using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UI_PressButton : MonoBehaviour, IPointerDownHandler
{
    public UnityEvent onPressed;
	
	//觸碰按鈕的瞬間觸發
    public void OnPointerDown(PointerEventData eventData)
    {
        onPressed?.Invoke();
    }
}
