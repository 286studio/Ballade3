using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Beat : RhythmObject
{
    public float lifetime = 1f;
    [SerializeField] Image spreading, outter;
    [SerializeField] Sprite[] animSprites;
    Vector2 startSize = new Vector2(40, 40);
    Vector2 endSize = new Vector2(150, 150);
    float startTime;
    bool needReleasing = true;
    public static float radius { get; } = 65;
    public override RhythmType Type => RhythmType.Misc;

    protected override void Start()
    {
        base.Start();
        startTime = Time.time;
        noAnim = true;
    }

    protected override void Update()
    {
        if (Time.time - startTime >= lifetime + fallingTime)
        {
            spreading.rectTransform.sizeDelta = endSize;
            Score(0, (transform as RectTransform).anchoredPosition);
            Destroy(gameObject);
        }
        else if (Time.time - startTime >= fallingTime)
        {
            float frac = (Time.time - startTime) / fallingTime;
            outter.sprite = animSprites[Mathf.RoundToInt(frac * (animSprites.Length - 1)) % animSprites.Length];
            if (ReallyGetTouched())
            {
                Score(2, rt.anchoredPosition);
                Destroy(gameObject);
            }
        }
        else
        {
            float frac = (Time.time - startTime) / fallingTime;
            spreading.rectTransform.sizeDelta = Vector2.Lerp(startSize, endSize, frac);
            outter.sprite = animSprites[Mathf.RoundToInt(frac * (animSprites.Length - 1))];
            if (ReallyGetTouched())
            {
                Score(1, rt.anchoredPosition);
                Destroy(gameObject);
            }
        }
    }
    protected override void Update_Activated()
    {
        
    }
    public bool ReallyGetTouched()
    {
        bool ret = GetTouched();
        if (needReleasing)
        {
            if (ret) ret = false;
            else needReleasing = false;
        }
        return ret;
    }
    bool GetTouched()
    {
        for (int i = 0; i < Input.touchCount; ++i) if (IsBeingTouchedBy(Input.GetTouch(i))) return true;
        return false;
    }
    public bool IsBeingTouchedBy(Touch t)
    {
        var pos = Utils.ScreenToCanvasPos(t.position);
        return Vector2.Distance(pos, rt.anchoredPosition + (parent ? parent.rt.anchoredPosition : Vector2.zero)) < radius;
    }
}
