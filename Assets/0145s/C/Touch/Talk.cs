using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    public Image ch01Image;
    public Image ch02Image;
    public Image ch03Image;

    [Header("文字")]
    public Text nameText;
    public Text talkText;

    [Header("圖片資料")]
    public GameImage imageDatabase;

    [Header("下一句輸入")]
    public InputActionReference nextInput;

    [Header("結束後啟用")]
    public GameObject[] enableObjects;

    private bool talking = false;
	
	private bool canNext = false;
	public float inputDelay = 0.1f;
	
	private List<StoryStop> storyObjects =
        new List<StoryStop>();

    void Awake()
    {
		childObjects = new GameObject[]
    	{
    	    gameObject
    	};
    }
	
	public void OnEnable()
    {
		if(nextInput != null)
        {
            nextInput.action.Enable();
        }
		
		StartDialogue(talkFileName, childObjects);
    }
	
    void OnDisable()
    {
        if(nextInput != null)
        {
            nextInput.action.Disable();
        }
    }

    void Update()
    {
        if(!talking || !canNext)
            return;

        //鍵盤
        if(nextInput != null)
        {
            if(nextInput.action.WasPressedThisFrame())
            {
                NextDialogue();
            }
        }

        //滑鼠 / 觸控
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
		StartStory();
        LoadJSON(fileName);

        if(dialogueFile == null)
            return;


        currentIndex = 0;
        talking = true;
		canNext = false;
		
        dialogueUI.SetActive(true);
        ShowDialogue();
		
		StartCoroutine(EnableNextInput());
    }
	
	IEnumerator EnableNextInput()
	{
    	yield return new WaitForSeconds(inputDelay);
    	canNext = true;
	}

    // 讀取JSON
    void LoadJSON(string fileName)
    {
        LanguageType language =
            GameManager.Instance.Setting.language;
        string suffix = language.ToString();
        string path =
            "Talk/" +
            fileName +
            "." +
            suffix;
        TextAsset json =
            Resources.Load<TextAsset>(path);
        if(json == null)
        {
            Debug.LogError(
                "找不到JSON : " + path
            );

            return;
        }
        dialogueFile =
            JsonUtility.FromJson<DialogueFile>(
                json.text
            );
    }

    // 顯示一句話
    void ShowDialogue()
    {

        DialogueLine data =
            dialogueFile.dialogues[currentIndex];

        //設定對話框
        talkBoxImage.sprite =
            imageDatabase.GetDialogueBox(
                data.talkBox
            );

        //姓名框
        nameBoxImage.sprite =
            imageDatabase.GetNameBox(
                data.nameBox
            );

        //文字
        nameText.text =
            data.name;
        talkText.text =
            data.talk;

        //角色圖片
        SetCharacter(
            ch01Image,
            data.ch01
        );

        SetCharacter(
            ch02Image,
            data.ch02
        );

        SetCharacter(
            ch03Image,
            data.ch03
        );
    }

    // 設定角色立繪
    void SetCharacter(Image image,CharacterSlot data)
    {
        //沒有角色
        if(data == null)
        {
            image.color =
                new Color(
                    1,
                    1,
                    1,
                    0
                );

            return;
        }

        Sprite sprite =
            imageDatabase.GetCharacterImage(
                data.id
            );
			
        image.sprite = sprite;

        //恢復透明
        image.color =
            new Color(
                1,
                1,
                1,
                1
            );

        //是否聚焦
        if(data.focus && data != null)
        {
            image.color =
                Color.white;
        }
        else
        {
            Color gray =
                new Color(
                    0.5f,
                    0.5f,
                    0.5f
                );
            image.color =
                gray;
        }
    }

    // 下一句
    void NextDialogue()
    {
        currentIndex++;
        if(currentIndex >= dialogueFile.dialogues.Count)
        {
            EndDialogue();
            return;
        }
        ShowDialogue();
    }

    // 結束對話
    void EndDialogue()
    {
        talking = false;
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
	
	        StoryStop story =
	            script as StoryStop;
	
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
