using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour, puzzle
{
    [Header("謎題UI")]
    public GameObject puzzleUI;

    [Header("謎題預置物")]
    public GameObject puzzlePrefab;
	
	[Header("通關後啟用物件")]
    public GameObject successObject;
	
	private GameObject currentPuzzle;
	
	private List<StoryStop> storyObjects =
        new List<StoryStop>();
		
	public static Puzzle CurrentPuzzle;
		
    void OnEnable()
    {
        StartPuzzle();
		CurrentPuzzle = this;
    }

    void StartPuzzle()
    {
        puzzleUI.SetActive(true);

        //生成謎題
        if(puzzlePrefab != null && puzzleUI != null)
        {
            currentPuzzle =
                Instantiate(
                    puzzlePrefab,
                    puzzleUI.transform
                );
        }
		StartStory();
    }
	
	//謎題完成
    public void PuzzleSuccess()
    {
        ClearPuzzle();

        puzzleUI.SetActive(false);


        if(successObject != null)
        {
            successObject.SetActive(true);
        }

        StopStory();
		gameObject.SetActive(false);
    }
	void OnDisable()
    {
        if(CurrentPuzzle == this)
        {
            CurrentPuzzle = null;
        }
    }
	
	//退出謎題
    public void PuzzleExit()
    {
        ClearPuzzle();

        puzzleUI.SetActive(false);

        StopStory();
		gameObject.SetActive(false);
    }
	
	//刪除謎題內容
    void ClearPuzzle()
    {
        if(currentPuzzle != null)
        {
            Destroy(currentPuzzle);
            currentPuzzle = null;
        }
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