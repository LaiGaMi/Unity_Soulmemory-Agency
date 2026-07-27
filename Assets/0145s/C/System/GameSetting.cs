using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameSetting",
    menuName = "Game/Game Setting"
)]
public class GameSetting : ScriptableObject
{
    [Header("語言設定")]
    public LanguageType language = LanguageType.TW;


    [Header("音量設定")]
    [Range(0f,1f)]
    public float bgmVolume = 1f;

    [Range(0f,1f)]
    public float seVolume = 1f;

    //修改語言
    public void SetLanguage(LanguageType newLanguage)
    {
        language = newLanguage;
    }


    //修改BGM音量
    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
    }


    //修改音效音量
    public void SetSEVolume(float value)
    {
        seVolume = Mathf.Clamp01(value);
    }
}

public enum LanguageType
{
    TW,
    EN,
    JP
}