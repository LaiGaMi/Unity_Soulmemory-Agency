using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleUI : MonoBehaviour
{
	[Header("謎題可活動範圍")]
    public RectTransform moveArea;
	
    public void PuzzleExit()
    {
        if(Puzzle.CurrentPuzzle != null)
        {
            Puzzle.CurrentPuzzle.PuzzleExit();
        }
    }
}
