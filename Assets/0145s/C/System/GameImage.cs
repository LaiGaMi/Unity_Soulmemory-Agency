using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameImage",
    menuName = "Game/Game Image"
)]

public class GameImage : ScriptableObject
{
    // 對話框清單
    [Header("對話框清單")]
    public List<DialogueBoxData> dialogueBoxList =
        new List<DialogueBoxData>();

    // 姓名框清單
    [Header("姓名框清單")]
    public List<NameBoxData> nameBoxList =
        new List<NameBoxData>();

    // 立繪清單
    [Header("立繪清單")]
    public List<CharacterImageData> characterImageList =
        new List<CharacterImageData>();


    //取得對話框
    public Sprite GetDialogueBox(string id)
    {
        foreach(DialogueBoxData data in dialogueBoxList)
        {
            if(data.id == id)
            {
                return data.image;
            }
        }

        Debug.LogWarning(
            "找不到對話框 ID : " + id
        );

        return null;
    }


    //取得姓名框
    public Sprite GetNameBox(string id)
    {
        foreach(NameBoxData data in nameBoxList)
        {
            if(data.id == id)
            {
                return data.image;
            }
        }

        Debug.LogWarning(
            "找不到姓名框 ID : " + id
        );

        return null;
    }


    //取得立繪
    public Sprite GetCharacterImage(string id)
    {
        foreach(CharacterImageData data in characterImageList)
        {
            if(data.id == id)
            {
                return data.image;
            }
        }

        Debug.LogWarning(
            "找不到立繪 ID : " + id
        );

        return null;
    }
}


// 對話框資料
[System.Serializable]
public class DialogueBoxData
{
    public string id;
    public Sprite image;
}


// 姓名框資料
[System.Serializable]
public class NameBoxData
{
    public string id;
    public Sprite image;
}


// 立繪資料
[System.Serializable]
public class CharacterImageData
{
    public string id;
    public Sprite image;
}