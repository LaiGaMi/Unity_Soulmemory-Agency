using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle00_box : MonoBehaviour
{
    [Header("目標Tag")]
    public string targetTag = "Relic";

    private RectTransform rect;


    void Start()
    {
        rect =
            GetComponent<RectTransform>();
    }


    void Update()
    {
        CheckTarget();
    }


    //檢查是否有遺物進入
    void CheckTarget()
    {
        GameObject[] relics =
            GameObject.FindGameObjectsWithTag(
                targetTag
            );

        foreach(GameObject relic in relics)
        {
            if(relic == null)
                continue;

            RectTransform target =
                relic.GetComponent<RectTransform>();

            if(target == null)
                continue;

            if(CheckOverlap(target))
            {
                //移除遺物
                Destroy(relic);

                //一次只處理一個
                return;
            }
        }
    }


    //檢查是否接觸
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


    //取得UI世界座標範圍
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
}