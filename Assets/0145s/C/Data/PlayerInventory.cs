using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;
	
	public static event System.Action OnInventoryChanged;
	
	[Header("物品資料庫")]
	public GameItem itemDatabase;

    //玩家持有物品
    public List<string> itemList =
        new List<string>();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //新增物品
    public void AddItem(string id)
    {
        //避免重複
        if(itemList.Contains(id))
        {
            return;
        }
        itemList.Add(id);
		SortItem();
		
		OnInventoryChanged?.Invoke();
    }

    //移除物品
    public void RemoveItem(string id)
    {
        if(itemList.Contains(id))
        {
            itemList.Remove(id);
			OnInventoryChanged?.Invoke();
        }
    }

    //確認是否擁有物品
    public bool HasItem(string id)
    {
        return itemList.Contains(id);
    }



    //取得所有物品
    public List<string> GetItems()
    {
        return itemList;
		SortItem();
    }

    //清空物品
    public void ClearItem()
    {
        itemList.Clear();
    }
	
	
	
	//整理物品排序
	void SortItem()
	{
	    itemList.Sort(
	        delegate(string a, string b)
	        {
   	        	int sortA =
   	            	itemDatabase.GetItemSort(a);
	
	            int sortB =
	                itemDatabase.GetItemSort(b);
	
	            return sortA.CompareTo(sortB);
	        }
   		);
	}
	
	

    //取得存檔資料
    public List<string> SaveItems()
    {
        return new List<string>(itemList);
    }

    //讀取存檔資料
    public void LoadItems(List<string> data)
    {
        itemList =
            new List<string>(data);
    }
}