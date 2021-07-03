using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LongFallingBlock : RhythmObject
{
    public int length = 3;
    [Header("Temp Art")]
    [SerializeField] Image frame;
    [SerializeField] Image bg;
    [SerializeField] Image shadow;

    bool[] checkpoints;
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void Start()
    {
        base.Start();
        ApplyLength();
        checkpoints = new bool[length];
    }
    int curScore;
    protected override void Update_Activated()
    {
        float diff = (rt.anchoredPosition.x - GetBottom()) * (panel == PanelType.Left ? 1 : -1);
        bool getTouched = autoMode ? diff < 0 : GetExit().IsBeingTouched();

        // 判定第一次按键
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * BlockSize.x)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
            }
            else if (diff > 0.5f * BlockSize.x)
            {
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
            }
            else if (diff > -0.5 * BlockSize.x)
            {
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
            }
        }
        if (!checkpoints[0] && diff <= -0.6 * BlockSize.x)
        {
            Score(0);
            curScore = 0;
            checkpoints[0] = true;
        }

        // 判定第2~N次按键
        for (int i = 1; i < length; ++i)
        {
            if (!checkpoints[i] && diff > (-i - 0.5f) * BlockSize.x && diff <= (-i + 0.5f) * BlockSize.x)
            {
                if (getTouched)
                {
                    curScore = Mathf.Clamp(curScore + 1, 0, 2);
                    Score(curScore, sndIdx: i);
                }
                else
                {
                    curScore = Mathf.Clamp(curScore - 1, 0, 2);
                    Score(curScore, sndIdx: i);
                }
                checkpoints[i] = true;
            }
        }

        // 移除
        if (diff <= (-length + 0.5f) * BlockSize.x)
        {
            DestroyRhythmObject(this);
        }
    }

    public void ApplyLength()
    {
        var sizeDelta = new Vector2(length * BlockSize.x, BlockSize.y);
        var anchoredPosition = new Vector2((length - 1f) * BlockSize.x / 2, 0) * (noUpdate || panel == PanelType.Left ? 1 : -1);

        // temp art
        frame.rectTransform.sizeDelta = sizeDelta + new Vector2(-10, -10);
        bg.rectTransform.sizeDelta = shadow.rectTransform.sizeDelta = sizeDelta + new Vector2(15, 15);
        frame.rectTransform.anchoredPosition = bg.rectTransform.anchoredPosition = shadow.rectTransform.anchoredPosition = anchoredPosition;

        Sprite[] s = Random.Range(0, 2) == 1 ? ResourceManager.ins.UpNotes : ResourceManager.ins.DownNotes;
        noteImages = new Sprite[length];
        for (int i = 0; i < length; ++i)
        {
            var n = Instantiate(Resources.Load<GameObject>("Note"), rt).GetComponent<Image>();
            noteImages[i] = n.sprite = s[Random.Range(0, s.Length)];
            n.rectTransform.anchoredPosition = new Vector2(i * BlockSize.x, 0) * (noUpdate || panel == PanelType.Left ? 1 : -1);
        }
    }
}
