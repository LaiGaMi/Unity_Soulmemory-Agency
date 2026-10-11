using System;
using System.Collections.Generic;

[Serializable]
public class DialogueFile
{
	public List<DialogueLine> dialogues;
}

[Serializable]
public class DialogueLine
{
	public string talkBox;
	public string nameBox;

	public CharacterSlot ch01;
	public CharacterSlot ch02;
	public CharacterSlot ch03;

	public string name;
	public string talk;

	public float textInterval;

	// 原有的逐字音效 ID
	public string soundId;

	// 新增：指定音效 ID
	public string effectSoundId;
}

[Serializable]
public class CharacterSlot
{
	public string id;
	public bool focus;
}