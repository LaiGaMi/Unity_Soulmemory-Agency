using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
	fileName = "GameImage",
	menuName = "Game/Game Image"
)]
public class GameImage : ScriptableObject
{
	[Header("對話框清單")]
	public List<DialogueBoxData> dialogueBoxList =
		new List<DialogueBoxData>();

	[Header("姓名框清單")]
	public List<NameBoxData> nameBoxList =
		new List<NameBoxData>();

	[Header("立繪清單")]
	public List<CharacterPortraitGroup> characterImageList =
		new List<CharacterPortraitGroup>();

	[Header("逐字音效清單")]
	public List<DialogueSoundData> dialogueSoundList =
		new List<DialogueSoundData>();

	[Header("指定音效清單")]
	public List<SoundEffectData> soundEffectList =
		new List<SoundEffectData>();


	// 取得對話框
	public Sprite GetDialogueBox(string id)
	{
		foreach(DialogueBoxData data in dialogueBoxList)
		{
			if(data.id == id)
			{
				return data.image;
			}
		}

		Debug.LogWarning("找不到對話框 ID : " + id);
		return null;
	}


	// 取得姓名框
	public Sprite GetNameBox(string id)
	{
		foreach(NameBoxData data in nameBoxList)
		{
			if(data.id == id)
			{
				return data.image;
			}
		}

		Debug.LogWarning("找不到姓名框 ID : " + id);
		return null;
	}


	// 取得逐字音效
	public AudioClip GetDialogueSound(string id)
	{
		if(string.IsNullOrEmpty(id))
		{
			return null;
		}

		foreach(DialogueSoundData data in dialogueSoundList)
		{
			if(data.id == id)
			{
				return data.clip;
			}
		}

		Debug.LogWarning("找不到逐字音效 ID : " + id);
		return null;
	}


	// 取得指定音效
	public AudioClip GetSoundEffect(string id)
	{
		if(string.IsNullOrEmpty(id))
		{
			return null;
		}

		foreach(SoundEffectData data in soundEffectList)
		{
			if(data.id == id)
			{
				return data.clip;
			}
		}

		Debug.LogWarning("找不到指定音效 ID : " + id);
		return null;
	}


	// 取得立繪資料
	// ID 格式：角色ID_立繪ID
	public CharacterPortraitData GetCharacterPortrait(string id)
	{
		if(string.IsNullOrEmpty(id))
		{
			Debug.LogWarning("立繪 ID 為空");
			return null;
		}

		int separatorIndex = id.IndexOf('_');

		if(separatorIndex <= 0 ||
			separatorIndex >= id.Length - 1)
		{
			Debug.LogWarning(
				"立繪 ID 格式錯誤，應為「角色ID_立繪ID」 : " + id
			);

			return null;
		}

		string characterId =
			id.Substring(0, separatorIndex);

		string portraitId =
			id.Substring(separatorIndex + 1);

		foreach(CharacterPortraitGroup character in characterImageList)
		{
			if(character.characterId != characterId)
			{
				continue;
			}

			foreach(CharacterPortraitData portrait in character.portraitList)
			{
				if(portrait.id == portraitId)
				{
					return portrait;
				}
			}

			Debug.LogWarning("找不到立繪 ID : " + id);
			return null;
		}

		Debug.LogWarning("找不到角色 ID : " + characterId);
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


// 逐字音效資料
[System.Serializable]
public class DialogueSoundData
{
	[Header("音效 ID")]
	public string id;

	[Header("音效檔案")]
	public AudioClip clip;
}


// 指定音效資料
[System.Serializable]
public class SoundEffectData
{
	[Header("音效 ID")]
	public string id;

	[Header("音效檔案")]
	public AudioClip clip;
}


// 角色資料
[System.Serializable]
public class CharacterPortraitGroup
{
	[Header("角色 ID")]
	public string characterId;

	[Header("立繪清單")]
	public List<CharacterPortraitData> portraitList =
		new List<CharacterPortraitData>();
}


// 角色的立繪資料
[System.Serializable]
public class CharacterPortraitData
{
	[Header("立繪 ID")]
	public string id;

	[Header("立繪圖片清單")]
	public List<Sprite> images =
		new List<Sprite>();

	[Header("立繪循環時間")]
	[Min(0.01f)]
	public float loopTime = 0.1f;
}