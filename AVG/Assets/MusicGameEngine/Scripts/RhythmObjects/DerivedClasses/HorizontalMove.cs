using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum Direction
{
    Up,
    Down
}
public class HorizontalMove : RhythmObject
{
    [Header("Properties")]
    public int width;
    public Direction direction = Direction.Down;
    [Header("Graphics")]
    [SerializeField] Image block;
    [SerializeField] Image line;
    [SerializeField] Image outterFrame;
    [SerializeField] Image outterBg;
    bool[] checkpoints;

    public override RhythmType Type => RhythmType.HorizontalMove;

    protected override void Start()
    {
        base.Start();
        ApplyWidth();
        checkpoints = new bool[width];
    }
    public override RhythmObject Initialize(int _exit, PanelType _panel, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        var ret = base.Initialize(_exit, _panel, c, _perfectScore, _goodScore, _badScore);
        line.color = c != null ? c.Value / 2 : Color.grey;
        return ret;
    }
    public override RhythmObject Initialize_LevelEditor(int _exit, PanelType _panel, Color? c = null)
    {
        var ret = base.Initialize_LevelEditor(_exit, _panel, c);
        line.color = c != null ? c.Value / 2 : Color.grey;
        return ret;
    }

    float notTouchedTimeCount;
    protected override void Update_Activated()
    {
        bool getTouched = false;
        for (int i = 0; i < width; ++i)
        {
            if (GetExit(direction == Direction.Down ? i : -i).IsBeingTouched())
            {
                getTouched = true;
                break;
            }
        }

        if (autoMode && Mathf.Abs(rt.anchoredPosition.x - GetBottom()) < 0.1f * BlockSize.x)
            getTouched = true;

        int curScore = 2; // 0 = bad, 1 = good, 2 = perfect
        float diff = (rt.anchoredPosition.x - GetBottom()) * (panel == PanelType.Left ? 1 : -1);

        // 累计玩家不触碰的时间
        if (!fallBelowBottom && !getTouched) notTouchedTimeCount += Time.deltaTime;
        else notTouchedTimeCount = 0;

        // 如果玩家不触碰的时间超过0.1秒就会把剩下的全判为miss
        if (!fallBelowBottom && notTouchedTimeCount > 0.1f)
        {
            fallBelowBottom = true;
            for (int i = 1; i < checkpoints.Length; ++i)
            {
                if (!checkpoints[i])
                {
                    Score(0, GetExit(direction == Direction.Down ? i : -i).center);
                    checkpoints[i] = true;
                }
            }
        }

        // 初次接触判定
        if (getTouched && !checkpoints[0])
        {
            if (diff > 1.5f * BlockSize.x || diff < -1.5f * BlockSize.x)
            {
                Score(0);
                curScore = 0;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
            else if (diff > 0.5f * BlockSize.x || diff < -0.5f * BlockSize.x)
            {
                Score(1);
                curScore = 1;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
            else
            {
                Score(2);
                curScore = 2;
                checkpoints[0] = true;
                fallBelowBottom = false;
            }
        }

        // 判定第2~N次按键
        if (!fallBelowBottom)
        {
            for (int i = 1; i < width; ++i)
            {
                if (!checkpoints[i])
                {
                    ExitData curExit = GetExit(direction == Direction.Down ? i : -i);
                    if (curExit.IsBeingTouched() || autoMode)
                    {
                        curScore = Mathf.Clamp(curScore + 1, 0, 2);
                        Score(curScore, curExit.center, false, i);
                        checkpoints[i] = true;
                        block.rectTransform.anchoredPosition = new Vector2(0, curExit.center.y - rt.anchoredPosition.y);
                    }
                }
            }
        }

        bool finished = true;
        foreach (bool b in checkpoints) if (!b) { finished = false; break; }
        if (finished) Deactivate();

        if (diff < -2f * BlockSize.x)
        {
            for (int i = 0; i < checkpoints.Length; ++i)
            {
                if (!checkpoints[i])
                {
                    Score(0, GetExit(direction == Direction.Down ? i : -i).center);
                }
            }
            DestroyRhythmObject(this);
        }
    }

    protected override void Activate()
    {
        if (!GetExit().current && !activated)
        {
            activated = true;
            for (int i = 0; i < width; ++i) GetExit(direction == Direction.Down ? i : -i).current = this;
        }
    }

    void ApplyWidth()
    {
        float height;
        switch (direction)
        {
            case Direction.Down:
                height = GetExit().y_top - GetExit(width - 1).y_bot;
                line.rectTransform.anchoredPosition = new Vector2(0, (BlockSize.y - height) / 2);
                line.rectTransform.sizeDelta = new Vector2(BlockSize.x, height);
                break;
            case Direction.Up:
                height = GetExit(1 - width).y_top - GetExit().y_bot;
                line.rectTransform.anchoredPosition = new Vector2(0, (height - BlockSize.y) / 2);
                line.rectTransform.sizeDelta = new Vector2(BlockSize.x, height);
                break;
        }
        line.enabled = false;
        outterFrame.rectTransform.sizeDelta = line.rectTransform.sizeDelta + new Vector2(-10, -10);
        outterBg.rectTransform.sizeDelta = line.rectTransform.sizeDelta + new Vector2(15, 15);

        Sprite[] s = Random.Range(0, 2) == 1 ? ResourceManager.ins.UpNotes : ResourceManager.ins.DownNotes;
        noteImages = new Sprite[width];
        for (int i = 0; i < width; ++i)
        {
            var n = Instantiate(Resources.Load<GameObject>("Note"), rt).GetComponent<Image>();
            noteImages[i] = n.sprite = s[Random.Range(0, s.Length)];
            n.rectTransform.anchoredPosition = new Vector2(0, GetExit(direction == Direction.Down ? i : -i).center.y - rt.anchoredPosition.y);
        }
    }


}
