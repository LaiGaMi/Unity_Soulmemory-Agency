using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Puzzle00_relics : MonoBehaviour
{
	[Header("一般遺物預置物")]
    public GameObject relicPrefab;

    [Header("特殊遺物預置物")]
    public GameObject specialRelicPrefab;

    [Header("生成位置")]
    public Transform spawnParent;

    [Header("遺物清單")]
    public List<RelicData> relicList =
        new List<RelicData>();

    //目前生成的遺物
    private GameObject currentRelic;

    //目前遺物編號
    private int currentIndex = 0;
	
	//是否正在等待遺物消失
    private bool waitingForDestroy = false;

    void OnEnable()
    {
        StartRelicSort();
    }
	
	void Update()
    {
        //沒有正在等待就不檢查
        if(!waitingForDestroy)
            return;

        //目前遺物已經被刪除
        if(currentRelic == null)
        {
            waitingForDestroy = false;

            SpawnNextRelic();
        }
    }
	
	//開始遺物整理
    void StartRelicSort()
    {
        currentIndex = 0;
		waitingForDestroy = false;
        //避免重新開始時留下舊物件
        ClearCurrentRelic();

        //沒有遺物
        if(relicList == null ||
           relicList.Count == 0)
        {
            Debug.LogWarning(
                "遺物清單是空的。"
            );

            return;
        }

        //生成第一個遺物
        SpawnNextRelic();
    }

    //生成下一個遺物
    public void SpawnNextRelic()
    {
        //刪除目前遺物
        ClearCurrentRelic();

        //超出清單
        if(currentIndex >= relicList.Count)
        {
            Debug.Log(
                "所有遺物已完成。"
            );

            return;
        }

        //取得目前遺物資料
        RelicData data =
            relicList[currentIndex];

        //判斷是不是最後一個
        bool isLastRelic =
            currentIndex ==
            relicList.Count - 1;

        //最後一個使用特殊遺物預置物
        if(isLastRelic)
        {
            SpawnRelic(
                specialRelicPrefab,
                data
            );
        }
        else
        {
            //前面的使用一般遺物預置物
            SpawnRelic(
                relicPrefab,
                data
            );
        }

        //進入下一個編號
        currentIndex++;
		waitingForDestroy = true;
    }

    //生成一般遺物
    void SpawnRelic( GameObject prefab, RelicData data)
    {
        if(prefab == null)
        {
            Debug.LogError(
                "沒有設定遺物預置物。"
            );

            return;
        }

        Transform parent =
            spawnParent != null
            ? spawnParent
            : transform;


        //生成
        currentRelic =
            Instantiate(
                prefab,
                parent
            );

        //設定圖片
        Image image =
            currentRelic.GetComponent<Image>();

        if(image != null)
        {
            image.sprite =
                data.image;
        }
        else
        {
            Debug.LogWarning(
                "遺物預置物沒有 Image 元件。"
            );
        }

        //設定Tag
        if(!string.IsNullOrEmpty(data.tag))
        {
            try
            {
                currentRelic.tag =
                    data.tag;
            }
            catch
            {
                Debug.LogError(
                    "Tag 不存在 : " +
                    data.tag
                );
            }
        }
    }
	
    //刪除目前遺物
    void ClearCurrentRelic()
    {
        if(currentRelic != null)
        {
            Destroy(currentRelic);

            currentRelic = null;
        }
    }
}

//遺物資料
[System.Serializable]
public class RelicData
{
    [Header("遺物圖片")]
    public Sprite image;

    [Header("遺物Tag")]
    public string tag;

    [Header("遺物名稱")]
    public string name;
}
