using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("遊戲設定")]
    public GameSetting gameSetting;

    private void Awake()
    {

        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
			LoadSetting();
        }
        else
        {
            Destroy(gameObject);
        }

    }

    //取得遊戲設定
    public GameSetting Setting
    {
        get
        {
            return gameSetting;
        }
    }
	
	//讀取設定
    public void LoadSetting()
    {
        gameSetting.language =
            (LanguageType)
            PlayerPrefs.GetInt(
                "Language",
                0
            );
        gameSetting.bgmVolume =
            PlayerPrefs.GetFloat(
                "BGM",
                1f
            );
        gameSetting.seVolume =
            PlayerPrefs.GetFloat(
                "SE",
                1f
            );
    }
	
	//保存設定
    public void SaveSetting()
    {
        PlayerPrefs.SetInt(
            "Language",
            (int)gameSetting.language
        );
        PlayerPrefs.SetFloat(
            "BGM",
            gameSetting.bgmVolume
        );
        PlayerPrefs.SetFloat(
            "SE",
            gameSetting.seVolume
        );
        PlayerPrefs.Save();
    }
}
