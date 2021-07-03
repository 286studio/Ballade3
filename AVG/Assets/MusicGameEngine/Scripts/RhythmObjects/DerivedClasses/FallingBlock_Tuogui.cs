using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBlock_Tuogui : FallingBlock
{
    public override RhythmType Type => RhythmType.FallingBlock;

    protected override void CheckActivateCondition()
    {
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom(panel) + BlockSize.y)
        {
            Activate();
        }
    }

    protected override void Update_Activated()
    {
        if (TouchedByBinggui())
        {
            Score(2);
            Destroy(gameObject);
        }
        if (rt.anchoredPosition.y < RhythmGameManager.GetBottom(panel) - 1.5f * BlockSize.y)
        {
            Score(0);
            Destroy(gameObject);
        }
    }
}
