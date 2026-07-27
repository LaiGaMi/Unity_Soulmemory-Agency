using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemTextFile
{
    public List<ItemTextData> items;
}

[System.Serializable]
public class ItemTextData
{
    public string id;
    public string name;
    public string text;
}

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    [Header("JSON檔名")]
    public string jsonFileName = "Item";

    private ItemTextFile itemTextFile;

    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadJSON();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //讀取JSON
    public void LoadJSON()
    {
        LanguageType language =
            GameManager.Instance.Setting.language;

        string suffix =
            language.ToString();

        string path =
            "Props/" +
            jsonFileName +
            "." +
            suffix;

        TextAsset json =
            Resources.Load<TextAsset>(path);

        if(json == null)
        {
            Debug.LogError(
                "找不到物品JSON : " +
                path
            );
            return;
        }

        itemTextFile =
            JsonUtility.FromJson<ItemTextFile>(
                json.text
            );
    }

    //取得物品資料
    ItemTextData GetData(string id)
    {
        if(itemTextFile == null)
            return null;

        foreach(ItemTextData data in itemTextFile.items)
        {
            if(data.id == id)
            {
                return data;
            }
        }

        return null;
    }

    //取得名字
    public string GetItemName(string id)
    {
        ItemTextData data =
            GetData(id);

        if(data == null)
            return "";

        return data.name;
    }

    //取得說明
    public string GetItemText(string id)
    {
        ItemTextData data =
            GetData(id);

        if(data == null)
            return "";

        return data.text;
    }
}