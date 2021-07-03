using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FallingBlock : RhythmObject
{
    [SerializeField] Image noteImage;
    bool isScored;
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void Start()
    {
        base.Start();
        Sprite[] s = Random.Range(0, 2) == 1 ? ResourceManager.ins.UpNotes : ResourceManager.ins.DownNotes;
        noteImage.sprite = s[Random.Range(0, s.Length)];
        noteImages = new Sprite[] { noteImage.sprite };
    }
    protected override void Update_Activated()
    {
        if (autoMode)
        {
            if (!isScored)
            {
                bool left = panel == PanelType.Left && rt.anchoredPosition.x < GetBottom() + 0.2f * BlockSize.x;
                bool right = panel == PanelType.Right && rt.anchoredPosition.x > GetBottom() - 0.2f * BlockSize.x;
                if (left || right) OnClick();
            }
            return;
        }

        if (GetExit().IsBeingTouched())
        {
            OnClick();
        }

        // 不按直接出界
        bool leftJudging = panel == PanelType.Left && rt.anchoredPosition.x < GetBottom() - 1.5f * BlockSize.x;
        bool rightJudging = panel == PanelType.Right && rt.anchoredPosition.x > GetBottom() + 1.5f * BlockSize.x;
        if (leftJudging || rightJudging)
        {
            if (!isScored) Score(0);
            DestroyRhythmObject(this);
        }

    }
    void OnClick()
    {
        float diff = Mathf.Abs(rt.anchoredPosition.x - GetBottom());
        if (diff > 1.5f * BlockSize.x)
        {
            Score(0);
            isScored = true;
            Deactivate();
            DestroyRhythmObject(this);
        }
        else if (diff > 0.5f * BlockSize.x)
        {
            Score(1);
            isScored = true;
            Deactivate();
            DestroyRhythmObject(this);
        }
        else
        {
            Score(2);
            isScored = true;
            Deactivate();
            DestroyRhythmObject(this);
        }
    }
}
