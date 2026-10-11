using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Talk : MonoBehaviour
{
	public string talkFileName;
	private GameObject[] childObjects;

	private DialogueFile dialogueFile;
	private int currentIndex = 0;

	[Header("UI父物件")]
	public GameObject dialogueUI;

	[Header("UI圖片")]
	public Image talkBoxImage;
	public Image nameBoxImage;
	public GameObject continueIconUI;

	public Image ch01Image;
	public Image ch02Image;
	public Image ch03Image;

	[Header("文字")]
	public Text nameText;
	public Text talkText;

	[Header("圖片資料")]
	public GameImage imageDatabase;

	[Header("逐字音效")]
	public AudioSource dialogueAudioSource;

	[Range(0f, 1f)]
	public float dialogueSoundVolume = 1f;

	[Header("指定音效播放器")]
	public AudioSource effectAudioSource;

	[Range(0f, 1f)]
	public float effectSoundVolume = 1f;

	[Header("逐字顯示設定")]
	[Min(0f)]
	public float defaultTextInterval = 0.04f;

	[Header("下一句輸入")]
	public InputActionReference nextInput;
	
	[Header("跳過對話確認")]
	public Button skipButton;
	public GameObject skipConfirmUI;
	public Button skipYesButton;
	public Button skipNoButton;

	private bool isSkipConfirmOpen = false;
	private bool ignoreMouseClick = false;

	[Header("結束後啟用")]
	public GameObject[] enableObjects;

	[Header("立繪動畫設定")]
	[Min(0.01f)]
	public float fadeTime = 0.2f;

	[Min(0.01f)]
	public float moveTime = 0.3f;

	[Min(0.01f)]
	public float bounceHeight = 20f;

	[Min(0.01f)]
	public float bounceTime = 0.2f;

	[Header("聚焦變暗動畫設定")]
	[Min(0.01f)]
	public float focusFadeTime = 0.2f;

	[Range(0f, 1f)]
	public float unfocusedBrightness = 0.5f;

	[Header("下一句設定")]
	public float inputDelay = 0.1f;

	private bool talking = false;
	private bool canNext = false;
	private bool transitioning = false;
	private bool isTyping = false;

	private string currentTypingText = "";
	private AudioClip currentTypingSound;

	private List<StoryStop> storyObjects =
		new List<StoryStop>();

	// 立繪循環動畫狀態
	private CharacterPortraitData[] activePortraits =
		new CharacterPortraitData[3];

	private int[] portraitFrameIndices =
		new int[3];

	private float[] portraitTimers =
		new float[3];

	private Coroutine portraitAnimationCoroutine;

	private Coroutine transitionCoroutine;
	private Coroutine inputCoroutine;
	private Coroutine typingCoroutine;

	private Vector2[] homePositions = new Vector2[3];

	private Image[] characterImages;
	private CanvasGroup[] characterCanvasGroups;
	
	private static Dictionary<string, TextAsset> dialogueCache
	= new Dictionary<string, TextAsset>();
	
	
	private TextAsset LoadDialogueAsset(string path)
	{
		if(dialogueCache.TryGetValue(path, out TextAsset cachedAsset))
		{
			return cachedAsset;
		}

		TextAsset asset = Resources.Load<TextAsset>(path);

		if(asset != null)
		{
		dialogueCache.Add(path, asset);
		}

		return asset;
	}

	void Awake()
	{
		childObjects = new GameObject[]
		{
			gameObject
		};

		characterImages = new Image[]
		{
			ch01Image,
			ch02Image,
			ch03Image
		};

		characterCanvasGroups =
			new CanvasGroup[characterImages.Length];

		for(int i = 0; i < characterImages.Length; i++)
		{
			if(characterImages[i] == null)
				continue;

			homePositions[i] =
				characterImages[i].rectTransform.anchoredPosition;

			CanvasGroup group =
				characterImages[i].GetComponent<CanvasGroup>();

			if(group == null)
			{
				group =
					characterImages[i].gameObject.AddComponent<CanvasGroup>();
			}

			characterCanvasGroups[i] = group;
		}
		
		if(skipButton != null)
		{
			skipButton.onClick.AddListener(ShowSkipConfirm);
		}

		if(skipYesButton != null)
		{
			skipYesButton.onClick.AddListener(ConfirmSkipDialogue);
		}

		if(skipNoButton != null)
		{
			skipNoButton.onClick.AddListener(CancelSkipDialogue);
		}

		if(skipConfirmUI != null)
		{
			skipConfirmUI.SetActive(false);
		}
	}


	void OnEnable()
	{
		if(nextInput != null)
		{
			nextInput.action.Enable();
		}

		if(portraitAnimationCoroutine == null)
		{
			portraitAnimationCoroutine =
				StartCoroutine(UpdatePortraitAnimations());
		}

		StartDialogue(talkFileName, childObjects);
	}


	void OnDisable()
	{
		if(nextInput != null)
		{
			nextInput.action.Disable();
		}

		StopPortraitAnimations();
		StopDialogueSound();

		if(effectAudioSource != null)
		{
			effectAudioSource.Stop();
		}

		if(transitionCoroutine != null)
		{
			StopCoroutine(transitionCoroutine);
			transitionCoroutine = null;
		}

		if(inputCoroutine != null)
		{
			StopCoroutine(inputCoroutine);
			inputCoroutine = null;
		}

		if(typingCoroutine != null)
		{
			StopCoroutine(typingCoroutine);
			typingCoroutine = null;
		}

		isTyping = false;
		transitioning = false;

		SetContinueIcon(false);
	}


	void Update()
	{
		if(isSkipConfirmOpen)
			return;

		if(ignoreMouseClick)
		{
			if(!Input.GetMouseButton(0))
			{
				ignoreMouseClick = false;
			}

			return;
		}

		if(!talking || !canNext || transitioning)
			return;

		if(nextInput != null &&
			nextInput.action.WasPressedThisFrame())
		{
			NextDialogue();
			return;
		}

		if(Input.GetMouseButtonDown(0))
		{
			NextDialogue();
		}
	}


	// 開始對話
	public void StartDialogue(
		string fileName,
		GameObject[] objects
	)
	{
		CloseSkipConfirm();
		
		StartStory();
		LoadJSON(fileName);

		if(dialogueFile == null)
			return;

		if(dialogueFile.dialogues == null ||
			dialogueFile.dialogues.Count == 0)
		{
			Debug.LogError(
				"對話 JSON 沒有任何對話內容 : " + fileName
			);

			dialogueFile = null;
			return;
		}

		currentIndex = 0;
		talking = true;
		canNext = false;
		transitioning = false;
		isTyping = false;

		StopDialogueSound();

		if(effectAudioSource != null)
		{
			effectAudioSource.Stop();
		}

		ClearCharacterImages();
		SetContinueIcon(false);

		dialogueUI.SetActive(true);

		ShowDialogue();

		StartInputDelay();
	}


	void StartInputDelay()
	{
		if(inputCoroutine != null)
		{
			StopCoroutine(inputCoroutine);
		}

		inputCoroutine = StartCoroutine(EnableNextInput());
	}


	IEnumerator EnableNextInput()
	{
		canNext = false;

		yield return new WaitForSeconds(inputDelay);

		inputCoroutine = null;

		if(!transitioning && talking)
		{
			canNext = true;
		}
	}


	// 讀取 JSON
	void LoadJSON(string fileName)
	{
		dialogueFile = null;

		LanguageType language =
			GameManager.Instance.Setting.language;

		string languageFolder = language.ToString();

		string path =
			"Talk/" +
			languageFolder +
			"/" +
			fileName;

		TextAsset json = LoadDialogueAsset(path);

		if(json == null)
		{
			Debug.LogError("找不到對話 JSON : " + path);
			return;
		}

		dialogueFile =
			JsonUtility.FromJson<DialogueFile>(json.text);
	}


	// 顯示一句話
	void ShowDialogue()
	{
		DialogueLine data =
			dialogueFile.dialogues[currentIndex];

		talkBoxImage.sprite =
			imageDatabase.GetDialogueBox(data.talkBox);

		nameBoxImage.sprite =
			imageDatabase.GetNameBox(data.nameBox);

		nameText.text = data.name;
		talkText.text = "";

		currentTypingText = data.talk;
		currentTypingSound = null;

		// 取得逐字音效
		if(!string.IsNullOrEmpty(data.soundId))
		{
			currentTypingSound =
				imageDatabase.GetDialogueSound(data.soundId);

			if(currentTypingSound == null)
			{
				Debug.LogWarning(
					"Talk：找不到逐字音效 ID：" +
					data.soundId
				);
			}
		}

		// 播放本句指定音效
		PlaySoundEffect(data.effectSoundId);

		DialogueLine previousData = null;

		if(currentIndex > 0)
		{
			previousData =
				dialogueFile.dialogues[currentIndex - 1];
		}

		if(transitionCoroutine != null)
		{
			StopCoroutine(transitionCoroutine);
			transitionCoroutine = null;
		}

		transitionCoroutine = StartCoroutine(
			TransitionCharacters(previousData, data)
		);

		if(typingCoroutine != null)
		{
			StopCoroutine(typingCoroutine);
			typingCoroutine = null;
		}

		typingCoroutine = StartCoroutine(
			TypeDialogue(currentTypingText, data.textInterval)
		);
	}


	// 播放 JSON 指定音效
	void PlaySoundEffect(string soundId)
	{
		// ID 為空時不播放
		if(string.IsNullOrEmpty(soundId))
			return;

		if(imageDatabase == null)
		{
			Debug.LogWarning(
				"Talk：尚未綁定 GameImage。"
			);
			return;
		}

		if(effectAudioSource == null)
		{
			Debug.LogWarning(
				"Talk：尚未綁定指定音效播放器 Effect Audio Source。"
			);
			return;
		}

		AudioClip clip =
			imageDatabase.GetSoundEffect(soundId);

		if(clip == null)
		{
			Debug.LogWarning(
				"Talk：指定音效不存在或未設定，ID：" +
				soundId
			);
			return;
		}

		// 不會中斷逐字音效，音效會自然播放至結束
		effectAudioSource.PlayOneShot(
			clip,
			effectSoundVolume
		);
	}


	// 逐字顯示對話
	IEnumerator TypeDialogue(
		string text,
		float customInterval
	)
	{
		SetContinueIcon(false);
		isTyping = true;

		if(text == null)
		{
			text = "";
		}

		talkText.text = "";

		float interval =
			customInterval > 0f
				? customInterval
				: defaultTextInterval;

		interval = Mathf.Max(0f, interval);

		for(int i = 0; i < text.Length; i++)
		{
			if(!isTyping)
				yield break;

			char character = text[i];

			talkText.text += character;

			if(!char.IsWhiteSpace(character) &&
				!IsPunctuation(character))
			{
				PlayDialogueSound();
			}

			if(interval > 0f)
			{
				yield return new WaitForSeconds(interval);
			}
			else
			{
				yield return null;
			}
		}

		talkText.text = text;

		isTyping = false;
		typingCoroutine = null;

		SetContinueIcon(true);
	}


	// 判斷是否為標點符號
	bool IsPunctuation(char character)
	{
		UnicodeCategory category =
			char.GetUnicodeCategory(character);

		switch(category)
		{
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.DashPunctuation:
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
			case UnicodeCategory.OtherPunctuation:
				return true;
		}

		return false;
	}


	// 播放逐字音效
	void PlayDialogueSound()
	{
		if(dialogueAudioSource == null)
		{
			Debug.LogWarning(
				"Talk：尚未綁定 Dialogue Audio Source。"
			);
			return;
		}

		if(currentTypingSound == null)
			return;

		dialogueAudioSource.PlayOneShot(
			currentTypingSound,
			dialogueSoundVolume
		);
	}


	// 停止逐字音效
	void StopDialogueSound()
	{
		if(dialogueAudioSource != null)
		{
			dialogueAudioSource.Stop();
		}
	}


	// 立刻顯示完整句子
	void FinishTypingImmediately()
	{
		if(!isTyping)
			return;

		isTyping = false;
		SetContinueIcon(true);

		if(typingCoroutine != null)
		{
			StopCoroutine(typingCoroutine);
			typingCoroutine = null;
		}

		talkText.text = currentTypingText;

		// 不停止音效，也不干涉立繪循環動畫
	}


	// 立繪轉場總流程
	IEnumerator TransitionCharacters(
		DialogueLine previousData,
		DialogueLine currentData
	)
	{
		transitioning = true;
		canNext = false;

		CharacterSlot[] previousSlots =
			GetCharacterSlots(previousData);

		CharacterSlot[] currentSlots =
			GetCharacterSlots(currentData);

		int[] moveSourceForTarget = new int[3];

		for(int i = 0; i < 3; i++)
		{
			moveSourceForTarget[i] = -1;
		}

		bool[] isMoveSource = new bool[3];
		bool[] isStationary = new bool[3];
		bool[] isMoveTarget = new bool[3];

		for(int targetIndex = 0; targetIndex < 3; targetIndex++)
		{
			string targetCharacter =
				GetCharacterId(currentSlots[targetIndex]);

			if(targetCharacter == "")
				continue;

			string previousCharacter =
				GetCharacterId(previousSlots[targetIndex]);

			if(targetCharacter == previousCharacter)
			{
				isStationary[targetIndex] = true;
				continue;
			}

			for(int sourceIndex = 0; sourceIndex < 3; sourceIndex++)
			{
				if(sourceIndex == targetIndex)
					continue;

				if(isMoveSource[sourceIndex])
					continue;

				string sourceCharacter =
					GetCharacterId(previousSlots[sourceIndex]);

				if(sourceCharacter != targetCharacter)
					continue;

				bool remainsAtSource =
					GetCharacterId(currentSlots[sourceIndex]) ==
					sourceCharacter;

				if(remainsAtSource)
					continue;

				moveSourceForTarget[targetIndex] = sourceIndex;
				isMoveSource[sourceIndex] = true;
				isMoveTarget[targetIndex] = true;

				break;
			}
		}

		// 第一階段：舊立繪淡出
		List<Coroutine> fadeOutCoroutines =
			new List<Coroutine>();

		for(int i = 0; i < 3; i++)
		{
			if(isMoveSource[i] || isStationary[i])
				continue;

			if(GetCharacterId(previousSlots[i]) == "")
				continue;

			if(characterImages[i] == null)
				continue;

			fadeOutCoroutines.Add(
				StartCoroutine(FadeImage(i, 0f, fadeTime))
			);
		}

		foreach(Coroutine coroutine in fadeOutCoroutines)
		{
			yield return coroutine;
		}

		// 第二階段：同角色移動至新位置
		for(int targetIndex = 0; targetIndex < 3; targetIndex++)
		{
			int sourceIndex = moveSourceForTarget[targetIndex];

			if(sourceIndex < 0)
				continue;

			Image sourceImage = characterImages[sourceIndex];
			Image targetImage = characterImages[targetIndex];

			if(sourceImage == null || targetImage == null)
				continue;

			yield return StartCoroutine(
				MoveImage(
					sourceImage,
					homePositions[targetIndex],
					moveTime
				)
			);

			CharacterPortraitData portrait =
				GetPortrait(currentSlots[targetIndex]);

			if(portrait != null &&
				portrait.images != null &&
				portrait.images.Count > 0)
			{
				TransferPortraitAnimation(
					sourceIndex,
					targetIndex,
					portrait
				);

				targetImage.sprite =
					portrait.images[portraitFrameIndices[targetIndex]];
			}
			else
			{
				targetImage.sprite = sourceImage.sprite;

				activePortraits[targetIndex] = null;
				activePortraits[sourceIndex] = null;
			}

			SetImageAlpha(targetIndex, 1f);

			sourceImage.sprite = null;
			SetImageAlpha(sourceIndex, 0f);

			sourceImage.rectTransform.anchoredPosition =
				homePositions[sourceIndex];
		}

		// 第三階段：處理原地角色
		for(int i = 0; i < 3; i++)
		{
			if(!isStationary[i])
				continue;

			Image image = characterImages[i];

			if(image == null)
				continue;

			CharacterPortraitData portrait =
				GetPortrait(currentSlots[i]);

			if(portrait == null ||
				portrait.images == null ||
				portrait.images.Count == 0)
			{
				image.sprite = null;
				SetImageAlpha(i, 0f);
				activePortraits[i] = null;
				continue;
			}

			SetPortraitAnimation(i, portrait);

			image.sprite =
				portrait.images[portraitFrameIndices[i]];

			SetImageAlpha(i, 1f);

			image.rectTransform.anchoredPosition =
				homePositions[i];
		}

		// 第四階段：新角色淡入
		List<Coroutine> fadeInCoroutines =
			new List<Coroutine>();

		for(int i = 0; i < 3; i++)
		{
			if(isStationary[i] || isMoveTarget[i])
				continue;

			CharacterPortraitData portrait =
				GetPortrait(currentSlots[i]);

			Image image = characterImages[i];

			if(image == null)
				continue;

			if(GetCharacterId(currentSlots[i]) == "" ||
				portrait == null ||
				portrait.images == null ||
				portrait.images.Count == 0)
			{
				image.sprite = null;
				SetImageAlpha(i, 0f);
				activePortraits[i] = null;
				continue;
			}

			image.rectTransform.anchoredPosition =
				homePositions[i];

			SetPortraitAnimation(i, portrait);

			image.sprite =
				portrait.images[portraitFrameIndices[i]];

			SetImageAlpha(i, 0f);

			fadeInCoroutines.Add(
				StartCoroutine(FadeImage(i, 1f, fadeTime))
			);
		}

		// 第五階段：聚焦顏色漸變
		List<Coroutine> focusCoroutines =
			new List<Coroutine>();

		for(int i = 0; i < 3; i++)
		{
			if(GetCharacterId(currentSlots[i]) == "")
				continue;

			Image image = characterImages[i];

			CharacterPortraitData portrait =
				GetPortrait(currentSlots[i]);

			if(image == null ||
				portrait == null ||
				portrait.images == null ||
				portrait.images.Count == 0)
			{
				continue;
			}

			focusCoroutines.Add(
				StartCoroutine(
					FadeFocusColor(
						image,
						GetFocusColor(currentSlots[i]),
						focusFadeTime
					)
				)
			);
		}

		foreach(Coroutine coroutine in fadeInCoroutines)
		{
			yield return coroutine;
		}

		foreach(Coroutine coroutine in focusCoroutines)
		{
			yield return coroutine;
		}

		// 第六階段：focus 為 true 的立繪彈跳
		List<Coroutine> bounceCoroutines =
			new List<Coroutine>();

		for(int i = 0; i < 3; i++)
		{
			if(currentSlots[i] == null ||
				!currentSlots[i].focus)
			{
				continue;
			}

			if(GetCharacterId(currentSlots[i]) == "")
				continue;

			Image image = characterImages[i];

			if(image == null || image.sprite == null)
				continue;

			bounceCoroutines.Add(
				StartCoroutine(
					BounceImage(
						image,
						bounceHeight,
						bounceTime
					)
				)
			);
		}

		foreach(Coroutine coroutine in bounceCoroutines)
		{
			yield return coroutine;
		}

		transitioning = false;
		transitionCoroutine = null;

		if(inputCoroutine == null && talking)
		{
			canNext = true;
		}
	}


	// 取得三個角色欄位
	CharacterSlot[] GetCharacterSlots(DialogueLine data)
	{
		if(data == null)
		{
			return new CharacterSlot[3];
		}

		return new CharacterSlot[]
		{
			data.ch01,
			data.ch02,
			data.ch03
		};
	}


	// 取得角色 ID
	string GetCharacterId(CharacterSlot data)
	{
		if(data == null ||
			string.IsNullOrEmpty(data.id) ||
			data.id == "no")
		{
			return "";
		}

		int separatorIndex = data.id.IndexOf('_');

		if(separatorIndex <= 0)
			return data.id;

		return data.id.Substring(0, separatorIndex);
	}


	// 取得立繪資料
	CharacterPortraitData GetPortrait(CharacterSlot data)
	{
		if(data == null ||
			string.IsNullOrEmpty(data.id) ||
			data.id == "no")
		{
			return null;
		}

		if(imageDatabase == null)
			return null;

		return imageDatabase.GetCharacterPortrait(data.id);
	}


	// 設定指定位置的立繪動畫資料
	void SetPortraitAnimation(
		int index,
		CharacterPortraitData portrait
	)
	{
		if(index < 0 || index >= activePortraits.Length)
			return;

		if(portrait == null ||
			portrait.images == null ||
			portrait.images.Count == 0)
		{
			activePortraits[index] = null;
			portraitFrameIndices[index] = 0;
			portraitTimers[index] = 0f;
			return;
		}

		if(activePortraits[index] == portrait)
			return;

		activePortraits[index] = portrait;
		portraitFrameIndices[index] = 0;
		portraitTimers[index] = 0f;
	}


	// 將動畫狀態從來源位置轉移至目標位置
	void TransferPortraitAnimation(
		int sourceIndex,
		int targetIndex,
		CharacterPortraitData targetPortrait
	)
	{
		if(sourceIndex < 0 ||
			sourceIndex >= activePortraits.Length ||
			targetIndex < 0 ||
			targetIndex >= activePortraits.Length)
		{
			return;
		}

		if(targetPortrait == null ||
			targetPortrait.images == null ||
			targetPortrait.images.Count == 0)
		{
			activePortraits[targetIndex] = null;
			portraitFrameIndices[targetIndex] = 0;
			portraitTimers[targetIndex] = 0f;
			activePortraits[sourceIndex] = null;
			return;
		}

		int sourceFrame = portraitFrameIndices[sourceIndex];
		float sourceTimer = portraitTimers[sourceIndex];

		portraitFrameIndices[targetIndex] =
			sourceFrame % targetPortrait.images.Count;

		activePortraits[targetIndex] = targetPortrait;
		portraitTimers[targetIndex] = sourceTimer;

		activePortraits[sourceIndex] = null;
		portraitFrameIndices[sourceIndex] = 0;
		portraitTimers[sourceIndex] = 0f;
	}


	// 持續更新所有立繪循環動畫
	IEnumerator UpdatePortraitAnimations()
	{
		while(true)
		{
			for(int i = 0; i < characterImages.Length; i++)
			{
				Image image = characterImages[i];

				CharacterPortraitData portrait =
					activePortraits[i];

				if(image == null ||
					portrait == null ||
					portrait.images == null ||
					portrait.images.Count <= 1)
				{
					continue;
				}

				float loopTime =
					Mathf.Max(0.01f, portrait.loopTime);

				portraitTimers[i] += Time.deltaTime;

				if(portraitTimers[i] >= loopTime)
				{
					portraitTimers[i] %= loopTime;
					portraitFrameIndices[i]++;

					if(portraitFrameIndices[i] >=
						portrait.images.Count)
					{
						portraitFrameIndices[i] = 0;
					}

					image.sprite =
						portrait.images[portraitFrameIndices[i]];
				}
			}

			yield return null;
		}
	}


	// 取得聚焦顏色
	Color GetFocusColor(CharacterSlot data)
	{
		if(data != null && data.focus)
		{
			return Color.white;
		}

		return new Color(
			unfocusedBrightness,
			unfocusedBrightness,
			unfocusedBrightness,
			1f
		);
	}


	// 聚焦顏色漸變
	IEnumerator FadeFocusColor(
		Image image,
		Color targetColor,
		float duration
	)
	{
		if(image == null)
			yield break;

		Color startColor = image.color;
		float timer = 0f;

		if(duration <= 0f)
		{
			image.color = targetColor;
			yield break;
		}

		while(timer < duration)
		{
			if(image == null)
				yield break;

			timer += Time.deltaTime;

			float t = Mathf.Clamp01(timer / duration);
			t = Mathf.SmoothStep(0f, 1f, t);

			Color color = image.color;

			color.r = Mathf.Lerp(startColor.r, targetColor.r, t);
			color.g = Mathf.Lerp(startColor.g, targetColor.g, t);
			color.b = Mathf.Lerp(startColor.b, targetColor.b, t);

			image.color = color;

			yield return null;
		}

		Color finalColor = image.color;

		finalColor.r = targetColor.r;
		finalColor.g = targetColor.g;
		finalColor.b = targetColor.b;

		image.color = finalColor;
	}


	// 設定圖片透明度
	void SetImageAlpha(int index, float alpha)
	{
		if(index < 0 ||
			index >= characterCanvasGroups.Length)
		{
			return;
		}

		CanvasGroup group = characterCanvasGroups[index];

		if(group == null)
			return;

		group.alpha = alpha;
	}


	// 立繪淡入淡出
	IEnumerator FadeImage(
		int index,
		float targetAlpha,
		float duration
	)
	{
		if(index < 0 ||
			index >= characterCanvasGroups.Length)
		{
			yield break;
		}

		CanvasGroup group = characterCanvasGroups[index];

		if(group == null)
			yield break;

		float startAlpha = group.alpha;
		float timer = 0f;

		if(duration <= 0f)
		{
			group.alpha = targetAlpha;
			yield break;
		}

		while(timer < duration)
		{
			if(group == null)
				yield break;

			timer += Time.deltaTime;

			float t = Mathf.Clamp01(timer / duration);
			t = Mathf.SmoothStep(0f, 1f, t);

			group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

			yield return null;
		}

		group.alpha = targetAlpha;
	}


	// 移動立繪
	IEnumerator MoveImage(
		Image image,
		Vector2 targetPosition,
		float duration
	)
	{
		if(image == null)
			yield break;

		RectTransform rect = image.rectTransform;
		Vector2 startPosition = rect.anchoredPosition;
		float timer = 0f;

		if(duration <= 0f)
		{
			rect.anchoredPosition = targetPosition;
			yield break;
		}

		while(timer < duration)
		{
			if(image == null)
				yield break;

			timer += Time.deltaTime;

			float t = Mathf.Clamp01(timer / duration);
			t = Mathf.SmoothStep(0f, 1f, t);

			rect.anchoredPosition =
				Vector2.Lerp(startPosition, targetPosition, t);

			yield return null;
		}

		rect.anchoredPosition = targetPosition;
	}


	// 彈跳動畫
	IEnumerator BounceImage(
		Image image,
		float height,
		float duration
	)
	{
		if(image == null)
			yield break;

		RectTransform rect = image.rectTransform;
		Vector2 startPosition = rect.anchoredPosition;
		float timer = 0f;

		if(duration <= 0f)
		{
			rect.anchoredPosition = startPosition;
			yield break;
		}

		while(timer < duration)
		{
			if(image == null)
				yield break;

			timer += Time.deltaTime;

			float t = Mathf.Clamp01(timer / duration);
			float offset = Mathf.Sin(t * Mathf.PI) * height;

			rect.anchoredPosition =
				startPosition + new Vector2(0f, offset);

			yield return null;
		}

		rect.anchoredPosition = startPosition;
	}


	// 清除所有立繪
	void ClearCharacterImages()
	{
		for(int i = 0; i < characterImages.Length; i++)
		{
			Image image = characterImages[i];

			activePortraits[i] = null;
			portraitFrameIndices[i] = 0;
			portraitTimers[i] = 0f;

			if(image == null)
				continue;

			image.sprite = null;

			SetImageAlpha(i, 0f);

			image.color = Color.white;

			image.rectTransform.anchoredPosition =
				homePositions[i];
		}
	}


	// 停止所有角色立繪動畫
	void StopPortraitAnimations()
	{
		if(portraitAnimationCoroutine != null)
		{
			StopCoroutine(portraitAnimationCoroutine);
			portraitAnimationCoroutine = null;
		}

		for(int i = 0; i < activePortraits.Length; i++)
		{
			activePortraits[i] = null;
			portraitFrameIndices[i] = 0;
			portraitTimers[i] = 0f;
		}
	}


	// 停止立繪循環並固定在最後一張圖片
	void StopPortraitAnimationsAndSetFinalFrame(
		CharacterSlot[] slots
	)
	{
		StopPortraitAnimations();

		for(int i = 0; i < 3; i++)
		{
			Image image = characterImages[i];

			if(image == null)
				continue;

			CharacterPortraitData portrait =
				GetPortrait(slots[i]);

			if(portrait == null ||
				portrait.images == null ||
				portrait.images.Count == 0)
			{
				continue;
			}

			image.sprite =
				portrait.images[portrait.images.Count - 1];
		}
	}


	// 控制繼續圖標
	void SetContinueIcon(bool isVisible)
	{
		if(continueIconUI != null)
		{
			continueIconUI.SetActive(isVisible);
		}
	}
	
	// 顯示跳過確認 UI
	void ShowSkipConfirm()
	{
		if(!talking || isSkipConfirmOpen)
			return;

		ignoreMouseClick = true;
		isSkipConfirmOpen = true;
		canNext = false;

		if(skipConfirmUI != null)
		{
			skipConfirmUI.SetActive(true);
		}
		else
		{
			Debug.LogWarning("Talk：尚未綁定 Skip Confirm UI。");
			isSkipConfirmOpen = false;

			if(!transitioning && talking)
			{
				canNext = true;
			}
		}
	}


	// 按下「是」：跳過整段對話
	public void ConfirmSkipDialogue()
	{
		if(!isSkipConfirmOpen)
			return;

		isSkipConfirmOpen = false;

		if(skipConfirmUI != null)
		{
			skipConfirmUI.SetActive(false);
		}

		EndDialogue();
	}


	// 按下「否」：關閉確認 UI
	public void CancelSkipDialogue()
	{
		if(!isSkipConfirmOpen)
			return;

		isSkipConfirmOpen = false;

		if(skipConfirmUI != null)
		{
			skipConfirmUI.SetActive(false);
		}

		if(!transitioning && talking)
		{
			canNext = true;
		}
	}


	// 關閉跳過確認 UI
	void CloseSkipConfirm()
	{
		isSkipConfirmOpen = false;

		if(skipConfirmUI != null)
		{
			skipConfirmUI.SetActive(false);
		}
	}


	// 下一句
	void NextDialogue()
	{
		if(transitioning)
			return;

		if(isTyping)
		{
			FinishTypingImmediately();
			return;
		}

		if(dialogueFile == null ||
			dialogueFile.dialogues == null)
		{
			return;
		}

		currentIndex++;

		if(currentIndex >= dialogueFile.dialogues.Count)
		{
			EndDialogue();
			return;
		}

		canNext = false;

		ShowDialogue();

		StartInputDelay();
	}


	// 結束對話
	void EndDialogue()
	{
		CloseSkipConfirm();

		talking = false;
		canNext = false;
		transitioning = false;
		isTyping = false;
		
		talking = false;
		canNext = false;
		transitioning = false;
		isTyping = false;

		StopPortraitAnimations();
		StopDialogueSound();

		if(effectAudioSource != null)
		{
			effectAudioSource.Stop();
		}

		if(transitionCoroutine != null)
		{
			StopCoroutine(transitionCoroutine);
			transitionCoroutine = null;
		}

		if(typingCoroutine != null)
		{
			StopCoroutine(typingCoroutine);
			typingCoroutine = null;
		}

		if(inputCoroutine != null)
		{
			StopCoroutine(inputCoroutine);
			inputCoroutine = null;
		}

		SetContinueIcon(false);

		dialogueUI.SetActive(false);

		foreach(GameObject obj in enableObjects)
		{
			if(obj != null)
			{
				obj.SetActive(true);
			}
		}

		StopStory();

		gameObject.SetActive(false);
	}


	void StartStory()
	{
		storyObjects.Clear();

		MonoBehaviour[] allScripts =
			FindObjectsOfType<MonoBehaviour>();

		foreach(MonoBehaviour script in allScripts)
		{
			if(!script.enabled)
				continue;

			StoryStop story = script as StoryStop;

			if(story == null)
				continue;

			storyObjects.Add(story);
			story.StartStory();
		}
	}


	void StopStory()
	{
		foreach(StoryStop story in storyObjects)
		{
			if(story != null)
			{
				story.StopStory();
			}
		}

		storyObjects.Clear();
	}
}