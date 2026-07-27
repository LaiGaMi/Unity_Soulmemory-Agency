using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ItemUI : MonoBehaviour
{
    [Header("物品資料")]
    public GameItem itemDatabase;

    [Header("物品欄")]
    public List<ItemSlotUI> itemSlots =
        new List<ItemSlotUI>();

    [Header("說明顯示時間")]
    public float showTime = 3f;

    private Coroutine hideCoroutine;

    void Start()
    {
        RefreshItem();
    }

    void Update()
    {
        List<string> items =
            PlayerInventory.Instance.GetItems();

        for(int i = 0; i < itemSlots.Count; i++)
        {
            if(i >= items.Count)
                continue;

            //Input
            if(itemSlots[i].input != null)
            {
                if(itemSlots[i].input.action.WasPressedThisFrame())
                {
                    ShowItemInfo(items[i], itemSlots[i]);
                }
            }

            //滑鼠、觸控
            if(Input.GetMouseButtonDown(0))
            {
                Vector2 pos =
                    Input.mousePosition;

                RectTransform rect =
                    itemSlots[i].icon.rectTransform;

                if(
                    RectTransformUtility.
                    RectangleContainsScreenPoint(
                        rect,
                        pos
                    )
                )
                {
                    ShowItemInfo(
                        items[i],
                        itemSlots[i]
                    );
                }
            }
        }
    }
	
	void OnEnable()
	{
		foreach(ItemSlotUI slot in itemSlots)
    	{
    	    if(slot.input != null)
    	    {
    	        slot.input.action.Enable();
    	    }
    	}
		
	    PlayerInventory.OnInventoryChanged += RefreshItem;
	}

    void OnDisable()
    {
		foreach(ItemSlotUI slot in itemSlots)
    	{
    	    if(slot.input != null)
    	    {
    	        slot.input.action.Disable();
    	    }
    	}
		
		PlayerInventory.OnInventoryChanged -= RefreshItem;
        HideAllInfo();
    }

    //更新物品欄
    public void RefreshItem()
    {
        List<string> items =
            PlayerInventory.Instance.GetItems();

        for(int i = 0;i < itemSlots.Count;i++)
        {
            if(i < items.Count)
            {
                Sprite icon =
                    itemDatabase.GetItemIcon(
                        items[i]
                    );

                itemSlots[i].icon.sprite =
                    icon;

                itemSlots[i].icon.color =
                    Color.white;
            }
            else
            {
                itemSlots[i].icon.sprite =
                    null;

                itemSlots[i].icon.color =
                    new Color(
                        1,
                        1,
                        1,
                        0
                    );

                itemSlots[i].infoUI.SetActive(
                    false
                );
            }
        }
    }

    //顯示說明
    void ShowItemInfo(
        string id,
        ItemSlotUI slot
    )
    {
        HideAllInfo();

        slot.infoUI.SetActive(true);

        slot.nameText.text =
            ItemManager.Instance.
            GetItemName(id);

        slot.infoText.text =
            ItemManager.Instance.
            GetItemText(id);

        if(hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );
        }

        hideCoroutine =
            StartCoroutine(
                AutoHide(slot.infoUI)
            );
    }

    IEnumerator AutoHide(GameObject ui)
    {
        yield return new WaitForSeconds(
            showTime
        );

        ui.SetActive(false);
    }

    //隱藏全部
    void HideAllInfo()
    {
		if(hideCoroutine != null)
    	{
    	    StopCoroutine(hideCoroutine);
    	    hideCoroutine = null;
    	}
		
        foreach(
            ItemSlotUI slot
            in itemSlots
        )
        {
            if(slot.infoUI != null)
            {
                slot.infoUI.SetActive(
                    false
                );
            }
        }
    }
}

[System.Serializable]
public class ItemSlotUI
{
    [Header("圖示")]
    public Image icon;

    [Header("說明UI")]
    public GameObject infoUI;

    [Header("名字")]
    public Text nameText;

    [Header("說明")]
    public Text infoText;

    [Header("Input")]
    public InputActionReference input;
}