using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle01_Goal : MonoBehaviour
{
    [Header("目標Tag")]
    public string targetTag = "Car";

    [Header("是否完成")]
    private bool complete = false;


    private RectTransform rect;


    void Start()
    {
        rect =
            GetComponent<RectTransform>();
    }

    void Update()
    {
        if(complete)
            return;

        CheckTarget();
    }

    void CheckTarget()
    {
        Puzzle01_car[] cars =
            FindObjectsOfType<Puzzle01_car>();

        foreach(Puzzle01_car car in cars)
        {
            if(car.gameObject.CompareTag(targetTag))
            {
                RectTransform target =
                    car.GetComponent<RectTransform>();
					
                if(CheckOverlap(target))
                {
                    PuzzleSuccess();
                    return;
                }
            }
        }
    }

    bool CheckOverlap(RectTransform target)
    {
        Rect goalRect =
            GetWorldRect(rect);
        Rect targetRect =
            GetWorldRect(target);
        return goalRect.xMin <= targetRect.xMax &&
               goalRect.xMax >= targetRect.xMin &&
               goalRect.yMin <= targetRect.yMax &&
               goalRect.yMax >= targetRect.yMin;
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners =
            new Vector3[4];
        rt.GetWorldCorners(corners);


        float x =
            corners[0].x;
        float y =
            corners[0].y;
        float width =
            corners[2].x -
            corners[0].x;
        float height =
            corners[2].y -
            corners[0].y;

        return new Rect(
            x,
            y,
            width,
            height
        );
    }


    //通關
    void PuzzleSuccess()
    {
        complete = true;
		
        if(Puzzle.CurrentPuzzle != null)
        {
            Puzzle.CurrentPuzzle.PuzzleSuccess();
        }
    }
}
